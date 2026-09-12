#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VeloxOptiSL.Core;

namespace VeloxOptiSL
{
    public partial class MainWindow : Window
    {
        private readonly System.Windows.Threading.DispatcherTimer _uiTimer;
        private readonly LogWatcher _logWatcher = new();
        private readonly TelemetryService _telemetryService = new();
        private Process? _trackedProcess;
        private int _logCounter = 0;
        private double _peakCpu = 0;
        private double _peakRam = 0;

        // Call this when starting a heavy task (Launch or Clear Cache)
        private void SetProcessingState(bool isProcessing, string activeMessage)
        {
            if (isProcessing)
            {
                // Update status text with a warm working color
                TxtStatus.Text = activeMessage;
                TxtStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 100));

                // Disable buttons to prevent double-clicks during execution
                BtnLaunch.IsEnabled = false;
                BtnClearCache.IsEnabled = false;

                // Apply a smooth pulsing opacity animation to the launch button
                DoubleAnimation pulseAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.6,
                    Duration = TimeSpan.FromSeconds(0.6),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                BtnLaunch.BeginAnimation(UIElement.OpacityProperty, pulseAnimation);
            }
            else
            {
                // Restore buttons and clear animation
                BtnLaunch.IsEnabled = true;
                BtnClearCache.IsEnabled = true;
                BtnLaunch.BeginAnimation(UIElement.OpacityProperty, null);
                BtnLaunch.Opacity = 1.0;
            }
        }

        // Call this when the process successfully starts or finishes
        private void SetReadyState(bool isRunning, string statusMessage)
        {
            SetProcessingState(false, string.Empty);

            TxtStatus.Text = statusMessage;
            if (isRunning)
            {
                TxtStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 200, 120)); // Soft Green
            }
            else
            {
                TxtStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 85, 85)); // Soft Red
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            DwmApi.EnableFluentBackdrop(this, DwmApi.BackdropType.MainWindow);

            LogMessage("INFO", "Initializing system telemetry...");
            LogMessage("SUCCESS", "Core telemetry engine linked.");
            LogMetrics("Telemetry stream initialized and awaiting active session.");

            RegisterHoverEvents();

            _logWatcher.OnWarningDetected += (warning) =>
            {
                Dispatcher.Invoke(() =>
                {
                    BorderLogAlert.Visibility = Visibility.Visible;
                    TxtLogAlertMessage.Text = warning;
                    LogMessage("WARN", warning);
                });
            };

            LoadSettings();
            Closing += MainWindow_Closing;

            _uiTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();
        }

        private void UpdateSystemMetrics(double cpuPercent, double ramPercent)
        {
            if (cpuPercent > _peakCpu) _peakCpu = cpuPercent;
            if (ramPercent > _peakRam) _peakRam = ramPercent;

            PbCpu.Value = cpuPercent;
            TxtCpuPercent.Text = $"{Math.Round(cpuPercent)}%";
            if (TxtCpuPeak != null) TxtCpuPeak.Text = $" (Peak: {Math.Round(_peakCpu)}%)";

            PbRam.Value = ramPercent;
            TxtRamPercent.Text = $"{Math.Round(ramPercent)}%";
            if (TxtRamPeak != null) TxtRamPeak.Text = $" (Peak: {Math.Round(_peakRam)}%)";

            PbCpu.Foreground = cpuPercent switch
            {
                > 85 => System.Windows.Media.Brushes.IndianRed,
                > 60 => System.Windows.Media.Brushes.DarkOrange,
                _ => System.Windows.Media.Brushes.LightGreen
            };

            PbRam.Foreground = ramPercent switch
            {
                > 85 => System.Windows.Media.Brushes.IndianRed,
                > 60 => System.Windows.Media.Brushes.DarkOrange,
                _ => System.Windows.Media.Brushes.LightGreen
            };
        }

        private void LogMetrics(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            if (TxtMetricsLog != null)
            {
                TxtMetricsLog.AppendText($"[{timestamp}] {message}\n");
                TxtMetricsLog.ScrollToEnd();
            }
        }

        private void RegisterHoverEvents()
        {
            void BindHover(FrameworkElement element, string title, string description)
            {
                if (element == null) return;
                element.MouseEnter += (s, e) => UpdateInspector(title, description);
                element.MouseLeave += (s, e) => ResetInspector();
            }

            void ResetInspector()
            {
                TxtInspectorTitle.Text = "ℹ️ Inspector Ready";
                TxtInspectorDescription.Text = "Hover over any setting to see what it does in plain English.";
            }

            BindHover(SldRenderAvatars, "Max Fully Rendered Avatars", "Limits how many full 3D player avatars render near you. Anyone beyond this limit renders as a low-cost placeholder to save memory.");
            BindHover(SldDrawDistance, "Draw Distance (Visibility)", "Controls how far away you can see objects and terrain in meters. Lowering this is the fastest way to gain extra FPS.");
            BindHover(SldArc, "Avatar Complexity Limit (ARC)", "Caps the maximum detail allowed for nearby avatars. Highly complex avatars above this limit render as simple grey figures so your screen doesn't stutter.");
            BindHover(SldLod, "Mesh Loading Detail (LOD)", "Controls how sharply 3D mesh buildings and objects load in the distance. Turning this down reduces lag when entering complex, mesh-heavy regions.");
            BindHover(ChkJellyDoll, "Auto Jelly Doll Inspection", "Monitors nearby avatars and automatically turns lagging or unoptimized avatars into simple colored 'jellydolls' to keep your framerate smooth.");
            BindHover(ChkImpostors, "Enable 2D Avatar Proxies", "Swaps out distant 3D avatars for flat 2D image cutouts. This drastically lowers graphics memory (VRAM) usage in crowded places.");
            BindHover(ChkHighPriority, "High CPU Priority", "Tells Windows to give the viewer first dibs on processor power. This prevents web browsers or background apps from stealing performance while you play.");
            BindHover(ChkCoreAffinity, "Optimize CPU Core Affinity", "Pins the viewer process to your fastest main CPU cores. This prevents Windows from jumping between cores, reducing sudden frame drops and stuttering.");
            BindHover(ChkMuteGpu, "Background GPU Throttling", "Reduces graphics card overhead when the viewer is minimized or loses focus. Keeps your PC quiet while tabbed out.");
            BindHover(ChkTextureOptimization, "Dynamic Texture VRAM Optimization", "Cleans out old texture data from video memory in real time to prevent memory leaks and stuttering in heavy regions.");
        }

        private void UpdateInspector(string title, string description)
        {
            TxtInspectorTitle.Text = $"ℹ️ {title}";
            TxtInspectorDescription.Text = description;
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            if (_trackedProcess == null || _trackedProcess.HasExited)
            {
                var runningViewers = Process.GetProcessesByName("Firestorm-x64");
                if (runningViewers.Length > 0)
                {
                    _trackedProcess = runningViewers[0];
                    _telemetryService.AttachToProcess(_trackedProcess);
                    LogMessage("SUCCESS", $"Auto-attached to active Firestorm-x64 (PID: {_trackedProcess.Id})");
                    LogMetrics($"Attached to target process PID: {_trackedProcess.Id}");
                }
            }

            if (_trackedProcess != null && !_trackedProcess.HasExited)
            {
                var (cpuVal, ramMb) = _telemetryService.GetProcessMetrics();

                double cpuValClamped = Math.Min(cpuVal, 100);

                double totalRamMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024.0);
                if (totalRamMb <= 0) totalRamMb = 16384.0;
                double ramPct = Math.Min((ramMb / totalRamMb) * 100, 100);

                UpdateSystemMetrics(cpuValClamped, ramPct);

                if (TxtStatus != null)
                {
                    TxtStatus.Text = "Running";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                }

                if (TxtRamPid != null)
                {
                    TxtRamPid.Text = $"RAM: {ramMb} MB | PID: {_trackedProcess.Id}";
                }

                _logCounter++;
                if (_logCounter >= 30)
                {
                    _logCounter = 0;
                    LogMetrics($"CPU Load: {TxtCpuPercent.Text} | RAM Usage: {TxtRamPercent.Text}");
                }
            }
            else
            {
                _trackedProcess = null;
                _peakCpu = 0;
                _peakRam = 0;
                UpdateSystemMetrics(0, 0);

                if (TxtStatus != null)
                {
                    TxtStatus.Text = "Not Running";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.IndianRed;
                }

                if (TxtRamPid != null)
                {
                    TxtRamPid.Text = "RAM: -- | PID: --";
                }
            }
        }

        private void SaveSettings()
        {
            var settings = new UserAppSettings
            {
                ExePath = TxtExePath.Text,
                RenderAvatars = SldRenderAvatars.Value,
                DrawDistance = SldDrawDistance.Value,
                Arc = SldArc.Value,
                Lod = SldLod.Value,
                JellyDoll = ChkJellyDoll.IsChecked ?? true,
                Impostors = ChkImpostors.IsChecked ?? true,
                HighPriority = ChkHighPriority.IsChecked ?? true,
                CoreAffinity = ChkCoreAffinity.IsChecked ?? true,
                MuteGpu = ChkMuteGpu.IsChecked ?? true,
                TextureOptimization = ChkTextureOptimization.IsChecked ?? true
            };

            if (!CacheManager.SaveSettings(settings, out string error))
            {
                LogMessage("ERROR", $"Settings Save Failed: {error}");
            }
        }

        private void LoadSettings()
        {
            UserAppSettings settings = CacheManager.LoadSettings();

            if (!string.IsNullOrEmpty(settings.ExePath) && File.Exists(settings.ExePath))
            {
                TxtExePath.Text = settings.ExePath;
            }
            else
            {
                string detectedPath = PathDiscoveryService.AutoDetectExecutablePath();
                if (!string.IsNullOrEmpty(detectedPath))
                {
                    TxtExePath.Text = detectedPath;
                    LogMessage("INFO", $"Auto-discovered Firestorm executable at: {detectedPath}");
                }
            }

            SldRenderAvatars.Value = settings.RenderAvatars;
            SldDrawDistance.Value = settings.DrawDistance;
            SldArc.Value = settings.Arc;
            SldLod.Value = settings.Lod;
            ChkJellyDoll.IsChecked = settings.JellyDoll;
            ChkImpostors.IsChecked = settings.Impostors;
            ChkHighPriority.IsChecked = settings.HighPriority;
            ChkCoreAffinity.IsChecked = settings.CoreAffinity;
            ChkMuteGpu.IsChecked = settings.MuteGpu;
            ChkTextureOptimization.IsChecked = settings.TextureOptimization;

            LogMessage("INFO", "Loaded user configurations from cache.");
        }

        private void LogMessage(string level, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            TxtLog.AppendText($"[{timestamp}] [{level}] {message}\n");
            TxtLog.ScrollToEnd();
        }

        private void BtnBrowseExe_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Firestorm Executable (*.exe)|*.exe|All Files (*.*)|*.*",
                FileName = "Firestorm-x64.exe"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TxtExePath.Text = openFileDialog.FileName;
                LogMessage("INFO", $"Target executable set: {Path.GetFileName(openFileDialog.FileName)}");
            }
        }

        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            SetProcessingState(true, "Purging cache and crash dumps...");

            ZombieProcessManager.KillZombieProcesses();

            bool clearBrowser = ChkClearBrowser.IsChecked ?? true;
            bool clearTextures = ChkClearTextures.IsChecked ?? true;

            var cleanResult = CacheManager.ClearTargetedCache(clearBrowser, clearTextures);
            var dumpResult = CrashDumpCleaner.PurgeCrashDumps();

            double freedMb = Math.Round((cleanResult.BytesFreed + dumpResult.BytesFreed) / (1024.0 * 1024.0), 2);

            SetReadyState(false, "Cache Cleared");

            System.Windows.MessageBox.Show(
                $"Cleanup Summary:\n" +
                $"• Cache Files Removed: {cleanResult.FilesDeleted}\n" +
                $"• Crash Dumps Removed: {dumpResult.FilesRemoved}\n" +
                $"• Total Space Freed: {freedMb} MB",
                "Maintenance Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            LogMessage("SUCCESS", $"Maintenance executed. Removed {cleanResult.FilesDeleted} cache files & {dumpResult.FilesRemoved} crash dumps. Freed {freedMb} MB.");
            LogMetrics($"Cache cleared. Freed {freedMb} MB.");

            ToastNotificationManager.ShowSuccess(
                "Cache Cleared",
                 "Viewer and texture caches have been successfully purged."
            );
        }

        private void BtnDismissAlert_Click(object sender, RoutedEventArgs e)
        {
            BorderLogAlert.Visibility = Visibility.Collapsed;
            TxtLogAlertMessage.Text = string.Empty;
            LogMessage("INFO", "User dismissed active viewer warning banner.");
        }

        private void BtnExportLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VeloxOptiSL_Logs.txt");
                File.WriteAllText(exportPath, TxtLog.Text);
                System.Windows.MessageBox.Show($"Logs successfully exported to Desktop:\n{exportPath}", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LogMessage("SUCCESS", "Audit logs exported to desktop.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to export logs: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogMessage("ERROR", $"Failed to export logs: {ex.Message}");
            }
        }

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            SetProcessingState(true, "Launching Second Life...");

            var zombieResult = ZombieProcessManager.KillZombieProcesses();
            if (zombieResult.KilledCount > 0)
            {
                LogMessage("INFO", zombieResult.Log);
            }

            SettingsManager.InjectFirestormSettings(
                SldDrawDistance.Value,
                SldRenderAvatars.Value,
                SldArc.Value,
                SldLod.Value,
                ChkImpostors.IsChecked ?? true,
                ChkMuteGpu.IsChecked ?? true,
                ChkTextureOptimization.IsChecked ?? true,
                out string xmlLog);

            LogMessage("INFO", xmlLog);

            bool highPriority = ChkHighPriority.IsChecked ?? true;
            bool coreAffinity = ChkCoreAffinity.IsChecked ?? true;

            var result = ProcessManager.LaunchViewer(
                TxtExePath.Text,
                highPriority,
                coreAffinity,
                TrackedProcess_Exited);

            if (result.Success && result.Process != null)
            {
                _trackedProcess = result.Process;
                _telemetryService.AttachToProcess(result.Process);

                SetReadyState(true, "Running");
                TxtLogStatus.Text = $"[🟢] Active — PID: {_trackedProcess.Id}";
                LogMessage("SUCCESS", result.Message);
                LogMetrics($"Viewer launched successfully (PID: {_trackedProcess.Id})");

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string activeLogPath = Path.Combine(appData, "Firestorm_x64", "logs", "debug_info.log");

                _logWatcher.StartMonitoring(activeLogPath);
                LogMessage("INFO", "LogWatcher stream connected to debug_info.log.");

                ToastNotificationManager.ShowSuccess(
                    "VeloxOpti SL",
                    "Optimizations applied. Launching Second Life..."
                );
            }
            else
            {
                SetReadyState(false, "Launch Failed");
                System.Windows.MessageBox.Show(result.Message, "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtLogStatus.Text = "[🔴] Launch failed.";
                LogMessage("ERROR", $"Launch error: {result.Message}");
                LogMetrics($"Launch failed: {result.Message}");

                ToastNotificationManager.ShowWarning(
                    "Launch Failed",
                    result.Message
                );
            }
        }

        private void TrackedProcess_Exited(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _telemetryService.Detach();
                _logWatcher.StopMonitoring();

                if (_trackedProcess != null)
                {
                    int exitCode = _trackedProcess.ExitCode;
                    TxtLogStatus.Text = exitCode != 0
                        ? $"[🔴] Viewer exited unexpectedly (Code {exitCode})"
                        : "[🟢] Log Reader Idle — Viewer closed normally";

                    LogMessage(exitCode != 0 ? "ERROR" : "INFO", $"Viewer process exited with code {exitCode}.");
                    LogMetrics($"Process exited with code {exitCode}.");
                }
                _trackedProcess = null;
            });
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _uiTimer.Stop();
            _logWatcher.StopMonitoring();
            _telemetryService.Dispose();

            SaveSettings();
        }

        private void BtnPresetLow_Click(object sender, RoutedEventArgs e)
        {
            SldRenderAvatars.Value = 2; SldDrawDistance.Value = 32; SldArc.Value = 25000; SldLod.Value = 0.8;
            ChkJellyDoll.IsChecked = true; ChkImpostors.IsChecked = true;
            LogMessage("INFO", "Applied Low-End Preset.");
        }

        private void BtnPresetBalanced_Click(object sender, RoutedEventArgs e)
        {
            SldRenderAvatars.Value = 6; SldDrawDistance.Value = 64; SldArc.Value = 75000; SldLod.Value = 1.0;
            ChkJellyDoll.IsChecked = true; ChkImpostors.IsChecked = true;
            LogMessage("INFO", "Applied Balanced Preset.");
        }

        private void BtnPresetHigh_Click(object sender, RoutedEventArgs e)
        {
            SldRenderAvatars.Value = 15; SldDrawDistance.Value = 128; SldArc.Value = 150000; SldLod.Value = 1.5;
            ChkJellyDoll.IsChecked = false; ChkImpostors.IsChecked = false;
            LogMessage("INFO", "Applied High-End Preset.");
        }
    }
}