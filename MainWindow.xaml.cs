﻿using ICSharpCode.SharpZipLib.Core;
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
                // todo : implement
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
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Title = "駅伝用のセーブデータを選択してください。";
            dialog.Filter = SaveDataFileName + ", *.zip|" + SaveDataFileName + ";*.zip";

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            var source = dialog.FileName;
            var destDirectory = textBoxSaveDataFolder.Text;
            var destFile = Path.Combine(destDirectory, SaveDataFileName);

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

            ShowMessageBoxComplete("駅伝用セーブデータのインポートが完了しました。");
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

        private static bool ExtractZipFile(string source, string destFile)
        {
            ZipFile zipFile = null;
            try
            {
                zipFile = new ZipFile(source);
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