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
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

// Invoked explicitly through UniCli Eval. Never rewrites assets on import or build.
public static class PowaGameplayAnimationBuilder
{
    public const string Art = "Assets/Art/Sprites/Characters/Powa/Gameplay";
    public const string Motions = "Assets/Art/Animations/PowaGameplay";
    public const string PrefabPath = "Assets/Prefabs/PowaGameplay.prefab";
    private const float Ppu = 250f;
    private static readonly Dictionary<string, Sprite> Sprites = new();
    private static Material _material;

    private readonly struct Slice
    {
        public readonly string Name;
        public readonly Rect Region;
        public readonly Vector2 Pivot;
        public Slice(string name, float x, float y, float w, float h, float px = .5f, float py = .5f)
        { Name = name; Region = new Rect(x, y, w, h); Pivot = new Vector2(px, py); }
    }

    public static void Build()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Exit Play mode and save the current scene before building Powa.");
        Directory.CreateDirectory(Motions);
        AssetDatabase.Refresh();
        ImportParts();
        string materialPath = Art + "/PowaCutout.mat";
        _material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (_material == null)
        {
            _material = new Material(Shader.Find("Powa/CutoutSprite"));
            AssetDatabase.CreateAsset(_material, materialPath);
        }
        GameObject rig = BuildRig();
        try
        {
            AnimatorController controller = BuildController(rig);
            Animator animator = rig.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PlayerAnimationController player = rig.AddComponent<PlayerAnimationController>();
            SerializedObject playerData = new(player);
            playerData.FindProperty("_animator").objectReferenceValue = animator;
            playerData.FindProperty("_useImmediateStates").boolValue = true;
            playerData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(rig, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(rig); }
        InstallInMain();
        AssetDatabase.SaveAssets();
        Debug.Log("[PowaGameplay] Built cutout prefab, weighted hat/dress, and four animation clips.");
    }

    private static void ImportParts()
    {
        Sprites.Clear();
        ImportSheet(Art + "/PowaParts.png", new[]
        {
            new Slice("Hat", 0, 0, 477, 410),
            new Slice("HatRear", 0, 0, 477, 410),
            new Slice("Head", 477, 0, 340, 410),
            new Slice("Dress", 817, 0, 437, 410),
            new Slice("ArmLeft", 0, 410, 418, 405, .88f, .92f),
            new Slice("ArmRight", 418, 410, 418, 405, .12f, .92f),
            new Slice("LegLeft", 836, 410, 418, 405, .55f, .96f),
            new Slice("LegRight", 0, 815, 418, 439, .73f, .95f),
            new Slice("Staff", 418, 815, 418, 439, .5f, .55f),
            new Slice("BadgeNormal", 836, 815, 418, 439)
        });
        ImportSheet(Art + "/PowaExpressions.png", new[]
        {
            new Slice("MouthNormal", 0, 0, 418, 627),
            new Slice("MouthHappy", 418, 0, 418, 627),
            new Slice("MouthSad", 836, 0, 418, 627),
            new Slice("BadgeHappy", 0, 627, 418, 627),
            new Slice("BadgeSad", 418, 627, 418, 627),
            new Slice("Tear", 836, 627, 418, 627)
        });
    }

