# Input UI layout catalog verification

Generated (UTC): 2026-08-26T13:32:01.1025747Z

Result: **PASS** — catalog geometry and presentation contracts are internally consistent.

This is deterministic engineering evidence only. It does not compare human UX outcomes and does not recommend a UI.

## Placement experiment

| condition | vehicle row | repair RectTransform | repair state |
|---|---|---|---|
| Placement_UpperFixed | centers 211/540/869 @ y190, size 308x315 | center 540x800, size 470x112 | always visible + interactable |
| Placement_BottomFixed | centers 211/540/869 @ y190, size 308x315 | center 540x462, size 470x112 | always visible + interactable |

The three vehicle controls, repair size/style, and repair presentation are exact matches; only repair anchoredPosition differs.

## State-assist experiment

All controls use the same RectTransforms for a selected immutable RepairAnchorMode. The table below is independent of anchor choice.

| condition | non-Broken | Broken | layout reserved while hidden |
|---|---|---|---|
| Assist_AlwaysActive | alpha=1, interactable=true, blocksRaycasts=true | alpha=1, interactable=true, blocksRaycasts=true | yes |
| Assist_DisabledWhenNormal | alpha=1, interactable=false, blocksRaycasts=true | alpha=1, interactable=true, blocksRaycasts=true | yes |
| Assist_BrokenOnly | alpha=0, interactable=false, blocksRaycasts=false | alpha=1, interactable=true, blocksRaycasts=true | yes |

## Rush package presets

| preset | condition | anchor | assist | vehicle layout | legacy geometry |
|---|---|---|---|---|---|
| CurrentEquivalent | Rush_CurrentEquivalent | BottomFixed | AlwaysActive | StandardThree | yes |
| FixedFourEquivalent | Rush_FixedFourEquivalent | BottomFixed | DisabledWhenNormal | FixedFour | yes |
| ContextualRepairEquivalent | Rush_ContextualRepairEquivalent | UpperFixed | BrokenOnly | StandardThree | yes |

Rush presets are package comparisons. Their results cannot attribute an effect to an individual layout or assist factor.
All three presets are retained for P00 technical rehearsal only. Human Rush testing must use only two candidates preregistered after State Assist participant-level review.

## Required resolution projections

| screen | Safe Area | projected 9:16 content | scale |
|---|---|---|---:|
| 1080x1920 | (0,0) 1080x1920 | (0,0) 1080x1920 | 1 |
| 1080x2340 | (0,0) 1080x2340 | (0,210) 1080x1920 | 1 |
| 720x1600 | (0,0) 720x1600 | (0,160) 720x1280 | 0.6667 |

For every causal condition and retained Rush preset, all four control rects are contained by the projected content frame and do not overlap each other, the target marker, or the car-at-judgment rect.

These are mathematical projections. Game View screenshots and Android Safe Area checks remain separate visual/P00 evidence.

## Validation errors

None.
