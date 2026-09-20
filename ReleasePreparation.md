# しわ賢者 リリース準備

確認日: 2026-09-07。基準コミット: `4dd973d`。
ゲーム内容は現状を採用する。公開先はGoogle Playのみ。Play Consoleで「気分で製作所」の個人用アカウントと既存の未公開アプリ「しわ賢者」を確認済み。アカウント確認は完了。ストア設定・配布ビルド・クローズドテスト等の公開準備は残る。公開可能と判定したものではない。

## 最新のアカウント状況（2026-09-08）

ユーザーによる電話番号確認後、Play Consoleホームでアカウント設定の警告と未完了タスクがすべて消え、「アプリを作成」が有効になったことを確認。本人確認・Android実機確認・電話番号確認は完了。ストア素材・AAB・クローズドテスト等のゲーム公開準備は別途残る。

## 配布準備の進捗（2026-09-08）

### 最新更新（05:40 JST）

### Unity Ads管理画面の確認（2026-09-08）

- ユーザーがログインしたUnity CloudのsiwakenjaプロジェクトでGoogle Play Game ID `6139687` を確認。実装と一致。`Interstitial_Android` / `Rewarded_Android` もアクティブで一致。
- Google Play広告配信は有効、管理画面からのテストモード上書きなし。アプリ側は引き続きtestMode=true。
- メディエーションなし、主に児童向けではない指定、Googleファミリー向け無効。ストアIDは未設定。
- 収益受け取りの支払い先プロフィールが未設定。登録画面は携帯電話による2段階認証を要求し、住所→支払い方法→納税申告用紙→完了の流れ。電話番号・受取先・税務情報は未入力。本人の入力を待つ。

- 設定画面にプライバシーポリシーを追加。公開済み本文をアプリへ同梱し、オフライン閲覧、スクロール末尾、Webリンクと戻る操作をEditorで確認。
- 実際のGame Viewから1080×1920のタイトル・遊び方・ステージ選択の3画像を撮影し、ストアに保存。生成したアイコンとバナーだけをAI素材として申告。掲載情報は「審査のために送信する準備ができました」。審査送信は未実施。
- 行政アプリ「いいえ」、金融取引機能なし、健康関連機能なしを保存。ゲーム/アーケード、公開サポートメールも保存。料金が無料であることを確認。
- ステージ選択の見出しと章タブの重なりを修正。修正後のEditMode 72件・PlayMode 39件すべて成功。証拠: `RegressionArtifacts/release-final-editmode.xml`、`release-final-playmode.xml`。
- AdMob移行前の署名済みAAB: `Builds/GooglePlay/siwakenja-20260908-053433.aab`（71,543,820 bytes）。SHA256: `09CF856DA6FFF26ACD75BEE981D953339FBFE19A8BB741C3D9B0F4B1FD56C0F4`。ビルド終了コード0、jarsigner検証成功。広告はテストモードの内部テスト候補。
- 製品版画面はアクセス申請前で地域設定を表示しない。日本限定配信は未設定。レーティング、データセーフティ、AABアップロード、広告管理権限・本番設定、実機検証、素材ライセンス照合、署名鍵バックアップ、12人14日クローズドテストは残る。
- 以下の進捗一覧は作業経過を含む。上記の最新更新を優先する。

