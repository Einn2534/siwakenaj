# Google Play スクリーンショット（2026-10-08）

Unity 6000.3.10f1 の実行中 Game View、1080×1920。ゲーム内描画を ScreenCapture.CaptureScreenshot で取得。画像の合成、車位置の変更、得点・進捗の注入は行っていない。

## 採用画像と掲載順（2026-10-08に保存・審査送信受付済み）

| 掲載順 | ファイル | 内容 | 確認 |
| --- | --- | --- | --- |
| 1 | 00-title.png | タイトル、30 STAGES のおしごとボタン | 文字切れ・黒帯なし |
| 2 | 04-gameplay.png | おしごと1、3/12台、通常プレイ中 | 中央に走行車、上の枠に仕分け済みトラック3台 |
| 3 | 02-stages.png | 第1章のおしごと選択、全6章タブ | 文字切れ・黒帯なし |
| 4 | 05-result.png | おしごと1の実クリア、120点・0ミス・3つ星 | 通常入力12回の結果。文字切れ・黒帯なし |

Title → Main → StageSelect → Result の4枚を採用。ファイル名の番号は撮影時の番号であり、掲載順は上表と manifest.json の uploadOrder を使用する。Google Playへアップロード・保存し、審査送信操作が受け付けられた。現在は「審査中の変更」でクイックチェック中。完了後に審査へ自動送信され、公開承認はまだ確認していない。生成キャラクター等を含む4枚にAIラベルを付けた。証跡は QAOutput/ReleaseReadiness20261008/store-review-submitted.png/.txt。

## 掲載対象外

- 01-how-to-play.png: HOW TO の番号と車画像が一部重なって見えるため、今回の差し替え候補から除外。
- 03-tutorial.png: SKIP が左上HUDと重なって見えるため、今回の差し替え候補から除外。

## 撮影と復元

- Game View の解像度は1080×1920。Editorが返した古い Screen.safeArea によるずれを避けるため、既存 SafeAreaFitter の Editor 専用機能で1080×1920全域を指定した。
- 04/05 は Result の通常 Retry → Main を観測してから、現在の車種に対応する Button.onClick を入力した。時間倍率1、車体移動・スポーンの調整なし。
- 結果は12正解、120点、0ミス、最大12コンボ、実行時間13.2947655秒。記録は QAOutput/ReleaseReadiness20261008/gameplay-capture-status.txt。
- 撮影後、保存してあったPlayerPrefs 10項目と BestTime_Stage1 を復元。Editor安全領域シミュレーションを解除、GameView選択を元のindex 0へ戻し、Play Modeを終了（Title）。
- ゲームのコード、シーン、アセットは編集していない。今回の確認は画像と通常の1面プレイに限り、Android実機確認の代わりにはならない。
- 4枚とも1080×1920、24bit RGB PNG。Title/StageSelectは元画像の全画素が不透明であることを確認し、RGB値が全画素一致する24bit PNGに再エンコードした（サイズ変更・画像加工なし）。各画像の寸法・SHA256は manifest.json。

上部枠と走行レーンの位置が異なる点は、得点表示用 ScoreLaneUI と CarSpawner.PlayZone が別領域であるため。詳細は QAOutput/ReleaseReadiness20261008/main-layout-review.md。
