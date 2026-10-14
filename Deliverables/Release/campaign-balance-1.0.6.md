# 30ステージ拡充・難易度調整 1.0.6

- Google Play内部テストへ2026-10-02 21:42 JSTに公開。Consoleで最新リリースと「内部テスターに公開」を確認。
- リリース名: `1.0.6 30ステージ拡充・難易度調整`。
- versionName `1.0.6` / versionCode `7` / release ID `7`。
- Package: `com.kibundeseisakujo.siwakenja`。
- AAB: `Builds/GooglePlay/siwakenja-20261002-212521.aab`。
- サイズ: 125,381,131 bytes。
- SHA256: `E7F01FD3A902E3DD283BB9D708ADF00A807198F6BB8AFE138F37EC617CD5260D`。
- アップロード証明書SHA256: `A3:BB:63:2D:D3:96:1A:86:7F:39:1A:1F:5A:8C:0E:12:BD:77:F8:19:F8:36:C6:19:72:1E:36:84:10:EF:3B:12`。前回1.0.5と一致。
- Unity `6000.3.10f1`。既存の鍵と `Tools/Build-GooglePlay.ps1 -SkipDependencyReport` を使用。任意のPlay依存メタデータ生成は以前の内部テストと同じ手順で省略し、ビルド後に署名・依存レポート設定を復元。

## 更新内容

おしごとを全30ステージに拡充。豆・爬虫類・ポーズ・時代・ひらがなの各テーマに応用と検定を追加。後半へ進むほど手応えが増すよう、出現速度・間隔・目標・ギミックを調整。詳細は `Specification.md` と `Assets/Resources/StageDatabase.asset`。

## 検証

- Unityコンパイル: エラー・警告0件。
- EditMode: 102成功、失敗・スキップ0。`Deliverables/SortingThemes/Tests/google-play-1-0-6-editmode.xml`。
- PlayMode: 74成功、失敗・スキップ0。全30面のMain/Result進行、テーマ、広告、購入画面を含む。`Deliverables/SortingThemes/Tests/google-play-1-0-6-playmode.xml`。
- AAB署名、ID、バージョン、target API 36、AdMob ID、課金権限、ARM64のみ、全7ネイティブライブラリのELF LOAD 16KB以上のアラインメントを検証。
- 検査記録: `QAOutput/CampaignBalanceRelease/aab-verification-1.0.6.txt`、`aab-manifest-1.0.6.xml`、`aab-release-1.0.6.json`。
- Play Console: 対応端末数の増減なし。従来と同じ難読化解除ファイルとネイティブデバッグシンボル未登録の警告2件。配信は成功。

## 配信

- [Consoleリリース](https://play.google.com/console/u/6/developers/6254143143837166570/app/4974169236260185677/tracks/4701640059087792263/releases/7/details)
- [内部テスター用リンク](https://play.google.com/apps/internaltest/4701640059087792263)
- 配信状態: 内部テスターに公開、未審査。製品版は引き続き未公開。Playへの反映には時間がかかる場合がある。
- 今回の実機インストール・更新後の実機動作は未検証。次のアップロードは未使用のversionCode 8以上が必要。

リリースノート:

```text
<ja-JP>
おしごとを全30ステージに拡充しました。
豆・爬虫類・ポーズ・時代・ひらがなの各テーマに、応用ステージと検定ステージを追加しました。
後半へ進むほど手応えが増すよう、出現速度・間隔・目標・ギミックの難易度を調整しました。
</ja-JP>
```
