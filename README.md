# NtfsChangeJournalExample

C++ Ntfs Change Journal Example with full file path extraction.
C# wrapper about Change Journals does not implemented completely. It's possible to to this, but makes little sense because it will suffer from poor performance. The better way to utilize Change Journals in C# is to refactor CppExample project to dll, leave all logic in C++, but performs callback to C# with FINAL RESULTS.