# 初期監査と v2.1 要件ギャップ

## 監査範囲

2026-08-26 に、独立実験フォルダーの Scene、Scripts、Editor、Data、Tests、Results と、その判定ルールが参照する本番コードを読み取り監査しました。本番ファイルは変更していません。

監査した主な経路は次のとおりです。

- `Scenes/InputUIComparison.unity` と実行時 `InputUIComparisonController`
- UI 条件カタログ `Conditions/ExperimentLayoutCatalog`
- Raw 取得 `InputCapture/GlobalRawPointerCapture` と仲裁 `InputArbitrator`
- 問題生成・割当 `InputUITrialPlan`
- CSV・分析 `InputUIExperimentAnalytics` と Editor 集約処理
- Manifest / checkpoint `Core/SessionPersistence`
- EditMode / PlayMode テストと `Results/Verification`
- 本番の車種、Modifier、判定、入力 cooldown、Safe Area の依存関係

## 修正前 v1 の監査結果

v1 の Baseline/A/B は次の構成でした。座標は 1080×1920 reference frame、anchor は左下 `(0,0)`、pivot は中央 `(0.5,0.5)` です。

| v1 案 | 車種ボタン RectTransform | 整備ボタン RectTransform | Normal / Express / Covered 時 | Broken 時 | 同時に変わっていた要素 |
|---|---|---|---|---|---|
| Baseline | center `(211,190)`, `(540,190)`, `(869,190)`、各 `308×315` | center `(540,462)`、`470×112` | 表示・有効 | 表示・有効 | 基準 |
| A：固定 4 枠 | center `(140,175)`, `(405,175)`, `(675,175)`、各 `240×260` | center `(940,175)`、`240×260` | 表示・disabled | 表示・有効 | 整備位置、整備サイズ、3 車種の位置・幅、disabled cue |
| B：コンテキスト整備 | Baseline と同じ | center `(540,800)`、`360×128` | 非表示・入力不可 | 表示・有効 | 整備位置、整備サイズ、Broken 通知 |

したがって、A/B の差を「配置の効果」と解釈することはできません。A は配置・全ボタン geometry・状態 cue、B は配置・サイズ・状態 cue が交絡しています。v1 の結果は完成パッケージ全体としてのみ扱います。

本番ルール側で確認した不変条件は次のとおりです。

- 車種ボタンの物理順は Sports / Compact / LightTruck。
- 判定対象は画面内で最も左の車両。
- `CarModifier` は Normal / Express / Covered / Broken の単一 enum で、同一車両の複合 Modifier は存在しない。
- Express は速度 1.55 倍・正解入力は Normal と同じ。
- Covered は公開前の車種入力を誤答として受理し、公開後に通常の車種判定を行う。
- Broken は Repair だけが正解で、整備後の車種分類はない。
- 誤答は -5、車両は残り、期限内なら再入力できる。正解または空間 timeout で試行を解決する。
- 同一フレームでは最後の物理入力だけを判定し、最後が無効でも先行する有効入力へフォールバックしない。受理後 0.08 秒は共通 cooldown。

## 初期ギャップと v2.1 の対応

