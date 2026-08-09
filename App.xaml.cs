using System;
using System.IO;
using System.Windows;

namespace crosshair_y
{
    public partial class App : Application
    {
        private MainWindow? _overlay;
        private SettingsWindow? _settings;
        private System.Windows.Forms.NotifyIcon? _trayIcon;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.DispatcherUnhandledException += (s, ex) =>
            {
                LogError(ex.Exception);
                ex.Handled = true;
            };

            try
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                _overlay = new MainWindow();
                _overlay.Show();

                _settings = new SettingsWindow(_overlay);
                _settings.Show();

                SetupTrayIcon();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void LogError(Exception ex)
        {
            try
            {
                string directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CrosshairY");
                Directory.CreateDirectory(directory);

                string path = Path.Combine(directory, "error.txt");
                File.WriteAllText(path, ex.ToString());
                MessageBox.Show($"An error occurred. Details were saved to:\n{path}", "Crosshair Y");
            }
            catch
            {
                MessageBox.Show("An unexpected error occurred and its details could not be saved.", "Crosshair Y");
            }
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "Crosshair Y"
            };

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("Settings", null, (s, e) => { _settings?.Show(); _settings?.Activate(); });
            menu.Items.Add("Exit", null, (s, e) => ExitApp());
            _trayIcon.ContextMenuStrip = menu;

            _trayIcon.DoubleClick += (s, e) => { _settings?.Show(); _settings?.Activate(); };
        }

        private void ExitApp()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            Shutdown();
        }
    }
} 
