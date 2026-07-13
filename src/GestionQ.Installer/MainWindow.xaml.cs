using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GestionQ.Installer
{
    public partial class MainWindow : Window
    {
        private int currentPage = 1;
        private InstallerLogic _logic;
        private bool isUpdating = false;

        public MainWindow()
        {
            InitializeComponent();
            _logic = new InstallerLogic();
            
            _logic.OnProgressChanged += (status, percent) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtProgressStatus.Text = status;
                    ProgressBar.Value = percent;
                    LogMessage(status);
                });
            };

            _logic.OnError += (errorMessage) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogMessage("ERROR: " + errorMessage);
                    ShowError(errorMessage);
                });
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Auto-detect existing installation
            string defaultPath = @"C:\GestionQ";
            if (File.Exists(Path.Combine(defaultPath, "app", "GestionQ.Web.exe")))
            {
                isUpdating = true;
                TxtHeader.Text = "Actualización de GestionQ";
                TxtInstallPath.Text = defaultPath;
                TxtInstallPath.IsEnabled = false; // Lock path for update
                ((TextBlock)((StackPanel)PageSettings).Children[0]).Text = "Configuración de Actualización";
                ((TextBlock)((StackPanel)PageSettings).Children[3]).Text = "Al actualizar, se realizará lo siguiente:";
                ((TextBlock)((StackPanel)PageSettings).Children[4]).Text = " - Se detendrá el servicio web actual";
                ((TextBlock)((StackPanel)PageSettings).Children[5]).Text = " - Se copiarán los nuevos archivos (manteniendo configuraciones)";
                ((TextBlock)((StackPanel)PageSettings).Children[6]).Text = " - Se reiniciará el servicio";
                ((TextBlock)((StackPanel)PageSettings).Children[7]).Visibility = Visibility.Collapsed;
            }

            // Check command line arguments as fallback
            string[] args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.Equals("--update", StringComparison.OrdinalIgnoreCase))
                {
                    isUpdating = true;
                }
            }

            UpdateNavigation();
        }

        private void LogMessage(string msg)
        {
            TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            TxtLog.ScrollToEnd();
        }

        private void UpdateNavigation()
        {
            PageWelcome.Visibility = Visibility.Collapsed;
            PageSettings.Visibility = Visibility.Collapsed;
            PageProgress.Visibility = Visibility.Collapsed;
            PageFinished.Visibility = Visibility.Collapsed;
            PageError.Visibility = Visibility.Collapsed;

            BtnBack.IsEnabled = true;
            BtnNext.IsEnabled = true;
            BtnNext.Content = "Siguiente >";

            if (currentPage == 1)
            {
                PageWelcome.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = false;
            }
            else if (currentPage == 2)
            {
                PageSettings.Visibility = Visibility.Visible;
                BtnNext.Content = "Instalar";
            }
            else if (currentPage == 3)
            {
                PageProgress.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = false;
                BtnNext.IsEnabled = false;
            }
            else if (currentPage == 4)
            {
                PageFinished.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = false;
                BtnNext.Content = "Finalizar";
                BtnCancel.IsEnabled = false;
            }
            else if (currentPage == 5)
            {
                PageError.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = false;
                BtnNext.Visibility = Visibility.Collapsed;
                BtnCancel.Content = "Cerrar";
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                UpdateNavigation();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage == 1)
            {
                currentPage++;
                UpdateNavigation();
            }
            else if (currentPage == 2)
            {
                // Start Installation
                currentPage++;
                UpdateNavigation();
                StartInstallation();
            }
            else if (currentPage == 4)
            {
                // Finish
                Application.Current.Shutdown();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage == 3)
            {
                MessageBox.Show("La instalación está en progreso y no se puede cancelar de forma segura.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Application.Current.Shutdown();
        }

        private async void StartInstallation()
        {
            string installPath = TxtInstallPath.Text;
            if (string.IsNullOrWhiteSpace(installPath))
            {
                installPath = @"C:\GestionQ";
            }

            try
            {
                await Task.Run(() =>
                {
                    if (isUpdating)
                    {
                        _logic.RunUpdate(installPath);
                    }
                    else
                    {
                        _logic.RunInstall(installPath);
                    }
                });

                // Completed successfully
                Dispatcher.Invoke(() =>
                {
                    currentPage = 4;
                    UpdateNavigation();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    LogMessage("Excepción no controlada: " + ex.Message);
                    ShowError(ex.Message);
                });
            }
        }

        private void ShowError(string errorMessage)
        {
            currentPage = 5;
            TxtErrorMessage.Text = errorMessage;
            UpdateNavigation();
        }
    }
}