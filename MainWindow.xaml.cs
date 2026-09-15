using System;
using System.Diagnostics;
using System.Windows;
using ProcMaster.Models;
using ProcMaster.ViewModels;

namespace ProcMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml. Hosts the Process Explorer split UI,
    /// toolbar actions (Kill, Kill Tree, Suspend, Resume, Priority), and lower-pane tabs.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        /// <summary>
        /// Initializes the MainWindow and instantiates the <see cref="MainViewModel"/>.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;
        }

        /// <summary>
        /// Handles TreeView selection change to trigger detailed lower pane inspection.
        /// </summary>
        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ProcessItem item)
            {
                _vm.SelectedProcess = item;
            }
        }

        /// <summary>Prompts confirmation and terminates the selected process.</summary>
        private void OnKillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedProcess != null)
            {
                var res = MessageBox.Show($"Are you sure you want to terminate '{_vm.SelectedProcess.ProcessName}' (PID: {_vm.SelectedProcess.ProcessId})?", 
                    "Terminate Process", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    _vm.KillSelected();
                }
            }
        }

        /// <summary>Prompts confirmation and terminates the selected process and all child processes.</summary>
        private void OnKillTree_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedProcess != null)
            {
                var res = MessageBox.Show($"Are you sure you want to terminate the process tree for '{_vm.SelectedProcess.ProcessName}' (PID: {_vm.SelectedProcess.ProcessId})?", 
                    "Kill Process Tree", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    _vm.KillTreeSelected();
                }
            }
        }

        /// <summary>Suspends execution of the selected process.</summary>
        private void OnSuspend_Click(object sender, RoutedEventArgs e)
        {
            _vm.SuspendSelected();
        }

        /// <summary>Resumes execution of the suspended process.</summary>
        private void OnResume_Click(object sender, RoutedEventArgs e)
        {
            _vm.ResumeSelected();
        }

        private void OnPriorityRealtime_Click(object sender, RoutedEventArgs e) => _vm.SetPriority(ProcessPriorityClass.RealTime);
        private void OnPriorityHigh_Click(object sender, RoutedEventArgs e) => _vm.SetPriority(ProcessPriorityClass.High);
        private void OnPriorityAboveNormal_Click(object sender, RoutedEventArgs e) => _vm.SetPriority(ProcessPriorityClass.AboveNormal);
        private void OnPriorityNormal_Click(object sender, RoutedEventArgs e) => _vm.SetPriority(ProcessPriorityClass.Normal);
        private void OnPriorityBelowNormal_Click(object sender, RoutedEventArgs e) => _vm.SetPriority(ProcessPriorityClass.BelowNormal);
        private void OnPriorityIdle_Click(object sender, RoutedEventArgs e) => _vm.SetPriority(ProcessPriorityClass.Idle);

        /// <summary>Forces an immediate refresh of process metrics.</summary>
        private async void OnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await _vm.RefreshAsync();
        }

        /// <summary>
        /// Prompts warning and closes an open kernel object handle in the selected process.
        /// </summary>
        private void OnCloseHandle_Click(object sender, RoutedEventArgs e)
        {
            if (HandlesGrid.SelectedItem is HandleItem handle)
            {
                var res = MessageBox.Show($"Closing handle {handle.HandleValue} ({handle.Type}: {handle.Name}) can cause the host process or system to crash. Do you want to proceed?",
                    "Close Handle Warning", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (res == MessageBoxResult.Yes)
                {
                    _vm.CloseHandle(handle);
                }
            }
        }
    }
}