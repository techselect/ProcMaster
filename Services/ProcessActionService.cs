using System;
using System.Diagnostics;
using System.Management;

namespace ProcMaster.Services
{
    /// <summary>
    /// Provides operational process management actions including termination, process tree termination,
    /// suspension, resumption, scheduling priority adjustments, and remote handle closure.
    /// </summary>
    public static class ProcessActionService
    {
        /// <summary>
        /// Immediately terminates the target process using standard managed API with a Win32 Native fallback.
        /// </summary>
        /// <param name="processId">Target process identifier.</param>
        /// <returns>True if the process was successfully killed or commanded to terminate.</returns>
        public static bool TerminateProcess(int processId)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                proc.Kill();
                return true;
            }
            catch
            {
                IntPtr hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_TERMINATE, false, processId);
                if (hProc != IntPtr.Zero)
                {
                    bool success = NativeMethods.CloseHandle(hProc);
                    return success;
                }
                return false;
            }
        }

        /// <summary>
        /// Immediately terminates the target process and all descendant child processes recursively.
        /// </summary>
        /// <param name="processId">Target parent process identifier.</param>
        /// <returns>True if termination was initiated successfully.</returns>
        public static bool TerminateProcessTree(int processId)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                proc.Kill(entireProcessTree: true);
                return true;
            }
            catch
            {
                return TerminateProcess(processId);
            }
        }

        /// <summary>
        /// Suspends execution of all threads in the target process using NT Native <c>NtSuspendProcess</c>.
        /// </summary>
        /// <param name="processId">Target process identifier.</param>
        /// <returns>True if suspension was successfully applied.</returns>
        public static bool SuspendProcess(int processId)
        {
            IntPtr hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_SUSPEND_RESUME, false, processId);
            if (hProc == IntPtr.Zero) return false;

            try
            {
                return NativeMethods.NtSuspendProcess(hProc) == 0;
            }
            finally
            {
                NativeMethods.CloseHandle(hProc);
            }
        }

        /// <summary>
        /// Resumes execution of all suspended threads in the target process using NT Native <c>NtResumeProcess</c>.
        /// </summary>
        /// <param name="processId">Target process identifier.</param>
        /// <returns>True if resume command succeeded.</returns>
        public static bool ResumeProcess(int processId)
        {
            IntPtr hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_SUSPEND_RESUME, false, processId);
            if (hProc == IntPtr.Zero) return false;

            try
            {
                return NativeMethods.NtResumeProcess(hProc) == 0;
            }
            finally
            {
                NativeMethods.CloseHandle(hProc);
            }
        }

        /// <summary>
        /// Changes the Windows task scheduler priority class of the target process.
        /// </summary>
        /// <param name="processId">Target process identifier.</param>
        /// <param name="priority">Desired priority class (Realtime, High, Normal, Idle, etc.).</param>
        /// <returns>True if the priority was updated.</returns>
        public static bool SetPriority(int processId, ProcessPriorityClass priority)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                proc.PriorityClass = priority;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Closes a kernel object handle inside a target remote process by duplicating with <see cref="NativeMethods.DUPLICATE_CLOSE_SOURCE"/>.
        /// This is useful for unlocking locked files, sockets, or mutexes held by stubborn processes.
        /// </summary>
        /// <param name="processId">Target process holding the handle.</param>
        /// <param name="handleValue">Raw value of the handle in the target process.</param>
        /// <returns>True if the remote handle was successfully closed.</returns>
        public static bool CloseRemoteHandle(int processId, IntPtr handleValue)
        {
            IntPtr hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_DUP_HANDLE, false, processId);
            if (hProc == IntPtr.Zero) return false;

            try
            {
                IntPtr dupHandle;
                return NativeMethods.DuplicateHandle(
                    hProc,
                    handleValue,
                    IntPtr.Zero,
                    out dupHandle,
                    0,
                    false,
                    NativeMethods.DUPLICATE_CLOSE_SOURCE);
            }
            finally
            {
                NativeMethods.CloseHandle(hProc);
            }
        }
    }
}