- ユーザー指定によりGitHub Pagesでポリシーを一般公開: `https://einn2534.github.io/siwakenja-privacy/`。専用公開リポジトリ `Einn2534/siwakenja-privacy`、ローカル `F:/Unity/siwakenja-policy-github`。ゲーム本体のリポジトリとは別。Pagesビルド成功、匿名HTTP 200と公開本文の一致を確認。
- 公開ポリシーURLをPlay Consoleのプライバシーポリシー欄へ保存。アプリ内の本文・リンク実装と再ビルドは残る。Sitesの確認用ページは所有者限定のまま。
- ユーザー確定: 無料・広告あり、日本のみ、13歳以上、既存Webサイトなし。
- Play Consoleで「広告あり」を保存し、保存完了表示を確認。審査送信は未実施。
- 「ログイン不要」と対象年齢「13～15歳、16～17歳、18歳以上」も保存完了。公開連絡先メール `kibundeseisakujo@gmail.com` の使用はユーザー承認済み。
- プライバシーポリシー用の別プロジェクト `F:/Unity/siwakenja-policy` を作成し、ページのビルドとHTTP 200確認が完了。Sites IDは同プロジェクトの `.openai/hosting.json` に記録。公開範囲は初期の所有者限定。
- ポリシーページの所有者限定デプロイ成功: `https://siwakenja-privacy.einn.chatgpt.site`。一般公開への切替とGoogle Playへの登録は未実施。Sites version 1、deployment `appgdep_6a9f0d9ae5c08191b01d1aab67503a3a`。
- 512×512のアイコンと1024×500のフィーチャーグラフィックを生成し、ストア掲載情報へアップロード・未公開保存。「変更を保存しました」を確認。
- Unityで会社名「気分で製作所」、アプリID `com.kibundeseisakujo.siwakenja`、既定アイコンを設定。旧IDのテストAPKとは別アプリになる。
- ポリシー原稿を `Deliverables/Release/privacy-policy-draft-ja.md` に保存。公開窓口、保管期間、公開HTTPSページ、アプリ内導線、SDK申告の最終確認は残る。
- 現行コードから42画面のEditorキャプチャとレイアウト検証を生成。通常MainのHUD欠落とResultの合成記録を目視で検出したため、ストアには未登録。実プレイから撮り直す。
- アップロード署名鍵をこのPCのLocalAppData内に新規作成。Windows DPAPI保護パスワードを環境変数でUnityへ渡すビルドスクリプトを用意。鍵のバックアップと別PCでの復旧手順の確保は公開前に必要。
- 標準AABビルドはUnityの `WritePlayDependencies` で3分以上停止し、該当補助プロセスのみ終了して切り分けた。任意の依存情報生成を一時的に無効にして内部テスト候補AABの生成に成功。広告は引き続きテストモード。
- AABは `Builds/GooglePlay/siwakenja-20260908-040526.aab`。署名検証成功、生成Gradleのtarget API 36、ARM64ライブラリ7本の16 KB ELFアラインメントを確認。詳細とSHA256は `Deliverables/Release/google-play-build.md`。Play生成APKと実機動作は未検証。

## Play Console確認結果（2026-09-07時点）

- ログイン中のアカウントはユーザー指定のGoogleアカウントと一致（Android実機確認ページの表示で確認）。公開名義は「気分で製作所」。
- 既存アプリ「しわ賢者」は未公開、インストール済みユーザー0。新規アプリの重複作成は不要。
- アカウント設定は本人確認、Androidモバイルデバイス確認、連絡先電話番号確認が残る。電話番号確認は本人確認の承認等が先に必要と表示されている。
- Android実機確認は所有者がAndroid端末のGoogle Play Consoleアプリに指定アカウントでログインし、「気分で製作所」を選び、端末上の手順を実施する。
- 本人確認は書類提出が必要。今回、書類の入力・取得・アップロードは行っていない。
- 製品版アクセスには12人以上が14日以上継続参加するクローズドテストが必要。現在のオプトイン人数は0。ゲーム設定完了後にクローズドテストを開始する。
- 初回確認時のゲーム設定タスクは0/11。プライバシーポリシー、ログインの詳細、広告、レーティング、ターゲットユーザー、データセーフティ、行政アプリ、金融取引機能、健康、カテゴリ・連絡先、ストア掲載情報。
- 日本語の簡単な説明（42文字）と詳しい説明（399文字）を入力し、「未公開として保存」を実施。「変更を保存しました」を確認。文面は `Deliverables/Release/store-listing-ja.md` と一致。公開・審査送信は未実施。
- アイコン、フィーチャーグラフィック、スクリーンショットは未登録。アプリID・課金/広告方針・署名は今回のConsole確認で確定していない。

