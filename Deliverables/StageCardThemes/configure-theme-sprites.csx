var root = "Assets/Art/Sprites/Backgrounds/StageCards/";
UnityEditor.AssetDatabase.Refresh();
var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
factory.Init();
var report = new System.Text.StringBuilder();
foreach (var path in System.IO.Directory.GetFiles(root, "*.png"))
{
    var assetPath = path.Replace('\\','/');
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(assetPath);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
    importer.spritePixelsPerUnit = 100f;
    importer.mipmapEnabled = false;
    importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
    importer.maxTextureSize = 2048;
    importer.textureCompression = UnityEditor.TextureImporterCompression.CompressedHQ;
    importer.filterMode = UnityEngine.FilterMode.Bilinear;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    var settings = new UnityEditor.TextureImporterSettings();
    importer.ReadTextureSettings(settings);
    settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
    importer.SetTextureSettings(settings);
    importer.SaveAndReimport();
    var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
    if (provider == null) throw new System.Exception("Sprite provider missing: " + assetPath);
    provider.InitSpriteEditorDataProvider();
    var capabilityProvider = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if (capabilityProvider == null) throw new System.Exception("Edit capability missing: " + assetPath);
    var capabilities = capabilityProvider.GetEditCapability();
    foreach (var capability in new[]{ UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite, UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect, UnityEditor.U2D.Sprites.EEditCapability.EditPivot, UnityEditor.U2D.Sprites.EEditCapability.EditBorder, UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName })
        if (!capabilities.HasCapability(capability)) throw new System.Exception("Unsupported sprite edit: " + capability + " " + assetPath);
    importer.GetSourceTextureWidthAndHeight(out int w, out int h);
    int unit = System.Math.Min(w / 12, h / 7);
    var previous = provider.GetSpriteRects();
    var id = previous.Length == 1 ? previous[0].spriteID : UnityEditor.GUID.Generate();
    var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
    var rect = new UnityEditor.SpriteRect { name=name, spriteID=id, rect=new UnityEngine.Rect((w-unit*12)/2, (h-unit*7)/2, unit*12, unit*7), alignment=UnityEngine.SpriteAlignment.Center, pivot=new UnityEngine.Vector2(0.5f,0.5f), border=UnityEngine.Vector4.zero };
    provider.SetSpriteRects(new[]{rect});
    var names = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
    if (names == null) throw new System.Exception("Name-file-ID provider missing: " + assetPath);
    names.SetNameFileIdPairs(new[]{new UnityEditor.SpriteNameFileIdPair(name,id)});
    provider.Apply();
    importer.SaveAndReimport();
    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(assetPath);
    if(sprite == null || UnityEngine.Mathf.Abs(sprite.rect.width / sprite.rect.height - 12f/7f) > 0.001f) throw new System.Exception("Invalid sprite crop: " + assetPath);
    report.AppendLine(name + ": " + w + "x" + h + " -> " + sprite.rect);
}
return report.ToString();
