using System;
using System.Threading.Tasks;
using System.Windows;
using CocoroAIGUI.Controls;
using CocoroAIGUI.Communication;
using CocoroAIGUI.Services;

namespace CocoroAIGUI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private CommunicationService? _communicationService;

        public MainWindow()
        {
            InitializeComponent();

            // 初期化と接続
            InitializeApp();

            // イベントハンドラ登録
            RegisterEventHandlers();
        }

        /// <summary>
        /// アプリケーション初期化
        /// </summary>
        private void InitializeApp()
        {
            try
            {
                // AppSettingsから設定を取得
                var settings = AppSettings.Instance;

                // 通信サービスを初期化
                _communicationService = new CommunicationService(settings.WebSocketUrl, settings.UserId);

                // 通信サービスのイベントハンドラを設定
                _communicationService.ChatMessageReceived += OnChatMessageReceived;
                _communicationService.ConfigResponseReceived += OnConfigResponseReceived;
                _communicationService.StatusUpdateReceived += OnStatusUpdateReceived;
                _communicationService.ErrorOccurred += OnErrorOccurred;
                _communicationService.Connected += OnConnected;
                _communicationService.Disconnected += OnDisconnected;

                // 接続
                _ = ConnectToServiceAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初期化エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// サービスに接続（非同期タスク）
        /// </summary>
        private async Task ConnectToServiceAsync()
        {
            try
            {
                // UI更新
                UpdateConnectionStatus(false, "接続中...");

                if (_communicationService != null)
                {
                    await _communicationService.ConnectAsync();
                }
            }
            catch (Exception ex)
            {
                // UI更新とエラー表示
                ShowError("接続エラー", ex.Message);
                UpdateConnectionStatus(false);
            }
        }

        /// <summary>
        /// UIコントロールのイベントハンドラを登録
        /// </summary>
        private void RegisterEventHandlers()
        {
            // チャットコントロールのイベント登録
            ChatControlInstance.MessageSent += OnChatMessageSent;
        }

        /// <summary>
        /// 接続ステータス表示を更新
        /// </summary>
        private void UpdateConnectionStatus(bool isConnected, string? customMessage = null)
        {
            // UIスレッドで実行
            RunOnUIThread(() =>
            {
                if (isConnected)
                {
                    StatusTextBlock.Text = "  (接続中)";
                    ConnectionStatusText.Text = "接続状態: 接続中";
                }
                else
                {
                    string statusText = customMessage ?? "切断中";
                    StatusTextBlock.Text = $"  ({statusText})";
                    ConnectionStatusText.Text = $"接続状態: {statusText}";
                }
            });
        }

        /// <summary>
        /// エラーをメッセージボックスで表示
        /// </summary>
        private void ShowError(string title, string message)
        {
            RunOnUIThread(() =>
            {
                MessageBox.Show($"{title}: {message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        /// <summary>
        /// UIスレッドでアクションを実行
        /// </summary>
        private void RunOnUIThread(Action action)
        {
            if (Application.Current?.Dispatcher != null)
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(action);
                }
            }
        }

        #region チャットコントロールイベントハンドラ

        /// <summary>
        /// チャットメッセージ送信時のハンドラ
        /// </summary>
        private async void OnChatMessageSent(object? sender, string message)
        {
            try
            {
                // WebSocketが接続されている場合のみ送信
                if (_communicationService != null && _communicationService.IsConnected)
                {
                    await _communicationService.SendChatMessageAsync(message);
                }
                else
                {
                    ChatControlInstance.AddAiMessage("WebSocket接続が確立されていません。");
                }
            }
            catch (Exception ex)
            {
                ChatControlInstance.AddAiMessage($"エラー: {ex.Message}");
            }
        }

        #endregion

        #region 通信サービスイベントハンドラ

        /// <summary>
        /// チャットメッセージ受信時のハンドラ
        /// </summary>
        private void OnChatMessageReceived(object? sender, string message)
        {
            RunOnUIThread(() => ChatControlInstance.AddAiMessage(message));
        }

        /// <summary>
        /// 設定レスポンス受信時のハンドラ
        /// </summary>
        private void OnConfigResponseReceived(object? sender, ConfigResponsePayload response)
        {
            RunOnUIThread(() => 
            {
                if (response.Status.ToLower() != "ok")
                {
                    MessageBox.Show($"設定変更エラー: {response.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
        }

        /// <summary>
        /// 状態更新受信時のハンドラ
        /// </summary>
        private void OnStatusUpdateReceived(object? sender, StatusMessagePayload status)
        {
            // 必要なステータス処理を実装する場合はここに追加
        }

        /// <summary>
        /// エラー発生時のハンドラ
        /// </summary>
        private void OnErrorOccurred(object? sender, string error)
        {
            ShowError("通信エラー", error);
        }

        /// <summary>
        /// 接続成功時のハンドラ
        /// </summary>
        private void OnConnected(object? sender, EventArgs e)
        {
            UpdateConnectionStatus(true);
        }

        /// <summary>
        /// 切断時のハンドラ
        /// </summary>
        private void OnDisconnected(object? sender, EventArgs e)
        {
            UpdateConnectionStatus(false);
        }

        #endregion

        /// <summary>
        /// アプリケーション終了時の処理
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // 接続中ならリソース解放
                if (_communicationService != null)
                {
                    _communicationService.Dispose();
                    _communicationService = null;
                }
            }
            catch (Exception)
            {
                // 切断中のエラーは無視
            }

            base.OnClosed(e);
            
            // Application.Current.ShutdownだけでOK
            // OnExitが自動的に実行される
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 管理ボタンクリック時のイベントハンドラ
        /// </summary>
        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            // 管理画面を表示
            var adminWindow = new AdminWindow();
            adminWindow.Owner = this; // メインウィンドウを親に設定
            adminWindow.ShowDialog(); // モーダルダイアログとして表示
        }
    }
}