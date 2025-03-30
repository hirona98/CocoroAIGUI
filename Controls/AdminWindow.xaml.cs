using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CocoroAIGUI.Controls
{
    /// <summary>
    /// AdminWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AdminWindow : Window
    {
        // 表示設定を保存するための辞書
        private Dictionary<string, object> _displaySettings = new Dictionary<string, object>();
        
        // キャラクター設定を保存するための辞書のリスト
        private List<Dictionary<string, string>> _characterSettings = new List<Dictionary<string, string>>();
        
        // 現在選択されているキャラクターのインデックス
        private int _currentCharacterIndex = 0;

        public AdminWindow()
        {
            InitializeComponent();
            
            // 表示設定の初期化
            InitializeDisplaySettings();
            
            // キャラクター設定の初期化
            InitializeCharacterSettings();
        }

        #region 初期化メソッド

        /// <summary>
        /// 表示設定の初期化
        /// </summary>
        private void InitializeDisplaySettings()
        {
            _displaySettings = new Dictionary<string, object>
            {
                { "UserFontSize", 14 },
                { "AIFontSize", 14 },
                { "Theme", "Light" },
                { "ShowTimestamp", false },
                { "MessageAnimation", true }
            };
            
            // 設定値をUIに反映
            UserFontSizeComboBox.SelectedIndex = 1; // 中(14pt)
            AIFontSizeComboBox.SelectedIndex = 1; // 中(14pt)
            LightThemeRadioButton.IsChecked = true;
            ShowTimestampCheckBox.IsChecked = false;
            MessageAnimationCheckBox.IsChecked = true;
        }

        /// <summary>
        /// キャラクター設定の初期化
        /// </summary>
        private void InitializeCharacterSettings()
        {
            _characterSettings = new List<Dictionary<string, string>>
            {
                // デフォルトキャラクター
                new Dictionary<string, string>
                {
                    { "Name", "デフォルト" },
                    { "Personality", "フレンドリーで親切" },
                    { "Settings", "ユーザーの質問に丁寧に答え、サポートします。" }
                },
                // カスタム1
                new Dictionary<string, string>
                {
                    { "Name", "カスタム1" },
                    { "Personality", "元気で明るい" },
                    { "Settings", "ポジティブな発言が多く、ユーザーを励まします。" }
                },
                // カスタム2
                new Dictionary<string, string>
                {
                    { "Name", "カスタム2" },
                    { "Personality", "冷静で論理的" },
                    { "Settings", "事実に基づいた回答を提供し、客観的な視点を保ちます。" }
                }
            };
            
            // 初期キャラクターの設定をUIに反映
            UpdateCharacterUI(0);
        }

        /// <summary>
        /// キャラクター情報をUIに反映
        /// </summary>
        private void UpdateCharacterUI(int index)
        {
            if (index >= 0 && index < _characterSettings.Count)
            {
                CharacterNameTextBox.Text = _characterSettings[index]["Name"];
                CharacterPersonalityTextBox.Text = _characterSettings[index]["Personality"];
                CharacterSettingsTextBox.Text = _characterSettings[index]["Settings"];
                _currentCharacterIndex = index;
            }
        }

        #endregion

        #region 表示設定イベントハンドラ

        /// <summary>
        /// 表示設定保存ボタンのクリックイベントハンドラ
        /// </summary>
        private void SaveDisplaySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // コンボボックスから選択された値を取得
            var userFontSizeItem = UserFontSizeComboBox.SelectedItem as ComboBoxItem;
            var aiFontSizeItem = AIFontSizeComboBox.SelectedItem as ComboBoxItem;
            
            if (userFontSizeItem != null && aiFontSizeItem != null)
            {
                // タグからフォントサイズを取得
                _displaySettings["UserFontSize"] = Convert.ToInt32(userFontSizeItem.Tag);
                _displaySettings["AIFontSize"] = Convert.ToInt32(aiFontSizeItem.Tag);
            }
            
            // テーマ設定を取得
            if (LightThemeRadioButton.IsChecked == true)
                _displaySettings["Theme"] = "Light";
            else if (DarkThemeRadioButton.IsChecked == true)
                _displaySettings["Theme"] = "Dark";
            else if (SystemThemeRadioButton.IsChecked == true)
                _displaySettings["Theme"] = "System";
            
            // その他の設定を取得
            _displaySettings["ShowTimestamp"] = ShowTimestampCheckBox.IsChecked ?? false;
            _displaySettings["MessageAnimation"] = MessageAnimationCheckBox.IsChecked ?? true;
            
            // 設定保存の処理
            SaveDisplaySettings();
            
            MessageBox.Show("表示設定を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 表示設定リセットボタンのクリックイベントハンドラ
        /// </summary>
        private void ResetDisplaySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 確認ダイアログを表示
            var result = MessageBox.Show("表示設定をデフォルトに戻しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // 設定を初期化
                InitializeDisplaySettings();
                MessageBox.Show("表示設定をリセットしました。", "リセット完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 表示設定を保存する
        /// </summary>
        private void SaveDisplaySettings()
        {
            // TODO: 設定ファイルへの保存処理を実装
            // 実際の実装では設定ファイルに保存する処理を追加
        }

        #endregion

        #region キャラクター設定イベントハンドラ

        /// <summary>
        /// キャラクター選択時のイベントハンドラ
        /// </summary>
        private void CharacterSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = CharacterSelectComboBox.SelectedIndex;
            if (selectedIndex >= 0)
            {
                UpdateCharacterUI(selectedIndex);
            }
        }

        /// <summary>
        /// 新しいキャラクター作成ボタンのクリックイベントハンドラ
        /// </summary>
        private void AddCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            // 新しいキャラクターの名前を入力するダイアログ
            var newName = "新しいキャラクター" + (_characterSettings.Count + 1);
            
            // 新しいキャラクター設定を作成
            var newCharacter = new Dictionary<string, string>
            {
                { "Name", newName },
                { "Personality", "" },
                { "Settings", "" }
            };
            
            // リストに追加
            _characterSettings.Add(newCharacter);
            
            // コンボボックスに項目を追加
            var newItem = new ComboBoxItem { Content = newName };
            CharacterSelectComboBox.Items.Add(newItem);
            
            // 新しいキャラクターを選択
            CharacterSelectComboBox.SelectedIndex = _characterSettings.Count - 1;
        }

        /// <summary>
        /// キャラクター設定保存ボタンのクリックイベントハンドラ
        /// </summary>
        private void SaveCharacterSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCharacterIndex >= 0 && _currentCharacterIndex < _characterSettings.Count)
            {
                // UIから値を取得して設定を更新
                var name = CharacterNameTextBox.Text;
                var personality = CharacterPersonalityTextBox.Text;
                var settings = CharacterSettingsTextBox.Text;
                
                _characterSettings[_currentCharacterIndex]["Name"] = name;
                _characterSettings[_currentCharacterIndex]["Personality"] = personality;
                _characterSettings[_currentCharacterIndex]["Settings"] = settings;
                
                // コンボボックスの表示も更新
                if (_currentCharacterIndex < CharacterSelectComboBox.Items.Count)
                {
                    var item = CharacterSelectComboBox.Items[_currentCharacterIndex] as ComboBoxItem;
                    if (item != null)
                    {
                        item.Content = name;
                    }
                }
                
                // 設定保存の処理
                SaveCharacterSettings();
                
                MessageBox.Show("キャラクター設定を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// キャラクター削除ボタンのクリックイベントハンドラ
        /// </summary>
        private void DeleteCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            // デフォルトキャラクターは削除不可
            if (_currentCharacterIndex == 0)
            {
                MessageBox.Show("デフォルトキャラクターは削除できません。", "削除不可", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // 確認ダイアログを表示
            var name = _characterSettings[_currentCharacterIndex]["Name"];
            var result = MessageBox.Show($"キャラクター「{name}」を削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // キャラクター設定を削除
                _characterSettings.RemoveAt(_currentCharacterIndex);
                CharacterSelectComboBox.Items.RemoveAt(_currentCharacterIndex);
                
                // デフォルトキャラクターを選択
                CharacterSelectComboBox.SelectedIndex = 0;
                
                // 設定保存の処理
                SaveCharacterSettings();
                
                MessageBox.Show("キャラクターを削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// キャラクター設定を保存する
        /// </summary>
        private void SaveCharacterSettings()
        {
            // TODO: 設定ファイルへの保存処理を実装
            // 実際の実装では設定ファイルに保存する処理を追加
        }

        #endregion
    }
}