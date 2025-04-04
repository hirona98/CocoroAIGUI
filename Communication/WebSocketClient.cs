using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace CocoroAIGUI.Communication
{
    /// <summary>
    /// WebSocketメッセージタイプ定義
    /// </summary>
    public enum MessageType
    {
        Chat,
        Config,
        Control,
        Status
    }

    /// <summary>
    /// WebSocketメッセージ基本構造
    /// </summary>
    public class WebSocketMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public object? Payload { get; set; }

        public WebSocketMessage(MessageType type, object payload)
        {
            Type = type.ToString().ToLower();
            Timestamp = DateTime.Now.ToString("o"); // ISO 8601
            Payload = payload;
        }
    }

    /// <summary>
    /// WebSocketクライアント実装
    /// </summary>
    public class WebSocketClient : IDisposable
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private readonly Uri _serverUri;
        private bool _isConnected;
        private Task? _receiveTask;

        public event EventHandler<string>? MessageReceived;
        public event EventHandler<string>? ConnectionError;
        public event EventHandler? Connected;
        public event EventHandler? Disconnected;
        public event EventHandler<ConfigSettings>? ConfigReceived;

        public bool IsConnected => _isConnected;

        /// <summary>
        /// WebSocketクライアントのコンストラクタ
        /// </summary>
        /// <param name="serverUrl">接続先WebSocketサーバーURL (例: ws://127.0.0.1:8080/)</param>
        public WebSocketClient(string serverUrl)
        {
            _serverUri = new Uri(serverUrl);
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            _isConnected = false;
        }

        /// <summary>
        /// WebSocketサーバーへの接続を開始
        /// </summary>
        public async Task ConnectAsync()
        {
            if (_isConnected) return;

            try
            {
                await _webSocket.ConnectAsync(_serverUri, _cts.Token);
                _isConnected = true;
                Connected?.Invoke(this, EventArgs.Empty);

                // 受信ループを開始
                _receiveTask = Task.Run(ReceiveLoop);
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke(this, $"接続エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// WebSocketサーバーから切断
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (!_isConnected) return;

            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "クライアントからの切断", _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"切断エラー: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
                _webSocket.Dispose();
                _webSocket = new ClientWebSocket();
            }
        }

        /// <summary>
        /// メッセージ受信ループ
        /// </summary>
        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];

            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    var result = new StringBuilder();
                    WebSocketReceiveResult receiveResult;

                    do
                    {
                        receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "サーバーからの切断", _cts.Token);
                            _isConnected = false;
                            Disconnected?.Invoke(this, EventArgs.Empty);
                            return;
                        }

                        var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                        result.Append(message);
                    }
                    while (!receiveResult.EndOfMessage);

                    var messageText = result.ToString();
                    MessageReceived?.Invoke(this, messageText);

                    // 設定情報のメッセージを処理
                    try
                    {
                        var messageObj = JsonSerializer.Deserialize<WebSocketMessage>(messageText, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (messageObj?.Type?.ToLower() == "config")
                        {
                            // 設定レスポンスを解析
                            var configResponse = JsonSerializer.Deserialize<ConfigResponseWithSettings>(messageText, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (configResponse?.Payload?.Settings != null)
                            {
                                ConfigReceived?.Invoke(this, configResponse.Payload.Settings);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"JSONパース失敗: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    ConnectionError?.Invoke(this, $"受信エラー: {ex.Message}");
                }
                _isConnected = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// WebSocketサーバーにメッセージを送信
        /// </summary>
        /// <param name="type">メッセージタイプ</param>
        /// <param name="payload">送信データ</param>
        public async Task SendMessageAsync(MessageType type, object payload)
        {
            if (!_isConnected) throw new InvalidOperationException("WebSocketが接続されていません");

            try
            {
                var message = new WebSocketMessage(type, payload);
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                System.Diagnostics.Debug.WriteLine($"送信するメッセージ: {json}");
                var buffer = Encoding.UTF8.GetBytes(json);

                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke(this, $"送信エラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 設定値を取得する
        /// </summary>
        /// <returns>設定取得リクエストの送信タスク</returns>
        public async Task RequestConfigAsync()
        {
            var payload = new ConfigRequestPayload
            {
                Action = "get"
            };

            await SendMessageAsync(MessageType.Config, payload);
        }

        /// <summary>
        /// 設定値を更新する
        /// </summary>
        /// <param name="settings">更新する設定値</param>
        /// <returns>設定更新リクエストの送信タスク</returns>
        public async Task UpdateConfigAsync(ConfigSettings settings)
        {
            var payload = new ConfigUpdatePayload
            {
                Action = "update",
                Settings = settings
            };

            await SendMessageAsync(MessageType.Config, payload);
        }

        /// <summary>
        /// リソース解放
        /// </summary>
        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _webSocket?.Dispose();
        }
    }

    /// <summary>
    /// チャットメッセージペイロードクラス
    /// </summary>
    public class ChatMessagePayload
    {
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// チャットレスポンスペイロードクラス
    /// </summary>
    public class ChatResponsePayload
    {
        public string Response { get; set; } = string.Empty;
    }

    /// <summary>
    /// 設定リクエストペイロードクラス
    /// </summary>
    public class ConfigRequestPayload
    {
        public string Action { get; set; } = string.Empty;
    }

    /// <summary>
    /// 設定更新ペイロードクラス
    /// </summary>
    public class ConfigUpdatePayload
    {
        public string Action { get; set; } = string.Empty;
        public ConfigSettings Settings { get; set; } = new ConfigSettings();
    }

    /// <summary>
    /// 設定メッセージペイロードクラス (旧)
    /// </summary>
    public class ConfigMessagePayload
    {
        public string SettingKey { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 設定レスポンスペイロードクラス
    /// </summary>
    public class ConfigResponsePayload
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ConfigSettings? Settings { get; set; }
    }

    /// <summary>
    /// 設定レスポンスを含むメッセージクラス
    /// </summary>
    public class ConfigResponseWithSettings
    {
        public string Type { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public ConfigResponsePayload? Payload { get; set; }
    }

    /// <summary>
    /// キャラクター設定クラス
    /// </summary>
    public class CharacterSettings
    {
        public bool IsReadOnly { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string VrmFilePath { get; set; } = string.Empty;
        public bool IsUseLLM { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string LLMModel { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public bool IsUseNijivoice { get; set; }
        public string NijivoiceApiKey { get; set; } = string.Empty;
        public string NijivoiceActorId { get; set; } = string.Empty;
    }

    /// <summary>
    /// アプリケーション設定クラス
    /// </summary>
    public class ConfigSettings
    {
        public bool IsTopmost { get; set; }
        public bool IsEscapeCursor { get; set; }
        public bool IsAutoMove { get; set; }
        public float WindowSize { get; set; }
        public int CurrentCharacterIndex { get; set; }
        public List<CharacterSettings> CharacterList { get; set; } = new List<CharacterSettings>();
    }

    /// <summary>
    /// 制御メッセージペイロードクラス
    /// </summary>
    public class ControlMessagePayload
    {
        public string Command { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 状態通知ペイロードクラス
    /// </summary>
    public class StatusMessagePayload
    {
        public int CurrentCPU { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
