# Mission layout update 1.0.2

- Built: 2026-09-12.
- AAB: `Builds/GooglePlay/siwakenja-20260912-051812.aab`
- Version name: `1.0.2`; version code: `3`.
- Package: `com.kibundeseisakujo.siwakenja`.
- Size: 87,083,332 bytes.
- SHA256: `685F0FF7FB64EC48E77120FB88BDF0C2195191B407E4D579D2E25A856A496F1C`.
- Change: center the complete two-line mission text block and increase its height in StageCardView, StageCard prefab, and the prefab upgrader.
- Built with the existing upload key using `Tools/Build-GooglePlay.ps1 -SkipDependencyReport`, matching the previous internal-test build procedure.
- Validation: Unity build succeeded; JAR signature verified; package, version, target SDK 36, AdMob ID and seven ARM64 libraries' 16 KB ELF alignment verified.
- Build log: `Logs/GooglePlayBuild.log`.
- Published to the existing Google Play internal testing track on 2026-09-12 at 06:14 JST. Console confirms active latest release `1.0.2 ミッション表示の修正`, release ID 3, version code 3.
- Supported device counts are unchanged. Only the existing missing R8 mapping/native debug symbol warnings remain.
- Tester update link: https://play.google.com/apps/internaltest/4701640059087792263
- Console: https://play.google.com/console/u/0/developers/6254143143837166570/app/4974169236260185677/tracks/4701640059087792263/releases/3/details
- Device installation and rendering of this update are not yet verified. Next upload must use version code 4 or higher.

Release name: `1.0.2 ミッション表示の修正`

Release notes:

```text
<ja-JP>
おしごと選択画面で、ミッションの見出しと説明文の表示位置を修正しました。
</ja-JP>
```
