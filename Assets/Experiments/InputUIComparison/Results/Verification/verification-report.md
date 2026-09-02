# Engineering verification — v2.1 pre-P00

実施日：2026-08-26（JST）  
`schema_version=2` / `experiment_version=2.1.0`

この文書が示すのは、P00へ進むための実装・自動検証ゲートです。物理端末で行うP00そのもの、version freeze、人間参加者による比較、UX上の優劣は未完了です。

## 自動検証結果

- Unity script compilation：0 errors / 0 warnings。
- EditMode 全体：72 passed / 0 failed / 0 skipped。
- EditMode 実験範囲：40 passed / 0 failed。
- PlayMode 全体：37 passed / 0 failed / 0 skipped。
- PlayMode 実験範囲：28 passed / 0 failed。
- 直交レイアウト検証：PASS。詳細は [layout-catalog-verification.md](layout-catalog-verification.md)。

PlayModeは、接続中Editorとの競合を避けるため、同一ソースを同期した一時cloneからUnity CLI batch実行しました。実験fixture 28件と、既存ゲームを含む全37件を別々に実行し、最終結果XML `TestResults-PlayMode-v21-integrated-r5.xml` / `TestResults-PlayMode-v21-all-final.xml` で成功を確認しています。

主な自動検証範囲：

- `Placement_UpperFixed` と `Placement_BottomFixed` が整備ボタン位置以外を変えない。
- State Assistがanchorを維持し、整備表示・入力可否だけを変える。
- disabled、hidden repair area、generic empty、cooldown、stale targetを区別する。
- Button callbackとglobal raw captureが1入力を二重記録しない。
- touch由来の合成Mouseだけを抑止し、独立touchは欠落させない。
- 同一frameの複数入力をすべてRaw保存し、最後の物理入力だけを判定する。最後が無効でも先行する有効入力へフォールバックしない。
- Coveredの反応時間原点を情報公開時刻にし、公開前入力を診断値として分離する。
- capture-time Raw WAL、交互checkpoint、完了Manifest自己修復を使い、force-stop時もRaw欠落、解決済みtrial再実行、survey二重保存、完了session再開を防ぐ。
- Rushでtouch時とaccept時のtarget IDを保持し、target変更後のstale入力を拒否する。
- P00 Rushは既存3presetを維持し、非P00 RushはManifestへ固定した事前登録2候補と明示referenceがなければ開始・集計を拒否する。
- 完了session集計は各条件24件の一意な計測trialを要求し、欠落・重複やtrial外Rawをfail closedで除外する。
- 新しいraw／accepted／correct-resolution時刻とcorrection cost、イベント単位／試行単位指標をCSVと集計へ出力する。

## Game View確認

`1080 × 2340`でv2.1 setup画面を確認し、version表示、段階実施の警告、条件選択、開始操作にclip・重なりがないことを目視確認しました。

画像：[v2_1_setup_runtime.png](v2_1_setup_runtime.png)

## P00でのみ合否を確定できる項目

[P00 Android実機予行手順](../../Data/Protocols/P00_Android_Rehearsal.md)の11項目を、主Android実機に加えて異なるcutoutのAndroid emulatorまたは第2端末で実施します。特に以下は自動検証だけで合格扱いにしません。

- 物理タップ回数とRaw行数の一致。
- OS／端末由来のtouch→Mouse重複の不存在。
- disabled・hidden・空白・cooldownの実機座標分類。
- force-stop前後のCSV／Manifest／checkpoint整合性。
- 端末の壁時計変更中も単調増加時計による反応時間が連続すること。
- Safe Areaとcutout差を含む表示・入力確認。

## 次のゲート

次に許可されるのはP00全技術予行だけです。合格後に`experiment_version=2.1.0`を固定し、`P01〜P06 Placementのみ → 参加者単位集計 → anchor暫定決定 → State Assistを別session（最低別日、別cohort推奨） → 参加者単位集計 → 2候補へ絞る → Rushで最終反証`の順を崩しません。
