using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ProcMaster.Models
{
    /// <summary>
    /// Represents a monitored Windows process item in the hierarchy tree and flat process list.
    /// Implements <see cref="INotifyPropertyChanged"/> for real-time WPF data binding updates.
    /// </summary>
    public class ProcessItem : INotifyPropertyChanged
    {
        private double _cpuUsage;
        private long _workingSetBytes;
        private long _privateBytes;
        private int _threadCount;
        private int _handleCount;
        private SolidColorBrush _highlightColor = Brushes.Transparent;
        private bool _isExpanded = true;
        private bool _isSelected;

        /// <summary>Unique Windows Process Identifier (PID).</summary>
        public int ProcessId { get; set; }

        /// <summary>Parent Process Identifier (PPID) used for constructing the process hierarchy tree.</summary>
        public int ParentProcessId { get; set; }

        /// <summary>Base executable image name without directory path (e.g. "explorer.exe").</summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>Friendly file description extracted from binary FileVersionInfo metadata.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Company or publisher name extracted from binary FileVersionInfo metadata.</summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Full filesystem path to the main process executable binary.</summary>
        public string ExecutablePath { get; set; } = string.Empty;

        /// <summary>Command line parameters passed when launching the process.</summary>
        public string CommandLine { get; set; } = string.Empty;

        /// <summary>User account owning the process context (e.g., Domain\User or NT AUTHORITY\SYSTEM).</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>Binary architecture of the process ("32-bit (x86)" or "64-bit (x64)").</summary>
        public string Architecture { get; set; } = "x64";

        /// <summary>Timestamp when the process was started.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>Process scheduling priority class string (Normal, High, Idle, etc.).</summary>
        public string Priority { get; set; } = "Normal";

        /// <summary>Extracted shell icon bitmap source for graphical display in the UI.</summary>
        public ImageSource? Icon { get; set; }

        /// <summary>
        /// Real-time CPU utilization percentage computed over the sampling interval across all CPU cores.
        /// </summary>
        public double CpuUsage
        {
            get => _cpuUsage;
            set { if (Math.Abs(_cpuUsage - value) > 0.01) { _cpuUsage = value; OnPropertyChanged(); } }
        }

        /// <summary>Working Set (physical memory currently dedicated to the process in RAM) in bytes.</summary>
        public long WorkingSetBytes
        {
            get => _workingSetBytes;
            set { if (_workingSetBytes != value) { _workingSetBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(WorkingSetDisplay)); } }
        }

        /// <summary>Private memory allocated to the process that cannot be shared with other processes, in bytes.</summary>
        public long PrivateBytes
        {
            get => _privateBytes;
            set { if (_privateBytes != value) { _privateBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(PrivateBytesDisplay)); } }
        }

        /// <summary>Total count of operating system threads executing within this process.</summary>
        public int ThreadCount
        {
            get => _threadCount;
            set { if (_threadCount != value) { _threadCount = value; OnPropertyChanged(); } }
        }

        /// <summary>Total count of open operating system object handles owned by this process.</summary>
        public int HandleCount
        {
            get => _handleCount;
            set { if (_handleCount != value) { _handleCount = value; OnPropertyChanged(); } }
        }

        /// <summary>Human-readable formatted Working Set size string (e.g. "45.2 MB").</summary>
        public string WorkingSetDisplay => FormatBytes(WorkingSetBytes);

        /// <summary>Human-readable formatted Private Bytes size string (e.g. "23.4 MB").</summary>
        public string PrivateBytesDisplay => FormatBytes(PrivateBytes);

        /// <summary>
        /// Background highlight brush used to color-code lifecycle changes (Green for newly created, Red for exiting).
        /// </summary>
        public SolidColorBrush HighlightColor
        {
            get => _highlightColor;
            set { _highlightColor = value; OnPropertyChanged(); }
        }

        /// <summary>Indicates whether this node is expanded in the TreeView.</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        /// <summary>Indicates whether this node is currently selected in the UI.</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        /// <summary>Child processes spawned by this process.</summary>
        public ObservableCollection<ProcessItem> Children { get; set; } = new ObservableCollection<ProcessItem>();

        /// <summary>Accumulated processor time from the preceding sampling cycle for CPU delta calculation.</summary>
        public TimeSpan LastTotalProcessorTime { get; set; }

        /// <summary>Timestamp of the preceding sampling cycle.</summary>
        public DateTime LastSampleTime { get; set; }

        /// <summary>Remaining refresh ticks before the lifecycle highlight (Green/Red) fades back to transparent.</summary>
        public int HighlightTicksRemaining { get; set; }

        /// <summary>Formats byte values into friendly KB / MB / GB strings.</summary>
        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
