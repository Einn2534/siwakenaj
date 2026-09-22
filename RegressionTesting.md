# P1 regression testing

## Full run

PowerShellでプロジェクトルートから実行する。

```powershell
.\Tools\Run-Regression.ps1 -OutputRoot .\RegressionArtifacts\goal-work\final
```

既定Editorは `ProjectSettings/ProjectVersion.txt` と同じUnity `6000.3.10f1`。別の場所へインストールした場合だけ `-UnityPath` を指定する。スクリプトは検証を順番に実行し、いずれかが失敗すると非ゼロで終了する。

1. EditMode
2. PlayMode実シーンフロー
3. 3画面比率×14状態のレイアウト検証とPNGキャプチャ

最終証拠は `RegressionArtifacts/goal-work/final` に出力する。

## EditMode coverage

テーマ追加後の期待件数は83件。既存回帰に加えて次を個別に証明する。

- `StageDatabase.asset` が連番15面、3章、5目標、面名、MISSIONを持つこと
- Score / TotalCorrect / Combo / VehicleQuota / Survivalそれぞれの成功とミス上限失敗
- Pause中にSurvival時間が進まないこと
- 通常面、ステージ5、Dailyの復活可否
- BalancedBagが袋1巡内で車種別ノルマを枯渇させないこと
- ステージ3/6/11のFixedIntroが最初の3台以内へギミックを含むこと
- スポーン延期中は予約命令を消費しないこと
- `daily-v1` の同日同Tierで全生成設定、seed、最初の100台が一致すること
- JST日付境界、Tier 1/2/3、Tier別ギミック、合計確率上限
- 日付更新で当日値をリセットし、ベスト、最大コンボ、完走、連続完走を単一JSONへ保存すること
- 旧1〜5面のベストと星を維持しながら明示クリアを追加する移行
- MissCount / Score / MaxCombo / ClearTimeの星評価と、ベスト/星/時間が後退しない保存
- 120秒または3面完了のインタースティシャル条件とDaily禁止
- ローカルJSONLへ初回、成否、失敗理由、時間、ミス、最大コンボ、Retry、復活選択を書けること

ファイル追記を行う計測テストは元のファイル長を保存し、テスト追記分を `finally` で復元する。

## PlayMode coverage

急送車チュートリアルは、1-3開始時の表示、旧ヒント既読からの移行、説明の自動送り、一時停止、正しい実入力を待つ停止車練習、走行車の練習とミス後の再試行、本番開始時のスコア・ミス初期化、再挑戦時の非表示、スキップ時の基本チュートリアル保存値の維持を確認する。`ExpressTutorial_Seen` も既存キーと同じくテスト前後でバックアップ・復元する。通常のステージ進行テストは急送車チュートリアル既読として実行し、専用テストで未読の流れを検証する。

テーマ追加後の期待件数は56件。実際のTitle、StageSelect、Main、Resultシーンと実ボタン/Pointer Downを使用する。

- 車なし、誤レーン、正解、同一フレーム複数入力、3台上限
- 実Mainでの目標成功、Result保存、Retryによる新しいMain
- ミス上限、Continue Prompt、辞退、失敗Result、Retry
- Express、Covered、Brokenと整備ボタンの専用ルール
- Stage 14のRush 3台、Fever HUD、カメラ設定、整備ボタン非重複
- Titleの「おしごと/デイリー/エンドレス」とStageSelectの章タブ/各章5カード
- `Title → StageSelect → Main → Result → Next Stage → Main`、StageClear/BestScoreのシーン越し復元
- `Title → Daily Main → 60秒Result → Retry → Daily Main`、3ミス、復活なし
- Daily MainのPause表示が「デイリー」で、Pause Retry後もDailyを維持すること
- `Title → Endless Main` が最高クリア済み面を使い、1ミス時に1回の復活を提示すること
- Stage 1/5/10とEndlessのHUD/ResultがMissLimitに応じて3/1/2/1個のミス枠を表示すること
- 実MainのPauseでSurvival時計が停止・再開すること
- 全15面を順に実Mainへロードし、各面のasset目標を実入力または実Survival時計で達成し、Resultの明示クリア保存から次面を解放できること

PlayModeが触るPlayerPrefs、BestTime、Daily JSON、広告時刻、Telemetry初回フラグ、ローカル計測ファイルはsuite開始時にバックアップし、終了時に復元する。

