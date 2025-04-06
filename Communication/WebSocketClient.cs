using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace CocoroAIGUI.Communication
{
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
                // 既存のWebSocketが使用されていた場合は破棄して新しいインスタンスを作成
                if (_webSocket.State != WebSocketState.None)
                {
                    _webSocket.Dispose();
                    _webSocket = new ClientWebSocket();
                }

                await _webSocket.ConnectAsync(_serverUri, _cts.Token);
                _isConnected = true;
                Connected?.Invoke(this, EventArgs.Empty);

                // 受信ループを開始
                _receiveTask = Task.Run(ReceiveLoop);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"接続エラー: {ex.Message}");
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
                    // 受信したメッセージをイベント購読者に通知するだけ
                    MessageReceived?.Invoke(this, messageText);
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
}
