# v2.1 データ辞書

## 共通規則

- `schema_version` は `2`、`experiment_version` は現在 `2.1.0`。
- CSV は UTF-8 BOM、RFC 4180 相当の quote/newline escape、atomic replace で更新する。
- 時刻は millisecond、画面座標は左下原点の pixel と画面全体に対する normalized 値を併記する。
- enum / condition / reason はこの文書の固定 ID を使い、表示用日本語を分析キーにしない。
- `is_practice=true` と `is_pilot=true` は主要分析から除外する。
- version または `experiment_type` が違うデータを同じ自動集計へ混ぜない。

## `*_input_events.csv`

1 物理 Pointer Down につき必ず 1 行です。Button callback は Raw 行の生成源ではなく、Input System の global capture と後段の UI hit test / input arbitration を分離します。正確な header と例は [Schemas/input_events_v2.csv](Schemas/input_events_v2.csv) にあります。

### 識別・順序

| 列 | 定義 |
|---|---|
| `schema_version` | データ構造 version |
| `experiment_version` | 実験設計 version |
| `participant_id` / `session_id` | 匿名参加者と 1 実行の ID |
| `experiment_type` | `placement` / `state_assist` / `rush_package` |
| `condition` / `condition_order` | 条件 ID と 1 始まりの提示位置 |
| `trial_set_id` / `trial_number` | `Set01`〜`Set03` と set 内番号 |
| `pointer_id` | touch id。mouse=`-1`、pen=`-2` |
| `event_time_ms` | Raw source と仲裁で共有するprocess内monotonic clockのms値。force-stop再開では新しいepochになる |
| `event_index` / `frame` | session内イベント連番とUnity frame。`event_index`はcheckpoint復元後も継続し、分析の主順序になる |
| `is_pilot` / `is_practice` | P00、練習の除外フラグ |

### 画面・hit

| 列 | 定義 |
|---|---|
| `screen_x_px`, `screen_y_px` | Pointer Down の物理画面座標 |
| `screen_x_normalized`, `screen_y_normalized` | `x/screen_width`, `y/screen_height` |
| `screen_width`, `screen_height` | event 時の画面 pixel |
| `safe_area_x`, `safe_area_y`, `safe_area_width`, `safe_area_height` | event 時の `Screen.safeArea` |
| `ui_scale_factor` | event 時 Canvas scale factor |
| `hit_ui_element` | raycast 最上位 UI 名。空白 tap は空文字になり得る |
| `hit_button_id` | 実験 control ID。button でなければ空文字 |
| `was_button_visible` | touch 時の control 可視性 |
| `was_button_interactable` | touch 時の input 可否 |
| `raw_input` | hit から得た `Sports` / `Compact` / `LightTruck` / `Repair` / `None` |
| `pointer_position` / `button_center_position` | legacy-compatible `x|y` normalized 表現 |
| `visually_enabled` | visual state の補助列 |
| `distance_to_expected_button_px` | hit control center と期待 control center の距離。算出不能は負値 |

### 受理・target 帰属

| 列 | 定義 |
|---|---|
| `was_input_accepted` | ゲーム判定へ渡した 1 行だけ `true` |
| `accepted_input` | 受理値。非受理は `None` |
| `ignored_reason` | 下表の排他的な主理由。受理時は `none` |
| `expected_input` | touch 時試行の期待入力 |
| `target_vehicle_id_at_touch` | Raw 捕捉時の target |
| `target_vehicle_id_at_accept` | frame 仲裁時の target |
| `resolved_vehicle_id` | 実際に入力を適用した車両。非受理は空 |
| `target_changed_between_touch_and_accept` | touch と accept 候補評価時で target ID が異なる |
| `is_inside_current_repair_rect` | touch 時 Repair Rect 内か |
| `is_inside_previous_repair_rect` | 直前 Repair Rect 内か |
| `previous_repair_rect_age_ms` | Rect 変更から touch まで。履歴なしは空 |
| `disposition` | `accepted` または `ignored_reason` の互換列 |
| `stimulus_time_ms` | 現試行提示から down まで |
| `actionable_time_ms` | decision origin から down まで。Covered では reveal 後だけ値を持ち、公開前は空 |
| `timestamp` | UTC ISO-8601 wall-clock timestamp |

