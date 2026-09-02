# 入力 UI 比較実験プロトコル v2.1

## 共通方針

Placement、State Assist、Rush Package は別 session・別段階で実行します。Placement と State Assist は一つの主要因だけを変える因果比較、Rush Package は複数要素を含む完成パッケージの最終反証です。実験種別をまたいだ数値の単純比較や、Rush の差を個々の要因へ分解する解釈は禁止します。

全条件で次を共通にします。

- 3 車種、Normal / Express / Covered / Broken、Low / Normal / High の quota
- 車両サイズ、走行距離、速度、Covered 公開、timeout、gap
- 正解・誤答・retry・得点・feedback・HUD・背景
- 練習 4 試行、計測 24 試行
- Pointer Down 判定、最後の物理入力だけを対象にする同一フレーム仲裁（最後が無効でも先行入力へフォールバックしない）、0.08 秒 cooldown
- 効果音なし、派手な状態別アニメーションなし
- Safe Area 内の 1080×1920 reference frame

ゲームルールは UI 条件で変えません。Normal / Express / Covered 公開後は車種ボタン、Broken は Repair が正解です。Covered 公開前の入力は本番同様に誤答として受理され得ます。誤答後も期限内なら再入力できます。

## 段階実施とfreeze gate

実施順は次に固定します。

1. `P00` で全技術予行を行い、11 項目の機械合格条件と端末・cutout matrixを完了する。
2. blocking defect を解消後、build、Git commit、`experiment_version=2.1.0`、条件、問題セット、指標、除外規則を version freeze する。
3. `P01`〜`P06` では Placement だけを実施する。
4. Placement を参加者×条件で集計し、二固定位置のどちらを State Assist へ持ち込むか暫定決定する。
5. State Assist を別 session かつ最低でも別日に実施する。学習持越しを避ける推奨設計は別 cohort である。
6. State Assist の参加者単位結果とアンケートを確認し、Rush へ送る完成パッケージを 2 候補に固定して事前登録する。
7. Rush ではその 2 候補だけを比較し、高密度・対象切替条件で候補が破綻しないか最終反証する。

Placement、State Assist、Rush の同日まとめ実施は禁止します。P00 を部分実施したまま version freeze や P01 へ進むこと、Placement の参加者単位集計前に State Assist の anchor を固定すること、Rush データを見ながら候補を入れ替えることも禁止します。

## 実験 1：Placement

比較する主要因は Repair の固定位置だけです。どちらも target 追従ではありません。

### ワイヤーフレーム

```text
Placement_UpperFixed              Placement_BottomFixed

      [判定対象]                       [判定対象]
        [整備]
                                          [整備]
[Sports][Compact][Truck]         [Sports][Compact][Truck]
```

### 正確な条件差分

座標は 1080×1920 reference frame、全 control の anchor `(0,0)`、pivot `(0.5,0.5)` です。

| 要素 | `Placement_UpperFixed` | `Placement_BottomFixed` | 一致／差 |
|---|---|---|---|
| Repair center | 固定 `(540,800)` | 固定 `(540,462)` | **唯一の操作因** |
| Repair size | `470×112` | `470×112` | 一致 |
| Repair label / font | `整備` / 27 | `整備` / 27 | 一致 |
| Repair visual style | `neutral_ugui_control_v2` | 同左 | 一致 |
| Sports | center `(211,190)`, `308×315` | 同左 | 一致 |
| Compact | center `(540,190)`, `308×315` | 同左 | 一致 |
| LightTruck | center `(869,190)`, `308×315` | 同左 | 一致 |
| Repair 表示 | 全状態で `alpha=1` | 同左 | 一致 |
| Repair 入力 | 全状態で interactable / raycast 有効 | 同左 | 一致 |
| 非 Broken で Repair | 受理して通常の誤答 | 同左 | 一致 |
| ルール・車両・問題・feedback | 共通 | 共通 | 一致 |

Placement の結論は、この画面構成・サイズ・端末群での `UpperFixed` 対 `BottomFixed` に限定します。「target 付近が良い」「contextual placement が良い」「上段なら一般に良い」とは結論しません。固定 4 枠の旧 A は車種ボタン幅まで変えるため Placement へ入れません。

