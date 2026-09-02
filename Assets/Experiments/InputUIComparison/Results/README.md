# Results

このフォルダーは、個人情報を除いた検証記録と結果の説明を置く場所です。実参加者の Raw CSV / Manifest / checkpoint は Unity AssetDatabase へ commit せず、`Application.persistentDataPath/InputUIComparison/v2/2.1.0/{session_id}` に保存します。複数参加者の集約は `Application.persistentDataPath/InputUIComparison/2.1.0/{experiment_type}` です。

## 区分

- `Verification/`：Editor / Game View のエンジニアリング証拠。既存画像と report は削除・移動しない。
- `LegacyV1/`：v1 Baseline/A/B の解釈と隔離規則。
- `P00/`：Android 実機予行の非個人・非 UX 結果概要。

既存 `Verification` の `baseline_broken_1080x2340.png`、`a_fixed_four_normal_1080x2340.png`、`b_separated_broken_1080x2340.png` は v1 package の見た目を確認した画像です。v2.1 Placement / State Assist の因果比較結果でも、人間行動結果でもありません。

## 集約

`Tools > しわ賢者 > Input UI Comparison > Aggregate V2 Complete Sessions` は completed、非 pilot、同一 `experiment_version` / `experiment_type` の session だけを集約し、同じ participant ID では最新の完了 session を使います。

出力は `summary_participant_condition.csv`、`summary_participant_difference.csv`、`summary_group.csv` です。P00、練習、途中終了、条件不足、別 version は除外します。

## 現在の結果状態

人間参加者データはありません。既存 screenshot と自動検証は実装成立の証拠だけです。現在は P00 前で、version freeze 前です。

結果を追加する順序は `P00全技術予行 → version freeze → P01〜P06 Placementのみ → 参加者単位集計 → anchor暫定 → State Assist別session（最低別日、推奨別cohort）→ 候補2案の事前登録 → Rush最終反証` です。実験種別を同日にまとめません。Placement の結論は `UpperFixed` / `BottomFixed` の二固定位置間だけに限定します。UI 案の採用推奨はありません。
