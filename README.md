# ProcMaster

**ProcMaster** is a Windows Process Explorer and Task Manager hybrid built with **C# (.NET 10)**, **WPF**, and Windows Native/NT APIs (`ntdll.dll`, `kernel32.dll`, `psapi.dll`, WMI).

---

## Features

- **Process Hierarchy Tree**: Visualizes parent-child process relationships with automatic nesting and expansion.
- **Process Lifecycle Highlighting**: Newly started processes flash green, while terminating processes flash red before being cleaned up.
- **Real-Time CPU Utilization**: Accurate CPU delta percentage computed across all logical cores.
- **Memory & Resource Metrics**: Tracks Private Bytes, Working Set, Thread count, Handle count, and 32-bit (x86) vs 64-bit (x64) architecture.
- **Lower Inspection Pane**:
  - **Loaded Modules (DLLs)**: Virtual base address, memory footprint, publisher, description, and file path.
  - **Open Handles**: System handle table scanning via `NtQuerySystemInformation` and handle duplication to inspect file paths, registry keys, and object types. Supports closing handles remotely.
  - **Threads**: Thread ID, current execution state, priority, and CPU time.
  - **Process Properties**: Command line arguments, executable path, start time, and priority class.
- **Process Actions**:
  - Kill Process / Kill Process Tree
  - Suspend / Resume Process (`NtSuspendProcess` / `NtResumeProcess`)
  - Adjust scheduling priority (Realtime, High, Above Normal, Normal, Below Normal, Idle)

---

## Build & Run

### Prerequisites
- Windows 10/11 / Windows Server
- [.NET 10.0 SDK](https://dotnet.microsoft.com/)

### Development Run
```powershell
cd ProcMaster
dotnet run
```

### Release Publish
```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o bin/Publish
```
*Note: To inspect handles of elevated or SYSTEM processes, run ProcMaster as Administrator.*
