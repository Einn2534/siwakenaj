# Google Play内部テストの配信記録

## 最新: 1.0.9（2026-10-15）

- 1.0.9 / versionCode 10を内部テストへ公開。Console表示は「1.0.9 チュートリアルと画面表示の改善」「内部テスターに公開」「10月15日 3:08 に公開」。
- ひみつ便チュートリアル、一部ステージの星評価、仕分けレーン・操作ボタン、テーマ別カード背景、ゲーム中のモニターが透ける問題の修正を反映。
- AAB: `Builds/GooglePlay/siwakenja-1.0.9-10.aab`（159,048,700 bytes）。SHA-256: `FE4D7B9F1F1E7E361B55946E5B4C9B1A53E0E880F01A755C32CE57E1632CF764`。アップロード名 `candidate.aab` と完成版のハッシュ一致を確認。
- Unityビルド成功、エラー0。直前の同じソースでEditMode 112件・PlayMode 78件成功。署名の既存配信版との一致、target API 36、課金権限、ARM64の全7ライブラリの16 KB配置、ライセンス同梱を確認。
- 同じcode 10をAlphaへプロモートし、変更1件の審査送信を確定。「審査中の変更」に1.0.9を確認。最終確認時はGoogleのクイックチェック中で、完了後に審査へ自動送信される。承認・Alphaへの1.0.9配信開始は未確認。管理対象の公開はオフ、既存の日本・テスター向け設定を維持。
- Playの警告は従来と同じ難読化解除ファイル・ネイティブデバッグシンボル未添付の2件。サポート対象デバイス数の変化なし。新規インストールの表示サイズは157 MB（前版比+33 MB）。
- [内部テスト参加ページ](https://play.google.com/apps/internaltest/4701640059087792263)、[クローズドテスト参加ページ](https://play.google.com/apps/testing/com.kibundeseisakujo.siwakenja)を継続利用。反映・審査にはGoogle側の処理時間がかかる。一般公開ではない。
- 証拠: `QAOutput/PlayUpdate20261015/internal-published.jpg/.txt`、`alpha-review-submitted.jpg/.txt`、ビルド・検証記録。次の新規AABはversionCode 11以上。

前版1.0.8 / code 9は、今回の更新前に内部テストとAlphaの両方で完全公開済みであることを確認した。以下の「未確認」等は各日付時点の記録。

## 履歴: 1.0.8（2026-10-12）

- 1.0.8 / versionCode 9を内部テストへ公開。Console表示は「1.0.8 広告頻度と購入導線の改善」「内部テスターに公開」「10月12日 15:23 に公開」。
- 強制広告を通常のおしごと成功時だけ「3分以上＋2回クリア」で表示。失敗・デイリー・エンドレスでは表示なし。タイトルのHOW TOを広告削除の買い切り購入ボタンへ変更し、未使用EmojiOneも除外。
- AAB: `Builds/GooglePlay/siwakenja-20261012-150040.aab`。SHA-256: `847B06A9C93BE247E576178B12B47EFBAEC3CDE650914A324A4A05659415877B`。
- コンパイル成功、EditMode 103件・PlayMode 76件・レイアウト検査240件成功。署名の前版との一致、16 KB配置、課金権限、ライセンス同梱、除外素材の非収録を確認。
- 同じAABをAlphaへ追加し、更新1件の審査送信を確定。公開の概要の「審査中の変更」に1.0.8があり、Googleの自動チェック完了後に審査へ送られる。Alphaの1.0.8の承認・公開は未確認。既存の日本・テスター向け設定を維持。
- [内部テスト参加ページ](https://play.google.com/apps/internaltest/4701640059087792263)は継続利用。Googleの反映に時間がかかる場合がある。
- 証拠: `QAOutput/PlayUpdate20261012/internal-published.png/.txt`、`alpha-review-submitted.png/.txt`。次の新規AABはversionCode 10以上。

## クローズドテストの配信確認（2026-10-08）

同じ1.0.7 / versionCode 8がAlphaにも配信済み。Consoleで「選択したテスターに公開されました」「10月7日 5:42 に公開」を確認。日本・登録済みテスター向けで一般公開ではない。

[クローズドテスト参加ページ](https://play.google.com/apps/testing/com.kibundeseisakujo.siwakenja)。内部テストから移る場合は先に内部テストを退出する。実機確認表は `QAOutput/ReleaseReadiness20261008/device-checklist.txt`、配信証跡は同フォルダの `closed-test-status.txt/.jpg`。

10月8日のストア画像・説明文更新は別の審査送信。ゲームAABの差し替えは行っていない。最新の残作業は `ReleasePreparation.md` 冒頭を参照。

## 履歴: 1.0.7（2026-10-07）

- `Builds/GooglePlay/siwakenja-20261006-065116.aab`、versionCode 8を内部テストへ公開済み。
- Console表示: 「最新リリース: 1.0.7 ボタン表示調整・未使用フォント整理」「内部テスターに公開」、10月7日4:22公開。
- 変更内容: 仕分けボタンの画像・表示位置調整、未使用フォントの除外、第三者ライセンス表示の整備。
- SHA-256: `A6C092F62980B476C191903265FE88CF020B25A2B6045C45934E56AC555CD4DE`。アップロード前にも一致を再確認。
- 難読化解除ファイル・ネイティブデバッグシンボル未添付の警告2件。配信を妨げるエラーなし。
- [内部テスト参加ページ](https://play.google.com/apps/internaltest/4701640059087792263)。既存リストは2人。実機での更新・購入・復元確認は別途必要。
- 証拠: `QAOutput/PlayRelease20261007/internal-published.png`、`internal-published.txt`、`internal-review.txt`。
- 内部テストは製品版アクセス用の「12人以上・14日間」のクローズドテスト要件に含まれない。

## 本番広告ID版・内部テスト初回リリース（履歴）

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

