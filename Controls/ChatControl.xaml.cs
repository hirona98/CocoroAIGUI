using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CocoroAIGUI.Controls
{
    /// <summary>
    /// チャットコントロール（バブルデザイン）
    /// </summary>
    public partial class ChatControl : UserControl
    {
        public event EventHandler<string>? MessageSent;

        public ChatControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ユーザーメッセージを送信
        /// </summary>
        private void SendMessage()
        {
            string message = MessageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            // UIにユーザーメッセージを追加
            AddUserMessage(message);

            // メッセージ送信イベント発火
            MessageSent?.Invoke(this, message);

            // テキストボックスをクリア
            MessageTextBox.Clear();
        }

        /// <summary>
        /// ユーザーメッセージをUIに追加
        /// </summary>
        /// <param name="message">メッセージ</param>
        public void AddUserMessage(string message)
        {
            var messageContainer = new StackPanel();

            var bubble = new Border
            {
                Style = (Style)Resources["UserBubbleStyle"]
            };

            var messageContent = new StackPanel();

            var messageText = new TextBlock
            {
                Style = (Style)Resources["MessageTextStyle"],
                Text = message
            };

            var timestamp = new TextBlock
            {
                Style = (Style)Resources["TimestampStyle"],
                Text = DateTime.Now.ToString("HH:mm")
            };

            messageContent.Children.Add(messageText);
            messageContent.Children.Add(timestamp);
            bubble.Child = messageContent;
            messageContainer.Children.Add(bubble);

            ChatMessagesPanel.Children.Add(messageContainer);

            // 自動スクロール
            ChatScrollViewer.ScrollToEnd();
        }

        /// <summary>
        /// AIレスポンスをUIに追加
        /// </summary>
        /// <param name="message">レスポンスメッセージ</param>
        public void AddAiMessage(string message)
        {
            var messageContainer = new StackPanel();

            var bubble = new Border
            {
                Style = (Style)Resources["AiBubbleStyle"]
            };

            var messageContent = new StackPanel();

            var messageText = new TextBlock
            {
                Style = (Style)Resources["MessageTextStyle"],
                Text = message
            };

            var timestamp = new TextBlock
            {
                Style = (Style)Resources["TimestampStyle"],
                Text = DateTime.Now.ToString("HH:mm")
            };

            messageContent.Children.Add(messageText);
            messageContent.Children.Add(timestamp);
            bubble.Child = messageContent;
            messageContainer.Children.Add(bubble);

            ChatMessagesPanel.Children.Add(messageContainer);

            // 自動スクロール
            ChatScrollViewer.ScrollToEnd();
        }

        /// <summary>
        /// チャット履歴をクリア
        /// </summary>
        public void ClearChat()
        {
            ChatMessagesPanel.Children.Clear();
        }

        /// <summary>
        /// 送信ボタンクリックハンドラ
        /// </summary>
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        /// <summary>
        /// テキストボックスのキー入力ハンドラ（Enterキーで送信）
        /// </summary>
        private void MessageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Enterキーが押された場合
            if (e.Key == Key.Enter)
            {
                // Shiftキーが押されていない場合は送信処理
                if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                {
                    // Enterキーのデフォルト動作（改行）を防止
                    e.Handled = true;

                    // 送信処理を実行
                    SendMessage();
                }
                // Shift+Enterの場合はデフォルト動作（改行）をそのまま許可
            }
        }
    }
}