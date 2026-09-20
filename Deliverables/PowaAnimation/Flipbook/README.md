# Powa ゲーム中コマアニメ

ユーザー確認済みの成功12コマ・0.8秒を採用し、Mainのキャラクターをコマアニメへ切り替えました。画像切り替えに加え、子オブジェクトMotionのY座標をAnimatorで連続的に動かします。PowaGameplayLayoutは親だけを配置するため、画面比率の調整が上下移動を上書きしません。

|状態|時間|絵と移動|
|---|---:|---|
|待機|2.4秒ループ|7種類の絵を往復12段階で再生し、上下に小さく浮遊|
|成功|0.8秒|確認済み12コマを各2/30秒表示。少し沈んで跳ね、元の高さへ戻る|
|ミス|0.55秒|8コマで肩・帽子・杖が下がり、涙が出て待機へ戻る|
|クリア|0.9秒|13種類の絵を14段階で再生。片手で杖をつかみ、逆の手を開いて大きく跳ねる|

描画は1つのSpriteRendererです。杖も各画像に1本だけ含まれます。成功・ミスは最新判定で即時再生し、終了後は待機へ戻ります。クリアは最優先。ポーズは絵と位置の両方を停止します。終了時の進行停止・結果の一度だけの保存・演出後のResult遷移と未接続時のタイムアウト処理は既存の接続を使用します。

## 実装と再生成

- 素材：`Assets/Art/Sprites/Characters/Powa/Flipbook/`。原本PNGと旧試作を保持。
- Prefab：`Assets/Prefabs/PowaFlipbook.prefab`
- Animator・クリップ：`Assets/Art/Animations/PowaFlipbook/`
- Builder：`Assets/Editor/PowaFlipbookBuilder.cs`
- Editorからの再生成：`unicli exec Eval --code 'PowaFlipbookBuilder.Build();'`
- Main Play中の録画：`unicli exec Eval --code 'PowaAnimationCapture.Begin();'`
- 成功素材の追加生成は行わず確認済み12枚を再利用。待機・ミス・クリアの24枚は内蔵image_genで個別生成。最終プロンプト・生成元・保存先は[generation-prompts.md](generation-prompts.md)。

Unity 6000.3.10f1と既存パッケージを使用。Prefab・Animator・シーン・インポート設定はUniCliからUnity APIを実行して保存しています。旧パーツ版は比較用に残し、他画面へは展開していません。

## 検証

生成画像には帽子・バッジ・裾などの細部のコマ間変化が残ります。画像の色にじみは既存のスプライト用マテリアルで低アルファの縁を抑えています。実機の性能測定は未実施です。

- コンパイル成功、今回の変更に起因するエラーなし。既存のGooglePlayReleaseSetupの非推奨API警告が再コンパイル時に1件出ます。
- EditMode：77/77 PASS（`editmode-results.xml`）。
- PlayMode：49/49 PASS（`playmode-results.xml`）。連続成功、成功直後のミス、Y座標の変化、画像切り替え、ポーズ・再開、Win優先、復活、結果の一度だけの記録、未接続フォールバック、Result・リトライを含みます。
- 画面回帰：42/42 PASS、レイアウト失敗0件（`Regression/layout-validation.md`）。
- Mainの1080×1920、1170×2532、1440×3200を確認。ジャンプ時も車の領域・HUD・操作ボタンに重ならない配置です。
- 全37枚のPNGが1254×1254で、Unity内でもピボット・PPU・スプライトサイズが揃っています。
- 録画後のUnity Consoleのエラー0件。MainをEdit状態・未保存変更なしで終了。

## プレビュー

- [Main画面で4種類を見る](Powa-gameplay-preview.mp4)
- [キャラクター単体で4種類を見る](Powa-flipbook-preview.mp4)
- [動きの抜き出し一覧](motion-contact-sheet.png)

動画はどちらもUnityの実Animatorを時刻指定して撮影した204フレーム、30 FPS、6.8秒です。Main版は進行を止めてアニメーションを見比べるプレビューです。全フレームのデコード確認済み。`Main_*.png`に8枚の実画面キャプチャを保存しています。連番の録画PNG（`frames/`、`gameplay-frames/`）と画面回帰の全PNGはローカルに保持し、Gitには動画・代表キャプチャ・検証結果を収録します。

修正前のPlayMode結果も`playmode-before-duration-fix.xml`に残しています。画像切り替えの終端キーが1/30秒を加えてしまう問題を修正し、成功の0.8秒を検証するアサーションは維持したまま全49件を再実行しました。
