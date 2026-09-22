# Powa animation execution notes

## Sorting themes goal (2026-09-21, completed)

- Scope: retain vehicles; add matching hands (rock/scissors/paper), clocks (3/6/9), leafy vegetables (cabbage/lettuce/napa). Stage IDs, goals, timing, saves and ads stay compatible.
- Allocation: 1–3 vehicles, 4–7 hands, 8–10 clocks, 11–15 vegetables. Daily keeps vehicles and daily-v1 sequence; Endless inherits the source stage theme.
- Baseline: Unity 6000.3.10f1 / UniCli connected, edit mode, clean scene. Existing user diff: PowaGameplayLayout Y .33 -> .42; preserve it. Existing generated/untracked deliverables also preserved.
- Art: built-in imagegen produced six transparent sprites, visually inspected for distinct silhouettes. Three exact clock faces and a neutral parcel are drawn by Tools/Build-ThemeIcons.ps1. Source images remain untouched. Import uses source pixel dimensions / 15 PPU because Unity preserves original world-space size when shrinking textures to 512px; this fixes clocks appearing too small compared with generated sprites.
- Implemented: theme metadata and stage references; spawner, buttons, collected icons, objective names/units, hints, result, stage cards and guides consume the same theme. Pause and StageSelect replay retain their selected stage/theme; replay in Daily/Endless retains the mode. The previously unused StageSelect guide is now connected to a visible button.
- Independent read-only reviewer: theme_review. First findings (forced stage1 tutorial, missing FEVER/RUSH copy) fixed; second pass found no new reference/save regression, requested Daily/Endless tutorial-return coverage. Added Daily skip and complete six-input Endless practice with the actual graduation button.
- Validation: final compile successful with 0 errors; EditMode 83/83 and PlayMode 56/56 pass with 0 failures or skipped tests. Theme captures 90/90 and existing layout matrix 42/42 pass with 0 failures across 1080x1920, 1170x2532 and 1440x3200. Final XML, capture reports and validation-summary.json are under Deliverables/SortingThemes; README.md links the evidence and allocation.
- Earlier focused test failures (test launch state, unused guide, tutorial input readiness, inactive scrim Button, modal validation scope, failure result expected-stage argument and graduation tap) remain under Tests for diagnosis and are superseded by final full-suite evidence. Final 3-ratio Main images show distinct hand silhouettes, correctly oriented clock hands and distinguishable round cabbage/frilled lettuce/long napa.
- Final visual audit found stretched nonvehicle collected icons: set preserveAspect on their Image and added exact sprite/aspect assertions plus 12 Collected captures (90 theme captures total). TutorialFocusGuideView uses its actual canvas camera when projecting screen coordinates; the normal Overlay path still uses null. Guided practice objects and the animated pointer are kept within their visible bounds, and the long cabbage instruction wraps explicitly. The existing 42-matrix preview refreshes themed buttons and Express text. Both full suites and all 132 captures passed after these fixes.
- Independent reviewer closed the final review with no unresolved findings, verifying the latest tutorial images, aspect ratios, quotas, mode returns and all final test/capture counts. Main is reopened in Edit mode with no dirty scene, compilation or Console errors. The pre-existing obsolete SetIconsForTargetGroup warning in GooglePlayReleaseSetup.cs remains. No Unity/package changes; PowaGameplayLayout Y .42 and pre-existing work preserved. Android/iOS device performance and a distribution build were not verified. No remaining required work for this goal.


## Outcome
The user accepted the frame-animation direction and success timing of 0.8s, and requested whole-character Y travel in game. Main now uses PowaFlipbook.prefab: sprite swaps plus a separate animated Motion child. Cutout assets and earlier previews are retained. Final evidence is in Deliverables/PowaAnimation/Flipbook.

