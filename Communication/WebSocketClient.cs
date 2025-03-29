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

        public bool IsConnected => _isConnected;

        /// <summary>
        /// WebSocketクライアントのコンストラクタ
        /// </summary>
        /// <param name="serverUrl">接続先WebSocketサーバーURL (例: ws://localhost:8080/)</param>
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

                    MessageReceived?.Invoke(this, result.ToString());
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
                var json = JsonSerializer.Serialize(message);
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
    /// 設定メッセージペイロードクラス
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