### `ignored_reason`

判定優先順位は、固定拒否を先に適用し、有効候補が複数ある場合に同一フレーム上書きを付けます。

| 値 | 意味 |
|---|---|
| `none` | この行が受理された、または拒否理由なし |
| `disabled` | 可視だが interactable=false の control |
| `hidden_or_empty` | invisible control / 予約領域、または button 外。分析時は下記の geometry 列で hidden Repair と generic empty を分離する |
| `cooldown` | 直前の受理 input から 80 ms 未満 |
| `same_frame_overwritten` | 同 frame に複数の有効候補があり、後の候補が採用された |
| `trial_not_actionable` | 解決中、gap 中など、試行が input を受けられない |
| `covered_not_revealed` | Covered 公開前領域への非受理 tap として分類された |
| `stale_target` | touch 後、仲裁前に target が変わった。次 target へ適用しない |
| `session_not_running` | setup、survey、complete など session/gameplay 外 |

Covered 公開前の有効な車種／Repair control は、本番ルールに合わせて誤答として受理し得ます。その場合は `was_input_accepted=true` であり、`covered_premature_accepted_input_rate` に含めます。`covered_not_revealed` と一括しません。

### hidden / generic empty / stale Repair の派生分類

`ignored_reason` の文字列だけで `hidden_or_empty` を一つの行動へまとめません。hidden Repair areaはcurrent Repair Rect内かつ`was_button_visible=false`、generic emptyは`hidden_or_empty`かつcurrent / previous Repair Rect外です。

stale Repairは別のdiagnostic overlayで、Repair入力／領域の`stale_target`、previous Repair Rect内かつcurrent Rect外、または直前trialのexpected inputがRepairで、その次の非Repair trialにhidden Repair areaをtapしたeventを含みます。最後のケースにより「Broken後にボタンが消えても同じ場所を押した」を集計できます。Rushのstale車種入力は含めません。時間的staleはhiddenにも含まれるため、各rateを合算しません。

`trial_with_hidden_repair_area_tap_rate` が trial-level primary です。`hidden_repair_area_tap_rate_per_trial` は同じ分子・分母を指す互換名であり、別指標として二重計上しません。

event-level diagnostic は `hidden_repair_area_tap_event_rate`、generic empty の `empty_area_tap_event_rate`、`stale_repair_area_tap_event_rate` です。対応する trial-level 値は `trial_with_hidden_repair_area_tap_rate`、`trial_with_empty_area_tap_rate`、`trial_with_stale_repair_area_tap_rate` で、いずれも「該当 event が1件以上あるdistinct trial数 ÷ 条件内の全trial数」です。`generic_empty_area_tap_rate_per_trial` と `stale_repair_area_tap_rate_per_trial` はそれぞれ対応する trial-level 値の互換名です。

## `*_trials.csv`

1 試行 1 行です。識別列に加え、vehicle/state/speed、Covered reveal progress、gap、seed、expected/first/final input、first correctness、eventual correctness、timeout、stimulus/actionable/final RT、retry と各拒否数を保存します。分析では `time_to_first_raw_touch_ms`、`time_to_first_accepted_input_ms`、`time_to_correct_resolution_ms`（first correct resolution）の 3 種へ明示的に派生し、混在させません。Covered は reveal を decision origin とし、公開前 event は premature 系へ分離します。`legacy_variant`、`legacy_variant_order`、`problem_set` は v1 checkpoint 可読性のための互換列で、v2.1 分析キーには `experiment_type`、`condition`、`trial_set_id` を使います。

## `*_survey.csv`

1 条件 1 行です。8 問それぞれの `*_rating`（1〜5）と `*_short_answer`、`analyst_explanation_code` を保存します。`legacy_variant`、`legacy_problem_set`、`legacy_noticed_special_state` は互換列であり、新規分析の主変数にしません。

## 分析 CSV

event は操作痕跡、trial は試行結果、participant×condition は比較・結論の単位です。event / trial 行を独立参加者として集計しません。

