# 仕様書（暫定版 v1.2）

## 1. 概要

自動車を車種ごとに仕分けるシンプルな反射神経ゲーム。画面左へ流れる車を観察し、対応するボタンを押して正しく仕分ける。目標スコア到達でステージクリア、許容ミス回数を超えるとゲームオーバー。

---

## 2. 対象プラットフォーム / 画面前提

* 対象：スマホ / タブレット
* 画面向き：縦固定
* アスペクト：9:16
* 基準解像度（Reference Resolution）：1080×1920（Portrait）
* Canvas Scaler：Scale With Screen Size（Reference Resolution=1080×1920）

---

## 3. 主要ルール

* 車種は3種類：LightTruck / CompactCar / SportsCar
* プレイヤーは3つのボタンで車種を選択
* 車が存在しない状態でボタン押下：ミス
* 車が画面左端を越える：時間切れミス
* 正解：スコア加算・車消去
* ミス：スコア減算

---

## 4. ゲームフロー

1. 起動時にゲーム開始処理
2. ステージ設定ロード → BGM再生 → スポーン開始
3. プレイ中は一定間隔で車をランダム生成
4. 入力により判定処理を実行
5. 目標スコア到達でステージクリア（Result）
6. 許容ミス超過でゲームオーバー（GameOver）

---

## 5. ステート管理

* Ready：初期待機
* Playing：プレイ中
* Result：クリア後の結果表示
* GameOver：失敗状態
* Playing以外では判定/遷移処理を行わない（重複ガード必須）

---

## 6. スコアリング

* 正解：+10
* ミス：-5
* 急送車の正解：基礎点×2
* フィーバー中の正解：獲得点×2（急送車と重複して最大×4）
* スコア下限：0未満にならない（減算後に0へクランプ）
* 目標スコア到達でステージクリア
* ミス回数が許容回数以上でゲームオーバー

---

## 7. ステージ設定（StageConfig）

ステージごとに以下の設定値を持つ。

### 必須項目

* targetScore（目標スコア）
* missLimit（許容ミス回数）
* carSpeed（車速度）：ステージ別
* spawnInterval（スポーン間隔）：ステージ別（固定値、揺らぎなし）

  * 制約：spawnInterval >= 0.2
* 車種出現重み（ステージ別）

  * weightLightTruck
  * weightCompactCar
  * weightSportsCar
* 車両ギミック出現率

  * expressChance（急送車）
  * coveredChance（覆面車）
  * brokenChance（故障車）
* rushEveryCorrect（大名行列の発生間隔。0で無効）
* feverComboThreshold（フィーバー開始コンボ数。0で無効）

### 抽選仕様

* スポーンごとに重みに比例したWeighted Randomで車種を1つ選ぶ
* 全weightが0の場合はスポーンしない（安全停止）

---

## 8. 車両スポーン / 移動

### 8.1 同時出現上限（固定）

* maxCarsOnScreen は固定：3台
* 既存車が3台以上の場合、そのスポーン試行は延期（スキップ）

### 8.2 座標系

* 車はワールド座標（World Space）のGameObjectとして存在し、左方向へ移動する
* スポーンY：固定（1レーン）

### 8.3 PlayZoneの役割

* PlayZone（UIのRectTransform）は「スポーンX基準」「左端越え判定X基準」のための基準矩形とする
* PlayZoneをワールド座標に変換して `PlayZoneWorldRect` を得る

### 8.4 スポーン位置

* spawnX：`PlayZoneWorldRect.xMax + spawnMarginX`
* spawnY（fixedSpawnY）：`PlayZoneWorldRect.center.y` に追従する

### 8.5 重なり防止（横方向）

* boundsは Collider2D 優先、無ければ Renderer を使用
* 車幅：`carWidth = bounds.size.x`
* 固定値：

  * spawnMarginX = carWidth * 0.6
  * minSpawnGapX = carWidth * 0.25
* 延期条件（生成しない）：

  * 既存車がいる場合に `spawnX - rightMostMaxX < minSpawnGapX` のとき延期（スキップ）
  * rightMostMaxX は既存車の max(bounds.max.x)

### 8.6 車速度の単位（確定）

* carSpeed は **PlayZone幅/秒**

  * carSpeed=1.0 のとき、1秒でPlayZone幅を横断
* world速度換算：

  * speedWorld = PlayZoneWorldRect.width * carSpeed
* 移動：

  * position += Vector3.left * speedWorld * deltaTime

---

## 9. 判定ロジック

* 判定対象：最も左に進んでいる車（xが最小の車）
* 判定対象車が存在しない：ミス、Cry、ミスSE
* 故障車を通常レーンへ送る：ミス
* 未公開の覆面車を押す：ミス（車は残る）
* 車種一致：正解、Happy、正解SE、車破棄
* 車種不一致：ミス、Cry、ミスSE

### 9.1 車両ギミック

