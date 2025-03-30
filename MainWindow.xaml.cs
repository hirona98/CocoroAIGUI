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
        private Dictionary<string, string> _currentSettings = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();

            // 初期設定
            InitializeApp();

            // イベントハンドラ登録
            RegisterEventHandlers();
        }

        /// <summary>
        /// アプリケーション初期化
        /// </summary>
        private void InitializeApp()
        {
            // 初期設定値
            _currentSettings["characterModel"] = "デフォルト";
            _currentSettings["reactionSpeed"] = "5";
            _currentSettings["aiModel"] = "OpenAI GPT-4";
            _currentSettings["temperature"] = "0.7";
            _currentSettings["useVoice"] = "false";

            // 通信サービスを初期化（まだ接続はしない）
            _communicationService = new CommunicationService(_currentWebSocketUrl, _currentUserId);

            // 通信サービスのイベントハンドラを設定
            _communicationService.ChatMessageReceived += OnChatMessageReceived;
            _communicationService.ConfigResponseReceived += OnConfigResponseReceived;
            _communicationService.StatusUpdateReceived += OnStatusUpdateReceived;
            _communicationService.ErrorOccurred += OnErrorOccurred;
            _communicationService.Connected += OnConnected;
            _communicationService.Disconnected += OnDisconnected;
        }

        /// <summary>
        /// UIコントロールのイベントハンドラを登録
        /// </summary>
        private void RegisterEventHandlers()
        {
            // チャットコントロールのイベント登録
            ChatControlInstance.MessageSent += OnChatMessageSent;

            // 設定コントロールのイベント登録
            SettingsControlInstance.ConnectionSettingsChanged += OnConnectionSettingsChanged;
            SettingsControlInstance.CharacterModelChanged += OnCharacterModelChanged;
            SettingsControlInstance.ReactionSpeedChanged += OnReactionSpeedChanged;
            SettingsControlInstance.AiModelChanged += OnAiModelChanged;
            SettingsControlInstance.TemperatureChanged += OnTemperatureChanged;
            SettingsControlInstance.VoiceOutputChanged += OnVoiceOutputChanged;
            SettingsControlInstance.ConnectRequested += OnConnectRequested;
            SettingsControlInstance.DisconnectRequested += OnDisconnectRequested;
            SettingsControlInstance.SaveSettingsRequested += OnSaveSettingsRequested;
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
                    ChatControlInstance.AddAiMessage("WebSocket接続が確立されていません。設定タブから接続してください。");
                }
            }
            catch (Exception ex)
            {
                ChatControlInstance.AddAiMessage($"エラー: {ex.Message}");
            }
        }

        #endregion

        #region 設定コントロールイベントハンドラ

        /// <summary>
        /// 接続設定変更時のハンドラ
        /// </summary>
        private void OnConnectionSettingsChanged(object? sender, SettingsControl.ConnectionSettings settings)
        {
            _currentWebSocketUrl = settings.WebSocketUrl;
            _currentUserId = settings.UserId;

            // 新しい接続情報で通信サービスを再作成
            if (_communicationService != null)
            {
                _communicationService.Dispose();
            }

            _communicationService = new CommunicationService(_currentWebSocketUrl, _currentUserId);

            // イベントハンドラを再設定
            _communicationService.ChatMessageReceived += OnChatMessageReceived;
            _communicationService.ConfigResponseReceived += OnConfigResponseReceived;
            _communicationService.StatusUpdateReceived += OnStatusUpdateReceived;
            _communicationService.ErrorOccurred += OnErrorOccurred;
            _communicationService.Connected += OnConnected;
            _communicationService.Disconnected += OnDisconnected;
        }

        /// <summary>
        /// キャラクターモデル変更時のハンドラ
        /// </summary>
        private async void OnCharacterModelChanged(object? sender, string modelName)
        {
            _currentSettings["characterModel"] = modelName;

            // WebSocketが接続されている場合、設定をUnityアプリに送信
            if (_communicationService != null && _communicationService.IsConnected)
            {
                await _communicationService.ChangeConfigAsync("characterModel", modelName);
            }
        }

        /// <summary>
        /// 反応速度変更時のハンドラ
        /// </summary>
        private async void OnReactionSpeedChanged(object? sender, int speed)
        {
            _currentSettings["reactionSpeed"] = speed.ToString();

            // WebSocketが接続されている場合、設定をUnityアプリに送信
            if (_communicationService != null && _communicationService.IsConnected)
            {
                await _communicationService.ChangeConfigAsync("reactionSpeed", speed.ToString());
            }
        }

        /// <summary>
        /// AIモデル変更時のハンドラ
        /// </summary>
        private async void OnAiModelChanged(object? sender, string modelName)
        {
            _currentSettings["aiModel"] = modelName;

            // WebSocketが接続されている場合、設定をUnityアプリに送信
            if (_communicationService != null && _communicationService.IsConnected)
            {
                await _communicationService.ChangeConfigAsync("aiModel", modelName);
            }
        }

        /// <summary>
        /// 応答温度変更時のハンドラ
        /// </summary>
        private async void OnTemperatureChanged(object? sender, double temperature)
        {
            _currentSettings["temperature"] = temperature.ToString();

            // WebSocketが接続されている場合、設定をUnityアプリに送信
            if (_communicationService != null && _communicationService.IsConnected)
            {
                await _communicationService.ChangeConfigAsync("temperature", temperature.ToString());
            }
        }

        /// <summary>
        /// 音声出力設定変更時のハンドラ
        /// </summary>
        private async void OnVoiceOutputChanged(object? sender, bool useVoice)
        {
            _currentSettings["useVoice"] = useVoice.ToString().ToLower();

            // WebSocketが接続されている場合、設定をUnityアプリに送信
            if (_communicationService != null && _communicationService.IsConnected)
            {
                await _communicationService.ChangeConfigAsync("useVoice", useVoice.ToString().ToLower());
            }
        }

        /// <summary>
        /// 接続リクエスト時のハンドラ
        /// </summary>
        private async void OnConnectRequested(object? sender, EventArgs e)
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
                SettingsControlInstance.UpdateConnectionStatus(false);
                StatusTextBlock.Text = "  (切断中)";
                ConnectionStatusText.Text = "接続状態: 切断中";
            }
        }

        /// <summary>
        /// 切断リクエスト時のハンドラ
        /// </summary>
        private async void OnDisconnectRequested(object? sender, EventArgs e)
        {
            try
            {
                if (_communicationService != null)
                {
                    await _communicationService.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切断エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 設定保存リクエスト時のハンドラ
        /// </summary>
        private void OnSaveSettingsRequested(object? sender, EventArgs e)
        {
            // 接続中の場合は全ての設定を一括で送信
            if (_communicationService != null && _communicationService.IsConnected)
            {
                SendAllSettings();
            }

            MessageBox.Show("設定を保存しました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
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
            // UIスレッドで実行
            Application.Current.Dispatcher.Invoke(() =>
            {
                SettingsControlInstance.UpdateCpuUsage(status.CurrentCPU);
            });
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
                SettingsControlInstance.UpdateConnectionStatus(true);
                StatusTextBlock.Text = "  (接続中)";
                ConnectionStatusText.Text = "接続状態: 接続中";

                // 接続後に全ての設定をUnityアプリに送信
                SendAllSettings();
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
                SettingsControlInstance.UpdateConnectionStatus(false);
                StatusTextBlock.Text = "  (切断中)";
                ConnectionStatusText.Text = "接続状態: 切断中";
            });
        }

        #endregion

        /// <summary>
        /// 全ての設定をUnityアプリに送信
        /// </summary>
        private async void SendAllSettings()
        {
            if (_communicationService == null || !_communicationService.IsConnected)
                return;

            try
            {
                foreach (var setting in _currentSettings)
                {
                    await _communicationService.ChangeConfigAsync(setting.Key, setting.Value);
                    // 短い遅延を入れて連続送信による問題を回避
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定送信エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// アプリケーション終了時の処理
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // 接続中なら切断
            if (_communicationService != null && _communicationService.IsConnected)
            {
                _communicationService.DisconnectAsync().Wait();
                _communicationService.Dispose();
            }

            base.OnClosed(e);
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