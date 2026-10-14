# Google Playビルド手順

## 最新の内部テスト配信版（1.0.8、2026-10-12）

強制広告の「3分以上＋2回クリア」、通常クリア時だけの表示、タイトルの広告削除購入ボタン、未使用EmojiOne除外を含む新版を内部テストへ公開済み。Consoleで「内部テスターに公開」「10月12日 15:23 に公開」を確認。同じAABをクローズドテストAlphaへ追加して審査送信を確定し、現在はGoogleの自動チェック待ち。Alphaの承認・1.0.8配信開始は未確認。

- AAB: `Builds/GooglePlay/siwakenja-20261012-150040.aab`。1.0.8 / versionCode 9、126,035,160 bytes。
- SHA-256: `847B06A9C93BE247E576178B12B47EFBAEC3CDE650914A324A4A05659415877B`。
- Unity 6000.3.10f1、既存 `CommandLineAndroidBuild.BuildGooglePlayAab` をUniCli経由で実行。署名資格情報は既存のDPAPI保存からプロセス環境変数へ渡し、既存ビルド処理で一時設定・復元。任意のPlay依存メタデータ生成は従来と同じく省略。ビルド結果はSucceeded、検証用Editorは終了済み。
- コンパイルはエラー・警告0。EditMode 103/103、PlayMode 76/76、レイアウト240/240成功。
- 署名検証成功。証明書SHA-256 `A3:BB:63:2D:D3:96:1A:86:7F:39:1A:1F:5A:8C:0E:12:BD:77:F8:19:F8:36:C6:19:72:1E:36:84:10:EF:3B:12` は前版1.0.7と一致。
- アプリID、target API 36、AdMob ID、課金権限、Unity Ads不在、ARM64のみ、7ライブラリのELF LOAD配置16 KB以上を確認。ThirdPartyNotices.txtは元ファイルと一致して同梱。
- ビルド依存241件に除外素材なし。旧code 8で検出できるEmojiOneの名前が新版の資産422エントリでは検出されず、未使用フォント名も検出なし。
- Playの難読化解除ファイル・ネイティブデバッグシンボル未添付の警告2件は従来どおりで、公開を妨げるエラーなし。新版の実機購入・復元は今回実施していない。

証拠: `QAOutput/PlayUpdate20261012/`。code 9はアップロード済みなので、次の新規AABはcode 10以上を使用する。以下は各日付時点の履歴。

## ビルド前の変更履歴（2026-10-11）

所有者指定により、強制広告の条件を「3分以上、かつ2回クリア」に調整。初回の猶予も最初のクリアから3分。通常のおしごと成功時だけという条件、失敗・デイリー・エンドレスの広告抑制、タイトルの広告削除購入ボタンを維持する。コンパイルはエラー0、EditMode全103件と関連PlayMode 3件が成功。証跡: `QAOutput/AdFrequencyThreeMinutesTwoClears20261011/`。下記の配信済み1.0.7/code8には未反映で、次の新規AABはversionCode 9以上。

## 変更履歴（2026-10-10）

Android実機フィードバックを反映し、強制広告を通常のおしごとのクリア時のみ、5分以上かつ5回クリアの条件へ変更。失敗・デイリー・エンドレスには表示せず、初回にも5分の猶予を設けた。タイトル右上のHOW TOを広告削除の買い切り購入ボタンに置き換え、既存の購入画面へ直接進めるようにした。コンパイル成功、EditMode 103件・PlayMode 76件・既存レイアウト240件とタイトル6件が成功。証跡: `QAOutput/AdFrequencyAndTitlePurchase20261010/`。この変更も下記の配信中AABには未反映。

未使用EmojiOneの既定TMP参照を解除し、関連4ファイルを `Assets/Editor/ThirdPartySamples/EmojiOne/` に退避済み。現在のビルド依存走査にはEmojiOneなし。証跡とバックアップは `QAOutput/EmojiCleanup20261010/`。下記の配信済み1.0.7/code8 AABは変更していない。次回はversionCode 9以上でビルドし、AABへの非収録を確認して配信する。

## 過去の内部テスト配信版（1.0.7、2026-10-07）

