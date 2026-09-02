# Execution notes

## Goal

添付仕様をsource of truthとして、15面キャンペーン、進行度対応エンドレス、JST 60秒デイリーをP1完成状態へする。

## Environment and baseline

- 開始日: 2026-08-24 JST
- Git baseline: `main` / `98da50b`、開始時clean
- Unity: `6000.3.10f1`
- 旧基準証拠: `RegressionArtifacts/hud-fix/final`（EditMode 20、PlayMode 4、capture 18）

## Implemented decisions

- 添付仕様を旧 `Specification.md` より優先した。
- 15面の目標/難易度/ギミック/スポーン/熟練値は `Assets/Resources/StageDatabase.asset` に集約した。
- `StageObjectiveEvaluator` をクリア、失敗、進捗、Result、復活可否の共通入力にした。
- ノーミスはStage 5の `TotalCorrect + MissLimit=1 + AllowRewardedContinue=false` とした。
- BalancedBag、FixedIntroThenBag、FNV-1a、Xorshift32を追加した。抽選は予約し、車間制約による延期では消費しない。
- Dailyは `daily-v1`、JST、60秒、3ミス、復活なし、無制限Retry、Tier 1/2/3、単一JSONとした。
- Endlessは開始時の最高クリア済み面から設定を複製し、1ミス、1回復活とした。
- 解放は `StageClear_N`、星は面別MasteryMetric、保存はbest/星/timeを単調改善とした。
- 旧1〜5面のbest/星を保持し、LegacyClearScore以上だけ明示クリアへ一度移行する。
- 強制広告はDaily禁止、その他は120秒またはキャンペーン3面完了で許可する。
- 計測8項目はバックエンドなしの `siwakenja-events-v1.jsonl` へ記録する。
- コイン、スキン、ブースター、ショップ、ランキング、複数Daily、不正時刻対策は追加しない。

## Defects found and resolved during verification

- スポーン間隔で延期された抽選が消費され、FixedIntro/Bagの実車保証がずれる問題を予約/Commit方式で修正した。
- Daily結果をMainとResultの2箇所で保存し、Resultの新記録判定が保存前値を見られない問題をResult一度だけの確定へ修正した。
- タイトルの説明重なり、章タブとカード/SafeAreaの位置、ページドット、Daily HUDの行とバー、モードResultの空星領域をキャプチャ目視で修正した。
- 最終read-onlyレビューで、Daily Pause RetryのStage化、到達不能なStage 3/10星閾値、固定3ミス枠、通常面SpawnPattern差、15面証拠の合成保存依存を検出した。
- Daily Pauseは表示とRetryをDaily対応、Stage 3/10は到達可能な星閾値、HUD/ResultはMissLimit別1/2/3枠、ノルマ/導入以外はWeightedRandomへ修正した。
- 合成保存だった15面テストを、全15面それぞれの実Main入力/Survival → 実Result保存 → 次面解放へ置き換えた。

## Current evidence

- read-only agentレビュー: 5指摘を受領し、全件を修正対象として反映
- `RegressionArtifacts/goal-work/review-fix-editmode.xml`: 32/32 PASS
- `RegressionArtifacts/goal-work/review-fix-playmode.xml`: 9/9 PASS、全15面の実Main→Resultを含む
- `RegressionArtifacts/goal-work/review-fix-capture/layout-validation.md`: 42 captures / 0 failures、Endlessの1ミス枠を3比率で目視確認
- `Tools/Run-Regression.ps1 -OutputRoot RegressionArtifacts/goal-work/final`: exit 0
- `RegressionArtifacts/goal-work/final/editmode-results.xml`: 32/32 PASS
- `RegressionArtifacts/goal-work/final/playmode-results.xml`: 9/9 PASS、全15面の実Main→Result、Daily Pause Retry、Endless経路を含む
- `RegressionArtifacts/goal-work/final/layout-validation.md`: 42 captures / 0 failures
- Android 1080x1920、iPhone 1170x2532、Android Tall 1440x3200の主要画面を目視し、未解決P0/P1/P2なし

## Completion audit

- 添付仕様、`Specification.md`、実装、回帰文書の整合を確認済み。
- 最終read-onlyレビューの正しさ/Doneに関わる5指摘はすべて修正し、フル回帰を再実行済み。
- Unityが検証時に自動追加した未要求のフォントカーニング差分は対象部分だけ除去した。
- 実ユーザーKPI、実機広告SDK、本番バックエンドは明示的な対象外であり、完了判定には含めない。

## Stop conditions audit

- 外部資格情報、本番設定、不可逆変更は不要。
- 同じ失敗が異なる3手段で継続した阻害要因なし。
- 安全に推定不能な必須仕様なし。
