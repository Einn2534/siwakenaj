# P00 Android 実機予行 — v2.1 freeze gate

P00 は制作者による技術予行であり、人間一般の UX データではありません。`participant_id=P00` と `is_pilot=true` を必ず確認し、P01 以降の集計へ含めません。P00 の PASS を UI 優位性として報告しません。

P00 の全項目が PASS し、blocking defect を修正した build / Git commit / protocol を固定するまで `experiment_version=2.1.0` を人間試験用に freeze しません。freeze 前に P01 を開始してはいけません。

## 事前条件

- Unity `6000.3.10f1` と Android Build Support を用意する。
- Android 実機で USB debugging を有効にし、端末時刻、空き容量、解像度、Android version、cutout / gesture inset を記録する。
- `Tools > しわ賢者 > Input UI Comparison > Open Experiment Scene` で独立 Scene を開く。
- Android の一時 Build Profile では `Assets/Experiments/InputUIComparison/Scenes/InputUIComparison.unity` だけを起動 Scene にする。既存 Build Settings の変更は commit しない。
- Development Build で build/install する。application id を Build Profile で上書きした場合は実値を記録する。
- 実行前に対象端末の旧 P00 出力を別場所へ退避し、新 session ID を確認できる状態にする。
- build version、Git commit、`schema_version=2`、`experiment_version=2.1.0` をチェックシートへ転記する。

## 必須の端末・cutout matrix

最低構成は **Android 実機 1 台 + 異なる cutout 構成 1 件**です。異なる cutout 構成は Android Emulator または第 2 実機で確認します。解像度変更だけでは cutout 差の代用になりません。

| 環境 | 必須 | 記録・確認 | 完了 |
|---|---|---|---|
| 主 Android 実機 | 必須 | model class、Android、実解像度、Safe Area、cutout、gesture navigation、touch | [ ] |
| 異 cutout Android Emulator | Emulator または第2実機の一方が必須 | 主実機と異なる notch / hole-punch / no-cutout profile、Safe Area、touch injection | [ ] |
| 第 2 Android 実機 | Emulator または本行の一方が必須。両方を推奨 | 異なる aspect / cutout / touch digitizer、Safe Area、実タッチ | [ ] |

各環境について 1080×1920、1080×2340、720×1600 相当の少なくとも該当する case を確認し、3 case 全体を matrix 内で網羅します。`adb shell wm size` は pixel 比の補助確認には使えますが、cutout 検証には使いません。override 後は `adb shell wm size reset` で戻します。

## 実施順

1. P00 の Placement、State Assist、Rush をそれぞれ別 session で起動する。
2. Placement は `Placement_UpperFixed` / `Placement_BottomFixed`、State Assist は disabled / hidden / Broken、Rush は既存 3 preset を技術確認する。
3. 各操作直前に environment、session、condition、trial、target、期待する分類を記録する。
4. 下の機械合格条件 11 項目を主実機で実施し、画面・cutout 項目を matrix の追加環境で反復する。
5. session を完了する場合は 8 問と短答を保存し、Manifest=`completed` を確認する。中断試験では `interrupted` と再開を確認する。

P00 は技術検証のため既存 3 Rush preset を確認しますが、これを人間 Rush の 3 条件比較計画と解釈しません。人間 Rush は State Assist 後に事前登録する 2 候補だけです。

## 機械合格条件 — 11項目

すべて必須です。各行に `PASS` / `FAIL` / `NOT_REPRODUCED`、証拠 path、event index、備考を残します。`NOT_REPRODUCED` は PASS ではありません。

