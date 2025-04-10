using System;
using System.Collections.Generic;
using CocoroAIGUI.Communication;

namespace CocoroAIGUI.Services
{
    /// <summary>
    /// アプリケーション設定を管理するクラス
    /// </summary>
    public class AppSettings
    {
        private static readonly Lazy<AppSettings> _instance = new Lazy<AppSettings>(() => new AppSettings());

        public static AppSettings Instance => _instance.Value;

        // 接続設定
        public string WebSocketUrl { get; set; } = "ws://127.0.0.1:8080/";
        public string UserId { get; set; } = "user01";

        // UI設定
        public bool IsTopmost { get; set; } = false;
        public bool IsEscapeCursor { get; set; } = false;
        public bool IsAutoMove { get; set; } = false;
        public int WindowSize { get; set; } = 650;

        // キャラクター設定
        public int CurrentCharacterIndex { get; set; } = 0;
        public List<CharacterSettings> CharacterList { get; set; } = new List<CharacterSettings>();

        // 設定が読み込まれたかどうかを示すフラグ
        public bool IsLoaded { get; private set; } = false;

        // コンストラクタはprivate（シングルトンパターン）
        private AppSettings()
        {
            // デフォルト設定を初期化
            InitializeDefaultSettings();
        }

        /// <summary>
        /// デフォルト設定を初期化
        /// </summary>
        private void InitializeDefaultSettings()
        {
            // デフォルトのキャラクター設定を初期化
            CharacterList = new List<CharacterSettings>
            {
                new CharacterSettings
                {
                    IsReadOnly = false,
                    ModelName = "model_name",
                    VrmFilePath = "vrm_file_path",
                    IsUseLLM = false,
                    ApiKey = "your_api_key",
                    LLMModel = "gpt-3.5-turbo",
                    SystemPrompt = "あなたは親切なアシスタントです。",
                    IsUseTTS = false,
                    TTSEndpointURL = "http://localhost:50021",
                    TTSSperkerID = "1",
                }
            };
        }

        /// <summary>
        /// 設定値を更新
        /// </summary>
        /// <param name="config">サーバーから受信した設定値</param>
        public void UpdateSettings(ConfigSettings config)
        {
            IsTopmost = config.IsTopmost;
            IsEscapeCursor = config.IsEscapeCursor;
            IsAutoMove = config.IsAutoMove;
            WindowSize = config.WindowSize > 0 ? (int)config.WindowSize : 650;
            CurrentCharacterIndex = config.CurrentCharacterIndex;

            // キャラクターリストを更新（もし受信したリストが空でなければ）
            if (config.CharacterList != null && config.CharacterList.Count > 0)
            {
                CharacterList = new List<CharacterSettings>(config.CharacterList);
            }

            // 設定読み込み完了フラグを設定
            IsLoaded = true;
        }

        /// <summary>
        /// 現在の設定からConfigSettingsオブジェクトを作成
        /// </summary>
        /// <returns>ConfigSettings オブジェクト</returns>
        public ConfigSettings GetConfigSettings()
        {
            return new ConfigSettings
            {
                IsTopmost = IsTopmost,
                IsEscapeCursor = IsEscapeCursor,
                IsAutoMove = IsAutoMove,
                WindowSize = WindowSize,
                CurrentCharacterIndex = CurrentCharacterIndex,
                CharacterList = new List<CharacterSettings>(CharacterList)
            };
        }

        /// <summary>
        /// 設定を保存（将来的な実装のためのメソッド）
        /// </summary>
        public void SaveSettings()
        {
            // 将来的に設定ファイルへの保存処理を実装
        }
    }
}