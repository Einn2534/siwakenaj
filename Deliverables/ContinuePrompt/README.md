# コンティニュー画面のデザイン調整

2026-09-21 / Unity 6000.3.10f1

## 変更

- `Assets/Scripts/UI/ContinuePromptController.cs` の実行時UIを変更。
- 既存の木枠、紙、ポワ、星、ボタン素材を再利用。クリーム色・紫・金色で背景と統一。
- 見出しとスコアの文字サイズに差を付け、広告による復活を主操作として表示。
- 広告の完了条件と、復活は1プレイに1回という既存ルールを表示。
- 日本語フォントとSafeAreaFitterを適用。装飾は入力を遮らず、背面へのタップはオーバーレイで遮断。
- シーン・Prefabの保存変更なし。既存の広告・復活・あきらめる処理を維持。

## プレビュー

- `Continue_Runtime_1080x1920.png`: MainのPlayModeで表示した実装。
- `Continue_*.png`: 既存のRegressionCaptureRunner / PowaScreenshotCaptureを通したレイアウト確認。通常・広告待機・広告失敗・最大intスコアを3つの画面サイズで検証。
- `layout-validation.md`: コンティニュー画面12パターンのレイアウト検証結果。

## 検証

- コンパイル: エラー0、警告0。
- EditMode: 77/77 passed (`editmode-results.xml`)。
- PlayMode: 49/49 passed (`playmode-results.xml`)。復活、あきらめる、Result、Retryの既存テストを含む。
- コンティニューレイアウト: 12/12 passed。1080×1920、1170×2532、1440×3200。
- 既存画面の回帰レイアウト: 42/42 passed (`Regression/layout-validation.md`)。
- 通常表示・大きなスコア・広告失敗・Main実行中の画像を目視確認。
- 終了時はMainをEditModeで開き、シーンの未保存変更なし、Consoleエラー0。

実機での広告配信確認やAndroidビルドは行っていない。
