# Google Playビルド手順

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