参照: [既存アプリのダッシュボード](https://play.google.com/console/u/0/developers/6254143143837166570/app/4974169236260185677/app-dashboard)、[アカウント設定](https://play.google.com/console/u/0/developers/6254143143837166570/app-list)。

## 初回調査時点の状態（2026-09-07、以後の変更は上記進捗を参照）

| 項目 | 確認結果 | 次の作業 |
| --- | --- | --- |
| Unity | 6000.3.10f1 | リリース候補も同バージョンで再現する |
| 本編シーン | Title / StageSelect / Main / Result の4つが有効 | 配布ビルドで起動と全導線を確認 |
| Android | IL2CPP / ARM64、最低API 25、target APIは自動 | 最終成果物のtarget APIと16 KB対応を検証 |
| アプリ名 | しわ賢者 | ストア表記と統一 |
| 発行元 / アプリID | DefaultCompany / com.DefaultCompany.kusoge | 発行元名と継続利用するIDを確定して設定 |
| バージョン | 1.0 / versionCode 1 | 初回リリース番号を確定、アップロードごとにcodeを増加 |
| 署名 | Keystore / aliasが空 | アップロード鍵の作成・安全な保管、Play App Signing設定 |
| ビルド手順 | APKに加えBuildGooglePlayAabと基本設定チェックを追加 | 最終ID・署名を設定後、AABを生成して検証 |
| アイコン | PlayerSettings内のアイコン割当が空 | ランチャー用とストア用を制作・設定 |
| 広告 | AdMob SDK 11.5.0へ移行、公式テスト広告IDを使用 | 実機検証とアカウント審査後に本番化 |
| 保存・計測 | PlayerPrefsと端末内JSONL | 更新時の保存維持、長期利用時のログ容量を確認 |
| 素材 | 音源出典表あり、複数の外部フォントあり | 配布対象の素材ごとに利用条件・必要な表示を確認 |

根拠: `ProjectSettings/ProjectSettings.asset`、`EditorBuildSettings.asset`、`Assets/Editor/CommandLineAndroidBuild.cs`、`Assets/Scripts/Ads/AdMobConfiguration.cs`、`Packages/manifest.json`、`Assets/Scripts/System/LocalTelemetryService.cs`。
署名やSDK設定が外部で管理されている可能性は未確認。Game IDの有効性と管理権限も未確認。

## 進める順序

1. 公開先はGoogle Play、アカウント取得済み。公開名義、配布地域・対象年齢、無料/有料と広告の有無を確定する。アカウントのテスト要件も確認する。
2. アプリID・バージョン・アイコン・署名・AABビルドを整える。鍵とパスワードはGitへ保存しない。
3. 広告SDKを含むデータ取扱いを調査し、プライバシーポリシーの公開URL、問い合わせ先、アプリ内の表示導線を用意する。データセーフティ回答を実装と一致させる。
4. 素材の出典・利用条件を整理し、必要なライセンス文やクレジットを同梱する。
5. ストア素材・紹介文・コンテンツレーティング等を準備する。
6. リリース候補をビルドし、内部テストと実機検証を実施する。対象アカウントではクローズドテストを先に日程へ組み込む。
7. Play Consoleの検査結果、実機結果、公開情報を突き合わせ、製品版申請へ進む。

## 広告・データ・素材で未確認のこと

- 広告はテストモードのまま。本番への切替だけでは準備完了にならない。報酬広告の完了/中断/失敗、在庫なし、オフライン、復帰を実機で検証する。
- アプリ独自の計測は `siwakenja-events-v1.jsonl` へ日時、モード、スコアではなくミス数・コンボ・プレイ時間・結果・再挑戦などを追記する。保存先はpersistentDataPath。ローテーションは現実装に見当たらない。
- 独自計測がローカル保存でも、広告SDKを含むアプリ全体を「データ収集なし」とは判断できない。SDKの挙動と最終AndroidManifestの権限を別途調べる。
- スクリプト検索ではプライバシーポリシーや広告同意の導線は確認できなかった。公開地域・対象年齢を確定して必要な実装を判断する。
- `com.unity.purchasing` は導入済みだが、課金処理は今回の本編スクリプト検索で確認できていない。実際の課金有無を確定して申告する。
- 音源: `Assets/Resources/Audio/AudioSources.md` にCC0として出典記録あり。原典との照合は未実施。
- フォント: Y1YomiyasuWide / Y1BroadBlack / Y1BadBoySlab / DotGothic16の利用条件と必要なライセンス文を確認する。LiberationSansにはOFL文書あり。画像も配布対象ごとに出典・権利情報を確認する。

## リリース候補の受入チェック

各項目は未実施。実施日・端末/OS・versionCode・結果・証拠パスを残す。

- [ ] 署名済みリリース候補を実機へ新規インストールし、Titleから開始できる。
- [ ] 15面の進行、次面解放、星・ベスト記録、Endless、Dailyが動く。
- [ ] 初回説明、Pause、Retry、Result、音量設定、端末の戻る操作を確認する。
- [ ] バックグラウンド移行・復帰・強制終了後も音/入力/進行が正常。
- [ ] オフラインで本編が進行し、広告取得失敗で操作が止まらない。
- [ ] 広告完了時のみ報酬付与、中断/失敗時は付与なし。Dailyでは広告なし。
- [ ] 旧版から同一ID・同一署名で更新して進行・設定が保持される。
- [ ] 小型画面・縦長画面・切り欠き端末でUIとタップ領域を確認する。
- [ ] 継続プレイでクラッシュ、著しい発熱、メモリ/ログ容量の増加を確認する。
- [ ] AABのtarget API、ARM64、全ネイティブライブラリの16 KB互換性を確認する。
- [ ] Play Consoleの事前公開レポートと申告内容を確認する。
- [ ] 最終ビルドのコミット、Unity版、versionCode、SHA-256、署名証明書情報を記録する。

既存の回帰入口は `Tools/Run-Regression.ps1`。過去の実績は `execution-notes.md` にEditMode 32/32、PlayMode 9/9、42 captures / 0 failuresと記録されているが、今回の準備作業で再実行した結果ではない。公開候補に対して再実行する。

## Google Play公式参照

2026-09-07参照。公開時にはConsoleで適用条件を再確認する。

- [アプリ作成・設定とAAB](https://support.google.com/googleplay/android-developer/answer/9859152?hl=ja)
- [ストア画像の仕様](https://support.google.com/googleplay/android-developer/answer/9866151?hl=ja): ストアアイコン512×512、フィーチャーグラフィック1024×500。実ゲームのスクリーンショットを用意する。
- [新しい個人アカウントのテスト要件](https://support.google.com/googleplay/android-developer/answer/14151465?hl=ja): 2023-11-13より後に作成された個人アカウントでは、12人以上が14日間連続で参加するクローズドテスト等の要件を確認する。期間経過だけで公開承認されるわけではない。
- [16 KBページサイズ対応](https://developer.android.com/guide/practices/page-sizes): Unityのバージョンだけで判定せず、SDKを含む成果物を検証する。

## AdMobへの移行（2026-09-08）

Unity Adsはユーザー希望で廃止。最新の実装・テスト・AABは Deliverables/Release/admob-setup.md と admob-build-verification.md を参照。上記のUnity Adsや古いAABの記述は作業履歴。

### 本番広告IDと内部テスト公開（2026-09-08）

本番広告IDへ切り替え、Pixel 9aをSDKテスト端末に指定。116テスト成功、署名・AAB検証成功。101458.aab（versionCode 1）をGoogle Playへアップロードし、内部テスター1名へ公開。最新詳細は Deliverables/Release/internal-test-release.md。AdMobアカウントは審査中。