| ファイル | 行の単位 | 主な列 |
|---|---|---|
| `summary_participant_condition.csv` | 参加者×session×条件×metric | `numerator`, `denominator`, `value` |
| `summary_participant_difference.csv` | 同一参加者の reference 対 comparison×metric | 両値、`difference=comparison-reference`、改善/悪化/不変 |
| `summary_group.csv` | version×type×condition×metric | participant count、参加者値の median/min/max、改善/悪化/不変人数 |

State Assist の primary metric ID は次の 7 個です。

```text
first_raw_touch_error_rate
trial_with_disabled_repair_tap_rate
trial_with_hidden_repair_area_tap_rate
time_to_correct_resolution_ms
correction_cost_ms
timeout_rate
eventual_correct_rate
```

`first_accepted_input_error_rate` と `time_to_first_accepted_input_ms` は diagnostic です。vehicle×state×speed×condition の細分 cell は danger signal として監査し、単独で結論や候補選択に使いません。

## `session_manifest.json`

Session 開始時に作成し、snake_case JSON で次を保存します。

```text
schema_version
experiment_version
participant_id
session_id
is_pilot
device_model
platform
screen_resolution
safe_area
unity_version
build_version
git_commit
experiment_type
condition_order
trial_set_order
seed_by_condition[{condition, trial_set_id, seed}]
repair_anchor_mode
repair_assist_mode
vehicle_button_layout_mode
rush_protocol_id
reference_condition_id
condition_definitions[{condition_id, repair_anchor_mode, repair_assist_mode, vehicle_button_layout_mode}]
started_at
completed_at
completion_status
```

`git_commit` を取得できない場合は `unknown`。`completion_status` は `in_progress` / `interrupted` / `completed` です。`completed_at` は completed 時だけ設定します。

## checkpoint

`checkpoint.a.json` と `checkpoint.b.json` を revision ごとに交互保存します。外側 envelope は `revision`、schema/version/participant/session identity、`payload_sha256`、`payload_json` を持ちます。読み込み時は SHA-256、revision、identity、version を検証し、有効な最新 slot だけを復元します。

payload は phase、condition/trial/event index、score/misses、経過時間、残 gap、random state、current target、Covered/resolve state、全 trial plan、active vehicle state、完了済み trial、全 Raw event、未仲裁 event、survey を含みます。atomic temporary write と交互 slot により、途中終了で 1 slot が壊れても直前 revision を残します。

`raw_events.wal.jsonl` は capture-time の Raw 行を gameplay 仲裁より前に 1 行ずつ `WriteThrough` + `Flush(true)` で同期追記します。各行は schema/version/participant/session/event index と payload SHA-256 を持ちます。復元時は malformed な末尾、identity不一致、checksum不一致を除外し、`event_index` でdedupeします。checkpointに同じindexの確定行があればcheckpointを正本とし、WALだけにある未仲裁行は `stale_target`（trial外は`session_not_running`）として1行だけ復元し、再開後の車両へ適用しません。

trial解決は完了行、次`trial_index`、残gapを一つのcheckpointへまとめます。最終trial後の`trial_index == active_trials.Count`はSurveyへ進み、解決済みtrialを再生成しません。Covered公開、trial開始、条件layout変更も遷移直後にcheckpointします。完了はcheckpointを先に、Manifestを後に保存し、二つの間でforce-stopした場合は次回探索が完了checkpointからManifestの`completion_status` / `completed_at`だけを自己修復して再開候補から除外します。提出済みsurveyも次blockへ進む前に保存し、再開時に二重追加しません。

## P00 と version の除外

`participant_id=P00` は schedule と全行で `is_pilot=true` になります。participant summary は `is_pilot` と `is_practice` を除外し、複数 session 集約は Manifest の `is_pilot` または P00 ID を再確認します。P00 を人間一般の UX 指標へ手動で足してはいけません。

P00 PASS 後に build、Git commit、条件、metric、除外規則を `experiment_version=2.1.0` として freeze します。P01〜P06 は Placement だけ、State Assist はその参加者単位集計後の別 session・最低別日、Rush は State Assist 後に事前固定した 2 候補だけです。段階や version の異なるデータを連結しません。
