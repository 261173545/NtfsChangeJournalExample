using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace CSharpExample
{
    class Program
    {
        //private static readonly IntPtr InvalidPtr = new IntPtr(-1L);

        static void Main(string[] args)
        {
            throw new NotImplementedException("C# wrapper about Change Journals does not implemented completely. It's possible to to this, but makes little sense because it will suffer from poor performance. The better way to utilize Change Journals in C# is to refactor CppExample project to dll, leave all logic in C++, but performs callback to C# with FINAL RESULTS.");

            var hVol = Unmanaged.CreateFile(
                "\\\\.\\c:",
                Unmanaged.DesiredAccessEnum.GENERIC_READ | Unmanaged.DesiredAccessEnum.GENERIC_WRITE,
                Unmanaged.SharedModeEnum.FILE_SHARE_READ | Unmanaged.SharedModeEnum.FILE_SHARE_WRITE,
                IntPtr.Zero,
                Unmanaged.CreationDispositionEnum.OPEN_EXISTING,
                0,
                IntPtr.Zero
                );

            if (hVol.IsInvalid)
            {
                Console.WriteLine("CreateFile failed {0}", Marshal.GetLastWin32Error());
                return;
            }
            
            using (hVol)
            {
                Unmanaged.USN_JOURNAL_DATA_V0 journalData;
                if (!QueryJournal(hVol, out journalData))
                {
                    return;
                }

                var readData = new Unmanaged.READ_USN_JOURNAL_DATA(
                    0,
                    0xFFFFFFFF,
                    0,
                    0,
                    0,
                    journalData.UsnJournalID
                    );

                Console.WriteLine("Journal ID: {0}", journalData.UsnJournalID);
                Console.WriteLine("FirstUsn: {0}", journalData.FirstUsn);

                while (readData.StartUsn < journalData.NextUsn)
                {
                    //memset(journalBuffer, 0, JOURNAL_BUFFER_SIZE);

                    //if (!DeviceIoControl(
                    //    hVol,
                    //    FSCTL_READ_USN_JOURNAL,
                    //    &ReadData,
                    //    sizeof(ReadData),
                    //    &journalBuffer,
                    //    JOURNAL_BUFFER_SIZE,
                    //    &dwBytes,
                    //    NULL
                    //    )
                    //)
                    //{
                    //    printf("Read journal failed (%d)\n", GetLastError());
                    //    return -1;
                    //}

                    var rjr = ReadJournal(
                        hVol,
                        ref readData
                        );

                    //DWORD dwRetBytes = dwBytes - sizeof(USN);

                    //// Find the first record
                    //auto record = (PUSN_RECORD)(((PUCHAR)journalBuffer) + sizeof(USN));

                    //while (dwRetBytes > 0)
                    //{
                    //    if (interested.compare(record->FileName) == 0)
                    //    {
                    //        printf("USN: %I64x\n", record->Usn);
                    //        printf(
                    //            "File name: %.*S\n",
                    //            record->FileNameLength / 2,
                    //            record->FileName
                    //            );

                    //        auto fullPath = GetFullPath(
                    //            hVol,
                    //            JournalData.NextUsn,
                    //            record
                    //            );

                    //        printf("Full path: %.*S\n", fullPath.length(), fullPath.c_str());
                    //        printf("Reason: %x\n", record->Reason);

                    //        SYSTEMTIME systemTime;
                    //        FileTimeToSystemTime(
                    //            (FILETIME*)&record->TimeStamp,
                    //            &systemTime
                    //            );

                    //        printf("Time stamp: %u.%u.%u %u:%u:%u.%u\n", systemTime.wYear, systemTime.wMonth, systemTime.wDay, systemTime.wHour, systemTime.wMinute, systemTime.wSecond, systemTime.wMilliseconds);

                    //        printf("\n");
                    //    }

                    //    dwRetBytes -= record->RecordLength;

                    //    // Find the next record
                    //    record = (PUSN_RECORD)(((PCHAR)record) + record->RecordLength);
                    //}
                    //// Update starting USN for next call
                    //ReadData.StartUsn = *(USN*)&journalBuffer;
                }

                Console.WriteLine("finished");
            }
        }

        private static bool ReadJournal(
            SafeFileHandle hVol,
            ref Unmanaged.READ_USN_JOURNAL_DATA readData
            )
        {
            const int JournalBufferSize = 65536;

            var unmanagedReadData = Marshal.AllocHGlobal(Marshal.SizeOf<Unmanaged.READ_USN_JOURNAL_DATA>());
            Marshal.StructureToPtr(readData, unmanagedReadData, false);
            var journalBuffer = Marshal.AllocHGlobal(JournalBufferSize);
            try
            {
                uint dwBytes;
                if (!Unmanaged.DeviceIoControl(
                        hVol,
                        Unmanaged.EIOControlCode.FsctlReadUsnJournal,
                        unmanagedReadData,
                        (uint)Marshal.SizeOf<Unmanaged.READ_USN_JOURNAL_DATA>(),
                        journalBuffer,
                        (uint)JournalBufferSize,
                        out dwBytes,
                        IntPtr.Zero
                        )
                    )
                {
                    Console.WriteLine("Read journal failed {0}", Marshal.GetLastWin32Error());

                    return false;
                }

                var dwRetBytes = dwBytes - sizeof(Int64);

                // Find the first record
                var record = journalBuffer + sizeof(Int64);

                while (dwRetBytes > 0)
                {
                    //if (   interested.compare(record->FileName) == 0)
                    //{
                        
                    //}
                }

                return
                    true;
            }
            finally
            {
                Marshal.FreeHGlobal(journalBuffer);
                Marshal.FreeHGlobal(unmanagedReadData);
            }
        }

        private static bool QueryJournal(
            SafeFileHandle hVol,
            out Unmanaged.USN_JOURNAL_DATA_V0 journalData
            )
        {
            var unmanagedJournalData = Marshal.AllocHGlobal(Marshal.SizeOf<Unmanaged.USN_JOURNAL_DATA_V0>());
            try
            {
                uint dwBytes;
                if (!Unmanaged.DeviceIoControl(
                        hVol,
                        Unmanaged.EIOControlCode.FsctlQueryUsnJournal,
                        IntPtr.Zero,
                        0u,
                        unmanagedJournalData,
                        (uint) Marshal.SizeOf<Unmanaged.USN_JOURNAL_DATA_V0>(), //Unmanaged.USN_JOURNAL_DATA_V0.SizeOf,
                        out dwBytes,
                        IntPtr.Zero
                        )
                    )
                {
                    Console.WriteLine("Query journal failed {0}", Marshal.GetLastWin32Error());

                    journalData = new Unmanaged.USN_JOURNAL_DATA_V0();
                    return false;
                }

                journalData = Marshal.PtrToStructure<Unmanaged.USN_JOURNAL_DATA_V0>(unmanagedJournalData);

                return
                    true;
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedJournalData);
            }
        }
    }


    internal static class Unmanaged
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct READ_USN_JOURNAL_DATA
        {
            public Int64 StartUsn;
            public UInt32 ReasonMask;
            public UInt32 ReturnOnlyOnClose;
            public UInt64 Timeout;
            public UInt64 BytesToWaitFor;
            public UInt64 UsnJournalID;

            public READ_USN_JOURNAL_DATA(long startUsn, uint reasonMask, uint returnOnlyOnClose, ulong timeout, ulong bytesToWaitFor, ulong usnJournalId)
            {
                StartUsn = startUsn;
                ReasonMask = reasonMask;
                ReturnOnlyOnClose = returnOnlyOnClose;
                Timeout = timeout;
                BytesToWaitFor = bytesToWaitFor;
                UsnJournalID = usnJournalId;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct USN_JOURNAL_DATA_V0
        {
            //public const int SizeOf = sizeof(long) * 7; //7 fields of long type

            public ulong UsnJournalID;       //DWORDLONG UsnJournalID
            public long FirstUsn;       //USN FirstUsn
            public long NextUsn;        //USN NextUsn
            public long LowestValidUsn;     //USN LowestValidUsn
            public long MaxUsn;         //USN MaxUsn
            public long MaximumSize;    //DWORDLONG MaximumSize
            public long AllocationDelta;    //DWORDLONG AllocationDelta
        }

        [DllImport("Kernel32.dll", SetLastError = false, CharSet = CharSet.Auto)]
        internal static extern bool DeviceIoControl(
            Microsoft.Win32.SafeHandles.SafeFileHandle hDevice,
            EIOControlCode ioControlCode,
            [In] IntPtr inBuffer,
            uint nInBufferSize,
            [Out] IntPtr outBuffer,
            uint nOutBufferSize,
            out uint pBytesReturned,
            [In] IntPtr overlapped
        );


        [Flags]
        public enum EMethod : uint
        {
            Buffered = 0,
            InDirect = 1,
            OutDirect = 2,
            Neither = 3
        }

        [Flags]
        public enum EFileDevice : uint
        {
            Beep = 0x00000001,
            CDRom = 0x00000002,
            CDRomFileSytem = 0x00000003,
            Controller = 0x00000004,
            Datalink = 0x00000005,
            Dfs = 0x00000006,
            Disk = 0x00000007,
            DiskFileSystem = 0x00000008,
            FileSystem = 0x00000009,
            InPortPort = 0x0000000a,
            Keyboard = 0x0000000b,
            Mailslot = 0x0000000c,
            MidiIn = 0x0000000d,
            MidiOut = 0x0000000e,
            Mouse = 0x0000000f,
            MultiUncProvider = 0x00000010,
            NamedPipe = 0x00000011,
            Network = 0x00000012,
            NetworkBrowser = 0x00000013,
            NetworkFileSystem = 0x00000014,
            Null = 0x00000015,
            ParallelPort = 0x00000016,
            PhysicalNetcard = 0x00000017,
            Printer = 0x00000018,
            Scanner = 0x00000019,
            SerialMousePort = 0x0000001a,
            SerialPort = 0x0000001b,
            Screen = 0x0000001c,
            Sound = 0x0000001d,
            Streams = 0x0000001e,
            Tape = 0x0000001f,
            TapeFileSystem = 0x00000020,
            Transport = 0x00000021,
            Unknown = 0x00000022,
            Video = 0x00000023,
            VirtualDisk = 0x00000024,
            WaveIn = 0x00000025,
            WaveOut = 0x00000026,
            Port8042 = 0x00000027,
            NetworkRedirector = 0x00000028,
            Battery = 0x00000029,
            BusExtender = 0x0000002a,
            Modem = 0x0000002b,
            Vdm = 0x0000002c,
            MassStorage = 0x0000002d,
            Smb = 0x0000002e,
            Ks = 0x0000002f,
            Changer = 0x00000030,
            Smartcard = 0x00000031,
            Acpi = 0x00000032,
            Dvd = 0x00000033,
            FullscreenVideo = 0x00000034,
            DfsFileSystem = 0x00000035,
            DfsVolume = 0x00000036,
            Serenum = 0x00000037,
            Termsrv = 0x00000038,
            Ksec = 0x00000039,
            // From Windows Driver Kit 7
            Fips = 0x0000003A,
            Infiniband = 0x0000003B,
            Vmbus = 0x0000003E,
            CryptProvider = 0x0000003F,
            Wpd = 0x00000040,
            Bluetooth = 0x00000041,
            MtComposite = 0x00000042,
            MtTransport = 0x00000043,
            Biometric = 0x00000044,
            Pmi = 0x00000045
        }

        /// <summary>
        /// IO Control Codes
        /// Useful links:
        ///     http://www.ioctls.net/
        ///     http://msdn.microsoft.com/en-us/library/windows/hardware/ff543023(v=vs.85).aspx
        /// </summary>
        [Flags]
        public enum EIOControlCode : uint
        {
            // STORAGE
            StorageCheckVerify = (EFileDevice.MassStorage << 16) | (0x0200 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            StorageCheckVerify2 = (EFileDevice.MassStorage << 16) | (0x0200 << 2) | EMethod.Buffered | (0 << 14), // FileAccess.Any
            StorageMediaRemoval = (EFileDevice.MassStorage << 16) | (0x0201 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            StorageEjectMedia = (EFileDevice.MassStorage << 16) | (0x0202 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            StorageLoadMedia = (EFileDevice.MassStorage << 16) | (0x0203 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            StorageLoadMedia2 = (EFileDevice.MassStorage << 16) | (0x0203 << 2) | EMethod.Buffered | (0 << 14),
            StorageReserve = (EFileDevice.MassStorage << 16) | (0x0204 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            StorageRelease = (EFileDevice.MassStorage << 16) | (0x0205 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            StorageFindNewDevices = (EFileDevice.MassStorage << 16) | (0x0206 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            StorageEjectionControl = (EFileDevice.MassStorage << 16) | (0x0250 << 2) | EMethod.Buffered | (0 << 14),
            StorageMcnControl = (EFileDevice.MassStorage << 16) | (0x0251 << 2) | EMethod.Buffered | (0 << 14),
            StorageGetMediaTypes = (EFileDevice.MassStorage << 16) | (0x0300 << 2) | EMethod.Buffered | (0 << 14),
            StorageGetMediaTypesEx = (EFileDevice.MassStorage << 16) | (0x0301 << 2) | EMethod.Buffered | (0 << 14),
            StorageResetBus = (EFileDevice.MassStorage << 16) | (0x0400 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            StorageResetDevice = (EFileDevice.MassStorage << 16) | (0x0401 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            StorageGetDeviceNumber = (EFileDevice.MassStorage << 16) | (0x0420 << 2) | EMethod.Buffered | (0 << 14),
            StoragePredictFailure = (EFileDevice.MassStorage << 16) | (0x0440 << 2) | EMethod.Buffered | (0 << 14),
            StorageObsoleteResetBus = (EFileDevice.MassStorage << 16) | (0x0400 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            StorageObsoleteResetDevice = (EFileDevice.MassStorage << 16) | (0x0401 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            StorageQueryProperty = (EFileDevice.MassStorage << 16) | (0x0500 << 2) | EMethod.Buffered | (0 << 14),
            // DISK
            DiskGetDriveGeometry = (EFileDevice.Disk << 16) | (0x0000 << 2) | EMethod.Buffered | (0 << 14),
            DiskGetDriveGeometryEx = (EFileDevice.Disk << 16) | (0x0028 << 2) | EMethod.Buffered | (0 << 14),
            DiskGetPartitionInfo = (EFileDevice.Disk << 16) | (0x0001 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskGetPartitionInfoEx = (EFileDevice.Disk << 16) | (0x0012 << 2) | EMethod.Buffered | (0 << 14),
            DiskSetPartitionInfo = (EFileDevice.Disk << 16) | (0x0002 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskGetDriveLayout = (EFileDevice.Disk << 16) | (0x0003 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskSetDriveLayout = (EFileDevice.Disk << 16) | (0x0004 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskVerify = (EFileDevice.Disk << 16) | (0x0005 << 2) | EMethod.Buffered | (0 << 14),
            DiskFormatTracks = (EFileDevice.Disk << 16) | (0x0006 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskReassignBlocks = (EFileDevice.Disk << 16) | (0x0007 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskPerformance = (EFileDevice.Disk << 16) | (0x0008 << 2) | EMethod.Buffered | (0 << 14),
            DiskIsWritable = (EFileDevice.Disk << 16) | (0x0009 << 2) | EMethod.Buffered | (0 << 14),
            DiskLogging = (EFileDevice.Disk << 16) | (0x000a << 2) | EMethod.Buffered | (0 << 14),
            DiskFormatTracksEx = (EFileDevice.Disk << 16) | (0x000b << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskHistogramStructure = (EFileDevice.Disk << 16) | (0x000c << 2) | EMethod.Buffered | (0 << 14),
            DiskHistogramData = (EFileDevice.Disk << 16) | (0x000d << 2) | EMethod.Buffered | (0 << 14),
            DiskHistogramReset = (EFileDevice.Disk << 16) | (0x000e << 2) | EMethod.Buffered | (0 << 14),
            DiskRequestStructure = (EFileDevice.Disk << 16) | (0x000f << 2) | EMethod.Buffered | (0 << 14),
            DiskRequestData = (EFileDevice.Disk << 16) | (0x0010 << 2) | EMethod.Buffered | (0 << 14),
            DiskControllerNumber = (EFileDevice.Disk << 16) | (0x0011 << 2) | EMethod.Buffered | (0 << 14),
            DiskSmartGetVersion = (EFileDevice.Disk << 16) | (0x0020 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskSmartSendDriveCommand = (EFileDevice.Disk << 16) | (0x0021 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskSmartRcvDriveData = (EFileDevice.Disk << 16) | (0x0022 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskUpdateDriveSize = (EFileDevice.Disk << 16) | (0x0032 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskGrowPartition = (EFileDevice.Disk << 16) | (0x0034 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskGetCacheInformation = (EFileDevice.Disk << 16) | (0x0035 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskSetCacheInformation = (EFileDevice.Disk << 16) | (0x0036 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskDeleteDriveLayout = (EFileDevice.Disk << 16) | (0x0040 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskFormatDrive = (EFileDevice.Disk << 16) | (0x00f3 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskSenseDevice = (EFileDevice.Disk << 16) | (0x00f8 << 2) | EMethod.Buffered | (0 << 14),
            DiskCheckVerify = (EFileDevice.Disk << 16) | (0x0200 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskMediaRemoval = (EFileDevice.Disk << 16) | (0x0201 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskEjectMedia = (EFileDevice.Disk << 16) | (0x0202 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskLoadMedia = (EFileDevice.Disk << 16) | (0x0203 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskReserve = (EFileDevice.Disk << 16) | (0x0204 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskRelease = (EFileDevice.Disk << 16) | (0x0205 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskFindNewDevices = (EFileDevice.Disk << 16) | (0x0206 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            DiskGetMediaTypes = (EFileDevice.Disk << 16) | (0x0300 << 2) | EMethod.Buffered | (0 << 14),
            DiskSetPartitionInfoEx = (EFileDevice.Disk << 16) | (0x0013 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskGetDriveLayoutEx = (EFileDevice.Disk << 16) | (0x0014 << 2) | EMethod.Buffered | (0 << 14),
            DiskSetDriveLayoutEx = (EFileDevice.Disk << 16) | (0x0015 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskCreateDisk = (EFileDevice.Disk << 16) | (0x0016 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            DiskGetLengthInfo = (EFileDevice.Disk << 16) | (0x0017 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            // CHANGER
            ChangerGetParameters = (EFileDevice.Changer << 16) | (0x0000 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            ChangerGetStatus = (EFileDevice.Changer << 16) | (0x0001 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            ChangerGetProductData = (EFileDevice.Changer << 16) | (0x0002 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            ChangerSetAccess = (EFileDevice.Changer << 16) | (0x0004 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            ChangerGetElementStatus = (EFileDevice.Changer << 16) | (0x0005 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            ChangerInitializeElementStatus = (EFileDevice.Changer << 16) | (0x0006 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            ChangerSetPosition = (EFileDevice.Changer << 16) | (0x0007 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            ChangerExchangeMedium = (EFileDevice.Changer << 16) | (0x0008 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            ChangerMoveMedium = (EFileDevice.Changer << 16) | (0x0009 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            ChangerReinitializeTarget = (EFileDevice.Changer << 16) | (0x000A << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            ChangerQueryVolumeTags = (EFileDevice.Changer << 16) | (0x000B << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            // FILESYSTEM
            FsctlRequestOplockLevel1 = (EFileDevice.FileSystem << 16) | (0 << 2) | EMethod.Buffered | (0 << 14),
            FsctlRequestOplockLevel2 = (EFileDevice.FileSystem << 16) | (1 << 2) | EMethod.Buffered | (0 << 14),
            FsctlRequestBatchOplock = (EFileDevice.FileSystem << 16) | (2 << 2) | EMethod.Buffered | (0 << 14),
            FsctlOplockBreakAcknowledge = (EFileDevice.FileSystem << 16) | (3 << 2) | EMethod.Buffered | (0 << 14),
            FsctlOpBatchAckClosePending = (EFileDevice.FileSystem << 16) | (4 << 2) | EMethod.Buffered | (0 << 14),
            FsctlOplockBreakNotify = (EFileDevice.FileSystem << 16) | (5 << 2) | EMethod.Buffered | (0 << 14),
            FsctlLockVolume = (EFileDevice.FileSystem << 16) | (6 << 2) | EMethod.Buffered | (0 << 14),
            FsctlUnlockVolume = (EFileDevice.FileSystem << 16) | (7 << 2) | EMethod.Buffered | (0 << 14),
            FsctlDismountVolume = (EFileDevice.FileSystem << 16) | (8 << 2) | EMethod.Buffered | (0 << 14),
            FsctlIsVolumeMounted = (EFileDevice.FileSystem << 16) | (10 << 2) | EMethod.Buffered | (0 << 14),
            FsctlIsPathnameValid = (EFileDevice.FileSystem << 16) | (11 << 2) | EMethod.Buffered | (0 << 14),
            FsctlMarkVolumeDirty = (EFileDevice.FileSystem << 16) | (12 << 2) | EMethod.Buffered | (0 << 14),
            FsctlQueryRetrievalPointers = (EFileDevice.FileSystem << 16) | (14 << 2) | EMethod.Neither | (0 << 14),
            FsctlGetCompression = (EFileDevice.FileSystem << 16) | (15 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSetCompression = (EFileDevice.FileSystem << 16) | (16 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            FsctlMarkAsSystemHive = (EFileDevice.FileSystem << 16) | (19 << 2) | EMethod.Neither | (0 << 14),
            FsctlOplockBreakAckNo2 = (EFileDevice.FileSystem << 16) | (20 << 2) | EMethod.Buffered | (0 << 14),
            FsctlInvalidateVolumes = (EFileDevice.FileSystem << 16) | (21 << 2) | EMethod.Buffered | (0 << 14),
            FsctlQueryFatBpb = (EFileDevice.FileSystem << 16) | (22 << 2) | EMethod.Buffered | (0 << 14),
            FsctlRequestFilterOplock = (EFileDevice.FileSystem << 16) | (23 << 2) | EMethod.Buffered | (0 << 14),
            FsctlFileSystemGetStatistics = (EFileDevice.FileSystem << 16) | (24 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetNtfsVolumeData = (EFileDevice.FileSystem << 16) | (25 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetNtfsFileRecord = (EFileDevice.FileSystem << 16) | (26 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetVolumeBitmap = (EFileDevice.FileSystem << 16) | (27 << 2) | EMethod.Neither | (0 << 14),
            FsctlGetRetrievalPointers = (EFileDevice.FileSystem << 16) | (28 << 2) | EMethod.Neither | (0 << 14),
            FsctlMoveFile = (EFileDevice.FileSystem << 16) | (29 << 2) | EMethod.Buffered | (0 << 14),
            FsctlIsVolumeDirty = (EFileDevice.FileSystem << 16) | (30 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetHfsInformation = (EFileDevice.FileSystem << 16) | (31 << 2) | EMethod.Buffered | (0 << 14),
            FsctlAllowExtendedDasdIo = (EFileDevice.FileSystem << 16) | (32 << 2) | EMethod.Neither | (0 << 14),
            FsctlReadPropertyData = (EFileDevice.FileSystem << 16) | (33 << 2) | EMethod.Neither | (0 << 14),
            FsctlWritePropertyData = (EFileDevice.FileSystem << 16) | (34 << 2) | EMethod.Neither | (0 << 14),
            FsctlFindFilesBySid = (EFileDevice.FileSystem << 16) | (35 << 2) | EMethod.Neither | (0 << 14),
            FsctlDumpPropertyData = (EFileDevice.FileSystem << 16) | (37 << 2) | EMethod.Neither | (0 << 14),
            FsctlSetObjectId = (EFileDevice.FileSystem << 16) | (38 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetObjectId = (EFileDevice.FileSystem << 16) | (39 << 2) | EMethod.Buffered | (0 << 14),
            FsctlDeleteObjectId = (EFileDevice.FileSystem << 16) | (40 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSetReparsePoint = (EFileDevice.FileSystem << 16) | (41 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetReparsePoint = (EFileDevice.FileSystem << 16) | (42 << 2) | EMethod.Buffered | (0 << 14),
            FsctlDeleteReparsePoint = (EFileDevice.FileSystem << 16) | (43 << 2) | EMethod.Buffered | (0 << 14),
            FsctlEnumUsnData = (EFileDevice.FileSystem << 16) | (44 << 2) | EMethod.Neither | (0 << 14),
            FsctlSecurityIdCheck = (EFileDevice.FileSystem << 16) | (45 << 2) | EMethod.Neither | (FileAccess.Read << 14),
            FsctlReadUsnJournal = (EFileDevice.FileSystem << 16) | (46 << 2) | EMethod.Neither | (0 << 14),
            FsctlSetObjectIdExtended = (EFileDevice.FileSystem << 16) | (47 << 2) | EMethod.Buffered | (0 << 14),
            FsctlCreateOrGetObjectId = (EFileDevice.FileSystem << 16) | (48 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSetSparse = (EFileDevice.FileSystem << 16) | (49 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSetZeroData = (EFileDevice.FileSystem << 16) | (50 << 2) | EMethod.Buffered | (FileAccess.Write << 14),
            FsctlQueryAllocatedRanges = (EFileDevice.FileSystem << 16) | (51 << 2) | EMethod.Neither | (FileAccess.Read << 14),
            FsctlEnableUpgrade = (EFileDevice.FileSystem << 16) | (52 << 2) | EMethod.Buffered | (FileAccess.Write << 14),
            FsctlSetEncryption = (EFileDevice.FileSystem << 16) | (53 << 2) | EMethod.Neither | (0 << 14),
            FsctlEncryptionFsctlIo = (EFileDevice.FileSystem << 16) | (54 << 2) | EMethod.Neither | (0 << 14),
            FsctlWriteRawEncrypted = (EFileDevice.FileSystem << 16) | (55 << 2) | EMethod.Neither | (0 << 14),
            FsctlReadRawEncrypted = (EFileDevice.FileSystem << 16) | (56 << 2) | EMethod.Neither | (0 << 14),
            FsctlCreateUsnJournal = (EFileDevice.FileSystem << 16) | (57 << 2) | EMethod.Neither | (0 << 14),
            FsctlReadFileUsnData = (EFileDevice.FileSystem << 16) | (58 << 2) | EMethod.Neither | (0 << 14),
            FsctlWriteUsnCloseRecord = (EFileDevice.FileSystem << 16) | (59 << 2) | EMethod.Neither | (0 << 14),
            FsctlExtendVolume = (EFileDevice.FileSystem << 16) | (60 << 2) | EMethod.Buffered | (0 << 14),
            FsctlQueryUsnJournal = (EFileDevice.FileSystem << 16) | (61 << 2) | EMethod.Buffered | (0 << 14),
            FsctlDeleteUsnJournal = (EFileDevice.FileSystem << 16) | (62 << 2) | EMethod.Buffered | (0 << 14),
            FsctlMarkHandle = (EFileDevice.FileSystem << 16) | (63 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSisCopyFile = (EFileDevice.FileSystem << 16) | (64 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSisLinkFiles = (EFileDevice.FileSystem << 16) | (65 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            FsctlHsmMsg = (EFileDevice.FileSystem << 16) | (66 << 2) | EMethod.Buffered | (FileAccess.ReadWrite << 14),
            FsctlNssControl = (EFileDevice.FileSystem << 16) | (67 << 2) | EMethod.Buffered | (FileAccess.Write << 14),
            FsctlHsmData = (EFileDevice.FileSystem << 16) | (68 << 2) | EMethod.Neither | (FileAccess.ReadWrite << 14),
            FsctlRecallFile = (EFileDevice.FileSystem << 16) | (69 << 2) | EMethod.Neither | (0 << 14),
            FsctlNssRcontrol = (EFileDevice.FileSystem << 16) | (70 << 2) | EMethod.Buffered | (FileAccess.Read << 14),
            // VIDEO
            VideoQuerySupportedBrightness = (EFileDevice.Video << 16) | (0x0125 << 2) | EMethod.Buffered | (0 << 14),
            VideoQueryDisplayBrightness = (EFileDevice.Video << 16) | (0x0126 << 2) | EMethod.Buffered | (0 << 14),
            VideoSetDisplayBrightness = (EFileDevice.Video << 16) | (0x0127 << 2) | EMethod.Buffered | (0 << 14)
        }


        internal enum CreationDispositionEnum : uint
        {
            CREATE_NEW        = 1,
            CREATE_ALWAYS     = 2,
            OPEN_EXISTING     = 3,
            OPEN_ALWAYS       = 4,
            TRUNCATE_EXISTING = 5,
        }

        [Flags]
        internal enum SharedModeEnum : uint
        {
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002,
            FILE_SHARE_DELETE = 0x00000004
        }

        [Flags]
        internal enum DesiredAccessEnum : uint
        {
            GENERIC_READ = (0x80000000),
            GENERIC_WRITE = (0x40000000),
            GENERIC_EXECUTE = (0x20000000),
            GENERIC_ALL = (0x10000000)
        }

        //[DllImport("kernel32.dll", SetLastError = true)]
        //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        //[SuppressUnmanagedCodeSecurity]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //internal static extern bool CloseHandle(SafeFileHandle hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern SafeFileHandle CreateFile(
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string fileName,
            DesiredAccessEnum desiredAccess,
            SharedModeEnum sharedMode,
            IntPtr securityAttributes,
            CreationDispositionEnum creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile
            );

        //[Flags]
        //internal enum FileMapProtectionEnum : uint
        //{
        //    PageReadonly = 0x02,
        //    PageReadWrite = 0x04,
        //    PageWriteCopy = 0x08,
        //    PageExecuteRead = 0x20,
        //    PageExecuteReadWrite = 0x40,
        //    SectionCommit = 0x8000000,
        //    SectionImage = 0x1000000,
        //    SectionNoCache = 0x10000000,
        //    SectionReserve = 0x4000000,
        //}
    }


}
