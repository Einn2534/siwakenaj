# Trial sets、seed、counterbalance — v2.1

実行時の source of truth は `InputUITrialPlan.BuildMeasuredSet("Set01" | "Set02" | "Set03", seed)` です。各条件は練習 4 試行と計測 24 試行を持ちます。人間試験で使用する契約は P00 PASS 後に `experiment_version=2.1.0` として freeze します。

## Set01〜Set03 の構成

各 set は 3 車種×4 状態×2 repetition の 24 trial です。

| quota | 各 set の件数 |
|---|---:|
| Sports / Compact / LightTruck | 各 8 |
| Normal / Express / Covered / Broken | 各 6 |
| Low / Normal Speed / High | 各 8 |
| 各状態内の Low / Normal / High | 各 2 |
| Covered reveal 42% / 52% / 62% | 各 2 |
| 同車種の隣接 | 4 pair |
| Broken → Normal | 2 pair |
| Normal → Broken | 2 pair |
| Sports → Compact → LightTruck の短 gap 列 | 1 列（3 trial） |

短 gap は 0.22 秒です。短 gap 列の trial 番号は Set01=`5〜7`、Set02=`11〜13`、Set03=`17〜19`。通常 gap は Low=1.00 秒、Normal=0.72 秒、High=0.56 秒です。

3 set は同じ trial 行の単純な並べ替えではありません。`speed=(vehicle_index + state_index + repetition + set_index) mod 3` により車種×状態への速度割当が変わり、Covered reveal の割当も set ごとに回転します。その上で seed 付き XorShift32 shuffle を行い、上記の隣接制約を満たす列だけを採用します。存在しない複合 Modifier は生成しません。

## 安定 seed

条件ごとの seed は次の canonical string を UTF-8 にし、32-bit FNV-1a（offset `2166136261`、prime `16777619`）で hash します。符号 bit を除き、0 は 1 に置換します。

```text
UPPER_TRIMMED_PARTICIPANT_ID|ExperimentType|condition|NormalizedTrialSetId|experiment_version
```

入力要素は `participant_id`、`experiment_type`、`condition`、`trial_set_id`、`experiment_version` の 5 つです。同じ入力は端末・process を越えて同じ seed、どれか 1 要素が変われば別 seed になります。.NET の `GetHashCode()` は使いません。生成値は Manifest の `seed_by_condition` と trial CSV の `seed` に保存します。

## Placement：P01〜P06のみ

P01〜P06 は Placement 専用です。この cohort に State Assist や Rush を同日追加しません。

| participant | 1 番目 | 2 番目 |
|---|---|---|
| P01 | UpperFixed / Set01 | BottomFixed / Set02 |
| P02 | BottomFixed / Set03 | UpperFixed / Set02 |
| P03 | UpperFixed / Set03 | BottomFixed / Set01 |
| P04 | BottomFixed / Set02 | UpperFixed / Set01 |
| P05 | UpperFixed / Set02 | BottomFixed / Set03 |
| P06 | BottomFixed / Set01 | UpperFixed / Set03 |

条件 ID は `Placement_UpperFixed` / `Placement_BottomFixed` です。各条件は 1 番目・2 番目に 3 回ずつ、各 condition×set は 2 回ずつ出現します。結果は二固定位置間だけで解釈します。

## State Assist：Placement集計後の別session

State Assist は P01〜P06 Placement の参加者単位集計と anchor 暫定選択が終わるまで開始しません。最低でも Placement とは別日・別 session、推奨は別 cohort です。次表の `S01`〜`S06` は State Assist 用の **counterbalance slot** を示します。別 cohort では新しい匿名 ID を各 slot へ割り当てます。同一参加者を再利用する場合だけ元の匿名 ID を継続し、対応する ordinal slot を使います。

| participant | 1 番目 | 2 番目 | 3 番目 |
|---|---|---|---|
| S01 | AlwaysActive / Set01 | DisabledWhenNormal / Set02 | BrokenOnly / Set03 |
| S02 | AlwaysActive / Set02 | BrokenOnly / Set01 | DisabledWhenNormal / Set03 |
| S03 | DisabledWhenNormal / Set01 | AlwaysActive / Set03 | BrokenOnly / Set02 |
| S04 | DisabledWhenNormal / Set02 | BrokenOnly / Set03 | AlwaysActive / Set01 |
| S05 | BrokenOnly / Set01 | AlwaysActive / Set02 | DisabledWhenNormal / Set03 |
| S06 | BrokenOnly / Set02 | DisabledWhenNormal / Set01 | AlwaysActive / Set03 |

6 順列を 1 回ずつ使い、各条件は各提示位置へ 2 回、各 condition×set は 2 回ずつ出現します。同一参加者が同じ set を連続使用しません。同じ P01〜P06 を再招集する場合も最低別日・新規 State Assist session とし、学習持越しを限界として報告します。

## Rush：事前登録した2候補

P00 は `CurrentEquivalent / FixedFourEquivalent / ContextualRepairEquivalent` の既存 3 preset を技術確認します。人間対象 Rush では State Assist の参加者単位集計後に、構成を変更しない完成パッケージ 2 候補を選び、候補 ID、reference、主要指標、除外規則を事前登録します。

2 候補を `Candidate1` / `Candidate2` とすると、提示順は参加者 ordinal の奇偶で AB / BA を均衡化し、set rotation は Placement と同じ 6 人 block を使います。推奨は新しい `R01`〜`R06` cohort です。Rush データを見た後に候補、reference、package 構成を差し替えません。結果は package 全体の最終反証であり、個別 factor の効果には分解しません。

## P00、後続block、日程

P00 は canonical 順、Set01/02/03 の技術予行 schedule を使い、`is_pilot=true` で主要集計から除外します。人間試験は次の順序を崩しません。

```text
P00全技術予行
→ version freeze
→ P01〜P06 Placementのみ
→ 参加者単位集計
→ anchor暫定
→ State Assist別session（最低別日、推奨別cohort）
→ 候補2案の事前登録
→ Rush最終反証
```

Placement、State Assist、Rush の同日まとめ実施は禁止です。P07 以降の Placement block は条件順を 6 人周期、set offset を 3 人周期で再現可能に繰り返します。末尾番号のない ID は安定 FNV participant hash へ fallback しますが、本試験では監査しやすい prefix+連番形式を使います。

自動テストは quota、非同一構成、隣接制約、順序、condition×set 均衡、seed 再現性を検証します。これらは設計上の均衡を示すだけで、人間にとっての主観的な等難度や UI 優位性を保証しません。
