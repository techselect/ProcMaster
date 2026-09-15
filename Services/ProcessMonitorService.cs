using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProcMaster.Models;

namespace ProcMaster.Services
{
    /// <summary>
    /// Background monitoring service responsible for collecting real-time process telemetry,
    /// tracking process lifecycles (creation/termination highlighting), calculating CPU delta percentages,
    /// extracting binary metadata and icons, and building parent-child hierarchy trees.
    /// </summary>
    public class ProcessMonitorService
    {
        private readonly ConcurrentDictionary<int, ProcessItem> _trackedProcesses = new();
        private readonly ConcurrentDictionary<int, int> _parentPids = new();
        private readonly ConcurrentDictionary<string, ImageSource?> _iconCache = new();
        private readonly int _processorCount = Environment.ProcessorCount;

        /// <summary>Color brush applied to newly detected processes (soft modern green tint matching screenshot).</summary>
        public static readonly SolidColorBrush NewProcessBrush = new(System.Windows.Media.Color.FromArgb(90, 46, 125, 50));

        /// <summary>Color brush applied to terminating processes before removal (soft red tint).</summary>
        public static readonly SolidColorBrush ExitingProcessBrush = new(System.Windows.Media.Color.FromArgb(90, 198, 40, 40));

        /// <summary>Standard transparent background brush for stable processes.</summary>
        public static readonly SolidColorBrush NormalBrush = System.Windows.Media.Brushes.Transparent;

        static ProcessMonitorService()
        {
            NewProcessBrush.Freeze();
            ExitingProcessBrush.Freeze();
        }

