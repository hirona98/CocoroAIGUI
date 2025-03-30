using System;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;

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
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // メインウィンドウを作成・表示
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // アプリケーション終了時の処理
            base.OnExit(e);
        }

        /// <summary>
        /// 通常スレッドでの未処理例外ハンドラ
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowFatalError(e.ExceptionObject as Exception, "未処理の例外が発生しました");
        }

        /// <summary>
        /// UIスレッドでの未処理例外ハンドラ
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // エラーをユーザーに表示
            MessageBox.Show($"エラーが発生しました: {e.Exception.Message}",
                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);

            // 例外を処理済みとしてマーク（アプリケーションを継続）
            e.Handled = true;
        }

        /// <summary>
        /// 未監視のタスク例外ハンドラ
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved(); // 例外を監視済みとしてマーク
        }

        /// <summary>
        /// 致命的エラーを表示し、アプリケーションを終了
        /// </summary>
        private void ShowFatalError(Exception? ex, string message)
        {
            try
            {
                string errorMessage = ex != null ? ex.Message : "不明なエラー";
                MessageBox.Show($"致命的なエラー: {errorMessage}\n\nアプリケーションを終了します。",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Environment.Exit(1);
            }
        }
    }
}