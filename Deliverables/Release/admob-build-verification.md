# AdMob テスト広告版AABの検証

2026-09-08

## 最終成果物

- AAB: Builds/GooglePlay/siwakenja-20260908-072925.aab
- サイズ: 73,423,448 bytes
- SHA256: 0A34A678CF8DAAC12B31A3FCF389E7F96E2DC4C60DB3C936EAA7BF961BF4FAA4
- パッケージ: com.kibundeseisakujo.siwakenja
- versionName 1.0 / versionCode 1（Playへのアップロードは未実施）
- Unity 6000.3.10f1 / IL2CPP / ARM64 / minSdk 25 / targetSdk 36
- Google Mobile Ads Unity SDK 11.5.0、Android SDK 25.4.0、UMP 4.0.0
- 公式テスト広告IDを使用。本番配信向けの広告設定ではない。
- 071943.aabは初期化順序修正前の中間生成物。配布には072925.aabを使用。

## 確認済み

- 修正後コンパイル: エラー0、警告0。
- EditMode: 76 / 76成功。
- PlayMode: 40 / 40成功。
- 追加検証: 報酬なしの終了は復活なし、報酬取得後の正常終了だけ復活、失敗時は復活なし、重複・遅延通知で二重復活しない。
- SDK初期化前のイベント実行基盤が動くこと、広告未準備でもコールバックが返ることをPlayModeテストで確認。
- Unity Editor用Google広告モック: リワードの表示、終了後の成功通知1回、再読み込みを確認。インタースティシャルは表示中の通知0回、終了後1回を確認。
- 最終Android生成コードにもMobileAdsEventExecutor.Initialize呼び出しが含まれることを確認。
- ビルド終了コード0。jarsignerで署名検証成功（アップロード用自己署名証明書・タイムスタンプなしの警告あり）。
- AABから抽出したmanifestでパッケージ名、targetSdk 36、登録したAdMobアプリID、Unity Adsコンポーネントがないことを確認。
- AAB内ARM64ライブラリ7本のELF LOADセグメントが16 KB以上のアラインメント。
- Unity Adsのランタイム、設定アセット、設定生成スクリプト、UPMパッケージとGradle依存を削除。
- プライバシーポリシー公開版とアプリ内本文をGoogle AdMob対応へ更新。
- Git差分の空白チェック成功。署名鍵のローカルパスやパスワードは共有設定に残していない。

## 残る確認

Android端末は未接続のため、実際の広告サーバーとの通信、Android上での動画完了・途中終了・復活・オフライン・バックグラウンド復帰は未検証。Editorモックは実機配信の成功を保証しない。
次はGoogle Play内部テスト経由でインストールし、テスト広告とゲーム進行を実機確認する。AdMobの審査・必要な同意メッセージ設定も確認する。
実機確認後、本番IDへの切り替えと本番用AABの再生成が必要。
Playデータセーフティ、コンテンツレーティング、地域設定、テスター募集とクローズドテストも残る。

ビルドは既存の -SkipDependencyReport を使用。Unityの任意のPlay依存メタデータ生成は省略している。製品版前に再確認する。
Playが生成するAPKのZIP配置と16 KB端末上の実行は未検証。

## ローカル証跡

- RegressionArtifacts/admob-editmode.xml
- RegressionArtifacts/admob-playmode.xml
- RegressionArtifacts/admob-rewarded-editor.png
- RegressionArtifacts/admob-bundle-verification.txt
- Logs/GooglePlayBuild.log
- Tools/Test-GooglePlayBundle.ps1 で署名・manifest・ELF・ハッシュの再検証が可能。
## 実機検証（2026-09-08 08時台）

Pixel 9a / Android 16 / 4 KBページ端末をADB承認後に接続。最終072925.aabからbundletoolで端末用分割APKを作成し、既存アップロード鍵で署名してインストール成功。Google Play経由の配布ではない。
- アプリ起動、初回説明、ステージ選択、ゲームオーバーを確認。
- リワード広告で「テスト広告」「報酬を獲得しました」を確認。閉じた後に復活カウントダウンとミス残数の回復を実機画面で確認。
- 別のテスト動画の途中で戻る操作・ホーム移動・Activity再表示を行い、復活せず結果画面へ移行したことを確認。操作を複合しているため、通常の広告Xボタンのみでの途中終了テストとは区別する。
- 起動時にUMPのメッセージ未設定警告を検出。AdMobに「しわ賢者・広告の同意」を作成し公開済みを確認。日本語＋英語、GDPR適用地域のみ、「同意しない」有効、既存ポリシーURLを登録。反映は最大1時間とのUI案内。反映後の警告解消は別途確認が必要。
- 全画面インタースティシャルは3ステージクリア後のため、今回のゲームオーバー試行では表示条件に到達せず。Editor検証のみで実機表示は未確認。
- オフラインと16 KB端末、Play経由インストールは引き続き未検証。
- スマホにテスト版を残し、最後にアプリを再起動。

証跡: RegressionArtifacts/admob-device-rewarded.png、admob-device-revived.png、admob-device-diagnostics.txt。

## ユーザーによる実機確認の完了

ユーザーから全画面広告の表示と、閉じた後に操作できたとの報告を受領。これにより実機のリワード・全画面広告・終了後操作を確認済みとした（全画面広告の最終操作はユーザー確認）。

