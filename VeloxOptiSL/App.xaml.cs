#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace VeloxOptiSL
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;
        private const string MutexName = "VeloxOptiSL_SingleInstance_Mutex_Key";

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                System.Windows.MessageBox.Show(
                    "An instance of VeloxOptiSL is already running.",
                    "VeloxOptiSL",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                Shutdown();
                return;
            }

            // Register global exception hooks
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Trigger update check asynchronously
            CheckForUpdates();

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception, "UI Thread Unhandled Exception");
            System.Windows.MessageBox.Show(
                "An unexpected error occurred. Details have been logged to crash_report.log.",
                "VeloxOptiSL Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);

            e.Handled = true;
            Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash(ex, "Background Thread Unhandled Exception");
            }
        }

        private async void LogCrash(Exception ex, string source)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_report.log");
                string logContent = $"[{DateTime.Now}] ({source}) {ex.GetType()}: {ex.Message}\n{ex.StackTrace}\n\n";
                await File.AppendAllTextAsync(logPath, logContent);
            }
            catch
            {
                // Fail gracefully if write access is restricted
            }
        }

        private async void CheckForUpdates()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "VeloxOptiSL-App");

                string url = "https://api.github.com/repos/KingVJoker/VeloxOptiSL/releases/latest";
                string json = await client.GetStringAsync(url);

                if (json.Contains("\"tag_name\":"))
                {
                    int tagIndex = json.IndexOf("\"tag_name\":") + 12;
                    int endIndex = json.IndexOf("\"", tagIndex);
                    string latestVersion = json.Substring(tagIndex, endIndex - tagIndex);

                    string currentVersion = "v1.0.0";

                    if (latestVersion != currentVersion)
                    {
                        System.Windows.MessageBox.Show(
                            $"A new version ({latestVersion}) of VeloxOptiSL is available on GitHub!",
                            "Update Available",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch
            {
                // Fail silently if offline or API limits hit
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (ObjectDisposedException) { }
            }

            base.OnExit(e);
        }
    }
}