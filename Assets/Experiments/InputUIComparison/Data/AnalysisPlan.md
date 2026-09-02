# v2.1 分析計画

## 分析対象と順序

同一 `experiment_version=2.1.0`、同一 `experiment_type`、Manifest=`completed` の `is_practice=false`、`is_pilot=false` だけを対象にします。version、実験種別、段階の違う session を混ぜません。

分析単位は次の階層を守ります。

1. **event**：全 Pointer Down を分類し、trial flag と時刻を作るための操作痕跡。event 行を独立参加者として検定しない。
2. **trial**：first touch、誤入力有無、解決、timeout、RT を一試行一値へ縮約する。
3. **participant×condition**：trial 値から参加者ごとの率・中央値を作る。これが主要な比較単位である。
4. **within-participant difference**：同一実験・同一参加者内で reference との差を作る。
5. **group summary**：参加者値の median/min/max と改善方向人数を記述する。

event → trial → participant の順を飛ばして全 event や全 trial を人数のように扱いません。P01〜P06 は Placement だけを集計し、その後に State Assist anchor を暫定選択します。State Assist は別 session・最低別日、推奨別 cohort です。

reference は Placement=`Placement_UpperFixed`、State Assist=`Assist_AlwaysActive`、Rush=事前登録した 2 候補の reference 側です。`difference = comparison - reference`。成功率・正答率と target clarity / vehicle judgment / explainability は高い方、それ以外の error/rate/time と負担・過剰 assist 質問は低い方を改善とします。

以下で `T` は対象の計測 trial、`E` はその trial に属する全 Raw event です。eventはcheckpoint復元後もsession内で増加する`event_index`を主順序とし、旧行でindexがない場合だけ`event_time_ms`へfallbackします。分母0は0%にせず欠測とします。

## 判断可能時点と Covered

decision origin は Normal / Express / Broken では stimulus onset、Covered では reveal onset です。

- Covered 公開前 event は `premature` として別集計し、first raw、first accepted、RT の公開後系列へ入れない。
- Covered の first raw / first accepted は公開後の最初の該当 event を指す。
- Covered の RT は reveal onset を 0 ms とする。stimulus onset を原点にした値と混在させない。
- 公開前の衝動入力、公開後の判断エラー、公開後 timeout を一つの「Covered 見落とし」に合算しない。

実装列では、公開後は `actionable_time_ms>=0`、公開前は `actionable_time_ms<0` または明示された premature state で判定します。wall-clock ではなく同一 monotonic clock の時刻差を使います。

## 3 種の反応時間

trial ごとに次の 3 値を分けます。

| metric | trial 値 | 条件値 |
|---|---|---|
| `time_to_first_raw_touch_ms` | decision origin から最初の Raw Pointer Down まで。generic empty や disabled/hidden も first raw になり得る | 有効 trial 値の中央値 |
| `time_to_first_accepted_input_ms` | decision origin から最初の `was_input_accepted=true` まで | 有効 trial 値の中央値。diagnostic |
| `time_to_correct_resolution_ms` | decision origin から最初に正解入力が受理され trial が解決するまで（first correct resolution） | eventually correct trial 値の中央値。State Assist primary |

first correct resolution を表す canonical 名は `time_to_correct_resolution_ms` だけです。同じ概念の別 metric ID を新設しません。

`correction_cost_ms = time_to_correct_resolution_ms - time_to_first_raw_touch_ms` とします。decision origin 後に first raw があり、最終的に正解した trial だけが対象です。first raw がそのまま正解解決になれば 0 ms、拒否・誤答後に訂正した場合は正の値です。Covered 公開前 Raw event はこの差の起点にせず、premature 系へ分離します。

## State Assist primary outcomes

State Assist の primary は次の 7 指標だけです。条件・分子・分母・欠測規則を version freeze 後に変更しません。

