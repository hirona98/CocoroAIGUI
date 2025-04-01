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
        private Dictionary<string, object> _originalDisplaySettings = new Dictionary<string, object>();

        // キャラクター設定を保存するための辞書のリスト
        private List<Dictionary<string, string>> _characterSettings = new List<Dictionary<string, string>>();
        private List<Dictionary<string, string>> _originalCharacterSettings = new List<Dictionary<string, string>>();

        // 現在選択されているキャラクターのインデックス
        private int _currentCharacterIndex = 0;

        // 設定が変更されたかどうかを追跡するフラグ
        private bool _settingsChanged = false;

        public AdminWindow()
        {
            InitializeComponent();

            // 表示設定の初期化
            InitializeDisplaySettings();

            // キャラクター設定の初期化
            InitializeCharacterSettings();

            // 元の設定のバックアップを作成
            BackupSettings();
        }

        #region 初期化メソッド

        /// <summary>
        /// 表示設定の初期化
        /// </summary>
        private void InitializeDisplaySettings()
        {
            _displaySettings = new Dictionary<string, object>
            {
                { "TopMost", false },
                { "EscapeCursor", true }
            };
            TopMostCheckBox.IsChecked = false;
            EscapeCursorCheckBox.IsChecked = true;
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
        /// 現在の設定をバックアップする
        /// </summary>
        private void BackupSettings()
        {
            // 表示設定のバックアップ
            _originalDisplaySettings = new Dictionary<string, object>(_displaySettings);

            // キャラクター設定のディープコピー
            _originalCharacterSettings = new List<Dictionary<string, string>>();
            foreach (var character in _characterSettings)
            {
                _originalCharacterSettings.Add(new Dictionary<string, string>(character));
            }
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

        #region キャラクター設定イベントハンドラ

        /// <summary>
        /// キャラクター選択時のイベントハンドラ
        /// </summary>
        private void CharacterSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 現在のキャラクター設定を保存
            SaveCurrentCharacterToMemory();

            // 新しいキャラクターのUIを更新
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
            // 現在のキャラクター設定を保存
            SaveCurrentCharacterToMemory();

            // 新しいキャラクターの名前を設定
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

            // 設定変更フラグを設定
            _settingsChanged = true;
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

                // 設定変更フラグを設定
                _settingsChanged = true;
            }
        }

        /// <summary>
        /// 現在のキャラクター設定をメモリに保存
        /// </summary>
        private void SaveCurrentCharacterToMemory()
        {
            if (_currentCharacterIndex >= 0 && _currentCharacterIndex < _characterSettings.Count)
            {
                // UIから値を取得して設定を更新
                var name = CharacterNameTextBox.Text;
                var personality = CharacterPersonalityTextBox.Text;
                var settings = CharacterSettingsTextBox.Text;

                // 値が変更された場合のみ更新
                if (_characterSettings[_currentCharacterIndex]["Name"] != name ||
                    _characterSettings[_currentCharacterIndex]["Personality"] != personality ||
                    _characterSettings[_currentCharacterIndex]["Settings"] != settings)
                {
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

                    // 設定変更フラグを設定
                    _settingsChanged = true;
                }
            }
        }

        #endregion

        #region 共通ボタンイベントハンドラ

        /// <summary>
        /// OKボタンのクリックイベントハンドラ
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // すべてのタブの設定を保存
            SaveAllSettings();

            // ウィンドウを閉じる
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// キャンセルボタンのクリックイベントハンドラ
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 設定が変更されていた場合は確認ダイアログを表示
            if (_settingsChanged)
            {
                var result = MessageBox.Show("変更した設定は保存されません。よろしいですか？",
                    "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            // 変更を破棄して元の設定に戻す
            RestoreOriginalSettings();

            // ウィンドウを閉じる
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// すべてのタブの設定を保存する
        /// </summary>
        private void SaveAllSettings()
        {
            // 表示設定タブの設定を保存
            SaveDisplaySettings();

            // 現在のキャラクター設定をメモリに保存してからキャラクター設定を保存
            SaveCurrentCharacterToMemory();
            SaveCharacterSettings();

            MessageBox.Show("すべての設定を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 元の設定に戻す
        /// </summary>
        private void RestoreOriginalSettings()
        {
            // 設定をバックアップから復元
            _displaySettings = new Dictionary<string, object>(_originalDisplaySettings);

            _characterSettings.Clear();
            foreach (var character in _originalCharacterSettings)
            {
                _characterSettings.Add(new Dictionary<string, string>(character));
            }
        }

        #endregion

        #region 設定保存メソッド

        /// <summary>
        /// 表示設定を保存する
        /// </summary>
        private void SaveDisplaySettings()
        {
            _displaySettings["TopMost"] = TopMostCheckBox.IsChecked ?? false;
            _displaySettings["EscapeCursor"] = EscapeCursorCheckBox.IsChecked ?? true;

            // TODO: 設定ファイルへの保存処理を実装
            // 実際の実装では設定ファイルに保存する処理を追加
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