## Current flipbook implementation
- Reused the accepted 12 success drawings; generated 24 idle/miss/win key and in-between drawings with built-in imagegen.
- Installed Main through UniCli. Authored clip durations: Idle 2.4s, Attack 0.8s, Damage 0.55s, Win 0.9s.
- Fixed Unity's one-sample extension from a terminal object-reference key; the full PlayMode test caught Attack being 0.8333s. Removed the terminal sprite key, set exact stop time, and retained the 0.8s test assertion.
- Existing input interruption, win priority, pause, revive, one-time result and fallback contracts remain. Updated the Powa PlayMode checks for sprite progression and Y travel surviving Main layout.
- Compile and EditMode 77/77 pass after duration fix. Full PlayMode rerun 49/49 pass, layout regression 42/42 pass with 0 failures.
- Captured 204 Unity Animator frames at 30fps for both isolated and Main previews (6.8 seconds); eight Main screenshots include three aspect ratios and win pose. Both MP4s decode fully. Inspected rendered poses and Main placement; no overlap with car area, HUD or buttons.
- Main is open in Edit mode with no dirty scene; Console errors 0. Fine drawing-to-drawing hat/badge/cloth variation remains, as discussed in the accepted trial. No device performance/release build performed.

## Earlier cutout candidate evidence
- Unity 6000.3.10f1; UniCli server connected. Existing packages preserved.
- Installed official sprite-editor and manage-sprite-atlas skills from Unity-Technologies/unity-agent-plugin at c7055702b44e58402ee481ff408aee407bcf9483.
- Generated transparent parts/expression atlases are in Assets/Art/Sprites/Characters/Powa/Gameplay. Reference preserved in Deliverables/PowaAnimation/idol-reference.png.
- Built Main prefab, complete Animator poses, skinned front/rear hat and dress, expressions and one staff. Staff grip is baked from the animated hand.
- Corrected the user-reported hat layering: rear plate < face/mouth < front brim. Both hat plates deform together from the same source texture.
- Camera-relative Main placement leaves the car lane, HUD and answer buttons clear. Inspected isolated idle/success/miss/win and 1080x1920 / 1170x2532 / 1440x3200 captures.
- Final EditMode: 77/77 pass. Final full PlayMode: 49/49 pass, including four new reaction/pause/continue/result/retry scenarios. XML results copied to Deliverables/PowaAnimation.
- Final layout regression: 42/42 pass, 0 layout failures. Fixed editor-only capture preparation (avoid creating icons immediately before runtime ResetAll) and explicitly updated SortingGroup before rendering; all captures rerun.
- Rendered 195 actual Animator frames (30fps, 6.5s), Powa-preview.mp4 and six Main screenshots. Capture waits for tutorial completion and restores tutorial preference keys.
- Generated-image prompts and selected source IDs are preserved in Deliverables/PowaAnimation/generation-prompts.md.

## Implemented cutout candidate
- Preview video decodes as 195 frames, 640x640 H.264, 30fps, 6.5 seconds. Final six gameplay captures rendered after the hat layer correction.
- Inspected final runtime visuals and regression Main/Title/Result/Pause; source changes passed diff whitespace checks. Main reopened in edit mode with no dirty scene.
- Implementation, reproduction instructions, evidence and existing editor-plugin diagnostics are in Deliverables/PowaAnimation/README.md.
- User feedback: the cutout result still feels unconvincing. Technical checks pass, but visual acceptance is not complete. The user suggested frame-by-frame generated art; making a six-frame success-motion comparison sample before changing the gameplay implementation.
- Comparison trial is ready: Deliverables/PowaAnimation/FlipbookTrial/Powa-flipbook-success.mp4 (5.4s, three repeats). Four individually generated frames plus the original at both ends, all six PNGs and prompts preserved. Character identity is closer to idol.png; modest hat/staff drift remains. Asked the user whether to proceed with frame animation, refine cutouts, or stop at the sample. No Main asset replacement made for this experiment.
- Android device performance and an application release build were not part of this animation task.

## Boundaries
- Do not overwrite other ongoing changes, other screens, original art, or upgrade Unity/packages.
- All asset/scene serialization belongs to Unity; no hand-written YAML or meta GUIDs.
- Main-thread implementation and self-review, with automated tests and visual evidence. No delegated workers needed for this bounded integration.
