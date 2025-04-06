using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CocoroAIGUI.Communication;
using CocoroAIGUI.Services;

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

        // 通信サービス
        private CommunicationService? _communicationService;

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

        /// <summary>
        /// ウィンドウがロードされた後に呼び出されるイベントハンドラ
        /// </summary>
        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Owner設定後にメインサービスを初期化
            InitializeMainServices();
        }

        #region 初期化メソッド

        /// <summary>
        /// メインサービスの初期化
        /// </summary>
        private void InitializeMainServices()
        {
            // 通信サービスの取得（メインウィンドウから）
            if (Owner is MainWindow mainWindow &&
                typeof(MainWindow).GetField("_communicationService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(mainWindow) is CommunicationService service)
            {
                _communicationService = service;
            }
        }

        /// <summary>
        /// 表示設定の初期化
        /// </summary>
        private void InitializeDisplaySettings()
        {
            // アプリ設定からの初期値を取得
            var appSettings = AppSettings.Instance;

            // UIに反映
            TopMostCheckBox.IsChecked = appSettings.IsTopmost;
            EscapeCursorCheckBox.IsChecked = appSettings.IsEscapeCursor;
            AutoMoveCheckBox.IsChecked = appSettings.IsAutoMove;
            WindowSizeSlider.Value = appSettings.WindowSize;

            // 設定を辞書に保存
            _displaySettings = new Dictionary<string, object>
            {
                { "TopMost", appSettings.IsTopmost },
                { "EscapeCursor", appSettings.IsEscapeCursor },
                { "AutoMove", appSettings.IsAutoMove },
                { "WindowSize", appSettings.WindowSize }
            };
        }

        /// <summary>
        /// キャラクター設定の初期化
        /// </summary>
        private void InitializeCharacterSettings()
        {
            // アプリ設定からキャラクター設定を取得
            var appSettings = AppSettings.Instance;

            // キャラクターリストのクリア
            _characterSettings.Clear();
            CharacterSelectComboBox.Items.Clear();

            // キャラクター設定を辞書のリストに変換
            foreach (var character in appSettings.CharacterList)
            {
                var characterDict = new Dictionary<string, string>
                {
                    { "Name", character.ModelName ?? "不明" },
                    { "VrmFilePath", character.VrmFilePath ?? "" },
                    { "IsUseLLM", character.IsUseLLM.ToString() },
                    { "ApiKey", character.ApiKey ?? "" },
                    { "LLMModel", character.LLMModel ?? "" },
                    { "SystemPrompt", character.SystemPrompt ?? "" }
                };
                _characterSettings.Add(characterDict);

                // コンボボックスに項目を追加
                var item = new ComboBoxItem { Content = character.ModelName ?? "不明" };
                CharacterSelectComboBox.Items.Add(item);
            }

            // 初期キャラクターの設定をUIに反映
            var currentIndex = appSettings.CurrentCharacterIndex;
            // 選択変更イベントを発生させないようにイベントハンドラを一時的に削除
            CharacterSelectComboBox.SelectionChanged -= CharacterSelectComboBox_SelectionChanged;
            CharacterSelectComboBox.SelectedIndex = currentIndex;
            CharacterSelectComboBox.SelectionChanged += CharacterSelectComboBox_SelectionChanged;
            UpdateCharacterUI(currentIndex);
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
                VrmFilePathTextBox.Text = _characterSettings[index]["VrmFilePath"];
                ApiKeyPasswordBox.Password = _characterSettings[index]["ApiKey"];
                LlmModelTextBox.Text = _characterSettings[index]["LLMModel"];
                SystemPromptTextBox.Text = _characterSettings[index]["SystemPrompt"];

                // IsUseLLMチェックボックスの状態を更新
                bool isUseLLM = false;
                if (_characterSettings[index].ContainsKey("IsUseLLM"))
                {
                    bool.TryParse(_characterSettings[index]["IsUseLLM"], out isUseLLM);
                }
                IsUseLLMCheckBox.IsChecked = isUseLLM;

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
            SaveCurrentCharacterSettings();

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
            SaveCurrentCharacterSettings();

            // 新しいキャラクターの名前を設定
            var newName = "新しいキャラクター" + (_characterSettings.Count + 1);

            // 新しいキャラクター設定を作成
            var newCharacter = new Dictionary<string, string>
            {
                { "Name", newName },
                { "SystemPrompt", "" },
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

            // 碧人ダイアログを表示
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
        private void SaveCurrentCharacterSettings()
        {
            if (_currentCharacterIndex >= 0 && _currentCharacterIndex < _characterSettings.Count)
            {
                // UIから値を取得して設定を更新
                var name = CharacterNameTextBox.Text;
                var systemPrompt = SystemPromptTextBox.Text;
                var vrmFilePath = VrmFilePathTextBox.Text;
                var apiKey = ApiKeyPasswordBox.Password;
                var llmModel = LlmModelTextBox.Text;
                var isUseLLM = IsUseLLMCheckBox.IsChecked ?? false;

                // 値が変更された場合のみ更新
                bool isUseLLMChanged = false;
                if (_characterSettings[_currentCharacterIndex].ContainsKey("IsUseLLM"))
                {
                    bool currentIsUseLLM = false;
                    bool.TryParse(_characterSettings[_currentCharacterIndex]["IsUseLLM"], out currentIsUseLLM);
                    isUseLLMChanged = currentIsUseLLM != isUseLLM;
                }
                else
                {
                    isUseLLMChanged = isUseLLM; // デフォルトはfalseとして扱う
                }

                bool vrmFilePathChanged = !_characterSettings[_currentCharacterIndex].ContainsKey("VrmFilePath") ||
                                         _characterSettings[_currentCharacterIndex]["VrmFilePath"] != vrmFilePath;

                bool apiKeyChanged = !_characterSettings[_currentCharacterIndex].ContainsKey("ApiKey") ||
                                    _characterSettings[_currentCharacterIndex]["ApiKey"] != apiKey;

                bool llmModelChanged = !_characterSettings[_currentCharacterIndex].ContainsKey("LLMModel") ||
                                     _characterSettings[_currentCharacterIndex]["LLMModel"] != llmModel;

                if (_characterSettings[_currentCharacterIndex]["Name"] != name ||
                    _characterSettings[_currentCharacterIndex]["SystemPrompt"] != systemPrompt ||
                    isUseLLMChanged || vrmFilePathChanged || apiKeyChanged || llmModelChanged)
                {
                    _characterSettings[_currentCharacterIndex]["Name"] = name;
                    _characterSettings[_currentCharacterIndex]["SystemPrompt"] = systemPrompt;
                    _characterSettings[_currentCharacterIndex]["VrmFilePath"] = vrmFilePath;
                    _characterSettings[_currentCharacterIndex]["ApiKey"] = apiKey;
                    _characterSettings[_currentCharacterIndex]["LLMModel"] = llmModel;
                    _characterSettings[_currentCharacterIndex]["IsUseLLM"] = isUseLLM.ToString();

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
        private async void SaveAllSettings()
        {
            try
            {
                SaveDisplaySettings();
                SaveCurrentCharacterSettings();
                // AppSettings に設定を反映
                UpdateAppSettings();

                // WebSocketを通じて設定を更新
                bool configUpdateSuccessful = false;
                string errorMessage = string.Empty;

                if (_communicationService != null && _communicationService.IsConnected)
                {
                    try
                    {
                        // 通信サービスによる設定更新を実行
                        await _communicationService.UpdateConfigAsync(AppSettings.Instance.GetConfigSettings());
                        configUpdateSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        errorMessage = ex.Message;
                        configUpdateSuccessful = false;
                    }
                }

                // 処理結果に応じてメッセージを表示
                if (!configUpdateSuccessful)
                {
                    MessageBox.Show($"設定に失敗しました。\n\nエラー: {errorMessage}",
                        "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"設定の保存中にエラーが発生しました: {ex.Message}",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            _displaySettings["EscapeCursor"] = EscapeCursorCheckBox.IsChecked ?? false;
            _displaySettings["AutoMove"] = AutoMoveCheckBox.IsChecked ?? false;
            _displaySettings["WindowSize"] = WindowSizeSlider.Value;
        }

        /// <summary>
        /// AppSettingsを更新する
        /// </summary>
        private void UpdateAppSettings()
        {
            var appSettings = AppSettings.Instance;

            // 表示設定の更新
            appSettings.IsTopmost = (bool)_displaySettings["TopMost"];
            appSettings.IsEscapeCursor = (bool)_displaySettings["EscapeCursor"];
            appSettings.IsAutoMove = (bool)_displaySettings["AutoMove"];
            appSettings.WindowSize = (double)_displaySettings["WindowSize"] > 0 ? (int)(double)_displaySettings["WindowSize"] : 650;

            // キャラクター設定の更新
            appSettings.CurrentCharacterIndex = _currentCharacterIndex;

            // キャラクターリストの更新
            var newCharacterList = new List<CharacterSettings>();

            for (int i = 0; i < _characterSettings.Count; i++)
            {
                var character = _characterSettings[i];

                // 既存のCharacterSettingsオブジェクトを取得（存在する場合）
                CharacterSettings? existingCharacter = null;
                if (i < appSettings.CharacterList.Count)
                {
                    existingCharacter = appSettings.CharacterList[i];
                }

                // 新しいCharacterSettingsオブジェクトを作成または既存のものを更新
                CharacterSettings newCharacter = existingCharacter ?? new CharacterSettings();
                // 基本項目の更新
                newCharacter.ModelName = character["Name"];
                newCharacter.SystemPrompt = character["SystemPrompt"];

                // IsUseLLMの設定を更新
                bool isUseLLM = false;
                if (character.ContainsKey("IsUseLLM"))
                {
                    bool.TryParse(character["IsUseLLM"], out isUseLLM);
                }
                newCharacter.IsUseLLM = isUseLLM;

                // VrmFilePathの設定を更新
                if (character.ContainsKey("VrmFilePath"))
                {
                    newCharacter.VrmFilePath = character["VrmFilePath"];
                }

                // ApiKeyの設定を更新
                if (character.ContainsKey("ApiKey"))
                {
                    newCharacter.ApiKey = character["ApiKey"];
                }

                // LLMModelの設定を更新
                if (character.ContainsKey("LLMModel"))
                {
                    newCharacter.LLMModel = character["LLMModel"];
                }

                // 既存の設定を保持（null になることはないという前提）
                newCharacter.IsReadOnly = existingCharacter?.IsReadOnly ?? false;
                // リストに追加
                newCharacterList.Add(newCharacter);
            }

            // 更新したリストをAppSettingsに設定
            appSettings.CharacterList = newCharacterList;
        }

        #endregion

        #region VRMファイル選択イベントハンドラ

        /// <summary>
        /// VRMファイル参照ボタンのクリックイベント
        /// </summary>
        private void BrowseVrmFileButton_Click(object sender, RoutedEventArgs e)
        {
            // ファイルダイアログの設定
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "VRMファイルを選択",
                Filter = "VRMファイル (*.vrm)|*.vrm|すべてのファイル (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            // ダイアログを表示
            if (dialog.ShowDialog() == true)
            {
                // 選択されたファイルのパスをテキストボックスに設定
                VrmFilePathTextBox.Text = dialog.FileName;
            }
        }

        #endregion
    }
}