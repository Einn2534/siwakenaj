using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only recorder: drives the real authored Animator and records rendered frames.
public static class PowaAnimationCapture
{
    private const string Output = "Deliverables/PowaAnimation/Flipbook";
    private static float _previousTimeScale;
    private static GameObject _preview;
    private static GameObject _cameraObject;
    private static IEnumerator _recording;
    private static int _lastFrame;

    public static void Begin()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Main Play mode first.");
        if (_recording != null) throw new InvalidOperationException("Capture already running.");
        _recording = Record();
        _lastFrame = -1;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { Cleanup(); return; }
        if (_lastFrame == Time.frameCount) return;
        _lastFrame = Time.frameCount;
        try { if (!_recording.MoveNext()) Cleanup(); }
        catch (Exception error) { Debug.LogException(error); Cleanup(); }
    }

    private static IEnumerator Record()
    {
        Directory.CreateDirectory(Output + "/frames");
        Directory.CreateDirectory(Output + "/gameplay-frames");
        _previousTimeScale = Time.timeScale;
        GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
        TutorialController tutorial = UnityEngine.Object.FindFirstObjectByType<TutorialController>();
        if (tutorial != null)
        {
            string[] keys = { "Tutorial_Completed", "Tutorial_Skipped", "ExpressTutorial_Seen" };
            bool[] existed = Array.ConvertAll(keys, PlayerPrefs.HasKey);
            int[] values = Array.ConvertAll(keys, key => PlayerPrefs.GetInt(key));
            try { typeof(TutorialController).GetMethod("SkipTutorial", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tutorial, null); }
            finally
            {
                for (int i = 0; i < keys.Length; i++)
                    if (existed[i]) PlayerPrefs.SetInt(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]);
                PlayerPrefs.Save();
            }
        }
        float deadline = Time.realtimeSinceStartup + 12;
        while ((!flow.IsPlaying() || (tutorial != null && tutorial.IsRunning)) && Time.realtimeSinceStartup < deadline) yield return null;
        if (!flow.IsPlaying()) throw new InvalidOperationException("Main did not start for capture.");
        CarSpawner spawner = UnityEngine.Object.FindFirstObjectByType<CarSpawner>();
        spawner.StopSpawning(); spawner.DespawnAllCars();
        flow.PauseGame();
        Time.timeScale = 0;
        PlayerAnimationController player = UnityEngine.Object.FindFirstObjectByType<PlayerAnimationController>();
        Animator mainAnimator = player.GetComponent<Animator>();
        _preview = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PowaFlipbookBuilder.PrefabPath));
        _preview.transform.position = new Vector3(10000, 0, 0);
        foreach (Transform child in _preview.GetComponentsInChildren<Transform>()) child.gameObject.layer = 31;
        Animator previewAnimator = _preview.GetComponent<Animator>();
        _cameraObject = new GameObject("PowaPreviewCamera");
        Camera previewCamera = _cameraObject.AddComponent<Camera>();
        previewCamera.transform.position = new Vector3(10000, 0, -10);
        previewCamera.orthographic = true; previewCamera.orthographicSize = 3.3f;
        previewCamera.cullingMask = 1 << 31;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(.15f, .19f, .28f);
        previewCamera.enabled = false;
        string[] states = { "Idle", "Attack", "Damage", "Win" };
        float[] clipLengths = { 2.4f, .8f, .55f, .9f };
        int[] counts = { 72, 45, 36, 51 };
        int frame = 0;
        for (int segment = 0; segment < states.Length; segment++)
        for (int i = 0; i < counts[segment]; i++)
        {
            float normalized = Mathf.Min(i / 30f / clipLengths[segment], .999f);
            // Hold the final win pose; completed normal reactions return to idle.
            string state = segment > 0 && segment < 3 && i / 30f >= clipLengths[segment] ? "Idle" : states[segment];
            if (state == "Idle" && segment > 0) normalized = (i / 30f - clipLengths[segment]) / 2.4f;
            mainAnimator.Play(state, 0, normalized); mainAnimator.Update(0);
            previewAnimator.Play(state, 0, normalized); previewAnimator.Update(0);
            yield return null;
            yield return null;
            Render(previewCamera, Output + $"/frames/frame-{frame:D4}.png", 640, 640);
            PowaScreenshotCapture.CaptureSceneForRegression(SceneManager.GetActiveScene(),
                Output + $"/gameplay-frames/frame-{frame:D4}.png", 540, 960, null);
            if (i == counts[segment] / 3)
            {
                PowaScreenshotCapture.CaptureSceneForRegression(SceneManager.GetActiveScene(),
                    Output + "/Main_" + states[segment] + "_1080x1920.png", 1080, 1920, null);
                if (segment == 0 || segment == 3)
                {
                    PowaScreenshotCapture.CaptureSceneForRegression(SceneManager.GetActiveScene(),
                        Output + "/Main_" + states[segment] + "_1170x2532.png", 1170, 2532, null);
                    PowaScreenshotCapture.CaptureSceneForRegression(SceneManager.GetActiveScene(),
                        Output + "/Main_" + states[segment] + "_1440x3200.png", 1440, 3200, null);
                }
            }
            frame++;
        }
        File.WriteAllText(Output + "/capture-complete.txt", $"Rendered {frame} frames at 30 fps. Idle/Attack/Damage/Win; eight Main screenshots.");
        Debug.Log("[PowaAnimationCapture] Complete: " + frame + " frames.");
    }

    private static void Cleanup()
    {
        EditorApplication.update -= Tick;
        _recording = null;
        Time.timeScale = _previousTimeScale;
        if (_preview != null) UnityEngine.Object.DestroyImmediate(_preview);
        if (_cameraObject != null) UnityEngine.Object.DestroyImmediate(_cameraObject);
    }

    private static void Render(Camera camera, string path, int width, int height)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D pixels = new(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