| 監査項目 | 修正前の問題 | v2 の対応 | 現在の証拠／残確認 |
|---|---|---|---|
| 実験要因 | Baseline/A/B で位置、サイズ、表示、入力可否が混在 | Placement、State Assist、Rush Package の 3 実験へ分離 | 条件カタログと直交性テスト。人間効果は未測定 |
| Placement geometry | A の 4 分割が車種幅まで変更 | 車種 3 ボタンを完全一致させ、整備の固定 `anchoredPosition` だけ変更 | UpperFixed `(540,800)` / BottomFixed `(540,462)`。サイズは両方 `470×112` |
| Placement 状態 | v1 案間で常時有効、disabled、hidden が混在 | 両条件とも全状態で表示・有効。非 Broken の Repair は通常どおり誤答 | カタログ・ルールテスト |
| State Assist geometry | 状態 cue と配置が混在 | 選択済み anchor を全 3 条件で固定し、CanvasGroup/input state だけ変更 | Session 開始後は設定をロック |
| 画面全体の Pointer Down | Button 中心の計測では空白・非表示領域・重複経路を完全に立証できない | Input System の EnhancedTouch / Mouse / Pen を単一 Raw source とし、UI hit test とゲーム受理を後段へ分離 | Android の 1 tap 1 row は P00 で確認が必要 |
| disabled / hidden | ゲーム非受理のため行動痕跡を失う危険 | Raw 行を残し `disabled` / `hidden_or_empty` を付与 | P00 実機確認が必要 |
| 同一フレーム | 最後の入力以外が分析から消える危険 | 全 Raw 行を保存し、最後の物理入力より前を `same_frame_overwritten`。最後が無効なら固有の拒否理由を保持し、先行入力は受理しない | 多指同時入力を P00 で確認 |
| cooldown | 捨てた入力の発生数を復元できない | Raw 行を保存し `cooldown` | 80 ms 内連打を P00 で確認 |
| Rush target 帰属 | touch 時点と受理時点の対象差を復元できない | touch / accept / resolved の vehicle id と target-change flag を記録し、変化時は `stale_target` | Rush 実機予行が必要 |
| Rush候補数 | 既存3presetをそのまま人間比較すると探索・reference変更が混入 | P00だけ3preset。非P00はprotocol/reference/構成を事前登録した2候補だけをAB/BA実行しManifestへ固定 | 登録不備は開始・集計ともfail closed。人間結果は未取得 |
| 問題セット | 1 順序または単純な並べ替えでは順序学習が残る | Set01〜03 を別の speed/reveal 構成で生成し、同じ quota と隣接制約を満たす | EditMode の多数 seed 検証。人間の等難度感は未確認 |
| seed | 全参加者固定 seed | participant/type/condition/set/version を FNV-1a 32-bit で安定 hash | `GetHashCode()` 不使用テスト |
| 条件順 | v1 の 3 案全順列だけ | Placement は P01〜P06 で AB/BA、State Assist は後段の別 6 counterbalance slot で全順列。set との組合せも均衡 | 段階別 schedule テスト |
| Session 識別 | v1 データと新データの混在余地 | schema 2 / experiment 2.1.0、Manifest、version/type 別出力・集約 | 違う version を拒否するテスト |
| 中断 | CSV checkpoint だけではcapture直後、timeout、survey、完了境界の欠落・重複余地 | capture-time Raw WAL、A/B交互checkpoint、解決後indexの一括commit、Covered／trial開始checkpoint、survey dedupe、完了Manifest自己修復 | 自動回帰はPASS。Android force-stop再開はP00で確認 |
| 分析単位 | 試行を独立人数のように扱う危険 | event→trial→参加者×条件→同一参加者差→group の順で集約 | P00 と practice は自動除外。細分 cell は danger signal のみ |
| 指標 | Broken/Covered/Express を特殊見落としへ一括 | State Assist primary 7 指標を固定し、first accepted は diagnostic。hidden/generic/stale、Covered premature/post-reveal、RT 3 種を分離 | 分子・分母は `AnalysisPlan.md` に固定 |
| アンケート | 5 問中心で状態 cue のゲーム性影響が不足 | 8 問の 5 段階評価と短答フィールドを v2 schema 化 | 実機で入力・CSV保持を確認 |
| 実機・画面比 | 旧 1080×2340 Game View 画像だけ | 1080×1920 / 1080×2340 / 720×1600 の投影検証を追加 | 主実機 + 異 cutout Emulator または第2実機の P00 は未実施 |

## v2.1 で意図的に変えないもの

- 正解・誤答・timeout・retry・得点のゲームルール
- 車種、Modifier の排他性、Express/Broken の速度倍率
- 車両サイズ、走行距離、状態ごとの判定時間
- 条件内の問題 quota、feedback、HUD、背景、効果音、アニメーション
- 本番 Scene、Prefab、Script、Tutorial、How To

## 監査時点で残る限界

- Placement / State Assist は原因分離のため単一対象を中心に測る。複数車両での外的妥当性は Rush Package で別に確認する。
- Rush は本番挙動を模した独立プロトタイプであり、本番 Stage 15、Daily、Endless の全生成器をそのまま動かすものではない。
- P00 Android 主実機、異 cutout Emulator／第2実機、3 画面比の目視、人間参加者テストは未実施。
- 視線、指のホバー、押しかけて戻る動作は Pointer Down ログだけでは分からない。
- 6 人程度の予備試験では推測統計や一般化に限界がある。
- 自動テストは contract の成立だけを確認し、発見性、疲労、楽しさ、ゲーム性の維持を証明しない。

## P00後の段階ゲート

```text
P00全技術予行
→ version freeze
→ P01〜P06 Placementのみ
→ 参加者単位集計
→ UpperFixed / BottomFixed のanchor暫定選択
→ State Assist別session（最低別日、推奨別cohort）
→ 完成パッケージ候補2案の事前登録
→ Rush最終反証
```

Placement の結論は二固定位置間だけです。target 追従や contextual placement 一般へ外挿しません。Placement、State Assist、Rush の同日まとめ実施は禁止します。P00 では Rush catalog の 3 preset を技術確認できますが、人間 Rush は State Assist 後に固定した 2 候補だけを扱います。

この監査だけから UI の採用案を推奨しません。
