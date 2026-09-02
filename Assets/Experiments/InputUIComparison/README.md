# しわ賢者 入力 UI 比較実験 v2.1

本番の Scene、Prefab、Script、ゲームルールに触れず、入力 UI の要因を分離して検証する独立プロトタイプです。現在のデータ契約は `schema_version=2`、`experiment_version=2.1.0` です。

現時点では人間参加者のデータを取得していません。自動テストや P00 実機予行が成功しても、いずれかの UI が UX 上優れていることは示しません。A 案／B 案を含め、採用推奨は出していません。

## 実験の分離

| 実験種別 | 条件 | 変える主要因 | 許される結論 |
|---|---|---|---|
| `placement` | `Placement_UpperFixed` / `Placement_BottomFixed` | 整備ボタンの固定 `anchoredPosition` だけ | 試した二つの固定位置間の差だけ。追従型・文脈型配置一般へ外挿しない |
| `state_assist` | `Assist_AlwaysActive` / `Assist_DisabledWhenNormal` / `Assist_BrokenOnly` | 整備ボタンの表示・入力可能状態だけ | 暫定採用した一つの固定 anchor 上での状態アシスト差だけ |
| `rush_package` | 事前登録した完成パッケージ 2 候補 | UI 一式の組合せ | 高密度状況で候補が破綻しないかの最終反証。個別要因へ帰属しない |

`Placement_UpperFixed` は target に追従する配置ではなく、reference frame 上の固定上段位置です。Placement の結論は `UpperFixed` と `BottomFixed` の二固定位置間に限定します。

旧 Baseline、A（固定 4 枠）、B（Broken 時だけコンテキスト表示）は削除していません。旧 A/B は複数要因を同時に変えるため、Placement／State Assist の因果比較には使いません。P00 では既存 3 Rush preset の技術動作を確認できますが、人間対象の Rush では State Assist 後に事前登録した 2 候補だけを比較します。

## 必須の実施順

順序は次から変更せず、後段を前倒ししません。

1. `P00` で全技術予行を行い、11 項目の機械合格条件と端末・cutout matrixを完了する。
2. P00 の blocking defect を解消し、build、Git commit、`experiment_version=2.1.0`、条件・指標・除外規則を version freeze する。
3. `P01`〜`P06` では Placement だけを実施する。
4. Placement を参加者×条件で集計し、二固定位置のうち State Assist に使う anchor を暫定選択する。
5. State Assist を Placement とは別 session で実施する。最低でも別日、推奨は別 cohort とする。
6. State Assist の参加者単位集計とゲーム性アンケートから、完成パッケージ候補を 2 案に固定して事前登録する。
7. Rush でその 2 案を最終反証し、高密度・対象切替・stale input 条件で破綻しないか確認する。

Placement、State Assist、Rush を同日にまとめて実施してはいけません。最低でも実験種別ごとに別 session／別日とし、State Assist は学習持越しを避けるため別 cohort を推奨します。

## 起動とセッション

1. Unity 6.0（プロジェクト記録値 `6000.3.10f1`）でプロジェクトを開く。
2. `Tools > しわ賢者 > Input UI Comparison > Open Experiment Scene` から独立 Scene を開く。
3. Play Mode で匿名 ID とその段階で許可された実験種別を設定する。実機予行は `P00` を使う。
4. State Assist では Placement の参加者単位集計後に暫定選択した `RepairAnchorMode.UpperFixed` または `RepairAnchorMode.BottomFixed` を開始前に記録し、3 条件で固定する。
5. 開始後は実験種別と `RepairAnchorMode` をロックする。各条件で練習 4 試行、計測 24 試行、条件後アンケートを行う。

参加者画面には内部条件名ではなく `UI 1 / N` を表示し、全条件が終わるまで成績による誘導を行いません。

## データと再開

物理 Pointer Down は uGUI の Button callback より下層で取得し、Button 外、disabled、非表示の予約領域、cooldown 中、同一フレームの複数入力も `*_input_events.csv` に 1 物理イベント 1 行で残します。ゲーム判定は別の仲裁処理で同一 frame の最後の物理入力だけを対象にします。最後の入力が disabled／hidden／空白なら、先行する有効入力へフォールバックしません。

主な出力は次のとおりです。

- `*_trials.csv`：試行単位の結果
- `*_input_events.csv`：全 Raw Pointer Down。hidden Repair、generic empty、stale Repair を区別する
- `*_survey.csv`：8 問の 5 段階回答と短い自由回答
- `*_summary_participant_condition.csv`：参加者×条件の指標
- `*_summary_participant_difference.csv`：同一参加者内の条件差
- `summary_group.csv`：複数完了セッションの参加者値分布と改善方向
- `session_manifest.json`：端末、順序、問題セット、seed、バージョン、完了状態
- `checkpoint.a.json` / `checkpoint.b.json`：交互保存・SHA-256 検証付き再開状態
- `raw_events.wal.jsonl`：Pointer Down時に同期追記するchecksum付きRaw write-ahead log。LateUpdate／checkpoint前のforce-stopでも物理入力を失わない

State Assist の primary は `first_raw_touch_error_rate`、`trial_with_disabled_repair_tap_rate`、`trial_with_hidden_repair_area_tap_rate`、`time_to_correct_resolution_ms`（first correct resolution）、`correction_cost_ms`、`timeout_rate`、`eventual_correct_rate` です。`hidden_repair_area_tap_rate_per_trial` は `trial_with_hidden_repair_area_tap_rate` の互換名に限り、新規報告では使いません。first accepted 系は diagnostic とし、採用判断の主根拠にしません。event は操作痕跡、trial は成否、participant は推論単位として順に集約します。

CSV は UTF-8 BOM です。session の JSON/CSV は `Application.persistentDataPath/InputUIComparison/v2/2.1.0/{session_id}`、複数参加者の集約は `Application.persistentDataPath/InputUIComparison/2.1.0/{experiment_type}` へ分離して保存します。`experiment_version` の異なる行は同じ自動集計へ混ぜません。

## 資料

- [修正前監査と v2.1 ギャップ表](Data/CurrentSpecificationAudit.md)
- [段階実施プロトコルと条件差分](Data/ExperimentProtocol.md)
- [問題セット・seed・counterbalance](Data/TrialSets/README.md)
- [CSV / Manifest / checkpoint データ辞書](Data/CSVDataDictionary.md)
- [指標の分子・分母と参加者単位分析](Data/AnalysisPlan.md)
- [P00 Android 実機予行](Data/Protocols/P00_Android_Rehearsal.md)
- [本番反映を検討する場合の境界](Data/ProductionIntegrationScope.md)
- [結果の保管規則](Results/README.md)

## 現在の判断

P00 と P01 以降の人間テスト結果がないため、UI の採用判断は未実施です。レイアウト検証・コンパイル・自動テストは実装成立の証拠であり、UX 優位性の証拠ではありません。現在は P00 前であり、version freeze も人間試験開始も未完了です。