* Normal：従来どおり車種ボタンで仕分ける
* Express：`!` マーカー。速度1.55倍、正解得点2倍
* Covered：`?` マーカー。全車共通の暗い外見で出現し、PlayZone左端から48%の位置で本来の車種を公開
* Broken：`X` マーカー。速度0.82倍。車種ボタンではなく専用の「整備へ送る」ボタンが正解

### 9.2 ステージ導入順

1. ステージ1：Normalのみ
2. ステージ2：Expressを追加
3. ステージ3：Coveredを追加
4. ステージ4：Brokenと整備ボタンを追加
5. ステージ5：大名行列とフィーバーを追加し、全ギミックを混合

### 9.3 大名行列

* 指定正解数ごとに警告を表示
* 警告0.65秒後、最大3台を0.22秒間隔で連続スポーン
* 大名行列中は通常スポーンを一時停止

### 9.4 コンボ / フィーバー

* 正解ごとにコンボ+1
* ミスまたはコンティニューでコンボを0へ戻す
* `feverComboThreshold` 到達時からフィーバーとなり、正解得点を2倍にする
* フィーバーは次のミスまで継続

---

## 10. 入力仕様（確定）

* 判定は PointerDown（押した瞬間）で実行（PointerUpは判定に使わない）
* 入力受付は Playing のみ
* マルチタッチ許容。ただし同一フレーム内の複数入力は1回だけ有効

  * 優先：最後に発生した入力を採用
* 連打クールダウン：0.08秒（ボタン共通のグローバル）

  * クールダウン中の入力は無視し、ミス判定もしない

---

## 11. UI配置（領域固定）

### 11.1 画面領域分割（1080×1920）

* TopZone：高さ 640px
* BottomZone：高さ 540px
* PlayZone：残り（車の表示・移動領域）

  * 1920 − 640 − 540 = 740px

### 11.2 RectTransformルール

* TopZone：Anchors(0,1)-(1,1), Pivot(0.5,1), Height=640, PosY=0
* BottomZone：Anchors(0,0)-(1,0), Pivot(0.5,0), Height=540, PosY=0
* PlayZone：Anchors(0,0)-(1,1), OffsetMin(0,540), OffsetMax(0,-640)

---

## 12. レーンUI（詳細）

* TopZone内構成（高さ640）

  * 上段：スコア/ミス表示（高さ160）
  * 下段：レーンUI領域（高さ480）
* レーン数：3（LightTruck / CompactCar / SportsCar）
* 配置：横並び、左右Padding=24、レーン間Spacing=16
* セル（アイコン）：

  * 56×56px、縦8px/横8px
  * 1列最大7行、超過は右に列追加
  * 最大3列まで表示。超過分は `+N` で表示
* 落下アニメ：

  * 生成位置：着地点の上（セル高さ×1.5）
  * 着地時間：0.18秒
  * バウンド演出：任意（0.06秒程度）

---

## 13. 左端越え（時間切れ）判定

* 左端は bounds.min.x（Collider2D優先、無ければRenderer）
* 判定ライン：

  * leftEdgeX = PlayZoneWorldRect.xMin
  * missMarginX = PlayZoneWorldRect.width * 0.02
* 条件：

  * leftMinX < (leftEdgeX - missMarginX) で時間切れミス
  * ミス処理を行い、対象車を破棄（またはプール返却）

---

## 14. アニメーション

* 正解：Happy（Attackトリガー）
* ミス：Cry（Damageトリガー）
* クリア：Win（Winトリガー）

---

## 15. サウンド

* BGM：ゲーム開始時にループ再生
* 効果音：正解 / ミス / クリア / ゲームオーバー

---

## 16. Result / GameOver

### 16.1 Result（クリア）

* 条件：Playing中に score >= targetScore
* 遷移：Playing → Result（重複禁止）
* 突入時処理：

  * スポーン停止
  * 既存車の移動停止（全車の速度を0にする。破棄しない）
  * 入力無効化
  * Winトリガー、クリアSE
  * 0.5秒後にResultパネル表示

### 16.2 GameOver（失敗）

* 条件：Playing中に missCount >= missLimit
* 遷移：Playing → GameOver（重複禁止）
* 突入時処理：

  * スポーン停止
  * 既存車の移動停止（全車の速度を0にする。破棄しない）
  * 入力無効化
  * ゲームオーバーSE
  * 0.5秒後にGameOverパネル表示

### 16.3 Result / GameOver 表示項目（共通）

* 最終スコア
* ミス回数
* 車種別正解数（3種）
* ボタン

  * Result：Retry / Next（次ステージがある場合のみ）/ Title
  * GameOver：Retry / Title

---

## 17. エラーハンドリング

* 参照未設定の場合は処理中断して安全終了
* 生成対象が存在しない場合はスポーンしない
* Playing以外ではクリア処理や重複処理を行わない
