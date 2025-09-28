using System;
using System.IO;
using System.Windows;
using BCCertMaker;
using Fiddler;
using Microsoft.Win32;

namespace BanBypass
{
    internal class FiddlerCore
    {
        private MainWindow Window;
        private ushort _randomPort;
        private static readonly string _certPassword = "ISO-GithubVersion";
        private static readonly string _certificateFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Isolumia",
            "Ban Bypass");
        private static readonly string _certificatePath = Path.Combine(_certificateFolder, "FiddlerRoot.cer");
        private static BCCertMaker.BCCertMaker? _certMaker;

        public FiddlerCore(MainWindow window)
        {
            this.Window = window;
        }

        public void StartFiddler()
        {
            try
            {
                if (FiddlerApplication.IsStarted())
                {
                    MessageBox.Show("Fiddler is already running", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _randomPort = (ushort)new Random().Next(1000, 9999);

                var startupSettings = new FiddlerCoreStartupSettingsBuilder()
                    .ListenOnPort(_randomPort)
                    .RegisterAsSystemProxy()
                    .ChainToUpstreamGateway()
                    .DecryptSSL()
                    .OptimizeThreadPool()
                    .Build();

                FiddlerApplication.Startup(startupSettings);
                FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
                
                Window.UpdateStatus("Proxy started successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred in StartFiddler: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StopFiddler()
        {
            try
            {
                using (RegistryKey? currentUserRegistry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true))
                using (RegistryKey? localMachineRegistry = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true))
                {
                    if (currentUserRegistry != null)
                    {
                        currentUserRegistry.SetValue("ProxyEnable", 0);
                        currentUserRegistry.DeleteValue("ProxyServer", false);
                    }

                    if (localMachineRegistry != null)
                    {
                        localMachineRegistry.SetValue("ProxyEnable", 0);
                        localMachineRegistry.DeleteValue("ProxyServer", false);
                    }
                }

                FiddlerApplication.Shutdown();
                Window.UpdateStatus("Proxy stopped successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to shutdown proxy: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void InstallCertificate()
        {
            try
            {
                Directory.CreateDirectory(_certificateFolder);

                _certMaker = new BCCertMaker.BCCertMaker();
                CertMaker.oCertProvider = _certMaker;

                if (!File.Exists(_certificatePath))
                {
                    if (!_certMaker.CreateRootCertificate())
                    {
                        MessageBox.Show("Failed to create Fiddler root certificate.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        StopFiddler();
                        return;
                    }

                    _certMaker.WriteRootCertificateAndPrivateKeyToPkcs12File(_certificatePath, _certPassword);
                }
                else
                {
                    _certMaker.ReadRootCertificateAndPrivateKeyFromPkcs12File(_certificatePath, _certPassword);
                }

                if (!CertMaker.rootCertIsTrusted())
                {
                    if (!CertMaker.trustRootCert())
                    {
                        MessageBox.Show("Failed to trust Fiddler root certificate.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        StopFiddler();
                        return;
                    }
                }

                Window.UpdateStatus("Security components initialized");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Certificate installation failed: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StopFiddler();
            }
        }

        private void FiddlerApplication_BeforeRequest(Session oSession)
        {
            if (oSession.fullUrl.Contains("api/v1/players/ban/status"))
            {
                oSession.utilCreateResponseAndBypassServer();

                oSession.responseCode = 204;
                oSession.oResponse.headers.HTTPResponseStatus = "204 No Content";

                oSession.oResponse.headers.Remove("Content-Type");
                oSession.oResponse.headers.Remove("Content-Length");
                oSession.utilSetResponseBody(string.Empty);

                oSession.oResponse.headers["Connection"] = "keep-alive";
                oSession.oResponse.headers["Cache-Control"] = "no-cache";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Window.UpdateStatus("Request intercepted successfully");
                });
            }
        }
    }
}