## 実験 2：State Assist

比較する主要因は、Repair control が Broken をどの程度通知するかだけです。これは配置実験ではなく、難易度へ影響し得る状態アシスト実験です。

P01〜P06 Placement の参加者単位集計後に、`RepairAnchorMode.UpperFixed` または `RepairAnchorMode.BottomFixed` を暫定選択します。State Assist は Placement とは別 session・最低別日に実施し、推奨は別 cohort です。選択 anchor は Manifest と事前登録へ記録し、3 条件すべてで固定し、Session 開始後は変更しません。

### 正確な条件差分

| 条件 | 非 Broken | Broken | 予約 Rect | ゲーム上の扱い |
|---|---|---|---|---|
| `Assist_AlwaysActive` | `alpha=1`, interactable=`true`, blocksRaycasts=`true` | 同左 | 保持 | 非 Broken Repair も受理し誤答 |
| `Assist_DisabledWhenNormal` | `alpha=1`, interactable=`false`, blocksRaycasts=`true` | `alpha=1`, interactable=`true`, blocksRaycasts=`true` | 保持 | disabled tap は非受理だが Raw 記録 |
| `Assist_BrokenOnly` | `alpha=0`, interactable=`false`, blocksRaycasts=`false` | `alpha=1`, interactable=`true`, blocksRaycasts=`true` | 保持 | hidden Repair 予約領域 tap は非受理だが Raw 記録 |

| 統制要素 | 3 条件の値 |
|---|---|
| Repair RectTransform | 暫定選択した固定 anchor の同一 `470×112` Rect |
| 車種 RectTransform | Sports `(211,190)`、Compact `(540,190)`、LightTruck `(869,190)`、各 `308×315` |
| style / label | Placement と同じ neutral style、同じ文字・色・font・pressed scale |
| 車両、問題セット、速度、判定 | 同一 schedule contract |
| feedback / HUD / 背景 / sound | 同一 |

`Assist_BrokenOnly` は GameObject を削除せず、CanvasGroup 相当の状態だけを変えます。周囲の layout は移動しません。

State Assist の primary outcome は次の 7 指標です。

```text
first_raw_touch_error_rate
trial_with_disabled_repair_tap_rate
trial_with_hidden_repair_area_tap_rate
time_to_correct_resolution_ms
correction_cost_ms
timeout_rate
eventual_correct_rate
```

`time_to_correct_resolution_ms` は first correct resolution までの時間です。`hidden_repair_area_tap_rate_per_trial` は `trial_with_hidden_repair_area_tap_rate` の互換名に限り、新規の表・結論では canonical 名だけを使います。first accepted 系は diagnostic であり primary ではありません。

## 実験 3：Rush Package

本番に近い高密度状況で、事前登録した完成パッケージ 2 候補が破綻しないか確認します。原因を説明する実験でも、探索しながら候補を選ぶ実験でもありません。

実装は `RepairAnchorMode`、`RepairAssistMode`、`VehicleButtonLayoutMode` の組合せと既存 3 preset を保持します。P00 では 3 preset の技術動作を確認します。人間対象 Rush では State Assist の分析完了後に、Controller Inspector の `Human Rush Preregistration` へ protocol ID、reference condition、相異なる2候補のIDと3構成値を固定します。非P00 Rushは有効な2候補登録がなければ開始を拒否し、登録候補だけをAB/BA提示します。Manifestにはprotocol、reference、2候補定義をsnapshotし、集計もManifestを正本にします。候補の構成、reference、除外規則、AB/BA順はデータ閲覧前に固定します。

| P00 で確認可能な preset | 旧称 | anchor / Repair geometry | assist | 車種 layout | 解釈 |
|---|---|---|---|---|---|
| `Rush_CurrentEquivalent` | Baseline / 現行相当 | BottomFixed、center `(540,462)`, `470×112` | AlwaysActive | StandardThree、各 `308×315` | 完成 package |
| `Rush_FixedFourEquivalent` | A / 固定 4 枠 | Repair center `(940,175)`, `240×260` | DisabledWhenNormal | FixedFour。車種 center `(140,175)`, `(405,175)`, `(675,175)`、各 `240×260` | 完成 package。配置・全幅・assist が混在 |
| `Rush_ContextualRepairEquivalent` | B / 旧コンテキスト表示 | UpperFixed、center `(540,800)`, `360×128` | BrokenOnly | StandardThree、各 `308×315` | 完成 package。位置・サイズ・assist が混在 |

