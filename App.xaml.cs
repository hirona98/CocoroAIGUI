using System;
using System.Windows;
using System.Windows.Threading;

namespace CocoroAIGUI
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 未処理の例外ハンドラを登録
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Application_DispatcherUnhandledException;

            // メインウィンドウを作成・表示
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        /// <summary>
        /// 通常スレッドでの未処理例外ハンドラ
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception? ex = e.ExceptionObject as Exception;
            string errorMessage = ex != null ? ex.Message : "不明なエラーが発生しました。";

            MessageBox.Show($"致命的なエラー: {errorMessage}\n\nアプリケーションを終了します。",
                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);

            // 可能であればログに記録
            // ここにログ記録のコードを追加

            // アプリケーションを終了
            Environment.Exit(1);
        }

        /// <summary>
        /// UIスレッドでの未処理例外ハンドラ
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 例外の処理
            string errorMessage = e.Exception != null ? e.Exception.Message : "不明なエラーが発生しました。";

            MessageBox.Show($"エラー: {errorMessage}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);

            // 可能であればログに記録
            // ここにログ記録のコードを追加

            // 例外を処理済みとしてマーク（アプリケーションを継続）
            e.Handled = true;
        }
    }
}