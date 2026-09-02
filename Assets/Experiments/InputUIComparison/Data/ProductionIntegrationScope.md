# 本番反映の境界（v2.1）

現時点では本番へ反映しません。本番 Scene、Prefab、Script、ゲームルールは変更しておらず、人間データもないため採用案はありません。

## 判断の順序

1. P00 の全技術予行と 11 項目の機械合格条件を完了する。
2. blocking defect 解消後、build / Git commit / protocol / `experiment_version=2.1.0` を freeze する。
3. P01〜P06 で Placement の `Placement_UpperFixed` / `Placement_BottomFixed` だけを比較する。
4. 参加者×条件で集計し、二固定位置の一方を State Assist 用 anchor として暫定選択する。
5. State Assist を別 session、最低別日、推奨別 cohort で実施し、状態 cue だけを比較する。
6. State Assist の primary 7 指標とゲーム性 trade-off から、完成パッケージ候補を 2 案に固定して事前登録する。
7. Rush で 2 候補が複数車両・target 切替時に破綻しないか最終反証する。
8. 全段階と速度・状態別 danger signal が揃ってからのみ、本番候補を検討する。

Placement、State Assist、Rush の同日まとめ実施は禁止します。Placement の結論は試した二固定位置間に限定し、target 追従配置一般の効果へ外挿しません。P00 は既存 3 Rush preset の技術確認に使えますが、人間 Rush は事前固定した 2 候補だけです。

Rush の旧 A/B が良くても、その結果だけから「位置が効いた」「Broken 通知が効いた」と説明しません。

## 採用後にだけ検討する変更

- 本番の 3 車種 ButtonZone と Repair UI の RectTransform / Safe Area layout。
- target 状態を UI へ伝える adapter と、target 切替時の stale input 防止。
- 必要な場合だけ Repair の CanvasGroup 状態制御。
- Tutorial / How To の Express、Covered 公開待ち、Broken、左端 target、Pointer Down 説明。
- Campaign Stage 11〜15、Daily Tier 3、Endless、Rush/Fever での regression。
- 対応端末の localization、font、親指到達性、アクセシビリティ確認。

## 変更しないもの

- 車両判定、Modifier の排他性、得点、timeout、retry、速度倍率。
- UI 実験だけを理由にした課金・広告・ゲームモード変更。
- 人間データ前の Main Scene 差替え。

## 本番移植前の追加証拠

- P00 Android の機械合格条件 11/11 PASS、主実機 + 異 cutout Emulator または第2実機、3 画面比の screenshot。
- Placement P01〜P06、および後段 State Assist / Rush の各 6 人 block における参加者単位差と 4/6 以上の方向一致。
- High、Normal、Broken、Covered の局所悪化がないこと。
- State Assist primary 7 指標、3 種 RT、Q4/Q8 と短答によるゲーム性 trade-off。
- 本番 Rush の実生成器で 2〜3 台、target 切替、stale tap が安全であること。

この文書は将来の変更範囲を示すだけで、いずれの UI も推奨しません。
