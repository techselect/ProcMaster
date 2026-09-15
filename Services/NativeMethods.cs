using System;
using System.Runtime.InteropServices;

namespace ProcMaster.Services
{
    /// <summary>
    /// Provides low-level Win32 and Windows NT Native API (ntdll.dll, kernel32.dll, psapi.dll)
    /// platform invoke (P/Invoke) definitions for process interrogation, handle duplication,
    /// memory management, and process suspension/resumption.
    /// </summary>
    public static class NativeMethods
    {
        #region Process Access Rights Constants

        /// <summary>Enables using the process handle in <c>TerminateProcess</c> to terminate the process.</summary>
        public const uint PROCESS_TERMINATE = 0x0001;

        /// <summary>Enables reading process memory using <c>ReadProcessMemory</c>.</summary>
        public const uint PROCESS_VM_READ = 0x0010;

        /// <summary>Enables querying full process information (exit code, priority class, etc.).</summary>
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;

        /// <summary>Enables querying limited process information (subset available without high privileges).</summary>
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        /// <summary>Enables suspending or resuming a process via <c>NtSuspendProcess</c> and <c>NtResumeProcess</c>.</summary>
        public const uint PROCESS_SUSPEND_RESUME = 0x0800;

        /// <summary>Enables duplicating object handles from the target process using <c>DuplicateHandle</c>.</summary>
        public const uint PROCESS_DUP_HANDLE = 0x0040;

        /// <summary>Specifies all possible access rights for a process object.</summary>
        public const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        #endregion

        #region Process Management P/Invoke

        /// <summary>
        /// Opens an existing local process object and returns a handle to it.
        /// </summary>
        /// <param name="processAccess">The required access flags.</param>
        /// <param name="bInheritHandle">Whether child processes inherit this handle.</param>
        /// <param name="processId">The Process Identifier (PID).</param>
        /// <returns>A valid process handle or <see cref="IntPtr.Zero"/> on failure.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        /// <summary>
        /// Closes an open object handle (process, file, event, etc.).
        /// </summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        /// <returns>True if the function succeeds; otherwise false.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Suspends all execution threads belonging to the specified process.
        /// </summary>
        /// <param name="processHandle">Handle to the process with <see cref="PROCESS_SUSPEND_RESUME"/> access.</param>
        /// <returns>NTSTATUS code (0 indicates STATUS_SUCCESS).</returns>
        [DllImport("ntdll.dll")]
        public static extern int NtSuspendProcess(IntPtr processHandle);

        /// <summary>
        /// Resumes execution of all suspended threads in the specified process.
        /// </summary>
        /// <param name="processHandle">Handle to the process with <see cref="PROCESS_SUSPEND_RESUME"/> access.</param>
        /// <returns>NTSTATUS code (0 indicates STATUS_SUCCESS).</returns>
        [DllImport("ntdll.dll")]
        public static extern int NtResumeProcess(IntPtr processHandle);

        /// <summary>
        /// Determines whether the specified process is running under WOW64 (32-bit process on 64-bit Windows).
        /// </summary>
        /// <param name="hProcess">A handle to the process.</param>
        /// <param name="wow64Process">Set to true if process is 32-bit running under WOW64 emulator.</param>
        /// <returns>True if check was successful.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        /// <summary>
        /// Retrieves a pseudo handle for the current calling process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        public const uint TH32CS_SNAPPROCESS = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        #endregion

        #region System Handles & Object Information

        /// <summary>System information class value for querying basic handle entries.</summary>
        public const int SystemHandleInformation = 16;

        /// <summary>System information class value for querying 64-bit extended handle entries.</summary>
        public const int SystemExtendedHandleInformation = 64;

        /// <summary>
        /// Retrieves specified system information directly from the NT kernel.
        /// </summary>
        [DllImport("ntdll.dll")]
        public static extern int NtQuerySystemInformation(
            int SystemInformationClass,
            IntPtr SystemInformation,
            int SystemInformationLength,
            out int ReturnLength);

        /// <summary>
        /// Retrieves information about a specified kernel object.
        /// </summary>
        [DllImport("ntdll.dll")]
        public static extern int NtQueryObject(
            IntPtr ObjectHandle,
            int ObjectInformationClass,
            IntPtr ObjectInformation,
            int ObjectInformationLength,
            out int ReturnLength);

        /// <summary>Query Object Basic Information class.</summary>
        public const int ObjectBasicInformation = 0;

        /// <summary>Query Object Name string class.</summary>
        public const int ObjectNameInformation = 1;

        /// <summary>Query Object Type name class.</summary>
        public const int ObjectTypeInformation = 2;

        /// <summary>Closes the source handle in the source process after duplicating.</summary>
        public const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;

        /// <summary>Ignores the dwDesiredAccess parameter and duplicates with identical access flags.</summary>
        public const uint DUPLICATE_SAME_ACCESS = 0x00000002;

        /// <summary>
        /// Duplicates an object handle from a source process into the target process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            out IntPtr lpTargetHandle,
            uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            uint dwOptions);

        #endregion

        #region Memory Statistics

        /// <summary>
        /// Retrieves information about the memory usage of the specified process.
        /// </summary>
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS_EX counters, uint size);

        /// <summary>
        /// Extended process memory statistics structure returned by psapi.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Size = 72)]
        public struct PROCESS_MEMORY_COUNTERS_EX
        {
            public uint cb;
            public uint PageFaultCount;
            public UIntPtr PeakWorkingSetSize;
            public UIntPtr WorkingSetSize;
            public UIntPtr QuotaPeakPagedPoolUsage;
            public UIntPtr QuotaPagedPoolUsage;
            public UIntPtr QuotaPeakNonPagedPoolUsage;
            public UIntPtr QuotaNonPagedPoolUsage;
            public UIntPtr PagefileUsage;
            public UIntPtr PeakPagefileUsage;
            public UIntPtr PrivateUsage;
        }

        #endregion

        #region Unicode Strings & Object Structures

        /// <summary>
        /// NT counted Unicode string structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        /// <summary>
        /// Contains the name of an NT kernel object.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_NAME_INFORMATION
        {
            public UNICODE_STRING Name;
        }

        /// <summary>
        /// Contains the type name and metadata of an NT kernel object.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_TYPE_INFORMATION
        {
            public UNICODE_STRING TypeName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
            public uint[] Reserved;
        }

        #endregion
    }
}
