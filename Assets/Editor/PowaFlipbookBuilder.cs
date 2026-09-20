using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;

// Explicit UniCli entry point. Retains the previous cutout candidate and source PNGs.
public static class PowaFlipbookBuilder
{
    public const string Art = "Assets/Art/Sprites/Characters/Powa/Flipbook";
    public const string Motions = "Assets/Art/Animations/PowaFlipbook";
    public const string PrefabPath = "Assets/Prefabs/PowaFlipbook.prefab";
    private static readonly Dictionary<string, Sprite> Sprites = new();

    public static void Build()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Exit Play mode and save the active scene before building Powa.");
        Directory.CreateDirectory(Motions);
        AssetDatabase.Refresh();
        Sprites.Clear();
        foreach (string path in Directory.GetFiles(Art, "*.png").OrderBy(p => p)) Import(path.Replace('\\', '/'));

        GameObject root = new("PowaFlipbook") { layer = 5 };
        try
        {
            root.AddComponent<SortingGroup>().sortingOrder = 1;
            Transform motion = new GameObject("Motion").transform;
            motion.SetParent(root.transform, false); motion.gameObject.layer = 5;
            SpriteRenderer artwork = new GameObject("Artwork").AddComponent<SpriteRenderer>();
            artwork.transform.SetParent(motion, false); artwork.gameObject.layer = 5;
            artwork.sprite = Sprites["idle"];
            artwork.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PowaGameplayAnimationBuilder.Art + "/PowaCutout.mat");
            if (artwork.sharedMaterial == null) throw new InvalidOperationException("Missing Powa sprite material.");
            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = BuildController();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PlayerAnimationController player = root.AddComponent<PlayerAnimationController>();
            SerializedObject data = new(player);
            data.FindProperty("_animator").objectReferenceValue = animator;
            data.FindProperty("_useImmediateStates").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        InstallInMain();
        AssetDatabase.SaveAssets();
        Debug.Log("[PowaFlipbook] Installed four sprite animations with independent vertical travel. Success = 0.8s.");
    }

    private static void Import(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing texture importer: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 250;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        TextureImporterSettings settings = new();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new(); factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) throw new InvalidOperationException("No sprite provider: " + path);
        provider.InitSpriteEditorDataProvider();
        ISpriteFrameEditCapability edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
        if (edit == null || !edit.GetEditCapability().HasCapability(EEditCapability.EditPivot))
            throw new InvalidOperationException("Pivot editing is unsupported: " + path);
        SpriteRect[] rects = provider.GetSpriteRects();
        if (rects.Length != 1) throw new InvalidOperationException("Expected one full-canvas sprite: " + path);
        rects[0].pivot = new Vector2(.5f, .5f);
        rects[0].alignment = SpriteAlignment.Center;
        provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Sprite import failed: " + path);
        Sprites.Add(Path.GetFileNameWithoutExtension(path), sprite);
    }

    private static AnimatorController BuildController()
    {
        string[] idle = new[] { "idle" }.Concat(Enumerable.Range(1, 5).Select(i => "idle-between-" + i))
            .Concat(new[] { "idle-peak" }).Concat(Enumerable.Range(1, 5).Reverse().Select(i => "idle-between-" + i)).ToArray();
        string[] success = Enumerable.Range(0, 12).Select(i => $"success-{i:D2}").ToArray();
        string[] damage = new[] { "idle" }.Concat(Enumerable.Range(1, 3).Select(i => "damage-in-" + i))
            .Concat(new[] { "damage-peak" }).Concat(Enumerable.Range(1, 3).Select(i => "damage-out-" + i)).ToArray();
        string[] win = new[] { "idle", "success-02" }.Concat(Enumerable.Range(1, 4).Select(i => "win-grasp-" + i))
            .Concat(new[] { "win-reach" }).Concat(Enumerable.Range(1, 5).Select(i => "win-lift-" + i))
            .Concat(new[] { "win-peak", "win-peak" }).ToArray();
        AnimationClip idleClip = Clip("Idle", 2.4f, idle, true,
            new[] { 0f, .6f, 1.2f, 1.8f, 2.4f }, new[] { 0f, .1f, 0f, -.1f, 0f });
        AnimationClip happyClip = Clip("Attack", .8f, success, false,
            new[] { 0f, .13f, .34f, .46f, .69f, .8f }, new[] { 0f, -.10f, .44f, .48f, -.035f, 0f });
        AnimationClip sadClip = Clip("Damage", .55f, damage, false,
            new[] { 0f, .10f, .25f, .40f, .55f }, new[] { 0f, -.09f, -.24f, -.12f, 0f });
        AnimationClip winClip = Clip("Win", .9f, win, false,
            new[] { 0f, .16f, .35f, .63f, .82f, .9f }, new[] { 0f, -.14f, .16f, .65f, .47f, .42f });
        string path = Motions + "/PowaFlipbook.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState state in machine.states) machine.RemoveState(state.state);
        AnimatorState idleState = machine.AddState("Idle"); idleState.motion = idleClip;
        idleState.writeDefaultValues = false; machine.defaultState = idleState;
        foreach (AnimationClip clip in new[] { happyClip, sadClip, winClip })
        {
            AnimatorState state = machine.AddState(clip.name); state.motion = clip; state.writeDefaultValues = false;
            if (clip == winClip) continue;
            AnimatorStateTransition transition = state.AddTransition(idleState);
            transition.hasExitTime = true; transition.exitTime = 1;
            transition.hasFixedDuration = true; transition.duration = 0;
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip Clip(string name, float duration, string[] frames, bool loop, float[] times, float[] heights)
    {
        string path = Motions + "/Powa_" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves(); clip.name = name; clip.frameRate = 30;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop; AnimationUtility.SetAnimationClipSettings(clip, settings);
        List<ObjectReferenceKeyframe> keys = new();
        for (int i = 0; i < frames.Length; i++)
            keys.Add(new ObjectReferenceKeyframe { time = duration * i / frames.Length, value = Sprites[frames[i]] });
        // Unity extends a sprite key by one sample. The float curve sets the exact
        // duration; a sprite key at duration would add an unwanted extra frame.
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("Motion/Artwork", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
        AnimationCurve travel = new(times.Select((t, i) => new Keyframe(t, heights[i])).ToArray());
        for (int i = 0; i < travel.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(travel, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(travel, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        // Layout writes only the prefab root, while all states write this child curve.
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Motion", typeof(Transform), "m_LocalPosition.y"), travel);
        settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.startTime = 0; settings.stopTime = duration; settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void InstallInMain()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
        GameObject old = GameObject.Find("PlayerCharacter");
        if (old == null) throw new InvalidOperationException("Main PlayerCharacter is missing.");
        GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
        if (flow == null) throw new InvalidOperationException("Main GameFlowController is missing.");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
        instance.name = "PlayerCharacter";
        instance.transform.SetSiblingIndex(old.transform.GetSiblingIndex());
        instance.AddComponent<PowaGameplayLayout>().ApplyLayout(Camera.main);
        SerializedObject data = new(flow);
        data.FindProperty("_playerAnimationController").objectReferenceValue = instance.GetComponent<PlayerAnimationController>();
        data.ApplyModifiedPropertiesWithoutUndo();
        UnityEngine.Object.DestroyImmediate(old);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }
}