| # | 操作 | 機械合格条件 | 結果 |
|---:|---|---|---|
| 1 | Android 実機で Scene を開始し session を完了 | setup→練習→計測→survey→complete が例外なく遷移。Manifest は schema=`2`、experiment=`2.1.0`、`is_pilot=true`、端末・Safe Area・順序・set・seed を持ち、完了時 `completed` | [ ] |
| 2 | 通常 button を物理的に 1 回 tap | `input_events.csv` がちょうど 1 行増える。Button callback や合成 Mouse による二重行がない | [ ] |
| 3 | `Assist_DisabledWhenNormal` の非 Broken 中に Repair を tap | Raw 行あり、Repair hit、visible=true、interactable=false、accepted=false、reason=`disabled` | [ ] |
| 4 | `Assist_BrokenOnly` でBrokenをRepair解決した直後の非Broken予約Rectと、Rect外の空白を別々にtap | 両方1行ずつ。前者はhidden Repair areaかつ時間的stale Repair、後者はgeneric emptyとして分離でき、どちらも非受理。layoutは移動しない | [ ] |
| 5 | 受理 tap 直後 80 ms 内に連打 | 全物理 tap が各 1 行で残り、cooldown 対象だけが reason=`cooldown` | [ ] |
| 6 | 同一 frame で「有効→無効」と「無効→有効」の順に 2 本指 down | 全 down が別行。最後の物理入力より前は `same_frame_overwritten`。最後が有効な場合だけ受理し、最後が無効なら固有の拒否理由を保持して先行する有効入力へフォールバックしない | [ ] |
| 7 | Covered公開前と公開後にtap。追加環境では自動日時を一時OFFにしてwall clockを手動変更（Emulatorはadb操作でも可） | premature raw / premature accepted と post-reveal event を分離でき、公開後 RT の原点が reveal。公開前 event が公開後 first raw / first acceptedへ混入せず、RT 3種とcorrection costは単調増加時計由来のまま負値・wall-clock分の跳躍を生じない。確認後は自動日時へ戻す | [ ] |
| 8 | 完了直前／完了直後でもforce-stopし、再起動 | CSV / Manifest / checkpoint が残り、3者のparticipant/session/versionが一致する。完了checkpointだけ書かれManifestが古いcutでもManifestが自己修復され、完了sessionを再開しない。CSV は UTF-8 BOM、日本語・quote・行数・event index が壊れない | [ ] |
| 9 | Pointer Down直後、timeout直後、trial間gap、survey送信直後でそれぞれforce-stopし、同じ P00 で再開 | Raw WALとchecksum有効な最新A/B checkpointからcondition/trial/event index、次trial index、Covered公開状態、全Raw / pending recordsが復元される。WALだけのRawは1行だけstale化され次targetへ適用されず、解決済みtrial／surveyの欠落・重複がない | [ ] |
| 10 | 主実機と異 cutout 環境で Safe Area端・3画面比を確認 | 座標は有限、normalized 値が破綻せず、4 control・target・label が Safe Area 内で非重複。`UpperFixed` と `BottomFixed` はどちらも固定位置のまま | [ ] |
| 11 | Rush で2〜3台表示中、target切替境界と直前 Repair Rectをtap | touch / accept / resolved vehicle ID、target-change flag、current / previous Repair Rect が記録される。target／旧Rect由来のstale Repairを識別でき、`stale_target`入力は次車両へ誤適用されない | [ ] |

同一フレーム試験は端末の frame log と `frame` 列が一致した行だけを証拠にします。手動で同一 frame を作れない場合は入力自動化を実機上で使います。PlayMode injection は補助証拠であり、P00 実機項目の代替にはしません。

## 画面・cutout目視チェック

各環境で次を記録します。

| case | 目視基準 | 完了 |
|---|---|---|
| 1080×1920相当 | 4 control が Safe Area 内、相互非重複、target 非重複 | [ ] |
| 1080×2340相当 | 9:16 content が中央に収まり、縦余白で入力距離が変わらない | [ ] |
| 720×1600相当 | label / 右端 clipping、Repair / 車種 / target 重複がない | [ ] |
| 異 cutout profile | notch / hole-punch / no-cutout の差が `Screen.safeArea` と座標列へ反映される | [ ] |

Placement 2 条件、State Assist の hidden / disabled / Broken、Rush 3 preset の screenshot を残します。Editor の数学的 projection PASS は実機・Emulator目視の代用にしません。

## ファイル抽出と照合

Android の `Application.persistentDataPath` は通常 `/storage/emulated/0/Android/data/<application-id>/files` 相当です。Android Studio Device Explorer、または端末／OS で許可される `adb pull` を使い、`InputUIComparison` 以下を PC へコピーします。権限制約がある Development Build では `adb shell run-as <application-id>` で実在パスを確認します。

抽出後に次を確認します。

- Manifest と CSV の participant/session/version/type が一致する。
- CSV header が [input_events_v2.csv](../Schemas/input_events_v2.csv) と一致し、UTF-8 BOM がある。
- event index が session 内で一意かつ増加し、物理 tap 数と Raw 行数が一致する。
- hidden Repairとgeneric emptyの分離、およびstale Repair overlayが証拠eventと一致する。
- checkpoint の 2 slot のうち少なくとも 1 つが checksum 検証を通り、再開できる。
- app 終了前後で file hash と行数が不意に減っていない。

## 記録テンプレート

結果概要は `Assets/Experiments/InputUIComparison/Results/P00/` に、個人情報・端末固有 ID を除いて保存します。

```text
date/time:
build_version / git_commit:
schema_version / experiment_version: 2 / 2.1.0
primary_device_class / Android / cutout:
secondary_emulator_or_device / cutout:
resolution_cases / safe_areas:
session_ids_by_experiment_type:
machine_checks_1_to_11:
checks_failed:
checks_not_reproduced:
artifact hashes or paths:
blocking defects:
operator note:
```

## Go / No-Go とversion freeze

次をすべて満たした場合だけ P00=`PASS` です。

- 機械合格条件 11/11 が PASS。
- 主 Android 実機に加え、異なる cutout の Emulator または第2実機が PASS。
- 3 画面比が matrix 全体で確認済み。
- データ欠落、二重記録、stale target 誤適用、checkpoint 復元不能がない。
- blocking defect が 0 件。

PASS 後に build、Git commit、protocol、schema、`experiment_version=2.1.0` を freeze し、そこで初めて「P01〜P06 Placement へ進める」と記録します。1 項目でも FAIL / NOT_REPRODUCED なら「修正未完了のため進めない」です。P00 PASS 後も State Assist や Rush へ直行せず、Placement の参加者単位集計を先に完了します。
