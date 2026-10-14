if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception("Finish tests first.");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
    if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.Exception("Unsaved scene; capture aborted.");
var previousPrefs=new System.Collections.Generic.Dictionary<string,int?>();
var keys=new System.Collections.Generic.List<string>{"LastStage","LastGameMode","SelectedStage"};
for(int i=1;i<=30;i++) keys.Add("StageClear_"+i);
foreach(string key in keys) previousPrefs[key]=UnityEngine.PlayerPrefs.HasKey(key)?UnityEngine.PlayerPrefs.GetInt(key):(int?)null;
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var type=typeof(RegressionCaptureRunner);
var profiles=(System.Array)type.GetField("DeviceProfiles",flags).GetValue(null);
var capture=typeof(PowaScreenshotCapture).GetMethod("CaptureSceneForRegression",flags);
var refresh=type.GetMethod("RefreshSafeAreaFitters",flags);
var layouts=type.GetMethod("ForceLayouts",flags);
var validate=type.GetMethod("ValidateLayout",flags);
var instanceFlags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var output="Deliverables/StageCardThemes/Preview";
System.IO.Directory.CreateDirectory(output);
var report=new System.Text.StringBuilder("Settled editor previews using real stage-select scene and card prefab. All stages temporarily unlocked; preferences and original scene restored afterwards.\n");
int count=0;
try
{
    for(int i=1;i<=30;i++) UnityEngine.PlayerPrefs.SetInt("StageClear_"+i,1);
    foreach(int stageNumber in new[]{1,4,8,11,16,17,18,19,20})
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/StageSelect.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
        PowaScreenshotCapture.PrepareStageSelectChapterForRegression((stageNumber-1)/5+1);
        var controller=UnityEngine.Object.FindFirstObjectByType<StageSelectController>();
        var so=new UnityEditor.SerializedObject(controller);
        var swipe=(SwipeSnapController)so.FindProperty("_swipeSnapController").objectReferenceValue;
        var container=(UnityEngine.RectTransform)so.FindProperty("_stageCardContainer").objectReferenceValue;
        var card=container.GetChild(stageNumber-1).GetComponent<StageCardView>();
        int selectedIndex=(stageNumber-1)%5;
        typeof(StageSelectController).GetMethod("ApplySelectionIndex",instanceFlags).Invoke(controller,new object[]{selectedIndex});
        foreach(var profile in profiles)
        {
            var pt=profile.GetType();
            int width=(int)pt.GetProperty("Width").GetValue(profile);
            int height=(int)pt.GetProperty("Height").GetValue(profile);
            string name=(string)pt.GetProperty("Name").GetValue(profile);
            var safe=(UnityEngine.Rect)pt.GetProperty("SafeArea").GetValue(profile);
            SafeAreaFitter.SetEditorSimulation(new UnityEngine.Vector2Int(width,height),safe);
            System.Action<UnityEngine.Camera> ready=camera=>{
                refresh.Invoke(null,new object[]{scene});
                layouts.Invoke(null,new object[]{scene});
                swipe.JumpToIndex(selectedIndex);
                layouts.Invoke(null,new object[]{scene});
                typeof(StageSelectController).GetMethod("AlignChapterTabs",instanceFlags).Invoke(controller,null);
                layouts.Invoke(null,new object[]{scene});
                var root=UnityEngine.GameObject.Find("Canvas").transform;
                var failures=(System.Collections.Generic.IEnumerable<string>)validate.Invoke(null,new object[]{scene,root,profile});
                foreach(string failure in failures) throw new System.Exception(failure);
                var corners=new UnityEngine.Vector3[4];
                ((UnityEngine.RectTransform)card.transform).GetWorldCorners(corners);
                var left=UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,corners[0]);
                var right=UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
                float center=(left.x+right.x)*0.5f;
                if(left.x<safe.xMin-4 || right.x>safe.xMax+4 || UnityEngine.Mathf.Abs(center-width*0.5f)>4)
                    throw new System.Exception("Selected card not centered: "+stageNumber+" "+name+" "+left+" "+right);
            };
            capture.Invoke(null,new object[]{scene,output+"/Stage"+stageNumber.ToString("00")+"_"+name+".png",width,height,ready});
            report.AppendLine("PASS Stage "+stageNumber+" / "+name+" / centered, safe area, buttons, text");
            count++;
        }
    }
}
finally
{
    SafeAreaFitter.ClearEditorSimulation();
    foreach(var pair in previousPrefs) { if(pair.Value.HasValue) UnityEngine.PlayerPrefs.SetInt(pair.Key,pair.Value.Value); else UnityEngine.PlayerPrefs.DeleteKey(pair.Key); }
    UnityEngine.PlayerPrefs.Save();
    UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);
}
report.AppendLine("PASS "+count+" theme/device previews");
System.IO.File.WriteAllText(output+"/validation.txt",report.ToString());
return report.ToString();
