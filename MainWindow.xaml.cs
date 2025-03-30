using System;
using System.Collections.Generic;
using System.Windows;
using System.Threading.Tasks;
using CocoroAIGUI.Controls;
using CocoroAIGUI.Services;
using CocoroAIGUI.Communication;

namespace CocoroAIGUI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private CommunicationService? _communicationService;
        private string _currentUserId = "user01";
        private string _currentWebSocketUrl = "ws://127.0.0.1:8080/";

        public MainWindow()
        {
            InitializeComponent();

            // 初期化
            InitializeApp();

            // イベントハンドラ登録
            RegisterEventHandlers();
        }

        /// <summary>
        /// アプリケーション初期化
        /// </summary>
        private void InitializeApp()
        {
            // 通信サービスを初期化（まだ接続はしない）
            _communicationService = new CommunicationService(_currentWebSocketUrl, _currentUserId);

            // 通信サービスのイベントハンドラを設定
            _communicationService.ChatMessageReceived += OnChatMessageReceived;
            _communicationService.ConfigResponseReceived += OnConfigResponseReceived;
            _communicationService.StatusUpdateReceived += OnStatusUpdateReceived;
            _communicationService.ErrorOccurred += OnErrorOccurred;
            _communicationService.Connected += OnConnected;
            _communicationService.Disconnected += OnDisconnected;

            // 接続
            ConnectToService();
        }

        /// <summary>
        /// サービスに接続
        /// </summary>
        private async void ConnectToService()
        {
            try
            {
                StatusTextBlock.Text = "  (接続中...)";
                ConnectionStatusText.Text = "接続状態: 接続中...";

                if (_communicationService != null)
                {
                    await _communicationService.ConnectAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"接続エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "  (切断中)";
                ConnectionStatusText.Text = "接続状態: 切断中";
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
            // UIスレッドで実行
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatControlInstance.AddAiMessage(message);
            });
        }

        /// <summary>
        /// 設定レスポンス受信時のハンドラ
        /// </summary>
        private void OnConfigResponseReceived(object? sender, ConfigResponsePayload response)
        {
            // UIスレッドで実行
            Application.Current.Dispatcher.Invoke(() =>
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
            // 必要なステータス処理はここに記述
        }

        /// <summary>
        /// エラー発生時のハンドラ
        /// </summary>
        private void OnErrorOccurred(object? sender, string error)
        {
            // UIスレッドで実行
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"エラー: {error}", "通信エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        /// <summary>
        /// 接続成功時のハンドラ
        /// </summary>
        private void OnConnected(object? sender, EventArgs e)
        {
            // UIスレッドで実行
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = "  (接続中)";
                ConnectionStatusText.Text = "接続状態: 接続中";
            });
        }

        /// <summary>
        /// 切断時のハンドラ
        /// </summary>
        private void OnDisconnected(object? sender, EventArgs e)
        {
            // UIスレッドで実行
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = "  (切断中)";
                ConnectionStatusText.Text = "接続状態: 切断中";
            });
        }

        #endregion

        /// <summary>
        /// アプリケーション終了時の処理
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // 接続中ならタイムアウト付きで切断処理
            if (_communicationService != null)
            {
                try
                {
                    // 切断処理を行うが待機せずに進む
                    _communicationService.Dispose();
                }
                catch (Exception)
                {
                    // 切断中のエラーは無視
                }
            }

            base.OnClosed(e);
            
            // アプリケーションを完全に終了（強制終了）
            Environment.Exit(0);
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