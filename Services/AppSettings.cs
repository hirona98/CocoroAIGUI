using System;
using System.Collections.Generic;

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

        // コンストラクタはprivate（シングルトンパターン）
        private AppSettings()
        {
            // 将来的に設定ファイルから読み込む場合はここに実装
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