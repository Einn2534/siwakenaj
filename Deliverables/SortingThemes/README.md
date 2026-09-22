# ステージ別の仕分けテーマ

| おしごと | テーマ | 3種類 |
| --- | --- | --- |
| 1〜3 | 既存の車 | トラック・普通車・スポーツカー |
| 4〜7 | じゃんけん | グー・チョキ・パー |
| 8〜10 | 時計 | 3時・6時・9時 |
| 11〜15 | 葉野菜 | キャベツ・レタス・白菜 |

じゃんけんは同じ手を選ぶ仕分け。ステージ番号、難度の設定値、目標、保存キーと広告条件は従来のまま。Dailyは車、Endlessは最高クリア面のテーマを使う。

## 遊び方と編集

StageSelectのカードにテーマ名と絵を表示する。「テーマのあそびかた」から選択中の説明・練習へ進める。MainのPauseからも現在のテーマで練習できる。Result、Retry、次ステージもテーマを引き継ぐ。

Unityの `Assets/Resources/StageDatabase.asset` の各面の `VisualTheme` が割り当て元。テーマは `Assets/Resources/SortingThemes`、既存車は `CarVisualDatabase` を参照する。各テーマの3スロットに名前、流れる絵、ボタンの絵を設定できる。未設定のステージは既存車へ戻る。

手と葉野菜の6枚は透過イラストとして生成。時計3枚は分針を12に固定し、短針を3/6/9に向けた図。同じ箱で正体を隠すひみつ便も用意した。画像は `Assets/Art/Sprites/SortingThemes`、プロンプトと再生成手順は [generation-prompts.md](generation-prompts.md)。PNG原本は変更せずUnityで512pxに取り込み、元画像の解像度からPPUを計算して表示サイズをそろえている。

## 検証証拠

全EditMode 83/83、全PlayMode 56/56、テーマ画像90/90、既存レイアウト42/42がPASS。各テーマで正解・誤答・結果・Retry・Nextと練習を確認済み。[最終集計](validation-summary.json)に件数を保存した。

- [EditMode XML](Tests/editmode-results.xml)、[PlayMode XML](Tests/playmode-results.xml)：全suiteの結果。
- [テーマ別の検証](Captures/layout-validation.md)：4テーマ×7画面×3比率と2/7面ノルマ×3比率、90枚。収集済みアイコンも含む。
- [従来42画面の検証](Regression/layout-validation.md)：既存RegressionCaptureRunnerによる3比率×14状態。
- [車](Captures/Stage01_Main_Android_1080x1920.png)、[じゃんけん](Captures/Stage04_Main_Android_1080x1920.png)、[時計](Captures/Stage08_Main_Android_1080x1920.png)、[葉野菜](Captures/Stage11_Main_Android_1080x1920.png)：実Mainの比較。

テーマ別の画像は実PlayModeの画面。Mainの撮影時のみ対象3個を横に並べ、直後に位置を復元する。Tutorialは本来の位置のまま、カメラのサイズ変更後にハイライトの座標を再投影する。従来42画面は既存のEditorプレビュー行列。テストは実入力・スコア・保存・Result・Retry・Nextを確認し、PlayerPrefsとテレメトリを元に戻す。

Gitには各テーマのMain代表画像4枚と最終XML・検証レポートを保存する。全132枚の元画像（約504 MiB）と途中の診断ログは、このローカルフォルダに保持している。

独立レビューで見つかった練習のstage1固定と説明のFEVER/RUSH欠落を修正。Dailyの練習スキップ、Endlessの練習完了と本番復帰も追加検証した。収集アイコンの縦横比、練習中の対象とポインターの収まり、長い説明の改行も修正し、全検証を再実行。最終レビューの未解決指摘はない。途中のfocused失敗ログは診断履歴として残し、最終結果は上記の全suite XMLを参照する。

Unity 6000.3.10f1を使用し、Unity・パッケージは更新していない。ユーザーのPowaGameplayLayoutのY位置変更と既存の未コミット素材・記録を維持。Android/iOS実機性能と配布ビルドは今回未確認。