        /// <summary>
        /// Asynchronously queries all running processes, calculates CPU and memory deltas,
        /// and returns updated process models.
        /// </summary>
        /// <returns>A tuple containing hierarchy roots and a flat list of monitored processes.</returns>
        public async Task<(List<ProcessItem> Hierarchy, List<ProcessItem> FlatList)> RefreshProcessesAsync()
        {
            return await Task.Run(() =>
            {
                // Refresh Parent PID mapping periodically via WMI
                RefreshParentPidMap();

                var currentProcesses = Process.GetProcesses();
                var currentPids = new HashSet<int>(currentProcesses.Select(p => p.Id));
                var now = DateTime.UtcNow;

                // Detect dead processes and set red highlight
                foreach (var kvp in _trackedProcesses.ToList())
                {
                    if (!currentPids.Contains(kvp.Key))
                    {
                        if (kvp.Value.HighlightTicksRemaining <= 0)
                        {
                            kvp.Value.HighlightColor = ExitingProcessBrush;
                            kvp.Value.HighlightTicksRemaining = 2; // Keep red for 2 intervals
                        }
                        else
                        {
                            kvp.Value.HighlightTicksRemaining--;
                            if (kvp.Value.HighlightTicksRemaining <= 0)
                            {
                                _trackedProcesses.TryRemove(kvp.Key, out _);
                            }
                        }
                    }
                }

                // Process active processes in parallel
                Parallel.ForEach(currentProcesses, proc =>
                {
                    try
                    {
                        int pid = proc.Id;
                        bool isNew = !_trackedProcesses.ContainsKey(pid);

                        var item = _trackedProcesses.GetOrAdd(pid, id =>
                        {
                            var newItem = new ProcessItem
                            {
                                ProcessId = id,
                                ProcessName = proc.ProcessName,
                                LastSampleTime = now,
                                HighlightColor = NewProcessBrush,
                                HighlightTicksRemaining = 2
                            };

                            // Query static info once
                            PopulateStaticProcessInfo(proc, newItem);
                            return newItem;
                        });

                        // Calculate CPU usage delta across cores
                        try
                        {
                            var currentTotalTime = proc.TotalProcessorTime;
                            var timeDelta = (now - item.LastSampleTime).TotalMilliseconds;

                            if (timeDelta > 0 && item.LastTotalProcessorTime > TimeSpan.Zero)
                            {
                                var cpuDelta = (currentTotalTime - item.LastTotalProcessorTime).TotalMilliseconds;
                                var cpuPercent = (cpuDelta / (timeDelta * _processorCount)) * 100.0;
                                item.CpuUsage = Math.Clamp(cpuPercent, 0, 100.0);
                            }

                            item.LastTotalProcessorTime = currentTotalTime;
                            item.LastSampleTime = now;
                        }
                        catch
                        {
                            item.CpuUsage = 0;
                        }

                        // Query memory working set and handle counts
                        try
                        {
                            item.WorkingSetBytes = proc.WorkingSet64;
                            item.PrivateBytes = proc.PrivateMemorySize64;
                            item.ThreadCount = proc.Threads.Count;
                            item.HandleCount = proc.HandleCount;
                        }
                        catch { }

                        // Fade highlight back to normal after duration
                        if (!isNew && item.HighlightTicksRemaining > 0)
                        {
                            item.HighlightTicksRemaining--;
                            if (item.HighlightTicksRemaining <= 0)
                            {
                                item.HighlightColor = NormalBrush;
                            }
                        }
                    }
                    catch
                    {
                        // Process may have exited while enumerating
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                });

                var flatList = _trackedProcesses.Values.ToList();
                return (new List<ProcessItem>(), flatList);
            });
        }

        /// <summary>
        /// Populates static metadata (executable path, description, publisher, architecture, icon) for a process.
        /// </summary>
        private void PopulateStaticProcessInfo(Process proc, ProcessItem item)
        {
            try
            {
                if (_parentPids.TryGetValue(item.ProcessId, out int ppid))
                {
                    item.ParentProcessId = ppid;
                }

                try { item.StartTime = proc.StartTime; } catch { }
                try { item.Priority = proc.PriorityClass.ToString(); } catch { }

                try
                {
                    string? mainModuleFileName = null;
                    try { mainModuleFileName = proc.MainModule?.FileName; } catch { }

                    if (!string.IsNullOrEmpty(mainModuleFileName))
                    {
                        item.ExecutablePath = mainModuleFileName;
                        var versionInfo = FileVersionInfo.GetVersionInfo(mainModuleFileName);
                        item.Description = versionInfo.FileDescription ?? string.Empty;
                        item.CompanyName = versionInfo.CompanyName ?? string.Empty;

                        item.Icon = GetProcessIcon(mainModuleFileName);
                    }
                }
                catch { }

                // Check architecture (32-bit WOW64 vs native 64-bit)
                try
                {
                    if (Environment.Is64BitOperatingSystem)
                    {
                        if (NativeMethods.IsWow64Process(proc.Handle, out bool isWow64))
                        {
                            item.Architecture = isWow64 ? "32-bit (x86)" : "64-bit (x64)";
                        }
                    }
                }
                catch { }
            }
            catch { }
        }

        /// <summary>
        /// Extracts and caches the application icon from the binary file on disk.
        /// </summary>
        private ImageSource? GetProcessIcon(string filePath)
        {
            if (_iconCache.TryGetValue(filePath, out var cachedIcon))
            {
                return cachedIcon;
            }

            try
            {
                if (File.Exists(filePath))
                {
                    using var sysIcon = System.Drawing.Icon.ExtractAssociatedIcon(filePath);
                    if (sysIcon != null)
                    {
                        var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                            sysIcon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bitmapSource.Freeze();
                        _iconCache[filePath] = bitmapSource;
                        return bitmapSource;
                    }
                }
            }
            catch { }

            _iconCache[filePath] = null;
            return null;
        }

        /// <summary>
        /// Queries the Windows Toolhelp32 process snapshot API to rapidly map ProcessId to ParentProcessId
        /// in sub-millisecond time, replacing slow WMI queries.
        /// </summary>
        private void RefreshParentPidMap()
        {
            IntPtr hSnapshot = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.TH32CS_SNAPPROCESS, 0);
            if (hSnapshot == IntPtr.Zero || hSnapshot == (IntPtr)(-1)) return;

            try
            {
                var entry = new NativeMethods.PROCESSENTRY32();
                entry.dwSize = (uint)Marshal.SizeOf(typeof(NativeMethods.PROCESSENTRY32));

                if (NativeMethods.Process32First(hSnapshot, ref entry))
                {
                    do
                    {
                        _parentPids[(int)entry.th32ProcessID] = (int)entry.th32ParentProcessID;
                    } while (NativeMethods.Process32Next(hSnapshot, ref entry));
                }
            }
            catch
            {
            }
            finally
            {
                NativeMethods.CloseHandle(hSnapshot);
            }
        }

