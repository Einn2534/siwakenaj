# P00 results

P00 は Android 実機での技術予行です。一般参加者の UX 結果ではなく、`is_pilot=true` のため主要集計から除外されます。

この directory へ保存してよいものは、日付、build/version、匿名化した device class、解像度/Safe Area、checklist の pass/fail、再現手順、個人情報を含まない screenshot、artifact hash です。端末固有 ID、アカウント情報、Raw touch dataset は commit しません。

結果ファイル名は `P00_YYYYMMDD_<device>_report.md` とし、機械合格条件 11 項目と端末・cutout matrixを明記します。

```text
status: PASS | FAIL | PARTIAL
checks_passed:
checks_failed:
checks_not_reproduced:
session_id:
schema_version / experiment_version: 2 / 2.1.0
resolution / safe_area:
primary_physical_device:
different_cutout_emulator_or_second_device:
machine_checks_1_to_11:
csv_persisted_after_exit:
checkpoint_resume:
rush_target_attribution:
blocking_defects:
```

PASS には 11/11、主 Android 実機、異なる cutout の Emulator または第2実機、3 画面比、blocking defect 0 件が必要です。現時点では P00 実機結果は未登録です。手順は [P00_Android_Rehearsal.md](../../Data/Protocols/P00_Android_Rehearsal.md) を参照してください。P00 が PASS しても UI 採用判断は行わず、次は version freeze と P01〜P06 Placement のみです。
