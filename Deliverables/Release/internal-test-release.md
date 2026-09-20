# 本番広告ID版・内部テスト初回リリース

2026-09-08

- AAB: Builds/GooglePlay/siwakenja-20260908-101458.aab
- サイズ: 73,424,117 bytes
- SHA256: 3FEF9E65BAF8E17B27FBF6EDFA0E0519531457BC3A6190976B9BA46B226D25D9
- versionCode 1 / versionName 1.0 / minSdk 25 / targetSdk 36 / ARM64
- AdMobConfiguration.UseTestAds=false。本番ユニットへ切り替え。
- 検証に使ったPixel 9aのSDKが表示したハッシュをTestDeviceIdsへ登録。該当端末は本番ユニットでもテスト広告を受け取る。
- コンパイル: エラー0・警告0。EditMode 76、PlayMode 40、すべて成功。
- 署名検証成功、AdMobアプリID一致、Unity Ads manifest要素なし、ARM64ライブラリ7本の16 KB ELFアラインメント確認。
- Play ConsoleへAABをアップロード成功。難読化解除ファイル・ネイティブデバッグシンボル未添付の警告2件は残る。内部テスト登録を妨げるものではない。
- 内部テスター: kibundeseisakujo@gmail.com の1名。リスト名「しわ賢者 内部テスト」。外部への案内メール等は送信していない。
- AdMobアカウントは審査中。アプリストアとの連携によるアプリ審査も未完了。本番広告収益の開始は未確認。
- -SkipDependencyReportにより任意のUnity Play依存メタデータ生成を省略。今後の製品版準備で再確認。
- 実機の広告表示・閉じた後の操作は、旧テストID版でユーザー確認済み。本番ID版そのものをPlay経由でインストールする確認は次の手順。

証跡: RegressionArtifacts/admob-production-editmode.xml、admob-production-playmode.xml、admob-production-bundle-verification.txt。
## 公開結果

Google Playの内部テストトラックは有効。リリース『1.0 内部テスト』、versionCode 1、2026-09-08 10:48に内部テスターへ公開済み。未審査のため一時名 com.kibundeseisakujo.siwakenja (unreviewed) が表示される。
参加URL: https://play.google.com/apps/internaltest/4701640059087792263
参加対象: kibundeseisakujo@gmail.com。内部テストであり一般公開ではない。次のアップロードにはversionCode 2以上が必要。

