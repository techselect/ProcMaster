using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ProcMaster.Models;

namespace ProcMaster.Services
{
    /// <summary>
    /// Enumerates open kernel object handles (files, registry keys, mutexes, threads)
    /// across system processes using low-level NT Native system information APIs.
    /// </summary>
    public static class HandleEnumerator
    {
        private const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);
        private const int STATUS_SUCCESS = 0;

        /// <summary>
        /// Asynchronously discovers and returns open handles owned by a target process.
        /// </summary>
        /// <param name="targetPid">The Process ID to inspect.</param>
        /// <param name="cancellationToken">Cancellation token to abort scanning if selection changes.</param>
        /// <returns>A list of resolved <see cref="HandleItem"/> entries.</returns>
        public static async Task<List<HandleItem>> GetHandlesForProcessAsync(int targetPid, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => GetHandlesForProcess(targetPid, cancellationToken));
        }

        /// <summary>
        /// Queries the system handle table via <see cref="NativeMethods.NtQuerySystemInformation"/>,
        /// filters entries by the specified process ID, and resolves their object types and names.
        /// </summary>
        /// <param name="targetPid">The target process identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of discovered <see cref="HandleItem"/> records.</returns>
        public static List<HandleItem> GetHandlesForProcess(int targetPid, CancellationToken cancellationToken)
        {
            var results = new List<HandleItem>();

            IntPtr hTargetProc = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_DUP_HANDLE,
                false,
                targetPid);

            if (hTargetProc == IntPtr.Zero)
            {
                // Fallback to query information
                hTargetProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_INFORMATION, false, targetPid);
                if (hTargetProc == IntPtr.Zero) return results;
            }

            int length = 0x10000;
            IntPtr buffer = Marshal.AllocHGlobal(length);

            try
            {
                int returnLength;
                while (NativeMethods.NtQuerySystemInformation(NativeMethods.SystemExtendedHandleInformation, buffer, length, out returnLength) == STATUS_INFO_LENGTH_MISMATCH)
                {
                    Marshal.FreeHGlobal(buffer);
                    length = returnLength + 0x2000;
                    buffer = Marshal.AllocHGlobal(length);
                }

                long handleCount = Marshal.ReadInt64(buffer);
                long offset = 16; // Skip NumberOfHandles and Reserved on 64-bit

                int entrySize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();

                for (long i = 0; i < handleCount; i++)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    IntPtr entryPtr = new IntPtr(buffer.ToInt64() + offset + (i * entrySize));
                    var entry = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(entryPtr);

                    if ((long)entry.UniqueProcessId == targetPid)
                    {
                        var handleItem = ResolveHandle(hTargetProc, targetPid, (IntPtr)entry.HandleValue, entry.ObjectTypeIndex, entry.GrantedAccess);
                        if (handleItem != null)
                        {
                            results.Add(handleItem);
                        }
                    }
                }
            }
            catch
            {
                // Ignore query failures gracefully
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
                NativeMethods.CloseHandle(hTargetProc);
            }

            return results;
        }

        /// <summary>
        /// Duplicates a handle from the target process into the current process to safely inspect its type and name.
        /// </summary>
        private static HandleItem? ResolveHandle(IntPtr hSourceProcess, int targetPid, IntPtr rawHandle, ushort typeIndex, uint grantedAccess)
        {
            IntPtr currentProc = NativeMethods.GetCurrentProcess();
            IntPtr dupHandle = IntPtr.Zero;

            bool dupSuccess = NativeMethods.DuplicateHandle(
                hSourceProcess,
                rawHandle,
                currentProc,
                out dupHandle,
                0,
                false,
                NativeMethods.DUPLICATE_SAME_ACCESS);

            if (!dupSuccess || dupHandle == IntPtr.Zero)
            {
                return new HandleItem
                {
                    ProcessId = targetPid,
                    HandleValue = $"0x{rawHandle.ToInt64():X}",
                    Type = $"Type #{typeIndex}",
                    Name = "<Access Denied>",
                    RawHandle = rawHandle
                };
            }

            string typeName = GetHandleType(dupHandle);
            string objectName = string.Empty;

            // Only query name if not a potentially hanging synchronous file or pipe
            if (typeName != "File" && typeName != "Pipe")
            {
                objectName = GetHandleName(dupHandle);
            }

            NativeMethods.CloseHandle(dupHandle);

            return new HandleItem
            {
                ProcessId = targetPid,
                HandleValue = $"0x{rawHandle.ToInt64():X}",
                Type = string.IsNullOrEmpty(typeName) ? $"Type #{typeIndex}" : typeName,
                Name = objectName,
                RawHandle = rawHandle
            };
        }

        /// <summary>Queries the type name of an open kernel object.</summary>
        private static string GetHandleType(IntPtr handle)
        {
            int length = 0x1000;
            IntPtr buffer = Marshal.AllocHGlobal(length);
            try
            {
                if (NativeMethods.NtQueryObject(handle, NativeMethods.ObjectTypeInformation, buffer, length, out _) == STATUS_SUCCESS)
                {
                    var info = Marshal.PtrToStructure<NativeMethods.OBJECT_TYPE_INFORMATION>(buffer);
                    if (info.TypeName.Buffer != IntPtr.Zero && info.TypeName.Length > 0)
                    {
                        return Marshal.PtrToStringUni(info.TypeName.Buffer, info.TypeName.Length / 2) ?? "";
                    }
                }
            }
            catch { }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return string.Empty;
        }

        /// <summary>Queries the object name string (e.g., file path or registry path) of an open kernel object.</summary>
        private static string GetHandleName(IntPtr handle)
        {
            int length = 0x2000;
            IntPtr buffer = Marshal.AllocHGlobal(length);
            try
            {
                if (NativeMethods.NtQueryObject(handle, NativeMethods.ObjectNameInformation, buffer, length, out _) == STATUS_SUCCESS)
                {
                    var info = Marshal.PtrToStructure<NativeMethods.OBJECT_NAME_INFORMATION>(buffer);
                    if (info.Name.Buffer != IntPtr.Zero && info.Name.Length > 0)
                    {
                        return Marshal.PtrToStringUni(info.Name.Buffer, info.Name.Length / 2) ?? "";
                    }
                }
            }
            catch { }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return string.Empty;
        }

        /// <summary>Layout structure representing an extended system handle entry.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
        {
            public IntPtr Object;
            public UIntPtr UniqueProcessId;
            public UIntPtr HandleValue;
            public uint GrantedAccess;
            public ushort CreatorBackTraceIndex;
            public ushort ObjectTypeIndex;
            public uint HandleAttributes;
            public uint Reserved;
        }
    }
}
