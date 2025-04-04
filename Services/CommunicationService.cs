using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CocoroAIGUI.Communication;

namespace CocoroAIGUI.Services
{
    /// <summary>
    /// CocoroAIとの通信を管理するサービスクラス
    /// </summary>
    public class CommunicationService : IDisposable
    {
        private readonly WebSocketClient _webSocketClient;
        private readonly string _userId;
        private string _sessionId;

        public event EventHandler<string>? ChatMessageReceived;
        public event EventHandler<ConfigResponsePayload>? ConfigResponseReceived;
        public event EventHandler<StatusMessagePayload>? StatusUpdateReceived;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler? Connected;
        public event EventHandler? Disconnected;
        public event EventHandler<ConfigSettings>? ConfigReceived;

        public bool IsConnected => _webSocketClient.IsConnected;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="serverUrl">WebSocketサーバーURL</param>
        /// <param name="userId">ユーザーID</param>
        public CommunicationService(string serverUrl, string userId)
        {
            _webSocketClient = new WebSocketClient(serverUrl);
            _userId = userId;
            _sessionId = GenerateSessionId();

            // WebSocketのイベントハンドラを設定
            _webSocketClient.MessageReceived += OnMessageReceived;
            _webSocketClient.ConnectionError += (sender, error) => ErrorOccurred?.Invoke(this, error);
            _webSocketClient.Connected += (sender, args) => Connected?.Invoke(this, EventArgs.Empty);
            _webSocketClient.Disconnected += (sender, args) => Disconnected?.Invoke(this, EventArgs.Empty);
            _webSocketClient.ConfigReceived += (sender, config) => ConfigReceived?.Invoke(this, config);
        }

        /// <summary>
        /// 新しいセッションIDを生成
        /// </summary>
        private string GenerateSessionId()
        {
            return $"session_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        /// <summary>
        /// 接続を開始
        /// </summary>
        public async Task ConnectAsync()
        {
            await _webSocketClient.ConnectAsync();
        }

        /// <summary>
        /// 接続を終了
        /// </summary>
        public async Task DisconnectAsync()
        {
            await _webSocketClient.DisconnectAsync();
        }

        /// <summary>
        /// 設定情報を要求
        /// </summary>
        public async Task RequestConfigAsync()
        {
            await _webSocketClient.RequestConfigAsync();
        }

        /// <summary>
        /// 設定情報を更新
        /// </summary>
        /// <param name="settings">更新する設定情報</param>
        public async Task UpdateConfigAsync(ConfigSettings settings)
        {
            await _webSocketClient.UpdateConfigAsync(settings);
        }

        /// <summary>
        /// チャットメッセージを送信
        /// </summary>
        /// <param name="message">送信メッセージ</param>
        public async Task SendChatMessageAsync(string message)
        {
            var payload = new ChatMessagePayload
            {
                UserId = _userId,
                SessionId = _sessionId,
                Message = message
            };

            await _webSocketClient.SendMessageAsync(MessageType.Chat, payload);
        }

        /// <summary>
        /// 設定を変更
        /// </summary>
        /// <param name="settingKey">設定キー</param>
        /// <param name="value">設定値</param>
        public async Task ChangeConfigAsync(string settingKey, string value)
        {
            var payload = new ConfigMessagePayload
            {
                SettingKey = settingKey,
                Value = value
            };

            await _webSocketClient.SendMessageAsync(MessageType.Config, payload);
        }

        /// <summary>
        /// 制御コマンドを送信
        /// </summary>
        /// <param name="command">コマンド名</param>
        /// <param name="reason">理由</param>
        public async Task SendControlCommandAsync(string command, string reason)
        {
            var payload = new ControlMessagePayload
            {
                Command = command,
                Reason = reason
            };

            await _webSocketClient.SendMessageAsync(MessageType.Control, payload);
        }

        /// <summary>
        /// 新しいチャットセッションを開始
        /// </summary>
        public void StartNewSession()
        {
            _sessionId = GenerateSessionId();
        }

        /// <summary>
        /// 受信したWebSocketメッセージを処理
        /// </summary>
        private void OnMessageReceived(object? sender, string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (message != null && message.TryGetValue("type", out var typeElement) &&
                    message.TryGetValue("payload", out var payloadElement))
                {
                    var type = typeElement.GetString()?.ToLower();

                    switch (type)
                    {
                        case "chat":
                            var chatResponse = payloadElement.GetProperty("response").GetString();
                            if (chatResponse != null)
                            {
                                ChatMessageReceived?.Invoke(this, chatResponse);
                            }
                            break;

                        case "config":
                            var configResponse = JsonSerializer.Deserialize<ConfigResponsePayload>(payloadElement.GetRawText());
                            if (configResponse != null)
                            {
                                ConfigResponseReceived?.Invoke(this, configResponse);
                            }
                            break;

                        case "status":
                            var statusUpdate = JsonSerializer.Deserialize<StatusMessagePayload>(payloadElement.GetRawText());
                            if (statusUpdate != null)
                            {
                                StatusUpdateReceived?.Invoke(this, statusUpdate);
                            }
                            break;

                        default:
                            // 未知のメッセージタイプは無視
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"メッセージ解析エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            _webSocketClient.Dispose();
        }
    }
}