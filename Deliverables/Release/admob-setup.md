# AdMob移行準備

2026-09-08。ユーザーはUnity Adsの利用を取りやめ、AdMobを選択。
Googleアカウント: kibundeseisakujo@gmail.com。

## 登録済み

- Android未公開アプリ「しわ賢者」
- App ID: `ca-app-pub-7699373274594279~5747435059`
- リワード `Rewarded_Android_Continue`: `ca-app-pub-7699373274594279/7395558641`
- 報酬: 復活、数量1
- インタースティシャル `Interstitial_Android_Result`: `ca-app-pub-7699373274594279/5809451135`
- 外部メディエーションでの入札は使用しない。
- Google Play用の既存個人支払いプロフィールを関連付けて送信。AdSense（日本）の支払いページが表示されたことを確認。本人・住所・税務の追加確認や銀行受取方法は未確認。

## 未完了

- アカウント審査の結果と、支払いに関する追加タスクの確認。プロフィール登録直後は上部の未完了バナーが残っているため、登録と承認を同一視しない。
- Android実機での広告通信、復活、全画面広告、バックグラウンド復帰の確認。
- 実機確認後、本番広告IDへの切り替えと本番用AABの再生成。
- Playデータセーフティの更新、公開後のストア紐付け・app-ads.txt等の確認。

ゲーム実装はAdMobへ移行済み。Google Mobile Ads Unity SDK 11.5.0（Android SDK 25.4.0 / UMP 4.0.0）。Unity Adsパッケージと旧設定を撤去。実機確認後にAdMobConfiguration.UseTestAds = falseへ切り替え済み。検証用Pixel 9aはTestDeviceIdsへ登録し、引き続きテスト広告を使用する。
Unity/Tipalti税務フォームは未送信、サポート問い合わせも送信していない。

## 実装と検証

- 同意情報更新 → 必要な同意画面 → CanRequestAds確認 → SDK初期化 → 広告読み込み。
- 報酬イベントを受信し、広告が閉じた後だけ復活通知。中断・表示失敗は報酬なし。通知は一度のみ。
- 読み込み失敗は30秒間隔、広告キャッシュは55分で更新。
- UMPのプライバシー設定が必要な場合、設定→プライバシーポリシーに変更ボタンを表示。
- Unity Editorでは通常SDK初期化なし。明示的にInitializeEditorTestAdsを実行してGoogleのEditor用モックを検証。
- Editorモック: リワード表示→閉じる→成功通知1回→再読み込み、インタースティシャル表示中の通知0回→閉じる→通知1回を確認。
- ネットワーク経由で実際のAndroid広告を配信した検証ではない。adb devicesに接続端末なし。
- 公開ポリシーとオフライン本文をAdMob対応へ更新。GitHub Pages commit 6f834c8、HTTP200、AdMob記載あり、Unity Ads記載なしを確認。
- 新AABと最終テスト結果はadmob-build-verification.mdを参照。

SDK tarballはPackages/Vendorに保存（OpenUPM、Google公式リポジトリ由来）。SHA1: 0e7fbe0879502e9f16967990993b1ddd01d9aea7。UPM Client APIで相対パス導入。

参照:
- https://developers.google.com/admob/unity/quick-start
- https://developers.google.com/admob/unity/privacy
- https://developers.google.com/admob/unity/rewarded
- https://developers.google.com/admob/android/privacy/play-data-disclosure
実機でリワードテスト広告と復活を確認済み。UMPメッセージ公開済み。詳細はadmob-build-verification.mdの実機検証追記を参照。

最新の本番ID版AABとPlay内部テスト公開結果はinternal-test-release.mdを参照。
