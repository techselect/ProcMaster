<div align="center">

<img src="app_icon.png" alt="ProcMaster Icon" width="128" height="128" />

# ProcMaster

**An advanced Windows Process Explorer & Task Manager hybrid built in C# (.NET 10) & WPF.**

[![Release: v1.0.0](https://img.shields.io/github/v/release/techselect/ProcMaster?color=success)](https://github.com/techselect/ProcMaster/releases/latest)
[![License: Unlicense](https://img.shields.io/badge/License-Unlicense-blue.svg)](http://unlicense.org/)
[![Target: .NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg)](https://www.microsoft.com/windows)

</div>

---

## 🌟 Overview

**ProcMaster** combines the interactive process hierarchy and deep inspection capabilities of Microsoft Sysinternals **Process Explorer** with the high-level agility of the Windows **Task Manager**.

Written entirely in modern C# (.NET 10) with Windows Presentation Foundation (WPF) and low-level native Win32/NT kernel APIs (`ntdll.dll`, `kernel32.dll`, `psapi.dll`), ProcMaster delivers responsive, sub-millisecond telemetry and deep process inspection without consuming unnecessary system resources.

---

## 🚀 Key Features

### 🌳 1. Real-Time Process Hierarchy (Process Tree)
- **Parent-Child Tree Visualization**: Processes are nested under their true parent process with interactive expand/collapse controls.
- **Process Lifecycle Highlighting**:
  - 🟢 **Green Highlight**: Newly spawned processes.
  - 🔴 **Red Highlight**: Terminating / exiting processes before cleanup.
- **Sub-Millisecond Snapshot Engine**: Uses native Win32 `CreateToolhelp32Snapshot` instead of heavy WMI queries for instantaneous hierarchy discovery.
- **UI Virtualization & In-Place Synchronization**: Only visible elements are rendered on screen, ensuring smooth 60 FPS scrolling and zero UI flickering during refresh cycles.

### 📊 2. Performance & Metric Telemetry
- **CPU Delta Calculation**: Accurately computes per-process CPU percentage across all logical processor cores.
- **Memory Metrics**: Tracks **Private Bytes** (unshareable commit) and **Working Set** (physical RAM).
- **Handle & Thread Counts**: Real-time count of open kernel object handles and active execution threads.
- **Architecture Detection**: Identifies whether a process is running 32-bit (x86 WOW64) or native 64-bit (x64).
- **Executable Metadata**: Automatically extracts binary publisher, company name, file version descriptions, and embedded application icons.

### 🔍 3. Lower Pane Deep Inspection (Process Explorer Style)
- **📦 Loaded Modules (DLLs)**: Virtual base memory addresses (`0x...`), module memory sizes, descriptions, publishers, and file paths on disk.
- **🔑 Open System Handles**: Scans the kernel system handle table via `NtQuerySystemInformation` and resolves object types (Files, Keys, Mutexes, Events, Ports).
  - *Remote Handle Termination*: Unlock files or resources by closing handles directly from the UI.
- **🧵 Thread Viewer**: Inspects individual execution threads, states (Running, Wait, Transition), priorities, start times, and consumed CPU time.
- **ℹ Process Properties**: Command line parameters, full binary path, start timestamp, and priority classes.

### ⚡ 4. Process Management & Action Controls
- **Kill Process**: Standard termination via managed API and native `TerminateProcess`.
- **Kill Process Tree**: Recursively kills the selected process and all descendant child processes.
- **Suspend & Resume**: Halts and resumes execution of all threads in a process using NT Native `NtSuspendProcess` and `NtResumeProcess`.
- **Set Priority**: Dynamic task scheduler adjustments (Realtime, High, Above Normal, Normal, Below Normal, Idle).
- **Fast Filter**: Instant search filter by Process Name, PID, or Description.

---

## 🛠 Project Architecture

```
ProcMaster/
├── Converters/
│   └── ValueConverters.cs          # WPF color and indentation converters
├── Models/
│   ├── ProcessItem.cs              # Core process telemetry and tree model
│   └── DetailItems.cs              # ModuleItem, HandleItem, ThreadItem
├── Services/
│   ├── NativeMethods.cs            # P/Invoke definitions (ntdll, kernel32, psapi)
│   ├── ProcessMonitorService.cs    # Sub-ms Toolhelp snapshots and telemetry engine
│   ├── HandleEnumerator.cs         # System handle scanner & duplication logic
│   └── ProcessActionService.cs     # Suspend, Resume, Kill, Priority commands
├── ViewModels/
│   └── MainViewModel.cs            # MVVM pattern, in-place list/tree sync, search
├── App.xaml / App.xaml.cs          # WPF application entry point
├── MainWindow.xaml / cs            # Virtualized split UI with dark theme
├── app_icon.ico / app_icon.png     # Custom application branding icons
├── ProcMaster.csproj               # .NET 10 Windows SDK project file
├── LICENSE                         # The Unlicense
└── README.md                       # Project documentation
```

---

## 💻 Prerequisites & Installation

### Requirements
- **Operating System**: Windows 10 (1809+), Windows 11, or Windows Server 2019+
- **Runtime / SDK**: [.NET 10.0 SDK](https://dotnet.microsoft.com/) (only required if building from source)

---

## 🏗 Building from Source

Clone the repository:
```bash
git clone https://github.com/techselect/ProcMaster.git
cd ProcMaster
```

### Run Locally (Development)
```powershell
dotnet run
```

### Build Standalone Release
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o bin/ReleaseStandalone
```

> **Note**: For full visibility into system processes and handles (such as `csrss.exe`, `lsass.exe`, or services), launch ProcMaster as **Administrator**.

---

## 📦 Downloads & Releases

Pre-compiled standalone Windows executables and portable archives are available on the [Releases](https://github.com/techselect/ProcMaster/releases) page.

- **[Latest Release (v1.0.0)](https://github.com/techselect/ProcMaster/releases/latest)**
- **Portable ZIP**: `ProcMaster-v1.0.0-win-x64.zip`
- **Standalone Binary**: `ProcMaster.exe` (No .NET installation required)

---

## 📄 License
 
This project is dedicated to the public domain under **[The Unlicense](LICENSE)**. You are completely free to use, copy, modify, distribute, sell, or do whatever you wish with it without restriction or attribution requirements.