最新のボタン見た目修正と第三者ライセンス表示を含み、未使用のゆうたONEフォントを除外したAABを、2026-10-07に内部テストへ公開。Consoleで「最新リリース: 1.0.7 ボタン表示調整・未使用フォント整理」「内部テスターに公開」を確認。証拠は `QAOutput/PlayRelease20261007/internal-published.png` と同名の `.txt`。既存テスターは[内部テスト参加ページ](https://play.google.com/apps/internaltest/4701640059087792263)から導入できる。旧候補 `siwakenja-20261006-060523.aab` は未使用フォントを含むため配信に使用していない。

- ファイル: `Builds/GooglePlay/siwakenja-20261006-065116.aab`
- 同じAABをクローズドテストAlphaの初回リリースに追加し、2026-10-07に審査送信操作を完了。最終確認はGoogleのクイックチェック中。承認・クローズド配信開始は未確認。証拠: `QAOutput/PlayRelease20261007/closed-review-submitted.png`。
- バージョン: 1.0.7 / versionCode 8。サイズ: 126,088,926 bytes。コード8はアップロード済みのため、次の新規AABには9以上を使用する。
- SHA-256: `A6C092F62980B476C191903265FE88CF020B25A2B6045C45934E56AC555CD4DE`
- Unity 6000.3.10f1、`Tools/Build-GooglePlay.ps1 -SkipDependencyReport`。終了コード0。
- コンパイル: エラー・警告0。EditMode 102/102成功。PlayMode初回は73/74成功で、浮遊アニメーションの0.15秒前後の位置差を測る既存テスト1件が閾値未満で失敗。同じテストをコード変更なしで単独再実行し成功。初回と再実行のXMLを両方保存している。
- 現行のシーン・Prefab9ファイルは整理前後のSHA-256が一致。レイアウト自動検査240件に失敗なし。
- 元OTF3個・TMP資産5個をUnity経由で削除し、古いEditor生成ツール3箇所をNotoへ変更。旧AABで検出できたYomiyasuWideの元OTF全バイト列とフォント名が新AABでは検出されず、該当GUIDの資産エントリも存在しない。Editorのビルド依存関係にも対象フォントなし。
- `Tools/Test-GooglePlayBundle.ps1` 成功。ID、target API 36、AdMobアプリID、Unity Ads不在、ARM64のみ、全7ネイティブライブラリのELF LOAD配置16 KB以上を確認。
- 署名検証成功。署名証明書SHA-256は `A3:BB:63:2D:D3:96:1A:86:7F:39:1A:1F:5A:8C:0E:12:BD:77:F8:19:F8:36:C6:19:72:1E:36:84:10:EF:3B:12` で、1.0.6のAABと一致。
- `com.android.vending.BILLING` 権限を確認。`base/assets/ThirdPartyNotices.txt` が同梱され、元ファイルのSHA-256と一致。
- jarsignerの自己署名・認証チェーン・タイムスタンプに関する警告は残るが、署名検証自体は成功。Android実機およびPlay生成APKの確認は未実施。

証拠: `QAOutput/FontCleanup20261006/` の `aab-verification.txt`、`aab-details.json`、`aab-fonts-before.json`、`aab-fonts-after.json`、`editmode.xml`、`playmode-initial.xml`、`playmode-powa-retest.xml`、`build.log`。フォントの復元用コピー先は同フォルダの `font-backup-manifest.json` に記録。

署名の同一PC内予備コピーを `F:/PrivateBackups/SiwakenjaSigning/20261006` に作成済み。DPAPI保護パスワードは元のWindows環境に依存するため、別PCで復元できるバックアップにはなっていない。

## このPCで署名済みAABを作る（2026-09-08追加）

`Tools/Build-GooglePlay.ps1` を実行する。現在のWindowsユーザーでDPAPI暗号化されたパスワードを復号し、子プロセスの環境変数経由でUnityへ渡す。秘密情報をログやGitへ出力しない。出力は日時付きの `Builds/GooglePlay/siwakenja-*.aab`。

署名ファイルは `%LOCALAPPDATA%/SiwakenjaSigning/upload.keystore` と `password.dpapi`。初回生成は実施済み。`-InitializeSigning` は新規の鍵を作る場合だけ使用し、既存ファイルがあれば停止する。
DPAPIの復号は現在のWindowsユーザー・環境に依存するため、鍵ファイルと暗号化ファイルをコピーするだけでは別PCでの復旧を保証できない。公開前に、パスワードを含めた安全な復旧方法とバックアップを確保する。

UnityのAABビルド入口は環境変数がある場合だけ一時的に署名を設定し、終了時に設定・パスワードを元へ戻す。手動でEditorに設定した既存署名も利用可能。

アプリIDは `com.kibundeseisakujo.siwakenja`。従来の `com.DefaultCompany.kusoge` と別アプリになるため、旧テストAPKのセーブデータは自動では引き継がれない。

Unity 6000.3.10f1とAndroidモジュールを使用する。

## 基本設定チェック

Editorメニュー `Siwakenja > Build > Check Google Play Settings`。
Editorを閉じた状態なら、プロジェクトルートから実行できる。

```powershell
unity run . --timeout 180 -- -executeMethod CommandLineAndroidBuild.ValidateGooglePlaySettings -logFile Logs/GooglePlayPreflight.log
```

仮のアプリID、署名未設定、IL2CPP/ARM64の不足、バージョン未設定を検出すると失敗する。
このチェックはストア審査への適合判定ではない。アイコン、広告、ポリシー、target API、16 KB互換性は別途確認する。

## AAB生成

1. Player Settingsで最終アプリIDとバージョンを設定する。
2. Publishing Settingsでアップロード用Keystoreとaliasを設定する。鍵はプロジェクト外で保管・バックアップする。パスワードはEditorで入力し、コマンド引数やGitに保存しない。
3. `Siwakenja > Build > Google Play AAB` を実行する。
4. `Builds/GooglePlay/siwakenja.aab` を取得する。同名の既存出力がある場合はビルド前に保管する。

既存の `Siwakenja > Build > Android APK` は端末確認用として利用できる。
AAB/APKの切替とGradleプロジェクト書き出し設定はビルド終了時に元へ戻す。Android以外から実行した場合、アクティブビルドターゲットはAndroidへ切り替わる。

バッチ環境で署名資格情報を別途設定できている場合の入口:

```powershell
unity run . --timeout 1800 -- -executeMethod CommandLineAndroidBuild.BuildGooglePlayAab -outputPath Builds/GooglePlay/siwakenja.aab -logFile Logs/GooglePlayBuild.log
```

バッチでは上記のPowerShellスクリプトを使う。スクリプトが署名用の環境変数を設定し、終了時に復元する。

## 生成後

内部テストへのアップロード、事前公開レポート、端末へのPlay経由インストール、更新時のデータ保持を確認する。広告テストモードの切替は本番配信準備と実機広告検証を終えてから行う。
同じアプリへのアップロードでは未使用のversionCodeを使う。

```powershell
Get-FileHash -Algorithm SHA256 Builds/GooglePlay/siwakenja.aab
```

ハッシュとコミット、versionCode、テスト結果をリリース記録へ残す。

## 今回の検証（2026-09-07）

- Unity 6000.3.10f1でEditorスクリプトのコンパイル成功。
- 基本設定チェックをバッチ実行し、仮のアプリIDとアップロード署名未設定の2項目を検出、期待どおり終了コード1で停止。
- ログ: `Logs/GooglePlayPreflight.log`（ローカル生成物）。
- `.jks` / `.keystore` / `.aab` のGit除外を確認。
- この時点では署名済みAABの生成・成功経路・実機検証は未実施。

## 内部テスト候補の生成結果（2026-09-08）

AdMob移行前の候補は `Builds/GooglePlay/siwakenja-20260908-053433.aab`（71,543,820 bytes）。SHA256: `09CF856DA6FFF26ACD75BEE981D953339FBFE19A8BB741C3D9B0F4B1FD56C0F4`。
アプリ内ポリシー閲覧とステージ見出しの重なり修正を含む。EditMode 72/72、PlayMode 39/39成功後に `-SkipDependencyReport` で生成。終了コード0、jarsigner検証成功。広告テストモード、versionCode 1。アップロード・実機動作確認は未実施。
以下は最初の候補の検証記録。

- `./Tools/Build-GooglePlay.ps1 -SkipDependencyReport` で署名済みAAB生成成功。通常の依存情報生成が停止したため、任意のUnity Play依存メタデータを省略。製品版前にこの問題を再確認する。
- 成果物: `Builds/GooglePlay/siwakenja-20260908-040526.aab`、71,534,526 bytes。
- SHA256: `2DA83D8BCFAB51BD24D348A2EBDFC13015870B0DAEC39CD8AA7FC6EBA2D78F8E`。
- 生成Gradle: `com.kibundeseisakujo.siwakenja`、versionName 1.0、versionCode 1、minSdk 25、targetSdk 36。
- jarsignerで署名検証成功（自己署名証明書・タイムスタンプなしの警告あり）。
- AAB内のARM64ネイティブライブラリ7本すべてのLOADセグメントが0x4000（16 KB）アラインメント。Play生成APKの配置と16 KB実機での動作は未検証。
- 広告はテストモード。Playへのアップロードと実機検証は未実施。
- ビルド後の設定保存処理を追加し、最終Editorコンパイルと署名未指定時のpreflight停止を再確認。個人PCの鍵パス・aliasは共有ProjectSettingsから除去し、スクリプト経由でのみ設定する。

## AdMob移行後

最新成果物と検証結果は [admob-build-verification.md](admob-build-verification.md) を参照。上記のAABは旧Unity Ads実装の履歴。
