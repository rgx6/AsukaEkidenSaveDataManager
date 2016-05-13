using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace AsukaEkidenSaveDataManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;

            Config.Load();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Config.Current.Save();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "error.txt");

            string message = string.Format(
                "{0}\r\n{1}\r\n{2}",
                e.Exception.TargetSite.Name,
                e.Exception.Message,
                e.Exception.StackTrace);

            try
            {
                File.WriteAllText(filePath, message, Encoding.UTF8);
            }
            catch
            {
                // ignore exception
            }

            MessageBox.Show(
                "予期しないエラーが発生しました。",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
