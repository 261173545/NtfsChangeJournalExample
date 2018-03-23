// NtfsChangeJournalExample.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <string>
#include <vector>
#include <Windows.h>
#include <WinIoCtl.h>
#include <stdio.h>

#define JOURNAL_BUFFER_SIZE (65536)
#define PATH_BUFFER_SIZE (65536)

using namespace std;

void * pathBuffer = new char[PATH_BUFFER_SIZE];


void ProcessFullPath (
    HANDLE drive, 
    USN nextusn, 
    DWORDLONG parentFileReferenceNumber, 
    vector<wstring>& path
    )
{
    memset(pathBuffer, 0, PATH_BUFFER_SIZE);

    if (pathBuffer == NULL)
    {
        printf("VirtualAlloc: %u\n", GetLastError());
        return;
    }

    MFT_ENUM_DATA_V0 mft_enum_data;
    mft_enum_data.StartFileReferenceNumber = parentFileReferenceNumber;
    mft_enum_data.LowUsn = 0;
    mft_enum_data.HighUsn = 
        nextusn;
        //maxusn;

    DWORD bytecount(0);
    if (!DeviceIoControl(drive, FSCTL_ENUM_USN_DATA, &mft_enum_data, sizeof(mft_enum_data), pathBuffer, PATH_BUFFER_SIZE, &bytecount, NULL))
    {
        //printf("FSCTL_ENUM_USN_DATA (show_record): %u\n", GetLastError());
        return;
    }

    auto parent_record_pointer = (USN_RECORD_V2 *)((USN *)pathBuffer + 1);

    if (parent_record_pointer->FileReferenceNumber != parentFileReferenceNumber)
    {
        //printf("=================================================================\n");
        //printf("Couldn't retrieve FileReferenceNumber %u\n", record->ParentFileReferenceNumber);
        return;
    }

    path.push_back(wstring(parent_record_pointer->FileName));

    ProcessFullPath(
        drive, 
        nextusn, 
        parent_record_pointer->ParentFileReferenceNumber,
        path
        );
}

wstring GetFullPath(
    HANDLE drive, 
    USN nextusn, 
    PUSN_RECORD record
    )
{
    vector<wstring> vpath;
    vpath.push_back(wstring(record->FileName));

    ProcessFullPath(
        drive,
        nextusn,
        record->ParentFileReferenceNumber,
        vpath
        );

    wstring path(L"c:");

    for (auto e = vpath.rbegin(); e != vpath.rend(); ++e)
    {
        path += L"\\";
        path += *e;
    }

    return path;
}


int _tmain(int argc, _TCHAR* argv[])
{
    const wstring interested(L"1.yyy"); // <--------- do not forget to change this file name!

    WCHAR journalBuffer[JOURNAL_BUFFER_SIZE];

    auto hVol = CreateFile(
        TEXT("\\\\.\\c:"), 
        GENERIC_READ | GENERIC_WRITE, 
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        0,
        NULL
        );

    if( hVol == INVALID_HANDLE_VALUE )
    {
        printf("CreateFile failed (%d)\n", GetLastError());
        return -1;
    }

    USN_JOURNAL_DATA_V0 JournalData;
    DWORD dwBytes;
    if( !DeviceIoControl( hVol, 
            FSCTL_QUERY_USN_JOURNAL, 
            NULL,
            0,
            &JournalData,
            sizeof(JournalData),
            &dwBytes,
            NULL) )
    {
        printf("Query journal failed (%d)\n", GetLastError());
        return -1;
    }

    READ_USN_JOURNAL_DATA_V0 ReadData = {0, 0xFFFFFFFF, FALSE, 0, 0};
    ReadData.UsnJournalID = JournalData.UsnJournalID;

    printf("Journal ID: %I64x\n", JournalData.UsnJournalID);
    printf("FirstUsn: %I64x\n\n", JournalData.FirstUsn);

    while(ReadData.StartUsn < JournalData.NextUsn)
    {
        memset(journalBuffer, 0, JOURNAL_BUFFER_SIZE);

        if( !DeviceIoControl(
            hVol, 
            FSCTL_READ_USN_JOURNAL, 
            &ReadData,
            sizeof(ReadData),
            &journalBuffer,
            JOURNAL_BUFFER_SIZE,
            &dwBytes,
            NULL
            )
        )
        {
            printf("Read journal failed (%d)\n", GetLastError());
            return -1;
        }

        DWORD dwRetBytes = dwBytes - sizeof(USN);

        // Find the first record
        auto record = (PUSN_RECORD)(((PUCHAR)journalBuffer) + sizeof(USN));

        while( dwRetBytes > 0 )
        {
            if(interested.compare(record->FileName) == 0)
            {
                printf("USN: %I64x\n", record->Usn);
                printf(
                    "File name: %.*S\n", 
                    record->FileNameLength/2, 
                    record->FileName
                    );

                auto fullPath = GetFullPath(
                    hVol,
                    JournalData.NextUsn,
                    record
                    );

                printf("Full path: %.*S\n", fullPath.length(), fullPath.c_str());
                printf("Reason: %x\n", record->Reason);

                SYSTEMTIME systemTime;
                FileTimeToSystemTime(
                    (FILETIME*)&record->TimeStamp,
                    &systemTime
                    );

                printf("Time stamp: %u.%u.%u %u:%u:%u.%u\n", systemTime.wYear, systemTime.wMonth, systemTime.wDay, systemTime.wHour, systemTime.wMinute, systemTime.wSecond, systemTime.wMilliseconds);

                printf("\n");
            }

            dwRetBytes -= record->RecordLength;

            // Find the next record
            record = (PUSN_RECORD)(((PCHAR)record) + record->RecordLength);
        }
        // Update starting USN for next call
        ReadData.StartUsn = *(USN *)&journalBuffer;
    }

    CloseHandle(hVol);

    printf("Finished\n");

    return 0;
}