Rush では画面内に 2〜3 台を同時表示し、短い 0.22 秒 gap、対象切替、近接した Broken/Normal、Express の速度差、直前 target 向け入力の残留を再現します。Raw 行には必ず次を残します。

- `target_vehicle_id_at_touch`
- `target_vehicle_id_at_accept`
- `resolved_vehicle_id`
- `target_changed_between_touch_and_accept`

touch と accept の対象が違うイベントは `stale_target` として拒否し、次車両へフォールスルーさせません。Rush の結果は候補パッケージ全体の反証材料であり、「位置が効いた」「assist が効いた」という因果説明には使いません。

## Raw Pointer Down とゲーム入力

1. Input System の EnhancedTouch、Mouse、Pen から物理 down を取得する。
2. 物理 down ごとに Raw 行を 1 行生成し、画面座標、Safe Area、UI hit、touch 時 target を保存する。
3. 同一フレームの候補をゲーム判定とは別の仲裁器へ渡す。
4. session、visibility、interactable、trial、target、cooldown を順に確認する。
5. 有効候補が複数なら最後だけを受理し、他を `same_frame_overwritten` とする。
6. 受理／無視の結果を同じ Raw 行へ追記する。Button callback から別行を作らない。

次の三つを合算しません。

- hidden Repair：非表示だが予約された現在の Repair Rect 内
- generic empty：現在・直前 Repair Rect ではない button 外
- stale Repair：直前 Repair Rect 内、target切替で`stale_target`になったRepair入力、またはBroken直後の非Repair trialでhidden予約領域を押した時間的stale

cooldown、同一フレーム上書き、session 外も別 reason のまま保持します。event 率は操作痕跡、trial 率は試行への影響、participant 値は比較・結論の単位です。

## 参加者手順

1. その段階で許可された一つの実験種別だけを実施する。P01〜P06 は Placement 専用である。
2. 各条件前に Normal / Broken / Covered / Express を 1 件ずつ練習する。練習行には `is_practice=true` を付け、主要集計から除く。
3. 計測 24 試行を行う。誤答後も再入力できること、Covered は公開を待つことを共通説明する。
4. 条件終了後に 8 問へ 1〜5（1=全くそう思わない、5=非常にそう思う）で回答し、短い理由・具体例も保存する。
5. 全条件終了後にだけ完了画面を表示する。

内部条件名、seed、前条件の成績、期待仮説は参加者へ見せません。別段階を同日に追加実施しません。

## 条件後アンケート

| # | question id | 質問 |
|---:|---|---|
| 1 | `target_clarity` | 判定対象がすぐに分かったか |
| 2 | `searched_for_button` | 押す場所を探したか |
| 3 | `judged_repair_need_from_vehicle` | 整備が必要かどうかを車両から判断したか |
| 4 | `ui_state_revealed_answer` | ボタンの表示や有効状態が答えを教えたと感じたか |
| 5 | `can_explain_mistake` | ミスした理由を説明できるか |
| 6 | `meaning_changed` | ボタンの意味が途中で変わったと感じたか |
| 7 | `most_confusing_moment` | 最も迷った場面はどこか |
| 8 | `broken_too_easy` | この UI では Broken を見分けるゲーム性が簡単になりすぎたと感じるか |

5 段階値だけでなく `*_short_answer` も保持します。「該当なし」は空欄にせず「なし」と入力します。State Assist で成績が改善しても、Q4/Q8 と自由回答がゲーム性低下を示す場合は、性能改善とアシスト過剰の trade-off を別々に報告します。

## セッション完了条件

- `completed`：その session に許可された全条件、全計測試行、条件後アンケートを保存した。
- `interrupted`：アプリ終了、pause、force-stop などで完了前に終了した。checkpoint から再開対象。
- `in_progress`：開始済みで、まだ完了も中断確定もしていない。

P00 は実装・端末・保存経路の予行専用です。P00 の性能値を一般 UX 結果へ含めず、P00 の成功を UI 推奨根拠にしません。
