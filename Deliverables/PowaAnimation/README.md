# powa ゲーム中アニメーション

MainのPlayerCharacterを、生成した透過パーツを組み合わせるPrefabに置き換えました。Unity 6000.3.10f1と導入済みの2D Animation / Animatorを使用しています。

## 確認用

- `Powa-preview.mp4`：6.5秒 / 30fps。待機 → 成功 → ミス → クリア。実際のPrefabとAnimatorをUnityで描画した195フレームを使用。
- `Main_Idle_1080x1920.png` / `Main_Idle_1170x2532.png` / `Main_Idle_1440x3200.png`：ゲーム中の3比率。
- `Main_Attack_1080x1920.png` / `Main_Damage_1080x1920.png` / `Main_Win_1080x1920.png`：各反応のゲーム内表示。
- `idol-reference.png`：元の参照画像のコピー。添付の原本は保持。
- `generation-prompts.md`：内蔵imagegenで使ったプロンプトと採用画像の記録。

## 実装

| 状態 | 時間 | 動作 |
|---|---|---|
| Idle | 2.4秒ループ | 浮遊、帽子・裾・杖の揺れ |
| Attack | 0.4秒 | 小さく弾む、腕と杖、喜ぶ口とバッジ |
| Damage | 0.55秒 | 体と帽子・杖が下がる、困る表情と涙 |
| Win | 0.9秒 | 溜めて跳ぶ、両腕を上げる、右手で杖を持つ |

杖は1オブジェクトのみです。クリア開始から0.4秒で、浮遊位置から動いている右手へ移動します。左手は開いたままです。

帽子は背面と前面に分け、背面 → 顔・口 → 前面の順に描画します。元の帽子画像を共有し、前のつばの縁に沿ってSpriteのメッシュを分割しています。帽子2層と服をSpriteSkinで変形し、同じ輪郭がずれないよう前後の帽子に同じウェイトと動きを設定しました。生成画像の半透明な内部と色にじみは専用Spriteマテリアルのアルファ補正で処理し、PNG自体は保存したままです。

成功・ミスは最新の判定に即座に切り替わり、終了後はIdleへ戻ります。Win中は他の反応を受け付けません。ポーズ時はAnimator速度も保存して停止し、再開時に復元します。終了時はゲーム進行を止めて結果を一度だけ記録し、反応が終わってからResultへ移ります。Animatorが未接続なら待機せず進み、異常な停止でも2秒の上限があります。

Mainのカメラ比率に合わせてキャラクターの位置と大きさを調整しています。他画面のpowa素材には展開していません。

## 検証結果（2026-09-20）

- コンパイル：0エラー。既存GooglePlayReleaseSetupの非推奨API警告1件。
- EditMode：77 / 77 PASS。`editmode-results.xml`。
- PlayMode：49 / 49 PASS。`playmode-results.xml`。既存45件と今回の4件。
- 追加ケース：連続成功 → ミス、即時ポーズと再開、Win優先、右手と杖の接合、結果の一度だけの記録、終了演出待ち、未接続Animator、復活後の反応、Resultからのリトライ。
- 既存画面回帰：42 / 42 PASS、配置違反0。`Regression/layout-validation.md`。
- 3比率と4動作を目視し、車のレーン・HUD・操作ボタンとの干渉、帽子の前後、腕と肩、1本の杖を確認。
- 動画は640×640 H.264 / yuv420p、195フレームをデコード確認済み。

UniCliはPlayModeテストのドメイン再ロードで接続が切れてコマンド側が失敗を返すことがあります。検証結果はUnityが最後に書き出したXMLの日時・件数・各テスト結果を確認し、ここへコピーしました。

初回の静止画回帰で、既存の撮影準備がアイコン生成直後に実行時のDestroyを呼ぶ問題と、同期的なシーン読み込み直後のSortingGroup更新不足が見つかりました。撮影準備の順序と描画更新を修正し、42画面を再撮影しています。別途、既存Google依存解決プラグインからGvhProjectSettings.xmlの一時的な共有違反が1件出ました。powaの再撮影・テストは完了しており、広告設定やパッケージには変更を加えていません。

## 再生成

Unityを編集モードにし、現在のシーンを保存してからUniCliで実行します。

```powershell
unicli exec Eval --code 'PowaGameplayAnimationBuilder.Build(); return "built";'
```

MainでPlayモードにしてから、録画を実行します。終了後はPlayモードを抜けてください。

```powershell
unicli exec Eval --code 'PowaAnimationCapture.Begin(); return "recording";'
```

素材：`Assets/Art/Sprites/Characters/Powa/Gameplay/`。
Prefab：`Assets/Prefabs/PowaGameplay.prefab`。
AnimatorとClip：`Assets/Art/Animations/PowaGameplay/`。

制作補助としてUnity公式の` sprite-editor`と`manage-sprite-atlas`スキルを追加しました。前者のSpriteデータプロバイダ手順を使用し、今回の2枚のシートには別のSpriteAtlas資産を追加していません。
