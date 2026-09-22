# ミス境界の実装・検証結果

2026-09-23 / Unity 6000.3.10f1

- 左から6%の位置に文字なしの赤い柵を表示。対象の左端が線を越えると見逃しになる。
- 領域幅の15%以内で接近に応じて発光を強め、見逃し発生時刻から0.2秒だけ強く光る。Pause中は停止する。
- 全テーマ・全モード・練習に対応。線と判定は同じPlayZone座標から計算し、画面サイズ変更にも追従する。

## 変更箇所

- `Assets/Scripts/Core/MissBoundaryGeometry.cs`: 境界位置と接近度の共通計算。
- `Assets/Scripts/Game/CarController.cs` / `CarSpawner.cs`: 現在のPlayZoneを使う見逃し判定、座標の読み取りAPI、停止中の判定停止。
- `Assets/Scripts/UI/MissBoundaryView.cs`: Mainへの自動生成、柵・接近時の光・見逃し時の発光。非インタラクティブなuGUIを対象より前、HUDより後に描画。
- `Assets/Scripts/Game/TutorialController.cs`: 停止練習対象を境界より右へ収める。
- EditMode / PlayModeの境界テスト、`Assets/Editor/MissBoundaryCapture.cs`、仕様書・回帰検証手順を追加・更新。

シーンやPrefabの保存変更は不要。新規スクリプトのmetaはUnityが生成した。移動速度、スポーン間隔、保存形式、既存のInitialize APIは維持している。

## 検証

- Unityコンパイル成功。終了時のConsoleエラー0件。
- [EditMode結果](../SortingThemes/Tests/miss-boundary-editmode-final.xml): **85 passed / 0 failed / 0 skipped**。
- [PlayMode結果](../SortingThemes/Tests/miss-boundary-playmode-final.xml): **62 passed / 0 failed / 0 skipped**。
- 線上と通過後の判定、ミス重複防止、通過後0.18秒の追加入力保護、接近表示、Pause・再開、再ロード、全モード、停止練習対象を確認。
- 4テーマ × 3画面比率 × 通常・接近・見逃しの**36枚**を実Mainから撮影。座標一致、SafeArea内の表示、タップを遮らないことを撮影時にも検証。
- 1080×1920、1170×2532、1440×3200で画像を確認。境界線がHUD・操作ボタンと重ならず、接近・見逃し時の光が識別できる。

画像名は `Stage{01/04/08/11}_{Idle/Near/Miss}_{幅}x{高さ}.png`。見逃し画像は実際の判定・消去処理の次フレームで撮影した。撮影用の対象位置・カメラ・SafeArea設定は復元する。

Gitには1080×1920の各テーマの接近画像4枚と、くるまの通常・見逃し画像2枚を代表画像として収録する。36枚すべては上記PlayModeテストで再生成できる。

## 作業前からの差分

`Assets/Resources/Fonts/NotoSansCJKjp-Regular SDF.asset` には開始時から変更があり、Unityの回帰検証中に動的フォントが追加で自動更新された。この既存差分を含む資産はHEADへ戻していない。検証対象はEditor内であり、端末実機での確認は行っていない。
