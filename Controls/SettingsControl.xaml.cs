using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CocoroAIGUI.Controls
{
    /// <summary>
    /// 設定コントロールの相互作用ロジック
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        // 各設定変更イベント
        public event EventHandler<ConnectionSettings>? ConnectionSettingsChanged;
        public event EventHandler<string>? CharacterModelChanged;
        public event EventHandler<int>? ReactionSpeedChanged;
        public event EventHandler<string>? AiModelChanged;
        public event EventHandler<double>? TemperatureChanged;
        public event EventHandler<bool>? VoiceOutputChanged;
        public event EventHandler? ConnectRequested;
        public event EventHandler? DisconnectRequested;
        public event EventHandler? SaveSettingsRequested;

        /// <summary>
        /// 接続設定情報保持クラス
        /// </summary>
        public class ConnectionSettings
        {
            public string WebSocketUrl { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
        }

        public SettingsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 接続ボタンクリックハンドラ
        /// </summary>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // 接続設定情報を取得してイベント発火
            var settings = new ConnectionSettings
            {
                WebSocketUrl = WebSocketUrlTextBox.Text,
                UserId = UserIdTextBox.Text
            };

            ConnectionSettingsChanged?.Invoke(this, settings);
            ConnectRequested?.Invoke(this, EventArgs.Empty);

            // UI状態更新
            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;
            ConnectionStatusTextBlock.Text = "接続状態: 接続中";
        }

        /// <summary>
        /// 切断ボタンクリックハンドラ
        /// </summary>
        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            DisconnectRequested?.Invoke(this, EventArgs.Empty);

            // UI状態更新
            ConnectButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
            ConnectionStatusTextBlock.Text = "接続状態: 切断";
            CpuUsageTextBlock.Text = "CPU使用率: --%";
        }

        /// <summary>
        /// キャラクターモデル変更ハンドラ
        /// </summary>
        private void CharacterModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CharacterModelComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string modelName = selectedItem.Content.ToString() ?? string.Empty;
                CharacterModelChanged?.Invoke(this, modelName);
            }
        }

        /// <summary>
        /// 反応速度変更ハンドラ
        /// </summary>
        private void ReactionSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // SliderのValueChangedは頻繁に呼ばれるため、値が確定したタイミングでのみイベント発火
            if (sender is Slider slider && !slider.IsFocused)
            {
                int speed = (int)e.NewValue;
                ReactionSpeedChanged?.Invoke(this, speed);
            }
        }

        /// <summary>
        /// AIモデル変更ハンドラ
        /// </summary>
        private void AiModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AiModelComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string modelName = selectedItem.Content.ToString() ?? string.Empty;
                AiModelChanged?.Invoke(this, modelName);
            }
        }

        /// <summary>
        /// 応答温度変更ハンドラ
        /// </summary>
        private void TemperatureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // SliderのValueChangedは頻繁に呼ばれるため、値が確定したタイミングでのみイベント発火
            if (sender is Slider slider && !slider.IsFocused)
            {
                double temperature = Math.Round(e.NewValue, 1);
                TemperatureChanged?.Invoke(this, temperature);
            }
        }

        /// <summary>
        /// 音声出力設定変更ハンドラ
        /// </summary>
        private void UseVoiceCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = UseVoiceCheckBox.IsChecked ?? false;
            VoiceOutputChanged?.Invoke(this, isChecked);
        }

        /// <summary>
        /// 設定保存ボタンクリックハンドラ
        /// </summary>
        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// CPU使用率表示を更新
        /// </summary>
        /// <param name="cpuUsage">CPU使用率</param>
        public void UpdateCpuUsage(int cpuUsage)
        {
            CpuUsageTextBlock.Text = $"CPU使用率: {cpuUsage}%";
        }

        /// <summary>
        /// 接続状態表示を更新
        /// </summary>
        /// <param name="isConnected">接続状態</param>
        public void UpdateConnectionStatus(bool isConnected)
        {
            if (isConnected)
            {
                ConnectionStatusTextBlock.Text = "接続状態: 接続中";
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
            }
            else
            {
                ConnectionStatusTextBlock.Text = "接続状態: 切断";
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
            }
        }
    }
}