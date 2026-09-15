using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ProcMaster.Models;
using ProcMaster.Services;

namespace ProcMaster.ViewModels
{
    /// <summary>
    /// Core ViewModel for the ProcMaster application. Manages process collection synchronization,
    /// lower detail pane loading, user search filtering, and process manipulation commands.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ProcessMonitorService _monitorService = new();
        private readonly DispatcherTimer _refreshTimer;
        private CancellationTokenSource? _handleCts;

        private ProcessItem? _selectedProcess;
        private string _searchText = string.Empty;
        private bool _isTreeView = true;
        private int _totalProcesses;
        private int _totalThreads;
        private int _totalHandles;
        private double _totalCpuUsage;
        private string _statusText = "Ready";

        /// <summary>Hierarchical collection of processes bound to the TreeView.</summary>
        public ObservableCollection<ProcessItem> ProcessTree { get; } = new();

        /// <summary>Flat collection of processes bound to the DataGrid.</summary>
        public ObservableCollection<ProcessItem> ProcessList { get; } = new();

        /// <summary>Loaded dynamic-link libraries for the currently selected process.</summary>
        public ObservableCollection<ModuleItem> Modules { get; } = new();

        /// <summary>Open handles for the currently selected process.</summary>
        public ObservableCollection<HandleItem> Handles { get; } = new();

        /// <summary>Operating system threads for the currently selected process.</summary>
        public ObservableCollection<ThreadItem> Threads { get; } = new();

        /// <summary>
        /// Initializes the ViewModel, starts the 1-second refresh timer, and executes the initial refresh.
        /// </summary>
        public MainViewModel()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshAsync();
            _refreshTimer.Start();