| metric | 分子／trial 値 | 分母／条件値集合 |
|---|---|---|
| `first_raw_touch_error_rate` | decision origin 後の first raw の `raw_input` が expected input と異なる trial 数。generic empty=`None` を含む | decision origin 後の first raw がある trial 数 |
| `trial_with_disabled_repair_tap_rate` | 非 Broken 中に、Repair Rect 内で visible=true、interactable=false、`ignored_reason=disabled` の event が 1 件以上ある trial 数 | 条件内の全 trial 数 |
| `trial_with_hidden_repair_area_tap_rate` | 非 Broken 中に、現在の予約 Repair Rect 内で visible=false の event が 1 件以上ある trial 数 | 条件内の全 trial 数 |
| `time_to_correct_resolution_ms` | decision origin から first correct resolution までの trial 値 | eventually correct trial 値集合の中央値 |
| `correction_cost_ms` | first raw touch から first correct resolution までの trial 値 | 上記差を算出できる eventually correct trial 値集合の中央値 |
| `timeout_rate` | `is_timeout=true` の trial 数 | 全 trial 数 |
| `eventual_correct_rate` | timeout 前に最終的に正解した trial 数 | 全 trial 数 |

`hidden_repair_area_tap_rate_per_trial` は `trial_with_hidden_repair_area_tap_rate` と同じ numerator / denominator を指す互換・派生名です。新規 CSV、表、本文、結論では canonical 名を使い、両名を別指標として二重計上しません。

`first_accepted_input_error_rate`、`time_to_first_accepted_input_ms`、event-level tap rates は diagnostic です。受理されない disabled/hidden 操作を first accepted だけでは失うため、primary へ昇格させません。

## hidden／generic empty の分離と stale Repair の重ね合わせ

`hidden_or_empty` は、まず次の二つへ分けます。

1. **hidden Repair area**：current Repair Rect 内、`was_button_visible=false`。直前Broken後の時間的staleもここに含める。
2. **generic empty**：`ignored_reason=hidden_or_empty` で、current / previous Repair Rect のどちらにも入らない。

**stale Repair** は別のdiagnostic overlayです。Repair入力／領域で `ignored_reason=stale_target`、previous Repair Rect内かつcurrent Rect外、または「直前trialのexpected inputがRepairで、次の非Repair trialにhidden Repair areaをtap」のいずれかを数えます。最後のケースが、Broken後にボタンが消えても同じ場所を押し続けた時間的staleです。Rushのstale車種入力は含めません。hiddenと時間的staleは意図的に重複するため、三つのrateを足し合わせません。

関連する diagnostic は次です。

| metric | 分子 | 分母 |
|---|---|---|
| `stale_repair_area_tap_event_rate` | target／旧Rect／直前Broken後の時間的stale Repair event 数 | 全 Raw event 数 |
| `hidden_repair_area_tap_event_rate` | hidden Repair area event 数 | 全 Raw event 数 |
| `empty_area_tap_event_rate` | generic empty event 数 | 全 Raw event 数 |
| `disabled_tap_event_rate` | `ignored_reason=disabled` の Raw event 数 | 全 Raw event 数 |
| `cooldown_tap_event_rate` | `ignored_reason=cooldown` の Raw event 数 | 全 Raw event 数 |
| `same_frame_overwritten_tap_event_rate` | `ignored_reason=same_frame_overwritten` の Raw event 数 | 全 Raw event 数 |

旧 `empty_area_tap_rate` は hidden と generic empty を混在し得るため、新規判断には使用しません。互換出力が残る場合も、上記二分類を併記し、合算値から原因を説明しません。event rate は連打量に影響されるため diagnostic に留め、trial-level primary と置き換えません。

## first accepted diagnostic

| metric | 分子 | 分母 |
|---|---|---|
| `first_accepted_input_error_rate` | decision origin 後の first accepted が expected input と異なる trial 数 | decision origin 後の accepted input が 1 件以上ある trial 数 |

first accepted は仲裁後のゲーム入力を説明する diagnostic です。disabled/hidden/generic empty、cooldown、same-frame overwritten を観測しないため、State Assist の主要結論には使いません。

## Broken

| metric | 分子 | 分母 |
|---|---|---|
| `broken_first_accepted_error_rate` | Broken の first accepted が Repair でない trial 数 | first accepted がある Broken trial 数 |
| `broken_timeout_rate` | timeout した Broken trial 数 | Broken trial 数 |
| `broken_vehicle_button_tap_rate` | Broken 中に Sports / Compact / LightTruck を raw input とした event 数 | Broken trial の全 Raw event 数 |
| `broken_repair_success_rate` | eventual correct かつ first correct resolution が Repair の Broken trial 数 | Broken trial 数 |

Broken への車種 tap は特殊状態の見落とし候補ですが、disabled、hidden、generic empty、cooldown とは別に残します。

## Covered

