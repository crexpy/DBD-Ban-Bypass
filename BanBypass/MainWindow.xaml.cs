using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace BanBypass
{
    public partial class MainWindow : Window
    {
        private FiddlerCore _fiddlerCore;
        private bool _isRunning = false;
        private NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            _fiddlerCore = new FiddlerCore(this);
            InitializeSystemTray();
        }
        
        private void InitializeSystemTray()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = false,
                Text = "Isolumia Bypass"
            };
            
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
            
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => System.Windows.Application.Current.Shutdown());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                StopBypass();
            }
            else
            {
                StartBypass();
            }
        }
        
        private void StartBypass()
        {
            try
            {
                UpdateStatus("Starting service...");
                
                _fiddlerCore.InstallCertificate();
                _fiddlerCore.StartFiddler();
                
                _isRunning = true;
                ToggleButton.Content = "Stop Service";
                
                UpdateStatus("Service is now active");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to start: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Failed to start service");
            }
        }

        private void StopBypass()
        {
            try
            {
                UpdateStatus("Stopping service...");
                
                _fiddlerCore.StopFiddler();
                
                _isRunning = false;
                ToggleButton.Content = "Start Service";
                
                UpdateStatus("Proxy stopped");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to stop: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Failed to stop service");
            }
        }
        
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }
        
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        
        private void TrayButton_Click(object sender, RoutedEventArgs e)
        {
            HideToSystemTray();
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private void HideToSystemTray()
        {
            this.Hide();
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(2000, "Isolumia Bypass", "Application minimized to system tray", ToolTipIcon.Info);
        }
        
        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            _notifyIcon.Visible = false;
        }
        
        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string infoMessage = "Ban Bypass Requirements & Limitations\n\n" +
                                   "IMPORTANT RESTRICTIONS:\n\n" +
                                   "• You CANNOT play as Killer\n\n" +
                                   "• You CANNOT play Survivor alone\n\n" +
                                   "• You can ONLY play Survivor when:\n" +
                                   "  - Someone else is the lobby host\n" +
                                   "  - The host is NOT using this bypass\n\n" +
                                   "These limitations are necessary for the bypass to work correctly. Make sure to follow them to avoid issues.";
                
                System.Windows.MessageBox.Show(
                    infoMessage,
                    "Bypass Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error displaying information: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateStatus(string status)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateStatus(status));
                return;
            }

            StatusText.Text = $"{DateTime.Now:HH:mm:ss} - {status}";
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_isRunning)
                {
                    _fiddlerCore.StopFiddler();
                }
                
                _notifyIcon?.Dispose();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error during shutdown: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            base.OnClosing(e);
        }
    }
}