    private static void ImportSheet(string path, Slice[] slices)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = Ppu;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) throw new InvalidOperationException("Missing sprite data provider: " + path);
        provider.InitSpriteEditorDataProvider();
        ISpriteFrameEditCapability edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
        if (edit == null) throw new InvalidOperationException("Missing sprite edit capabilities: " + path);
        var capabilities = edit.GetEditCapability();
        foreach (EEditCapability capability in new[] { EEditCapability.CreateAndDeleteSprite,
            EEditCapability.EditSpriteRect, EEditCapability.EditPivot, EEditCapability.EditSpriteName })
            if (!capabilities.HasCapability(capability))
                throw new InvalidOperationException("Unsupported sprite operation: " + capability);

        Dictionary<string, SpriteRect> previous = provider.GetSpriteRects().ToDictionary(s => s.name);
        Texture2D source = new(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(path));
        List<SpriteRect> rects = new();
        try
        {
            Color32[] pixels = source.GetPixels32();
            foreach (Slice slice in slices)
            {
                Rect tight = AlphaBounds(pixels, source.width, source.height, slice.Region);
                rects.Add(new SpriteRect
                {
                    name = slice.Name, rect = tight, pivot = slice.Pivot,
                    alignment = SpriteAlignment.Custom,
                    spriteID = previous.TryGetValue(slice.Name, out SpriteRect old) ? old.spriteID : GUID.Generate()
                });
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
        provider.SetSpriteRects(rects.ToArray());
        var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (ids == null) throw new InvalidOperationException("Missing sprite name ID provider.");
        ids.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        foreach (SpriteRect rect in rects.Where(r => r.name == "Hat" || r.name == "HatRear" || r.name == "Dress"))
            AddSkinData(provider, rect);
        provider.Apply();
        importer.SaveAndReimport();
        foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            Sprites[sprite.name] = sprite;
    }

    private static Rect AlphaBounds(Color32[] pixels, int width, int height, Rect designRegion)
    {
        // Source files share a 1254px reference layout; use actual dimensions, not a guessed grid.
        int x0 = Mathf.RoundToInt(designRegion.xMin * width / 1254f);
        int x1 = Mathf.RoundToInt(designRegion.xMax * width / 1254f);
        int y0 = height - Mathf.RoundToInt(designRegion.yMax * height / 1254f);
        int y1 = height - Mathf.RoundToInt(designRegion.yMin * height / 1254f);
        int minX = x1, minY = y1, maxX = x0, maxY = y0;
        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
            if (pixels[y * width + x].a >= 128)
            { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
        if (minX > maxX) throw new InvalidOperationException("An expected Powa part is empty.");
        return Rect.MinMaxRect(Math.Max(x0, minX - 2), Math.Max(y0, minY - 2),
            Math.Min(x1, maxX + 3), Math.Min(y1, maxY + 3));
    }

    private static void AddSkinData(ISpriteEditorDataProvider provider, SpriteRect rect)
    {
        var bones = provider.GetDataProvider<ISpriteBoneDataProvider>();
        var mesh = provider.GetDataProvider<ISpriteMeshDataProvider>();
        if (bones == null || mesh == null) throw new InvalidOperationException("Importer does not support skin data.");
        Vector2 size = rect.rect.size;
        Vector2 pivot = Vector2.Scale(size, rect.pivot);
        bones.SetBones(rect.spriteID, new List<SpriteBone>
        {
            new() { name = "Root", position = pivot, rotation = Quaternion.identity, length = size.y * .3f, parentId = -1 },
            new() { name = "Flex", position = Vector3.zero, rotation = Quaternion.identity, length = size.y * .4f, parentId = 0 }
        });
        const int columns = 12, rows = 10;
        Vertex2DMetaData[] vertices = new Vertex2DMetaData[(columns + 1) * (rows + 1)];
        List<int> indices = new();
        for (int y = 0; y <= rows; y++)
        for (int x = 0; x <= columns; x++)
        {
            float u = (float)x / columns, v = (float)y / rows;
            // The front plate ends at the visible front brim edge. The full rear
            // plate sits behind the face, so the underside cannot cover cheeks.
            float bottom = rect.name == "Hat" ? FrontBrimBottom(rect.rect, u) : 0;
            v = Mathf.Lerp(bottom / size.y, 1, v);
            float influence = rect.name.StartsWith("Hat", StringComparison.Ordinal)
                ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((.65f - u) / .65f)) * Mathf.SmoothStep(0, 1, v)
                : Mathf.SmoothStep(0, 1, Mathf.Clamp01((.7f - v) / .7f));
            int index = y * (columns + 1) + x;
            vertices[index] = new Vertex2DMetaData
            {
                position = new Vector2(u * size.x, v * size.y),
                boneWeight = new BoneWeight { boneIndex0 = 0, weight0 = 1 - influence, boneIndex1 = 1, weight1 = influence }
            };
            if (x < columns && y < rows)
            { int next = index + columns + 1; indices.AddRange(new[] { index, next, index + 1, index + 1, next, next + 1 }); }
        }
        mesh.SetVertices(rect.spriteID, vertices);
        mesh.SetIndices(rect.spriteID, indices.ToArray());
        mesh.SetEdges(rect.spriteID, Array.Empty<Vector2Int>());
    }

    private static float FrontBrimBottom(Rect rect, float u)
    {
        // Contour landmarks in the source atlas, measured along the FRONT lip.
        // This is sprite mesh geometry; the original generated texture is unchanged.
        Vector2[] edge = { new(46, 215), new(91, 238), new(120, 251), new(160, 271),
            new(205, 294), new(255, 315), new(305, 333), new(360, 342), new(405, 343), new(452, 325) };
        float x = rect.xMin + rect.width * u;
        float y = edge[^1].y;
        for (int i = 1; i < edge.Length; i++)
            if (x <= edge[i].x)
            { y = Mathf.Lerp(edge[i - 1].y, edge[i].y, Mathf.InverseLerp(edge[i - 1].x, edge[i].x, x)); break; }
        return Mathf.Clamp(1254 - y - rect.yMin, 0, rect.height);
    }

    private static Vector3 Point(float x, float y) => new((x - 627f) / Ppu, (627f - y) / Ppu, 0);

    private static Transform Node(string name, Transform parent, float x, float y)
    {
        Transform node = new GameObject(name).transform;
        node.gameObject.layer = 5;
        node.SetParent(parent, false);
        node.position = Point(x, y);
        return node;
    }

    private static SpriteRenderer Part(string name, string spriteName, Transform parent,
        float x, float y, float width, float height, int order, bool skin = false)
    {
        Sprite sprite = Sprites[spriteName];
        Vector2 pivot = sprite.pivot / sprite.rect.size;
        Transform node = Node(name, parent, x + width * pivot.x, y + height * (1 - pivot.y));
        node.localScale = new Vector3(width / sprite.rect.width, height / sprite.rect.height, 1);
        SpriteRenderer renderer = node.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = _material;
        renderer.sortingOrder = order;
        if (skin)
        {
            SpriteBone[] bones = sprite.GetBones();
            Transform[] transforms = new Transform[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                transforms[i] = new GameObject(bones[i].name).transform;
                transforms[i].SetParent(bones[i].parentId < 0 ? node : transforms[bones[i].parentId], false);
                transforms[i].localPosition = bones[i].position;
                transforms[i].localRotation = bones[i].rotation;
            }
            SpriteSkin spriteSkin = node.gameObject.AddComponent<SpriteSkin>();
            spriteSkin.SetRootBone(transforms[0]);
            spriteSkin.SetBoneTransforms(transforms);
        }
        return renderer;
    }

    private static GameObject BuildRig()
    {
        GameObject root = new("PowaGameplay");
        root.layer = 5;
        root.AddComponent<SortingGroup>().sortingOrder = 1;
        Transform motion = Node("Motion", root.transform, 627, 627);
        Transform torso = Node("Torso", motion, 625, 650);
        Part("LegLeft", "LegLeft", torso, 566, 930, 137, 310, 0);
        Part("LegRight", "LegRight", torso, 642, 906, 142, 275, 0);
        Part("ArmLeft", "ArmLeft", torso, 412, 600, 188, 244, 2);
        SpriteRenderer armRight = Part("ArmRight", "ArmRight", torso, 722, 600, 160, 238, 1);
        Node("Grip", armRight.transform, 854, 823);
        Part("Dress", "Dress", torso, 337, 575, 664, 416, 3, true);
        Transform head = Node("Head", torso, 650, 510);
        Part("Face", "Head", head, 438, 311, 366, 322, 4);
        Part("HatRear", "HatRear", head, 24, -17, 1063, 558, 3, true);
        SpriteRenderer hat = Part("Hat", "Hat", head, 24, -17, 1063, 558, 6, true);
        Transform badge = Node("Badge", hat.transform, 748, 195);
        // Node positions are in reference space; remove inherited sprite scaling for attached art.
        badge.localScale = new Vector3(1 / hat.transform.lossyScale.x, 1 / hat.transform.lossyScale.y, 1);
        Part("Normal", "BadgeNormal", badge, 670, 120, 156, 156, 7);
        Part("Happy", "BadgeHappy", badge, 670, 120, 156, 156, 7).color = new Color(1, 1, 1, 0);
        Part("Sad", "BadgeSad", badge, 670, 120, 156, 156, 7).color = new Color(1, 1, 1, 0);
        Transform mouth = Node("Mouth", head, 656, 548);
        Part("Normal", "MouthNormal", mouth, 646, 546, 20, 20, 5);
        Part("Happy", "MouthHappy", mouth, 628, 534, 56, 42, 5).color = new Color(1, 1, 1, 0);
        Part("Sad", "MouthSad", mouth, 642, 552, 28, 9, 5).color = new Color(1, 1, 1, 0);
        Part("Tear", "Tear", head, 486, 542, 13, 36, 5).color = new Color(1, 1, 1, 0);
        SpriteRenderer staff = Part("Staff", "Staff", motion, 988, 598, 104, 568, 8);
        staff.transform.localRotation = Quaternion.Euler(0, 0, -20);
        return root;
    }

    private static AnimatorController BuildController(GameObject rig)
    {
        AnimationClip idle = MakeClip(rig, "Idle", 2.4f, true);
        AnimationClip happy = MakeClip(rig, "Attack", .4f, false);
        AnimationClip sad = MakeClip(rig, "Damage", .55f, false);
        AnimationClip win = MakeClip(rig, "Win", .9f, false);
        string path = Motions + "/PowaGameplay.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState state in machine.states) machine.RemoveState(state.state);
        AnimatorState idleState = machine.AddState("Idle"); idleState.motion = idle;
        machine.defaultState = idleState;
        foreach (AnimationClip clip in new[] { happy, sad, win })
        {
            AnimatorState state = machine.AddState(clip.name); state.motion = clip;
            state.writeDefaultValues = false;
            if (clip == win) continue;
            AnimatorStateTransition back = state.AddTransition(idleState);
            back.hasExitTime = true; back.exitTime = 1; back.duration = .07f;
            back.hasFixedDuration = true;
        }
        idleState.writeDefaultValues = false;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip MakeClip(GameObject rig, string name, float duration, bool loop)
    {
        string path = Motions + "/Powa_" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves(); clip.name = name; clip.frameRate = 60;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop; AnimationUtility.SetAnimationClipSettings(clip, settings);
        // Every state writes a complete pose so rapid interruptions cannot leave stale limbs/expressions.
        foreach (Transform node in rig.GetComponentsInChildren<Transform>())
        {
            if (node == rig.transform) continue;
            string nodePath = AnimationUtility.CalculateTransformPath(node, rig.transform);
            SetCurve(clip, nodePath, typeof(Transform), "m_LocalPosition.x", duration, node.localPosition.x);
            SetCurve(clip, nodePath, typeof(Transform), "m_LocalPosition.y", duration, node.localPosition.y);
            SetCurve(clip, nodePath, typeof(Transform), "localEulerAnglesRaw.z", duration, Mathf.DeltaAngle(0, node.localEulerAngles.z));
        }
        foreach (SpriteRenderer renderer in rig.GetComponentsInChildren<SpriteRenderer>())
        {
            SetCurve(clip, AnimationUtility.CalculateTransformPath(renderer.transform, rig.transform), typeof(SpriteRenderer), "m_Color.a", duration, renderer.color.a);
            SetCurve(clip, AnimationUtility.CalculateTransformPath(renderer.transform, rig.transform), typeof(SpriteRenderer), "m_SortingOrder", duration, renderer.sortingOrder);
        }

        string torso = "Motion/Torso", head = torso + "/Head";
        bool isWin = name == "Win", isSad = name == "Damage";
        if (loop)
        {
            Offset(clip, rig, "Motion", "m_LocalPosition.y", duration, 0, .06f, 0, -.06f, 0);
            Offset(clip, rig, torso, "localEulerAnglesRaw.z", duration, 0, -1.2f, 0, 1.2f, 0);
            Offset(clip, rig, head + "/Hat/Root/Flex", "localEulerAnglesRaw.z", duration, 0, 3, 0, -3, 0);
            Offset(clip, rig, head + "/HatRear/Root/Flex", "localEulerAnglesRaw.z", duration, 0, 3, 0, -3, 0);
            Offset(clip, rig, torso + "/Dress/Root/Flex", "localEulerAnglesRaw.z", duration, 0, -2, 0, 2, 0);
            Offset(clip, rig, "Motion/Staff", "m_LocalPosition.y", duration, 0, -.035f, 0, .035f, 0);
            Offset(clip, rig, "Motion/Staff", "localEulerAnglesRaw.z", duration, 0, 3, 0, -3, 0);
        }
        else
        {
            float bounce = isSad ? -.12f : isWin ? .30f : .12f;
            Offset(clip, rig, "Motion", "m_LocalPosition.y", duration, 0, -bounce * .25f, bounce, bounce * .5f, isWin ? .12f : 0);
            Offset(clip, rig, torso, "localEulerAnglesRaw.z", duration, 0, isSad ? 4 : -3, isSad ? 6 : -7, isSad ? 2 : -3, isWin ? -4 : 0);
            Offset(clip, rig, head, "localEulerAnglesRaw.z", duration, 0, 2, isSad ? 7 : -4, 2, 0);
            Offset(clip, rig, torso + "/ArmLeft", "localEulerAnglesRaw.z", duration, 0, 5, isSad ? 12 : isWin ? -105 : -20, isSad ? 8 : isWin ? -90 : -10, isWin ? -90 : 0);
            Offset(clip, rig, torso + "/ArmRight", "localEulerAnglesRaw.z", duration, 0, -5, isSad ? -12 : isWin ? 105 : 22, isSad ? -8 : isWin ? 98 : 10, isWin ? 98 : 0);
            Offset(clip, rig, torso + "/LegLeft", "localEulerAnglesRaw.z", duration, 0, -2, isWin ? 24 : 5, isWin ? 15 : 2, isWin ? 15 : 0);
            Offset(clip, rig, torso + "/LegRight", "localEulerAnglesRaw.z", duration, 0, 2, isWin ? -22 : -5, isWin ? -15 : -2, isWin ? -15 : 0);
            Offset(clip, rig, head + "/Hat/Root/Flex", "localEulerAnglesRaw.z", duration, 0, -3, isSad ? -5 : 7, -4, 0);
            Offset(clip, rig, head + "/HatRear/Root/Flex", "localEulerAnglesRaw.z", duration, 0, -3, isSad ? -5 : 7, -4, 0);
            Offset(clip, rig, torso + "/Dress/Root/Flex", "localEulerAnglesRaw.z", duration, 0, -2, isWin ? -11 : 5, 3, isWin ? -4 : 0);
            Offset(clip, rig, "Motion/Staff", "localEulerAnglesRaw.z", duration, 0, -5, isSad ? -22 : isWin ? 8 : 25, isSad ? -10 : isWin ? 5 : 10, isWin ? 5 : 0);
            Offset(clip, rig, "Motion/Staff", "m_LocalPosition.y", duration, 0, 0, isSad ? -.14f : isWin ? 1.48f : .14f, isWin ? 1.48f : .04f, isWin ? 1.48f : 0);
            Offset(clip, rig, "Motion/Staff", "m_LocalPosition.x", duration, 0, 0, isWin ? -.42f : 0, isWin ? -.42f : 0, isWin ? -.42f : 0);
        }
        string expression = loop ? "Normal" : isSad ? "Sad" : "Happy";
        foreach (string category in new[] { head + "/Mouth/", head + "/Hat/Badge/" })
        foreach (string face in new[] { "Normal", "Happy", "Sad" })
            SetCurve(clip, category + face, typeof(SpriteRenderer), "m_Color.a", duration, face == expression ? 1 : 0);
        SetCurve(clip, head + "/Tear", typeof(SpriteRenderer), "m_Color.a", duration, isSad ? 1 : 0);
        if (isSad) Offset(clip, rig, head + "/Tear", "m_LocalPosition.y", duration, 0, -.015f, -.04f, -.07f, -.10f);
        if (isWin)
        {
            SetCurve(clip, torso + "/ArmLeft", typeof(SpriteRenderer), "m_SortingOrder", duration, 9);
            SetCurve(clip, torso + "/ArmRight", typeof(SpriteRenderer), "m_SortingOrder", duration, 9);
            BakeStaffGrip(clip, rig, duration);
        }
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void BakeStaffGrip(AnimationClip clip, GameObject rig, float duration)
    {
        Transform staff = rig.transform.Find("Motion/Staff");
        Transform hand = rig.transform.Find("Motion/Torso/ArmRight/Grip");
        Sprite sprite = staff.GetComponent<SpriteRenderer>().sprite;
        Vector3 staffGrip = new(0, sprite.bounds.size.y * .05f, 0);
        var positions = rig.GetComponentsInChildren<Transform>().ToDictionary(t => t, t => (t.localPosition, t.localRotation));
        var colors = rig.GetComponentsInChildren<SpriteRenderer>().ToDictionary(r => r, r => (r.color, r.sortingOrder));
        List<float> x = new(), y = new();
        const int samples = 55;
        for (int i = 0; i < samples; i++)
        {
            float time = duration * i / (samples - 1);
            clip.SampleAnimation(rig, time);
            Vector3 target = staff.parent.InverseTransformPoint(hand.position - staff.TransformVector(staffGrip));
            Vector3 gripPosition = Vector3.Lerp(staff.localPosition, target, Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / .4f)));
            x.Add(gripPosition.x); y.Add(gripPosition.y);
        }
        SetCurve(clip, "Motion/Staff", typeof(Transform), "m_LocalPosition.x", duration, x.ToArray());
        SetCurve(clip, "Motion/Staff", typeof(Transform), "m_LocalPosition.y", duration, y.ToArray());
        foreach (var pair in positions) { pair.Key.localPosition = pair.Value.localPosition; pair.Key.localRotation = pair.Value.localRotation; }
        foreach (var pair in colors) { pair.Key.color = pair.Value.color; pair.Key.sortingOrder = pair.Value.sortingOrder; }
    }

    private static void Offset(AnimationClip clip, GameObject rig, string path, string property, float duration, params float[] values)
    {
        Transform node = rig.transform.Find(path);
        if (node == null) throw new InvalidOperationException("Missing animated node: " + path);
        float baseline = property.EndsWith("Position.x") ? node.localPosition.x
            : property.EndsWith("Position.y") ? node.localPosition.y : Mathf.DeltaAngle(0, node.localEulerAngles.z);
        SetCurve(clip, path, typeof(Transform), property, duration, values.Select(v => baseline + v).ToArray());
    }

    private static void SetCurve(AnimationClip clip, string path, Type type, string property, float duration, params float[] values)
    {
        if (values.Length == 1) values = new[] { values[0], values[0] };
        AnimationCurve curve = new(values.Select((v, i) => new Keyframe(duration * i / (values.Length - 1), v)).ToArray());
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);
    }

    private static void InstallInMain()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
        GameObject old = GameObject.Find("PlayerCharacter");
        if (old == null) throw new InvalidOperationException("Main PlayerCharacter is missing.");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
        instance.name = "PlayerCharacter";
        instance.transform.SetPositionAndRotation(new Vector3(-24, -22, 0), old.transform.rotation);
        instance.transform.localScale = Vector3.one * 7.5f;
        instance.AddComponent<PowaGameplayLayout>().ApplyLayout(Camera.main);
        GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
        SerializedObject flowData = new(flow);
        flowData.FindProperty("_playerAnimationController").objectReferenceValue = instance.GetComponent<PlayerAnimationController>();
        flowData.ApplyModifiedPropertiesWithoutUndo();
        UnityEngine.Object.DestroyImmediate(old);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
