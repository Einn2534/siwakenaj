# Regression testing

## Full run

Run from PowerShell at the project root:

```powershell
.\Tools\Run-Regression.ps1
```

The script defaults to Unity `6000.3.10f1`, matching `ProjectSettings/ProjectVersion.txt`. Pass `-UnityPath` when the Editor is installed elsewhere. It runs these steps sequentially:

1. EditMode tests
2. PlayMode gameplay-flow tests
3. Main / Pause / How To / Result layout validation and screenshots

Artifacts are written to `RegressionArtifacts` unless `-OutputRoot` is supplied. The layout-only pass is also available from `Tools > Siwakenja > Regression > Capture Layout Matrix`.

## Gameplay coverage

The PlayMode suite loads the real `Main` and `Result` scenes and verifies:

- Pointer Down with no active car
- wrong-lane and correct-lane answers
- two Pointer Down events in one frame (last input wins, one answer is processed)
- three-car active limit
- target-score clear and stored result
- miss-limit continue prompt, give-up game over, and stored result
- Result action unlock with the Editor ad stub
- retry from both clear and game-over results back to a fresh Main scene
- Express, Covered, and Broken vehicle rules
- Stage 5 rush spawning, fever HUD, camera viewport binding, and repair-button separation

PlayerPrefs touched by the test are backed up and restored after the suite.

## Layout matrix

Each of Main, Main Stage 5 HUD, Pause, How To, Result Clear, and Result Game Over is captured for:

- Android 1080 x 1920, full safe area
- iPhone 1170 x 2532, top and bottom safe-area insets
- tall Android 1440 x 3200, top and gesture-area insets

For each state the validator checks active buttons against the simulated safe area, clipped TextMesh Pro text for truncation, and active button rectangles for overlap. Results are recorded in `layout-validation.md` next to the screenshots. Main keeps its existing zone sizes and positions at full safe area; on notched devices only the top and bottom zones are translated inward by the reported inset.

Unity Ads initialization is disabled in the Editor, so EditMode, PlayMode, and capture runs cannot contact the ads service. No IAP runtime implementation is present in this project, and the regression suite does not initialize Purchasing.
