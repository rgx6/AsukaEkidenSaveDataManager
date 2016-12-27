using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace AsukaEkidenSaveDataManager
{
    public partial class MainWindow : Window
    {
        #region field

        private static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private static readonly string ExportPath = Path.Combine(AssemblyPath, "export");
        private static readonly string SaveDataFileName = "ASUKA____002";
        private static readonly string BackupFileSuffix = ".bak";

        #endregion

        #region constructor

        public MainWindow()
        {
            InitializeComponent();

            if (string.IsNullOrWhiteSpace(Config.Current.SaveDataFolder))
            {
                Config.Current.SaveDataFolder = GetInitialSaveDataFolder();
            }

            textBoxSaveDataFolder.Text = Config.Current.SaveDataFolder;
        }

        #endregion

        #region event

        private void textBoxSaveDataFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            Config.Current.SaveDataFolder = textBoxSaveDataFolder.Text;
        }

        private void buttonSelectSaveDataFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "セーブデータ保存フォルダを選択してください";
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            dialog.SelectedPath = textBoxSaveDataFolder.Text;
            dialog.ShowNewFolderButton = false;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxSaveDataFolder.Text = dialog.SelectedPath;
            }
        }

        private void buttonBackup_Click(object sender, RoutedEventArgs e)
        {
            var source = Path.Combine(textBoxSaveDataFolder.Text, SaveDataFileName);
            var dest = source + BackupFileSuffix;

            if (!File.Exists(source))
            {
                ShowMessageBoxError("セーブデータが存在しません。");
                return;
            }

            if (File.Exists(dest))
            {
                var result = ShowMessageBoxConfirm("バックアップが既に存在します。上書きしますか？");
                if (result != MessageBoxResult.Yes) return;
            }

            File.Copy(source, dest, true);

            ShowMessageBoxComplete("セーブデータのバックアップが完了しました。");
        }

        private void buttonImport_Click(object sender, RoutedEventArgs e)
        {
            // HACK : ASUKA____002 と ASUKA____002.zip が同時に存在すると ASUKA____002 を指定したときに source が ASUKA____002.zip になってしまうので対策する。
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Title = "駅伝用のセーブデータを選択してください。";
            dialog.Filter = SaveDataFileName + ", *.zip|" + SaveDataFileName + ";*.zip";

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            var source = dialog.FileName;
            var destDirectory = textBoxSaveDataFolder.Text;
            var destFile = Path.Combine(destDirectory, SaveDataFileName);

            if (source == destFile)
            {
                ShowMessageBoxError("インポート元とインポート先が同じです。");
                return;
            }

            if (!Directory.Exists(destDirectory))
            {
                ShowMessageBoxError("セーブデータ保存フォルダが存在しません。");
                return;
            }

            if (source.ToLower().EndsWith(".zip"))
            {
                var done = ExtractZipFile(source, destFile);
                if (!done) return;
            }
            else
            {
                if (File.Exists(destFile))
                {
                    var result = ShowMessageBoxConfirm("セーブデータが既に存在します。上書きしますか？");
                    if (result != MessageBoxResult.Yes) return;
                }

                File.Copy(source, destFile, true);
            }

            DeleteRegistry();

            ShowMessageBoxComplete(
                "駅伝用セーブデータのインポートが完了しました。\nインポートしたセーブデータの更新日時: "
                + File.GetLastWriteTime(destFile).ToString("yyyy/MM/dd HH:mm:ss"));
        }

        private void buttonExport_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(ExportPath)) Directory.CreateDirectory(ExportPath);

            var sourceDirectory = textBoxSaveDataFolder.Text;
            var sourceFile = Path.Combine(sourceDirectory, SaveDataFileName);
            var dest = Path.Combine(ExportPath, SaveDataFileName + ".zip");

            if (!File.Exists(sourceFile))
            {
                ShowMessageBoxError("セーブデータが存在しません。");
                return;
            }

            var fastZip = new FastZip();
            var fileFilter = @"\\" + SaveDataFileName + "$";
            fastZip.CreateZip(dest, sourceDirectory, false, fileFilter);

            ShowMessageBoxComplete("駅伝用セーブデータのエクスポートが完了しました。");

            Process.Start("EXPLORER.EXE", @"/select,""" + dest + @"""");
        }

        private void buttonRestore_Click(object sender, RoutedEventArgs e)
        {
            var source = Path.Combine(textBoxSaveDataFolder.Text, SaveDataFileName + BackupFileSuffix);
            var dest = Path.Combine(textBoxSaveDataFolder.Text, SaveDataFileName);

            if (!File.Exists(source))
            {
                ShowMessageBoxError("セーブデータのバックアップが存在しません。");
                return;
            }

            if (File.Exists(dest))
            {
                var result = ShowMessageBoxConfirm("セーブデータが既に存在します。上書きしますか？");
                if (result != MessageBoxResult.Yes) return;
            }

            File.Copy(source, dest, true);

            DeleteRegistry();

            ShowMessageBoxComplete("セーブデータの復元が完了しました。");
        }

        #endregion

        #region method

        private static string GetInitialSaveDataFolder()
        {
            var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var virtualStore = @"VirtualStore\Program Files (x86)";
            var asukaPC = @"CHUNSOFT\AsukaPC\save";

            var path = Path.Combine(localApplicationData, virtualStore, asukaPC);

            if (Directory.Exists(path) && 0 < Directory.GetFiles(path).Length) return path;

            path = Path.Combine(programFilesX86, asukaPC);

            if (Directory.Exists(path) && 0 < Directory.GetFiles(path).Length) return path;

            path = Path.Combine(programFiles, asukaPC);

            if (Directory.Exists(path) && 0 < Directory.GetFiles(path).Length) return path;

            return string.Empty;
        }

        private static bool ExtractZipFile(string source, string destFile)
        {
            ZipFile zipFile = null;
            try
            {
                zipFile = new ZipFile(source);

                if (!zipFile.TestArchive(true))
                {
                    zipFile.Password = Microsoft.VisualBasic.Interaction.InputBox("パスワードを入力してください。", "パスワード入力");

                    if (!zipFile.TestArchive(true))
                    {
                        ShowMessageBoxError("パスワードが違うか、ファイルが破損しています。");
                        return false;
                    }
                }

                ZipEntry targetZipEntry = null;

                foreach (ZipEntry zipEntry in zipFile)
                {
                    if (!zipEntry.IsFile) continue;

                    if (Path.GetFileName(zipEntry.Name) == SaveDataFileName)
                    {
                        targetZipEntry = zipEntry;
                        break;
                    }
                }

                if (targetZipEntry == null)
                {
                    ShowMessageBoxError("駅伝用セーブデータが存在しません。");
                    return false;
                }

                if (File.Exists(destFile))
                {
                    var result = ShowMessageBoxConfirm("セーブデータが既に存在します。上書きしますか？");
                    if (result != MessageBoxResult.Yes) return false;
                }

                var buffer = new byte[4096];
                var zipStream = zipFile.GetInputStream(targetZipEntry);
                using (var writer = File.Create(destFile))
                {
                    StreamUtils.Copy(zipStream, writer, buffer);
                }

                File.SetCreationTime(destFile, targetZipEntry.DateTime);
                File.SetLastWriteTime(destFile, targetZipEntry.DateTime);
                File.SetLastAccessTime(destFile, targetZipEntry.DateTime);

                return true;
            }
            finally
            {
                if (zipFile != null)
                {
                    zipFile.IsStreamOwner = true;
                    zipFile.Close();
                }
            }
        }

        private static void DeleteRegistry()
        {
            var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\ChunSoft\Asuka4Windows", true);

            registryKey.DeleteValue("SSC_Value02", false);
            registryKey.DeleteValue("SSK_Value02", false);

            registryKey.Close();
        }

        private static void ShowMessageBoxComplete(string message)
        {
            MessageBox.Show(
                message,
                "完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static MessageBoxResult ShowMessageBoxConfirm(string message)
        {
            return MessageBox.Show(
                message,
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
        }

        private static void ShowMessageBoxError(string message)
        {
            MessageBox.Show(
                message,
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion
    }
}