        /// <summary>
        /// Constructs a tree of <see cref="ProcessItem"/> nodes based on parent-child PID links.
        /// Must be invoked on the UI Dispatcher thread to safely update observable collections.
        /// </summary>
        /// <param name="allItems">The complete flat list of active processes.</param>
        /// <returns>Root process items with nested children.</returns>
        public List<ProcessItem> BuildTree(List<ProcessItem> allItems)
        {
            var dict = allItems.ToDictionary(p => p.ProcessId, p => p);
            var expectedChildren = new Dictionary<int, List<ProcessItem>>();
            var roots = new List<ProcessItem>();

            foreach (var item in allItems)
            {
                if (item.ParentProcessId > 0 && dict.TryGetValue(item.ParentProcessId, out var parent) && parent.ProcessId != item.ProcessId)
                {
                    if (!expectedChildren.TryGetValue(parent.ProcessId, out var childList))
                    {
                        childList = new List<ProcessItem>();
                        expectedChildren[parent.ProcessId] = childList;
                    }
                    childList.Add(item);
                }
                else
                {
                    roots.Add(item);
                }
            }

            // Sync children for each parent in-place
            foreach (var item in allItems)
            {
                if (expectedChildren.TryGetValue(item.ProcessId, out var children))
                {
                    SyncChildren(item.Children, children);
                }
                else if (item.Children.Count > 0)
                {
                    item.Children.Clear();
                }
            }

            return roots.OrderBy(r => r.ProcessName).ToList();
        }

        private static void SyncChildren(System.Collections.ObjectModel.ObservableCollection<ProcessItem> target, List<ProcessItem> source)
        {
            var sourceDict = new HashSet<int>(source.Select(p => p.ProcessId));
            for (int i = target.Count - 1; i >= 0; i--)
            {
                if (!sourceDict.Contains(target[i].ProcessId))
                {
                    target.RemoveAt(i);
                }
            }

            for (int i = 0; i < source.Count; i++)
            {
                var src = source[i];
                if (i < target.Count && target[i].ProcessId == src.ProcessId) continue;

                int existingIdx = -1;
                for (int j = i + 1; j < target.Count; j++)
                {
                    if (target[j].ProcessId == src.ProcessId)
                    {
                        existingIdx = j;
                        break;
                    }
                }

                if (existingIdx >= 0)
                {
                    target.Move(existingIdx, i);
                }
                else
                {
                    target.Insert(i, src);
                }
            }
        }

        /// <summary>
        /// Retrieves the list of loaded dynamic-link libraries (DLLs) mapped in the target process.
        /// </summary>
        /// <param name="pid">Target process ID.</param>
        /// <returns>List of <see cref="ModuleItem"/> instances.</returns>
        public static List<ModuleItem> GetModulesForProcess(int pid)
        {
            var list = new List<ModuleItem>();
            try
            {
                using var proc = Process.GetProcessById(pid);
                foreach (ProcessModule mod in proc.Modules)
                {
                    string desc = "";
                    string comp = "";
                    string ver = "";
                    try
                    {
                        var info = mod.FileVersionInfo;
                        desc = info.FileDescription ?? "";
                        comp = info.CompanyName ?? "";
                        ver = info.FileVersion ?? "";
                    }
                    catch { }

                    list.Add(new ModuleItem
                    {
                        Name = mod.ModuleName,
                        BaseAddress = $"0x{mod.BaseAddress.ToInt64():X}",
                        SizeDisplay = $"{mod.ModuleMemorySize / 1024.0:F1} KB",
                        FilePath = mod.FileName,
                        Description = desc,
                        Company = comp,
                        Version = ver
                    });
                }
            }
            catch { }
            return list.OrderBy(m => m.Name).ToList();
        }

        /// <summary>
        /// Retrieves the list of operating system execution threads inside the target process.
        /// </summary>
        /// <param name="pid">Target process ID.</param>
        /// <returns>List of <see cref="ThreadItem"/> instances.</returns>
        public static List<ThreadItem> GetThreadsForProcess(int pid)
        {
            var list = new List<ThreadItem>();
            try
            {
                using var proc = Process.GetProcessById(pid);
                foreach (ProcessThread thread in proc.Threads)
                {
                    string start = "";
                    string cpu = "";
                    try { start = thread.StartTime.ToShortTimeString(); } catch { }
                    try { cpu = $"{thread.TotalProcessorTime.TotalSeconds:F2}s"; } catch { }

                    list.Add(new ThreadItem
                    {
                        ThreadId = thread.Id,
                        State = thread.ThreadState.ToString(),
                        Priority = thread.CurrentPriority,
                        StartTime = start,
                        CpuTime = cpu
                    });
                }
            }
            catch { }
            return list.OrderBy(t => t.ThreadId).ToList();
        }
    }
}
