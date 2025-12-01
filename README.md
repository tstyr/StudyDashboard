# Study Dashboard
Windowsデスクトップアプリケーションです。

## ダウンロード

最新版は [Releases](../../releases) からダウンロードできます。

📦 **[StudyDashboard-v1.1.0-win-x64.zip](../../releases/latest/download/StudyDashboard-v1.1.0-win-x64.zip)** - Windows x64版

## 機能

### 🎯 Focus Timer (勉強タイマー)
- ポモドーロテクニック対応
- 25分の集中時間と5分の休憩時間
- 進捗バー表示
- 開始/一時停止/リセット機能

### 📊 Session Stats & Analysis (勉強時間の推移)
- 今日のセッション数と総時間
- 週間統計とトレンドグラフ
- 目標達成率の表示
- 14日間の集中時間推移

### 🎵 Sound Player (サウンドプレイヤー)
- YouTube、Spotify URL対応
- 音量調整機能
- プリセット音楽（Lo-Fi、雨音など）
- 外部プレイヤーとの連携

### 🎶 Audio Visualizer (オーディオスペクトログラム)
- リアルタイム音声可視化
- 感度とアップデート速度調整
- 32バンドスペクトラム表示
- マイク入力対応

### ✅ Today's Tasks (今日のタスク)
- タスクの追加・完了管理
- 進捗統計表示
- チェックボックス形式
- 残りタスク数表示

### 💻 System Status (システムステータス)
- CPU使用率とメモリ使用量
- CPU温度監視
- 現在時刻表示
- リアルタイム更新

## 特徴

### 🎨 デザイン
- 半透明の黒背景
- モダンなUI/UX
- 視認性の高いコントラスト

### 🖱️ インタラクション
- 全ウィジェットがドラッグ可能
- 右下角でサイズ変更
- 右クリックメニュー対応
- ESCキーで終了

## 必要な環境

- Windows 10/11
- .NET 8.0
- Visual Studio 2022 (推奨)

## インストール・実行

1. リポジトリをクローン
```bash
git clone [repository-url]
cd StudyDashboard
```

2. プロジェクトをビルド
```bash
dotnet build
```

3. アプリケーションを実行
```bash
dotnet run --project StudyDashboard
```

または Visual Studio で `StudyDashboard.sln` を開いて実行

## 使用技術

- **WPF** - UIフレームワーク
- **NAudio** - オーディオ処理
- **OxyPlot** - グラフ表示
- **System.Management** - システム情報取得

## 操作方法

- **ドラッグ**: ウィジェットをマウスで左クリックしながら移動
- **リサイズ**: ウィジェットの右下角を左クリックしながらドラッグ
- **終了**: ESCキーを押す
- **リセット**: 右クリックメニューから位置をリセット

## カスタマイズ

各ウィジェットは独立しており、必要に応じて機能を追加・変更できます。

- タイマー時間の変更: `FocusTimerWidget.xaml.cs`
- 統計データの保存: `SessionStatsWidget.xaml.cs`
- 音楽プリセットの追加: `SoundPlayerWidget.xaml.cs`

## ライセンス

MIT License
