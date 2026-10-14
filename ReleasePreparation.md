# しわ賢者 リリース準備

## 2026-10-15の追記：1.0.9へテスト配信を更新

所有者の「googleplayアプデしといて」に基づき、1.0.9（versionCode 10）を既存のアップロード署名鍵でビルドし、内部テストへ公開した。Consoleで「1.0.9 チュートリアルと画面表示の改善」「内部テスターに公開」「10月15日 3:08 に公開」を確認。[内部テスト参加ページ](https://play.google.com/apps/internaltest/4701640059087792263)から反映後に更新できる。

ひみつ便チュートリアル、星評価の調整、仕分けレーンと操作ボタン、テーマ別カード背景、モニターの透過修正を含む。Unity 6000.3.10f1でビルド成功、エラー0。直前の同じソースでEditMode 112件・PlayMode 78件成功。署名の既存版との一致、target API 36、課金権限、ARM64の7ライブラリの16 KB配置、第三者ライセンス同梱を確認した。

同じcode 10をクローズドテストAlphaへプロモートし、変更1件の審査送信を確定。「審査中の変更」に1.0.9を確認し、最終確認時はGoogleのクイックチェック中。完了後に審査へ自動送信される。管理対象の公開はオフで、承認後に既存の日本・テスター向け設定で配信される。Alphaの1.0.9承認・配信開始は未確認。製品版アクセスは未取得で、一般公開ではない。

AABは `Builds/GooglePlay/siwakenja-1.0.9-10.aab`（159,048,700 bytes）、SHA-256は `FE4D7B9F1F1E7E361B55946E5B4C9B1A53E0E880F01A755C32CE57E1632CF764`。Unityの150 MB超過確認は所有者がサイズ計算・結果確認で解消し、ビルド完了を確認した。Play表示の新規インストールサイズは157 MB（前版比+33 MB）、サポート対象デバイス数の変化なし。実機での更新・購入・復元は今回未実施。

証拠は `QAOutput/PlayUpdate20261015/`。配信詳細は `Deliverables/Release/internal-test-release.md`、ビルド検証は `Deliverables/Release/google-play-build.md`。次の新規AABはversionCode 11以上。前版1.0.8がAlphaでも配信済みであることを今回確認した。以下は各日付時点の履歴。

## 2026-10-12の追記：1.0.8へテスト配信を更新

所有者の「アプデしといて」に基づき、1.0.8（versionCode 9）を同じアップロード署名鍵でビルドし、内部テストへ公開。Consoleで「最新リリース: 1.0.8 広告頻度と購入導線の改善」「内部テスターに公開」「10月12日 15:23 に公開」を確認した。[内部テスト参加ページ](https://play.google.com/apps/internaltest/4701640059087792263)から反映後に更新できる。

通常のおしごとのクリア時だけ「3分以上、かつ2回クリア」で強制広告を許可し、失敗・デイリー・エンドレスでは表示しない。タイトル右上の広告削除購入ボタンと、未使用EmojiOneの除外を配信版へ反映した。Unity 6000.3.10f1、コンパイルはエラー・警告0、EditMode 103件・PlayMode 76件・レイアウト検査240件がすべて成功。署名の前版との一致、target API 36、課金権限、ARM64 7ライブラリの16 KB配置、第三者ライセンス同梱、除外素材の非収録を確認した。

同じcode 9をクローズドテストAlphaへ追加し、更新1件の審査送信を確定。公開の概要で「審査中の変更」と1.0.8を確認した。現在はGoogleのクイックチェック待ちで、完了後に審査へ自動送信される。管理対象の公開はオフで、承認後に既存の日本・テスター向け設定で配信される。Alphaの1.0.8の承認・配信開始は未確認で、一般公開ではない。

新AABは `Builds/GooglePlay/siwakenja-20261012-150040.aab`（126,035,160 bytes）、SHA-256は `847B06A9C93BE247E576178B12B47EFBAEC3CDE650914A324A4A05659415877B`。証拠は `QAOutput/PlayUpdate20261012/` の `internal-published.png/.txt`、`alpha-review-submitted.png/.txt`、検証記録とXML。次の新規AABはversionCode 10以上。以下の「配信中1.0.7/code8」「未反映」等は各日付時点の履歴。

## 2026-10-11の追記：強制広告を3分以上＋2回クリアへ調整

所有者の指定により、強制広告の条件を「前回表示から180秒以上、かつ2回クリア」に変更。初回も最初のクリアから3分待つ。通常のおしごとの成功時だけに表示し、失敗時・デイリー・エンドレスの広告抑制とタイトルの購入ボタンは維持した。仕様と関連テストも更新。

Unity 6000.3.10f1でコンパイル成功（エラー0、既存のEditorアイコンAPIの非推奨警告1件）。EditMode全103件と関連PlayMode 3件（通常クリア・結果・再挑戦、失敗結果、購入済み広告抑制）が成功。証跡は `QAOutput/AdFrequencyThreeMinutesTwoClears20261011/`。今回もAABの生成・配信更新は行っておらず、配信中1.0.7/code8には未反映。次の新規AABはversionCode 9以上。

## 2026-10-10の追記：広告頻度とタイトルの購入導線

所有者によるAndroid実機確認で「広告が多すぎる」「課金導線が薄い」とのフィードバックを受け、強制広告を通常のおしごとの成功時だけに限定。前回の広告から5分以上、かつ5回以上のクリアがそろった場合だけ表示する。失敗時・デイリー・エンドレスでは表示せず、初回も最初のクリアから5分間は表示しない。任意の復活広告は従来どおり。

タイトル右上の「仕事の手引き（HOW TO）」を「広告を削除／買い切り」ボタンに置き換え、既存の購入画面を直接開くように変更。購入済みは「広告削除済み／購入済み」に更新し、閉じるとタイトルへ戻る。ステージ選択とポーズの遊び方、設定からの購入・復元は引き続き利用できる。Titleシーンの変更はUnity APIで保存し、他のシーン・Prefabは変更していない。

Unity 6000.3.10f1でコンパイル成功（エラー・警告0）。EditMode 103件、PlayMode 76件、既存のレイアウト検査240件とタイトルの未購入・購入済み表示6件がすべて成功。3種類の画面サイズでタイトルと購入画面を撮影し、文字表示、スクロール、戻る操作を確認した。購入済み表示はEditorでの模擬状態で、Google Playの実決済・復元の検証ではない。証拠と作業前バックアップは `QAOutput/AdFrequencyAndTitlePurchase20261010/`。

**現在のプロジェクトに反映済み。配信中の1.0.7/code8には未反映で、配信への反映にはversionCode 9以上の新しいAABが必要。** 今回はAAB生成・Play Consoleへのアップロードを行っていない。

## 2026-10-10の追記：画像の出典確認と未使用素材の除外

所有者が、ポワ・車・背景・UIの制作方法への回答として「ぜんぶAI生成」と申告。これらの元絵と、それを参照したアイコン・Powaアニメーション等は、所有者申告によるAI生成素材として記録し、**元絵の出典分類についての回答待ちは解消**した。詳細は `QAOutput/ReleaseReadiness20261008/image-audit-summary.txt` と `image-inventory.json/.csv` の10月10日追記を参照。

未使用のUnity同梱EmojiOneは、Unity APIでTMP既定Sprite参照を解除し、画像・Sprite Asset・JSON・帰属表示の4ファイルを `Assets/Editor/ThirdPartySamples/EmojiOne/` へ退避した。ファイル内容・GUID・metaを維持し、全有効シーン・Resources・StreamingAssets・プリロード資産・既定アイコンからの依存関係にEmojiOneが残らないことを確認。シーン・Prefabの内容は変更なし。証跡と元設定のバックアップは `QAOutput/EmojiCleanup20261010/`。コンパイルはエラー・警告0、EditMode 102件・PlayMode 74件・レイアウト検査240件すべて成功。Resultの自動撮影には動的表示を確認できない箇所があるため、表示確認の制約は同フォルダのREADME・検証記録を参照。

フォント・スクリプト作図等の確認済み出典は従来の記録を維持する。以下の配信・審査・AdMobの情報は10月8日の確認結果。**除外は現在のプロジェクトに反映済みで、配信中1.0.7/code8のAABは未変更**。配信版へ反映するにはversionCode 9以上で新しいAABを作成・配信する。

## 2026-10-10の追記：Google Play売上の受取口座

所有者の口座情報・名義の指定と、Google Payments規約に同意して保存する旨の直前確認を受け、Google Playアプリの収益受取用銀行口座をメインのお支払い方法として登録。お支払いセンターの「お受け取り方法」に口座と「確認待ち」が表示された。口座確認は未完了。Googleの案内では3営業日以内に25円未満のデポジットが入金されるため、実際の入金額を確認して、お支払い方法のページで正確な金額を入力する。口座番号等の個人金融情報はこの文書へ記載しない。

## 公開準備の状況（2026-10-08確認）

この節は10月8日の確認記録。画像の出典は上の10月10日追記を優先する。下の10月7日・9月の節は履歴。**1.0.7（versionCode 8）は内部テストとクローズドテストAlphaに配信済み**。10月8日にAlphaの「選択したテスターに公開されました」「10月7日 5:42 に公開」を確認した。初回審査の承認待ちは解消。一般公開・製品版アクセスの承認ではない。

完了:

- ストア画像を1080×1920・24bit RGBの新しい4枚へ差し替え、タイトル → 通常プレイ → ステージ選択 → 実クリア結果の順に保存。通常入力で120点・0ミスを達成した実画面で、得点・進行・車位置の注入や合成はない。画像と撮影記録は `Deliverables/Release/screenshots/20261008/`。撮影後の設定・保存データを復元し、Play Modeを終了。ゲームコード・アセット・配信AABは変更していない。
- 日本語の簡単な説明・詳しい説明・携帯電話スクリーンショットの3件を審査送信し、「審査中の変更」へ移ったことを確認。現在はクイックチェック中で、完了後に審査へ自動送信される。10月8日にも説明文2件が未送信変更に含まれていたため、下の10月7日の記録だけから新説明文を公開済みとは扱わない。生成キャラクター等を含む4枚にもAIラベルを付け、既存アイコン・バナーの申告は維持した。証跡: `QAOutput/ReleaseReadiness20261008/store-review-submitted.png/.txt`。
- 開発者サイト [気分で製作所](https://einn2534.github.io/) と [app-ads.txt](https://einn2534.github.io/app-ads.txt) を専用公開リポジトリ `Einn2534/Einn2534.github.io` から公開。Pagesビルド成功・匿名HTTP 200・AdMob販売者行の一致を確認。Play Consoleのストア連絡先のWebサイト欄へルートURLを登録し「変更内容が公開されました」を確認。既存ポリシーサイトは継続使用。証跡: `app-ads-verification.json`、`play-developer-website-published.png/.txt`（同QAフォルダ）。
- 指定されたF:へ移植可能な暗号化署名バックアップを作成。`F:/PrivateBackups/SiwakenjaSigning/20261008-portable` に保存し、復旧キーは別の `C:/Users/einn/SiwakenjaRecoveryKeys/20261008/signing-recovery.key` に保存。復元パスワードでJKSを開き、元の鍵・証明書との一致を確認。同じPCの別フォルダでの検証であり、別の実機PCでの復元試験ではない。秘密値をGit・作業記録へ保存していない。証跡: `signing-backup-verification.txt`。
- 画像163点を棚卸し。現行の依存関係とPlayerSettingsアイコンを合わせ公開関連122点を整理。制作・作図記録あり25点、元絵の所有確認待ち96点、EmojiOneの適用許諾未確定1点。Powa Flipbook全37点は原本/承認済み試作フレームとSHA256照合済み。詳細: `image-audit-summary.txt`、`image-inventory.json/.csv`。

残り:

1. **所有者のAndroid実機確認。** ユーザー指定によりスマホ側で実施する。導入、30面・9題材、広告、テスト購入・取消・承認待ち・復元、保存・復帰を確認。実施表: `QAOutput/ReleaseReadiness20261008/device-checklist.txt`。HOW TOの番号と画像、TutorialのSKIPとHUDの重なりも実機で確認する。
2. **12人以上・14日間のクローズドテスト。** [参加ページ](https://play.google.com/apps/testing/com.kibundeseisakujo.siwakenja)。10月7日の確認値はリスト登録2人・オプトイン0人。10月8日の最新人数は未再確認。内部テスト参加者は先に内部テストを退出する。実際の利用・フィードバック・改善記録を残す。毎日起動という一律条件は公式に示されていない。[公式要件](https://support.google.com/googleplay/android-developer/answer/14151465?hl=ja)。招待文は既存の `QAOutput/ReleaseReadiness20261006/tester-invitation-draft.txt`、未送信。
3. **AdMobストア連携・app-ads.txt認証。** 10月8日も正確なパッケージ名の検索結果は0件で「次へ」は無効。アプリは「要審査」「広告配信を制限しています」。サイト公開・Console登録とAdMob認証完了は別。公式手順はサポート対象ストアでの一般公開を求め、検索反映に時間がかかる場合もある。今回の未検出原因は断定せず、掲載が検索可能になった段階で連携する。[公式手順](https://support.google.com/admob/answer/10037806?hl=ja)。証跡: `admob-store-search.png/.txt`。
4. **次の配信ビルドへEmojiOne除外を反映。** 元絵の出典分類とプロジェクト内の未使用EmojiOne除外は10月10日に完了。現在配信中のcode8は未変更なので、次候補のビルド・AAB非収録確認・テスト配信が残る。
5. **受取口座の確認と復旧キーのPC外保管。** Google Play売上の受取口座は10月10日に登録済みで「確認待ち」。Googleからのデポジット入金額を入力して口座確認を完了する。税務情報は本人が確認・登録する。F:のバックアップだけでは復旧キー紛失時に復元できないため、上記キーを別媒体等へ保管する。
6. **今回のストア変更の審査結果確認。** 審査送信の受付と承認・公開は別。現在はGoogleの処理待ち。

今回の証跡は `QAOutput/ReleaseReadiness20261008/`。配信AABは引き続き `Builds/GooglePlay/siwakenja-20261006-065116.aab`。署名・16 KB対応・テストの検証記録は `Deliverables/Release/google-play-build.md`。新ビルドではversionCode 9以上が必要。

## 2026-10-07の記録（履歴）

以下は10月7日時点の記録。現在の状態は冒頭の10月8日の節を参照する。現在のゲームは6章30面・9題材。未使用フォントを除外し、最新のボタン画像・サイズ・中央揃え修正を含む1.0.7（versionCode 8）を内部テストに公開済み。同じAABでクローズドテストAlphaのリリースを作成し、IARC等を含む15件の変更の審査送信操作も完了。最終確認時は「審査中の変更」に入り、Googleのクイックチェック中（完了後に審査へ自動送信）だった。クローズドテストの承認・配信開始はまだ確認していない。

今回完了したこと:

- 2026-10-07、所有者の「IARC規約への同意」「テスト配信して」に基づきIARCを回答・保存。IARC Genericは3歳以上、ゲーム内購入あり。対象ユーザーの13歳以上設定とは別。回答・地域別判定は `QAOutput/PlayRelease20261007/iarc-answers.txt` と `iarc-rating-preview.txt`。
- 同日、1.0.7を内部テストに公開。Consoleに「内部テスターに公開」、10月7日4:22公開と表示。既存2人は[内部テスト参加ページ](https://play.google.com/apps/internaltest/4701640059087792263)から導入可能。実機購入・復元等の確認は未実施。
- クローズドテストAlphaへ同じversionCode 8を追加し、「1.0.7 クローズドテスト開始」を作成。審査前チェックで判明した広告IDの未申告を、AdMobの利用実態に合わせて「使用あり・分析・広告/マーケティング・不正防止」に修正保存。クローズドテスト、ストア掲載情報、IARC等の審査送信を確定し、Googleの自動チェック待ちまで進めた。管理対象の公開はオフで、承認後は設定済みの日本・テスターリスト向けに配信される。証拠は `QAOutput/PlayRelease20261007/closed-review-submitted.png` と同名 `.txt`。
- フォント整理後にUnity 6000.3.10f1でコンパイル成功（エラー・警告0）、EditMode 102件成功。PlayModeは初回73件成功・浮遊量の時間依存テスト1件失敗で、該当テストの単独再実行はコード変更なしで成功。結果は `QAOutput/FontCleanup20261006/` に保存。
- `Builds/GooglePlay/siwakenja-20261006-065116.aab` を生成し、署名・前版との署名証明書一致、versionCode 8、target API 36、課金権限、ARM64の7ライブラリの16 KB配置、ライセンス文書の同梱、不要フォントの非同梱を検証。詳細は `Deliverables/Release/google-play-build.md`。
- Play Consoleの説明文を30面・9題材・広告削除購入に更新し、10月7日の審査送信対象に含めた。原稿は `Deliverables/Release/store-listing-ja.md`、10月6日時点の保存証拠は `QAOutput/ReleaseReadiness20261006/store-listing-saved.jpg`。スクリーンショットは既存の3枚で、最新画面への更新は残る。
- クローズドテストAlphaは日本限定、既存のテスターリスト2人と問い合わせメールを設定済み。10月7日の審査送信前にダッシュボードでオプトイン0人を確認。内部テスター登録人数はクローズドテスト参加人数ではない。
- 音源7ファイルの出典6ページを照合し、CC0の表示を確認。Noto Sans CJK JP・DotGothic16・Liberation Sansの著作権表示とOFL全文、音源クレジットを `Assets/StreamingAssets/ThirdPartyNotices.txt` に追加。
- 署名鍵とDPAPI保護済みパスワードを、Git管理外の `F:/PrivateBackups/SiwakenjaSigning/20261006` へ複製し、コピー元との一致を確認。アクセスを現在のWindowsユーザーへ限定。これは同一PC上の予備コピーであり、別PCで復元できるバックアップは未完了。
- AdMobはアカウント承認済み。アプリは「要審査」「広告配信を制限しています」で、ストア連携が残る。パッケージID検索ではストア掲載が見つからず、連携未実施。管理画面から取得した `app-ads.txt` を `QAOutput/ReleaseReadiness20261006/app-ads.txt` に用意したが、ドメイン直下への公開とストア連絡先へのWebサイト登録は未実施。

次に必要なこと:

1. **未使用フォントの整理は実施済み。** 現在の文字設定67箇所と実行時コードはすでにNoto系で、ゆうたONEはResourcesに残った未使用資産として同梱されていた。所有者の「整理して」に基づき、元OTF3個・TMP資産5個をUnity経由で除去し、古い画面生成ツールの参照3箇所をNoto系へ変更。シーン・Prefab9ファイルのSHA-256は変更前と一致。見た目を変更するためのフォント置換は不要だった。復元用コピーと調査結果は `QAOutput/FontCleanup20261006/font-cleanup-audit.txt` を参照。旧候補 `siwakenja-20261006-060523.aab` には当該フォントが残るので配信に使用しない。全画像の網羅的な権利照合は別途未完了。
2. **IARCの回答・保存と審査送信操作は完了。** Googleの自動チェック・審査結果を確認する。クローズドテストは承認待ちで、14日間の要件を満たす参加はまだ始まっていない。
3. 配信済みの1.0.7をGoogle Playから実機で更新・新規導入し、購入・キャンセル・承認待ち・復元、広告と復活、30面と各テーマを確認する。今回実機検証は行っていない。実施表は `QAOutput/ReleaseReadiness20261006/device-checklist.txt`。
4. クローズドテストの承認後、12人以上にオプトインしてもらい、14日間連続で参加状態を維持して実際のフィードバックと改善記録を残す。公式資料に毎日必ず起動するという一律の要件はないが、利用実態も審査対象。現在登録された2人だけでは不足し、2人とも参加する場合でも追加10人以上が必要。内部テスト参加者は一度内部テストを退出してからクローズドテストに参加する。招待文案は `QAOutput/ReleaseReadiness20261006/tester-invitation-draft.txt`（未送信）。最新ストア画像の整備も残る。[Googleのテスト要件](https://support.google.com/googleplay/android-developer/answer/14151465?hl=ja)、[テスト間の切り替え](https://support.google.com/googleplay/android-developer/answer/9845334?hl=ja)。
5. ストア掲載が検索できるようになったらAdMobと連携する。app-ads.txtは開発者Webサイトの**ドメイン直下**へ公開する必要がある。既存のGitHub Pagesプロジェクト配下への配置だけでは完了にならない。[AdMob公式手順](https://support.google.com/admob/answer/9363762?hl=ja)。
6. 売上受取方法の本人登録と、署名鍵・パスワードのPC故障にも耐える復旧方法を確保する。秘密情報はGitや通常の作業記録へ保存しない。

最新の配信・申告・審査送信の証拠: `QAOutput/PlayRelease20261007/`。10月6日の準備記録は `QAOutput/ReleaseReadiness20261006/`。署名付きAABの確定結果は `Deliverables/Release/google-play-build.md` を参照。

## 以前の記録

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
- [ ] 30面の進行、次面解放、星・ベスト記録、Endless、Dailyが動く。
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