            _ = RefreshAsync();
        }

        /// <summary>Currently selected process item in the TreeView or process list.</summary>
        public ProcessItem? SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                if (_selectedProcess != value)
                {
                    _selectedProcess = value;
                    OnPropertyChanged();
                    _ = LoadDetailsAsync(value);
                }
            }
        }

        /// <summary>Search filter query to filter processes by name, PID, or description.</summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FilterProcesses();
                }
            }
        }

        /// <summary>Indicates if the hierarchy TreeView mode is active.</summary>
        public bool IsTreeView
        {
            get => _isTreeView;
            set
            {
                if (_isTreeView != value)
                {
                    _isTreeView = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsFlatView));
                }
            }
        }

        /// <summary>Indicates if the flat list view mode is active.</summary>
        public bool IsFlatView => !IsTreeView;

        /// <summary>Total number of active system processes.</summary>
        public int TotalProcesses
        {
            get => _totalProcesses;
            set { _totalProcesses = value; OnPropertyChanged(); }
        }

        /// <summary>Total number of active system threads.</summary>
        public int TotalThreads
        {
            get => _totalThreads;
            set { _totalThreads = value; OnPropertyChanged(); }
        }

        /// <summary>Total number of open system handles.</summary>
        public int TotalHandles
        {
            get => _totalHandles;
            set { _totalHandles = value; OnPropertyChanged(); }
        }

        /// <summary>Aggregate total system CPU utilization percentage.</summary>
        public double TotalCpuUsage
        {
            get => _totalCpuUsage;
            set { _totalCpuUsage = value; OnPropertyChanged(); }
        }

        /// <summary>Status bar message display.</summary>
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Executes a full refresh cycle: queries processes, calculates deltas,
        /// builds the UI tree, and updates status metrics.
        /// </summary>
        public async Task RefreshAsync()
        {
            try
            {
                var (_, flat) = await _monitorService.RefreshProcessesAsync();

                TotalProcesses = flat.Count;
                TotalThreads = flat.Sum(p => p.ThreadCount);
                TotalHandles = flat.Sum(p => p.HandleCount);
                TotalCpuUsage = Math.Round(flat.Sum(p => p.CpuUsage), 1);

                // Update Flat View
                var filteredFlat = string.IsNullOrWhiteSpace(SearchText)
                    ? flat.OrderBy(p => p.ProcessName).ToList()
                    : flat.Where(p => p.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                      p.ProcessId.ToString().Contains(SearchText) ||
                                      p.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                          .OrderBy(p => p.ProcessName).ToList();

                SyncCollection(ProcessList, filteredFlat);

                // Update Tree View on UI Dispatcher thread
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    var treeRoots = _monitorService.BuildTree(flat);
                    SyncCollection(ProcessTree, treeRoots);
                }
                else
                {
                    // When search is active, show matching results in the tree view directly
                    SyncCollection(ProcessTree, filteredFlat);
                }

                StatusText = $"Updated at {DateTime.Now:HH:mm:ss} | {TotalProcesses} Processes | {TotalThreads} Threads | {TotalHandles} Handles";
            }
            catch (Exception ex)
            {
                StatusText = $"Refresh Error: {ex.Message}";
            }
        }

        private void FilterProcesses()
        {
            _ = RefreshAsync();
        }

        /// <summary>
        /// Loads modules, threads, and open handles for the selected process into the lower detail pane.
        /// </summary>
        private async Task LoadDetailsAsync(ProcessItem? proc)
        {
            Modules.Clear();
            Handles.Clear();
            Threads.Clear();

            if (proc == null) return;

            int pid = proc.ProcessId;

            // Load Modules
            var modules = ProcessMonitorService.GetModulesForProcess(pid);
            foreach (var m in modules) Modules.Add(m);

            // Load Threads
            var threads = ProcessMonitorService.GetThreadsForProcess(pid);
            foreach (var t in threads) Threads.Add(t);

            // Cancel any prior handle query
            _handleCts?.Cancel();
            _handleCts = new CancellationTokenSource();
            var token = _handleCts.Token;

            // Load Handles in background
            try
            {
                var handles = await HandleEnumerator.GetHandlesForProcessAsync(pid, token);
                if (!token.IsCancellationRequested)
                {
                    foreach (var h in handles) Handles.Add(h);
                }
            }
            catch { }
        }

        /// <summary>Kills the currently selected process.</summary>
        public void KillSelected()
        {
            if (SelectedProcess != null)
            {
                ProcessActionService.TerminateProcess(SelectedProcess.ProcessId);
                _ = RefreshAsync();
            }
        }

        /// <summary>Kills the currently selected process and its entire process tree.</summary>
        public void KillTreeSelected()
        {
            if (SelectedProcess != null)
            {
                ProcessActionService.TerminateProcessTree(SelectedProcess.ProcessId);
                _ = RefreshAsync();
            }
        }

        /// <summary>Suspends the currently selected process.</summary>
        public void SuspendSelected()
        {
            if (SelectedProcess != null)
            {
                ProcessActionService.SuspendProcess(SelectedProcess.ProcessId);
            }
        }

        /// <summary>Resumes the currently selected process.</summary>
        public void ResumeSelected()
        {
            if (SelectedProcess != null)
            {
                ProcessActionService.ResumeProcess(SelectedProcess.ProcessId);
            }
        }

        /// <summary>Adjusts the scheduling priority of the currently selected process.</summary>
        public void SetPriority(ProcessPriorityClass priority)
        {
            if (SelectedProcess != null)
            {
                ProcessActionService.SetPriority(SelectedProcess.ProcessId, priority);
                SelectedProcess.Priority = priority.ToString();
            }
        }

        /// <summary>Closes an open handle inside the remote process.</summary>
        public void CloseHandle(HandleItem handle)
        {
            if (handle != null && handle.ProcessId > 0)
            {
                ProcessActionService.CloseRemoteHandle(handle.ProcessId, handle.RawHandle);
                Handles.Remove(handle);
            }
        }

        /// <summary>Synchronizes an ObservableCollection with a source list without discarding view state unnecessarily.</summary>
        private static void SyncCollection(ObservableCollection<ProcessItem> target, IList<ProcessItem> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