| metric | 分子 | 分母／値集合 |
|---|---|---|
| `covered_premature_raw_touch_rate` | reveal 前 Raw event が 1 件以上ある Covered trial 数 | Covered trial 数 |
| `covered_premature_accepted_input_rate` | reveal 前 accepted event が 1 件以上ある Covered trial 数 | Covered trial 数 |
| `covered_post_reveal_error_rate` | reveal 後 first accepted が expected input と異なる trial 数 | reveal 後 accepted input がある Covered trial 数 |
| `covered_post_reveal_timeout_rate` | reveal 後も正解せず timeout した Covered trial 数 | reveal に到達した Covered trial 数 |
| `covered_time_to_first_raw_touch_ms` | reveal から first post-reveal Raw まで | 有効 trial 値の中央値 |
| `covered_time_to_correct_resolution_ms` | reveal から first correct resolution まで | eventually correct Covered trial 値の中央値 |

premature 指標と post-reveal 指標を同じ numerator に入れません。Covered の RT に stimulus onset 基準を代用しません。

## Express

| metric | 分子 | 分母／値集合 |
|---|---|---|
| `express_accuracy` | first accepted が expected 車種と一致する Express trial 数 | first accepted がある Express trial 数 |
| `express_response_time_ms` | decision origin から first accepted correct まで | 有効 trial 値の中央値 |
| `express_timeout_rate` | timeout した Express trial 数 | Express trial 数 |

Express は Normal と正解入力が同じなので、入力ログだけから認識有無を断定しません。`express_miss_rate` や「Express 見落とし率」は生成しません。

## 細分 cell の扱い

vehicle×state×speed×condition、特に High、Broken、Covered の細分 cell は件数が小さいため **danger signal 専用**です。局所悪化、端末依存、floor/ceiling、実装異常を見つけるために確認しますが、次を禁止します。

- 細分 cell だけで優位性、因果効果、anchor 採用を決める。
- cell 内 trial/event を独立参加者として扱う。
- 事後的に良い cell だけを primary へ昇格する。
- danger signal が出た cell を全体中央値で打ち消す。

signal が出た場合は個別 event、動画／観察記録、端末、順序を監査し、必要なら次段階を止めます。

## アンケート指標

各条件について、有効な 1〜5 回答の中央値を次として出力します。numerator は空、denominator は有効回答数、value は中央値です。

```text
survey_target_clarity_median
survey_searched_for_button_median
survey_judged_repair_need_from_vehicle_median
survey_ui_state_revealed_answer_median
survey_can_explain_mistake_median
survey_meaning_changed_median
survey_most_confusing_moment_median
survey_broken_too_easy_median
```

短答は数値へ自動変換せず、`survey_coding_template.csv` へ question id、原文、analyst code、coder note を残します。状態アシスト成績と Q4/Q8・短答が逆方向なら trade-off として報告します。

## 出力テーブル

- `summary_participant_condition.csv`：各参加者・条件・metric の numerator / denominator / value。
- `summary_participant_difference.csv`：reference/comparison 値、差、improved/worsened/unchanged。
- `summary_group.csv`：`participant_count`、`participant_metric_median`、`participant_metric_min`、`participant_metric_max`、`improved_participant_count`、`worsened_participant_count`、`unchanged_participant_count`。

複数完了 session の現行集約は、同じ participant ID・実験種別では最新 completed session を使い、P00、練習、途中終了、条件不足、別 version/type を除外します。再測定を許す条件、旧 session の除外理由、匿名 ID の扱いはデータ閲覧前に固定し、「都合の良い最新値」へ差し替えません。

## 判断ガードレール

6 人程度の予備試験では統計的有意差や母集団効果を断定しません。「相対 20% 改善」だけでも採用しません。最低限、次を同時に確認します。

- 6 人中 4 人以上で primary の改善方向が一致するか。
- 1 人の大量 tap / 大量ミスが group 値を支配していないか。
- 7 primary 間で error 改善と `time_to_correct_resolution_ms` / `correction_cost_ms` 悪化が交換条件になっていないか。
- High、Broken、Covered の danger signal がないか。
- State Assist が答えを教えすぎ、Broken を見分けるゲーム性を損なっていないか。
- Rush package 差を配置または assist 単独の効果と誤解していないか。

Placement の結論は `UpperFixed` と `BottomFixed` の二固定位置間だけです。State Assist 後に Rush 候補 2 案を事前登録し、Rush は候補の最終反証として使います。参加者データがない現在、どの条件も推奨しません。
