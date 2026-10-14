var root = "Assets/Art/Sprites/Backgrounds/StageCards/";
var ids = new[]{"cars","hands","clocks","greens","beans","reptiles","poses","eras","hiragana"};
var names = new[]{"","Hands","Clocks","Greens","Beans","Reptiles","Poses","Eras","Hiragana"};
var backgrounds = new System.Collections.Generic.Dictionary<string,UnityEngine.Sprite>();
var report = new System.Text.StringBuilder();
for (int i=0;i<ids.Length;i++)
{
    string assetPath = i==0 ? "Assets/Resources/CarVisualDatabase.asset" : "Assets/Resources/SortingThemes/"+names[i]+".asset";
    var theme = UnityEditor.AssetDatabase.LoadAssetAtPath<CarVisualDatabase>(assetPath);
    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(root+"stage_thumb_"+ids[i]+".png");
    if(theme==null || sprite==null) throw new System.Exception("Missing theme or sprite: "+ids[i]);
    var so = new UnityEditor.SerializedObject(theme);
    so.FindProperty("_stageSelectBackground").objectReferenceValue=sprite;
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(theme);
    backgrounds.Add(ids[i],sprite);
    report.AppendLine(theme.ThemeName+" -> "+sprite.name);
}
UnityEditor.AssetDatabase.SaveAssets();
const string prefabPath="Assets/Prefabs/StageCard.prefab";
var cardRoot=UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var card=cardRoot.GetComponent<StageCardView>();
    var so=new UnityEditor.SerializedObject(card);
    var fallbacks=so.FindProperty("_stageThumbnailSprites");
    if(fallbacks.arraySize==0) fallbacks.arraySize=1;
    for(int i=0;i<fallbacks.arraySize;i++) fallbacks.GetArrayElementAtIndex(i).objectReferenceValue=backgrounds["cars"];
    var image=(UnityEngine.UI.Image)so.FindProperty("_thumbnailImage").objectReferenceValue;
    so.ApplyModifiedPropertiesWithoutUndo();
    image.sprite=backgrounds["cars"];
    image.type=UnityEngine.UI.Image.Type.Simple;
    image.preserveAspect=true;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(cardRoot,prefabPath);
    var database=UnityEditor.AssetDatabase.LoadAssetAtPath<StageDatabase>("Assets/Resources/StageDatabase.asset");
    int checks=0;
    foreach(var stage in database.Stages)
    {
        foreach(StageCardStatus status in System.Enum.GetValues(typeof(StageCardStatus)))
        {
            card.SetData(stage,120,status,3,System.Math.Max(1,stage.StageNumber-1));
            if(image.sprite==null || image.sprite!=stage.Theme.StageSelectBackground || !image.preserveAspect)
                throw new System.Exception("Thumbnail routing failed: "+stage.StageNumber+" "+status);
            if(UnityEngine.Mathf.Abs(image.sprite.rect.width/image.sprite.rect.height-12f/7f)>0.001f)
                throw new System.Exception("Thumbnail aspect failed: "+stage.StageNumber);
            checks++;
        }
        report.AppendLine("Stage "+stage.StageNumber+": "+image.sprite.name);
    }
    card.SetEndlessData(120);
    if(image.sprite!=backgrounds["cars"]) throw new System.Exception("Endless card fallback failed");
    report.AppendLine("PASS "+checks+" stage/status checks; endless fallback PASS");
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(cardRoot); }
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllText("Deliverables/StageCardThemes/assignment-validation.txt",report.ToString());
return report.ToString();
