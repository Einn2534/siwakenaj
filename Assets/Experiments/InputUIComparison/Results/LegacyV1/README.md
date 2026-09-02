# Legacy v1

v1 の Baseline/A/B は削除せず、履歴として保持します。既存 screenshot と `Verification/verification-report.md` は保存要件のため元の場所から移動していません。この README が論理的な隔離境界です。

| v1 名 | v2.1 catalog での分類 | 使用可能な解釈 |
|---|---|---|
| Baseline | `Rush_CurrentEquivalent` | 現行相当 package 全体 |
| A：固定 4 枠 | `Rush_FixedFourEquivalent` | 位置・車種幅・Repair サイズ・disabled cue を含む package 全体 |
| B：Broken 時コンテキスト表示 | `Rush_ContextualRepairEquivalent` | 位置・Repair サイズ・BrokenOnly cue を含む package 全体 |

これらを v2.1 の Placement または State Assist 条件として再利用しません。v1 CSV を schema 2 / experiment 2.1.0 の集計へ変換・連結せず、比較する場合も「参考となる package 履歴」と明記します。P00 では 3 preset の技術確認が可能ですが、人間 Rush は State Assist 後に事前固定した 2 候補だけを使います。

旧自動テストの成功、Game View screenshot、v1 の仮説から A/B の UX 優位性を主張しません。