## Layout matrix

次の3比率を使用する。

| Profile | Viewport | Safe area |
| --- | ---: | --- |
| Android | 1080×1920 | full |
| iPhone | 1170×2532 | top/bottom inset |
| Android Tall | 1440×3200 | top/gesture inset |

各比率で次の14状態、合計42枚を撮影する。

1. Title（拡張モード未解放）
2. Title（拡張モード解放済み）
3. StageSelect 第1章
4. StageSelect 第3章
5. Main 基本
6. Main ステージ15 HUD
7. Main Daily HUD
8. Main Endless HUD
9. Pause
10. How To
11. Result Clear
12. Result Game Over
13. Result Daily
14. Result Endless

検証器は有効ボタンのSafeArea外はみ出し、TMPの切り詰め、有効ボタン同士の矩形重なりを失敗にする。自動PASSに加え、Titleの3導線、章タブ/カード、5ページドット、目標別HUD、星なしDaily/Endless Resultを目視し、P0/P1/P2の未解決表示問題がないことを確認する。

Unity EditorではAds初期化を無効にしているため、回帰中に広告サービスへ接続しない。IAPランタイムは本プロジェクトに含めない。

## Evidence reading

- `editmode-results.xml`: `<test-run ... result="Passed" failed="0">`
- `playmode-results.xml`: `<test-run ... result="Passed" failed="0">`
- `layout-validation.md`: `Final result: PASS (42 captures, 0 failures)`
- `*.png`: 42枚の実キャプチャ
- `editmode.log` / `playmode.log` / `capture.log`: Unityの完全ログ

テストを削除、Ignore化、アサーション弱化、偽データへの置換、検証経路の迂回によってPASSさせてはならない。仕様変更が必要な場合は先に実装と本書の期待値を一致させる。

## Sorting themes regression (2026-09-21)

`SortingThemesEditModeTests` は4テーマの名前・3種類の別画像・単位、15面の割り当て、Dailyの車固定、Endlessのテーマ継承、非車の共通覆面画像を検証する。

`SortingThemesPlayModeTests` は以下を既存GameplayFlow fixtureの保存値バックアップ・復元の下で実行する。

- 各テーマでStageSelectの実PlayからMainへ入り、3種の画像・ボタン・名前、正解・誤答、Pauseの説明、Resultの名前と保存、Retry、現在テーマでの練習を検証。
- 3→4、7→8、10→11のNext、Endless→Daily→Stageで前テーマが残らないこと。
- 2/7面の種類別ノルマと車・手・時計・野菜の失敗Result。
- DailyのPause→練習スキップ、EndlessのPause→練習6回入力と卒業画面のボタンから本番復帰し、モード・テーマ・得点初期化・Daily時間・Endless無目標を維持すること。
- Stage11の既存Express/Covered/Brokenテストは、元の車用覆面資産のサイズ検証を残し、現在の野菜テーマの箱と公開後のレタス画像を厳密比較する。

実フロー中のStageSelect / 選択テーマの説明 / Main / 3回正解後の収集欄 / Pause説明 / Result / Tutorialの7状態×4テーマ×3比率と、2/7面ノルマ×3比率、計90枚を `Deliverables/SortingThemes/Captures` に保存する。Main撮影時のみ現在の対象を横並びにして識別性を記録し、直後に位置を復元する。Tutorialは位置を変えず、撮影用カメラに対してcutoutを再投影する。モーダル画面は背景を遮るscrimより前のUIを検証対象とする。既存42状態のアサーションは変更せず、Editorプレビューのボタン・ギミック名も現在テーマから再解決する。

開いているEditorでUniCliから全検証を行う場合、`SortingThemeTestRunner.Run("EditMode", "", "editmode-results")`、終了後に `SortingThemeTestRunner.Run("PlayMode", "", "playmode-results")` をEvalで呼ぶ。domain reloadでCLI接続が切れても結果は `Deliverables/SortingThemes/Tests` のXMLとprogressへ記録される。完了はprogressのCOMPLETEとXMLのresult/failedで確認する。既存レイアウト行列は `RegressionCaptureRunner.Run` を同じEditorで呼び出す（出力先 `Deliverables/SortingThemes/Regression`）。バッチの `Tools/Run-Regression.ps1` も引き続き利用できる。
