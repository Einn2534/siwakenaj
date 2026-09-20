# UI readability update 1.0.1

- Build: `Builds/GooglePlay/siwakenja-20260908-185834.aab`
- Android versionCode: 2; versionName: 1.0.1
- Size: 87,078,561 bytes
- SHA256: `538A2CEA04F8C821FFE4675DBED3BFB8E1D7FEC7B071D9F9AE5AE8DACD27BC89`
- Replaced UI text fonts with Noto Sans CJK JP Regular; removed synthetic bold and reduced text outlines to 0.045.
- Font source: https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/Japanese
- SIL OFL license: `Assets/Fonts/NotoSansCJK-LICENSE.txt`.
- Updated scene/prefab text using Unity Editor APIs; runtime UI uses the same regular font.
- Tutorial speech tail follows its panel, including pop animations.
- Lane display follows the HUD below the safe area, preventing the large frame from overlapping the counters.
- Verified title, stage selection and tutorial at 1080x2340 in the Editor.
- EditMode: 76/76 passed. PlayMode: 40/40 passed. No compiler errors.
- AAB signature, package ID, AdMob application ID, target SDK 36 and all seven ARM64 libraries' 16 KB alignment passed.
- Evidence: `RegressionArtifacts/typography-*.png`, `typography-editmode.xml`, `typography-playmode.xml`, `typography-bundle-verification.txt`.
- Published to Google Play internal testing on 2026-09-08 at 19:13 JST. Console confirms active latest release `1.0.1 UIの読みやすさ改善`, release ID 2, versionCode 2.
- Tester update link: https://play.google.com/apps/internaltest/4701640059087792263
- Console reports unchanged supported devices. Only existing missing R8 mapping/native debug symbol warnings remain.
- Device installation of this update is not yet verified. Next upload must use versionCode 3 or higher.
