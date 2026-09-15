using System;

namespace ProcMaster.Models
{
    /// <summary>
    /// Represents a dynamic-link library (DLL) or executable module mapped into a target process address space.
    /// </summary>
    public class ModuleItem
    {
        /// <summary>Filename of the module (e.g., "ntdll.dll", "kernel32.dll").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Hexadecimal virtual base memory address where the module is loaded in process memory.</summary>
        public string BaseAddress { get; set; } = string.Empty;

        /// <summary>Formatted memory footprint size of the module in KB or MB.</summary>
        public string SizeDisplay { get; set; } = string.Empty;

        /// <summary>Full path on disk to the loaded module file.</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>File description from the binary version resources.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Product/file version string from binary version resources.</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>Publisher or company that authored the binary module.</summary>
        public string Company { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an open operating system kernel handle (File, Key, Mutant, Section, etc.) held by a target process.
    /// </summary>
    public class HandleItem
    {
        /// <summary>Hexadecimal value of the handle in the target process handle table.</summary>
        public string HandleValue { get; set; } = string.Empty;

        /// <summary>Object type name (e.g. "File", "Key", "Mutant", "Section", "Event", "ALPC Port").</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Resolved object name (e.g. file path, registry key path, named pipe identifier).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Kernel memory address pointer to the underlying object header.</summary>
        public ulong Pointer { get; set; }

        /// <summary>Granted access rights mask for this handle.</summary>
        public uint GrantedAccess { get; set; }

        /// <summary>Process ID of the process holding the handle.</summary>
        public int ProcessId { get; set; }

        /// <summary>Raw pointer handle value in the host process for programmatic operations.</summary>
        public IntPtr RawHandle { get; set; }
    }

    /// <summary>
    /// Represents an execution thread running inside the target process.
    /// </summary>
    public class ThreadItem
    {
        /// <summary>Unique Thread Identifier (TID).</summary>
        public int ThreadId { get; set; }

        /// <summary>Execution state (Running, Wait, Transition, Terminated).</summary>
        public string State { get; set; } = string.Empty;

        /// <summary>Thread scheduling priority level.</summary>
        public int Priority { get; set; }

        /// <summary>Timestamp when the thread was created.</summary>
        public string StartTime { get; set; } = string.Empty;

        /// <summary>Total accumulated user and kernel processor time used by this thread.</summary>
        public string CpuTime { get; set; } = string.Empty;
    }
}
