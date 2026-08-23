using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class GameplayFlowPlayModeTests
{
    private const float SceneTimeoutSeconds = 12f;
    private const float ResultUnlockTimeoutSeconds = 5f;
    private const float TestTimeScale = 20f;

    private static readonly string[] PlayerPrefsKeys =
    {
        "BGM_On",
        "SE_On",
        "Vibration_On",
        "HowTo_Shown",
        "Tutorial_Completed",
        "Tutorial_Skipped",
        "SelectedStage",
        "LastStage",
        "LastGameMode",
        "BestScore_Stage1",
        "StarRating_Stage1"
    };

    private readonly Dictionary<string, SavedIntPreference> _savedPreferences = new();

    [OneTimeSetUp]
    public void SavePlayerPreferences()
    {
        foreach (string key in PlayerPrefsKeys)
        {
            _savedPreferences[key] = new SavedIntPreference(PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
        }
    }

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.SetInt("BGM_On", 0);
        PlayerPrefs.SetInt("SE_On", 0);
        PlayerPrefs.SetInt("Vibration_On", 0);
        PlayerPrefs.SetInt("HowTo_Shown", 1);
        PlayerPrefs.SetInt("Tutorial_Completed", 1);
        PlayerPrefs.SetInt("Tutorial_Skipped", 0);
        PlayerPrefs.SetInt("SelectedStage", 1);
        PlayerPrefs.SetInt("LastStage", 1);
        PlayerPrefs.SetInt("LastGameMode", 0);
        PlayerPrefs.SetInt("BestScore_Stage1", 0);
        PlayerPrefs.SetInt("StarRating_Stage1", 0);
        PlayerPrefs.Save();

        InvokeStatic(RequiredType("SessionState"), "SelectStage", 1);
        Time.timeScale = TestTimeScale;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
    }

    [OneTimeTearDown]
    public void RestorePlayerPreferences()
    {
        foreach (KeyValuePair<string, SavedIntPreference> pair in _savedPreferences)
        {
            if (pair.Value.Existed)
            {
                PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
            }
            else
            {
                PlayerPrefs.DeleteKey(pair.Key);
            }
        }

        PlayerPrefs.Save();
    }

    [UnityTest]
    [Order(1)]
    public IEnumerator PointerDownScenarios_Clear_Result_AndRetryCompleteEndToEnd()
    {
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object scoreManager = FindSceneComponent("ScoreManager");
        object carSpawner = FindSceneComponent("CarSpawner");
        object laneInput = FindSceneComponent("LaneInputController");
        Assert.That(scoreManager, Is.Not.Null);
        Assert.That(carSpawner, Is.Not.Null);
        Assert.That(laneInput, Is.Not.Null);

        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        AssertSpawnerLimit(carSpawner);
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        AssertScore(scoreManager, score: 0, misses: 1);

        SpawnFixedCar(carSpawner, "CompactCar");
        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        AssertScore(scoreManager, score: 0, misses: 2);
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        SpawnFixedCar(carSpawner, "LightTruck");
        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        AssertScore(scoreManager, score: 10, misses: 2);
        Assert.That(Invoke(scoreManager, "GetCorrectCount", EnumValue("CarType", "LightTruck")), Is.EqualTo(1));

        SpawnFixedCar(carSpawner, "SportsCar");
        PointerDown(laneInput, "LightTruck");
        PointerDown(laneInput, "SportsCar");
        yield return SettleInput();
        AssertScore(scoreManager, score: 20, misses: 2);
        Assert.That(Invoke(scoreManager, "GetCorrectCount", EnumValue("CarType", "SportsCar")), Is.EqualTo(1),
            "The last Pointer Down in the frame should win and only one answer should be processed.");

        for (int i = 0; i < 4; i += 1)
        {
            SpawnFixedCar(carSpawner, "CompactCar");
            PointerDown(laneInput, "CompactCar");
            yield return SettleInput();
        }

        yield return WaitForScene("Result", SceneTimeoutSeconds);
        AssertStoredResult(isClear: true, expectedScore: 60, expectedMisses: 2);
        yield return RetryFromResult();
    }

    [UnityTest]
    [Order(2)]
    public IEnumerator MissLimit_GiveUp_Result_AndRetryCompleteEndToEnd()
    {
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object scoreManager = FindSceneComponent("ScoreManager");
        object carSpawner = FindSceneComponent("CarSpawner");
        object laneInput = FindSceneComponent("LaneInputController");
        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        for (int i = 0; i < 4; i += 1)
        {
            PointerDown(laneInput, "LightTruck");
            yield return SettleInput();
        }

        AssertScore(scoreManager, score: 0, misses: 4);
        object continuePrompt = FindSceneComponent("ContinuePromptController");
        Assert.That(continuePrompt, Is.Not.Null, "The miss limit should open the continue prompt before game over.");
        object giveUpButton = GetField(continuePrompt, "_giveUpButton");
        object onClick = GetProperty(giveUpButton, "onClick");
        Invoke(onClick, "Invoke");

        yield return WaitForScene("Result", SceneTimeoutSeconds);
        AssertStoredResult(isClear: false, expectedScore: 0, expectedMisses: 4);
        yield return RetryFromResult();
    }

    [UnityTest]
    [Order(3)]
    public IEnumerator ExpressCoveredAndBrokenCarsUseTheirDedicatedRules()
    {
        InvokeStatic(RequiredType("SessionState"), "SelectStage", 4);
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object scoreManager = FindSceneComponent("ScoreManager");
        object carSpawner = FindSceneComponent("CarSpawner");
        object laneInput = FindSceneComponent("LaneInputController");
        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        SpawnFixedCar(carSpawner, "LightTruck", "Express");
        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        AssertScore(scoreManager, score: 20, misses: 0);

        object coveredCar = SpawnFixedCar(carSpawner, "CompactCar", "Covered");
        Assert.That(GetProperty(coveredCar, "IsRevealed"), Is.False);
        PointerDown(laneInput, "CompactCar");
        yield return SettleInput();
        AssertScore(scoreManager, score: 15, misses: 1);

        Invoke(coveredCar, "Reveal");
        PointerDown(laneInput, "CompactCar");
        yield return SettleInput();
        AssertScore(scoreManager, score: 25, misses: 1);

        SpawnFixedCar(carSpawner, "SportsCar", "Broken");
        RepairPointerDown();
        yield return SettleInput();
        AssertScore(scoreManager, score: 35, misses: 1);
    }

    [UnityTest]
    [Order(4)]
    public IEnumerator StageFiveRushSpawnsAThreeCarProcessionAndShowsFeverHud()
    {
        InvokeStatic(RequiredType("SessionState"), "SelectStage", 5);
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object carSpawner = FindSceneComponent("CarSpawner");
        object gimmickHud = FindSceneComponent("GimmickHudController");
        object progressHud = FindSceneComponent("StageProgressHudController");
        Assert.That(carSpawner, Is.Not.Null);
        Assert.That(gimmickHud, Is.Not.Null);
        Assert.That(progressHud, Is.Not.Null);

        Canvas gimmickCanvas = (gimmickHud as Component)?.GetComponentInChildren<Canvas>(true);
        Canvas progressCanvas = FindSceneCanvas("StageProgressHudCanvas");
        Assert.That(gimmickCanvas, Is.Not.Null);
        Assert.That(progressCanvas, Is.Not.Null);
        Assert.That(gimmickCanvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
        Assert.That(progressCanvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
        Assert.That(gimmickCanvas.worldCamera, Is.SameAs(Camera.main));
        Assert.That(progressCanvas.worldCamera, Is.SameAs(Camera.main));

        RectTransform comboRoot = GetField(gimmickHud, "_comboRoot") as RectTransform;
        RectTransform repairRoot = GetField(gimmickHud, "_repairRoot") as RectTransform;
        Assert.That(comboRoot, Is.Not.Null);
        Assert.That(repairRoot, Is.Not.Null);
        Assert.That(comboRoot.gameObject.activeInHierarchy, Is.True, "Stage 5 should expose the fever combo HUD.");
        Assert.That(comboRoot.rect.width, Is.GreaterThan(0f));
        Assert.That(comboRoot.rect.height, Is.GreaterThan(0f));

        Canvas.ForceUpdateCanvases();
        Rect repairScreenRect = GetScreenRect(repairRoot, gimmickCanvas.worldCamera);
        Component laneInput = FindSceneComponent("LaneInputController") as Component;
        Assert.That(laneInput, Is.Not.Null);
        UnityEngine.UI.Button[] laneButtons = laneInput.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .Where(button => button.transform.parent == laneInput.transform)
            .ToArray();
        Assert.That(laneButtons, Has.Length.EqualTo(3));
        foreach (UnityEngine.UI.Button laneButton in laneButtons)
        {
            Rect laneScreenRect = GetScreenRect(laneButton.transform as RectTransform, Camera.main);
            Assert.That(
                repairScreenRect.Overlaps(laneScreenRect),
                Is.False,
                $"Repair button overlaps lane button '{laneButton.name}'.");
        }

        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
        Invoke(carSpawner, "StartSpawning");
        Assert.That(Invoke(carSpawner, "TryStartRush"), Is.EqualTo(true));

        yield return WaitUntil(
            () => (int)GetProperty(carSpawner, "LastRushSpawnCount") == 3,
            2f,
            "Rush should send all three cars instead of dropping attempts blocked by the safe spawn gap.");

        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
    }

    private static IEnumerator RetryFromResult()
    {
        object resultController = FindSceneComponent("ResultController");
        Assert.That(resultController, Is.Not.Null);
        yield return WaitUntil(
            () => (bool)GetField(resultController, "_areResultActionsUnlocked"),
            ResultUnlockTimeoutSeconds,
            "Result actions did not unlock after the presentation and disabled ad stub.");

        Invoke(resultController, "OnRetryPressed");
        yield return WaitForScene("Main", SceneTimeoutSeconds);

        Type sessionState = RequiredType("SessionState");
        Assert.That(GetStaticProperty(sessionState, "SelectedStageNumber"), Is.EqualTo(1));
        Assert.That(FindSceneComponent("GameFlowController"), Is.Not.Null, "Retry should load a fresh Main flow.");
    }

    private static void AssertSpawnerLimit(object carSpawner)
    {
        Assert.That(SpawnFixedCar(carSpawner, "LightTruck"), Is.Not.Null);
        Assert.That(SpawnFixedCar(carSpawner, "CompactCar"), Is.Not.Null);
        Assert.That(SpawnFixedCar(carSpawner, "SportsCar"), Is.Not.Null);
        Assert.That(SpawnFixedCar(carSpawner, "LightTruck"), Is.Null, "At most three cars may be active.");
    }

    private static object SpawnFixedCar(object carSpawner, string carTypeName)
    {
        return Invoke(carSpawner, "SpawnFixedCar", EnumValue("CarType", carTypeName), 0f);
    }

    private static object SpawnFixedCar(object carSpawner, string carTypeName, string modifierName)
    {
        return Invoke(
            carSpawner,
            "SpawnFixedCar",
            EnumValue("CarType", carTypeName),
            0f,
            EnumValue("CarModifier", modifierName));
    }

    private static void RepairPointerDown()
    {
        object gimmickHud = FindSceneComponent("GimmickHudController");
        Assert.That(gimmickHud, Is.Not.Null);
        Type forwarderType = RequiredType("RepairButtonPointerDownForwarder");
        Component hudComponent = gimmickHud as Component;
        Component forwarder = hudComponent.GetComponentsInChildren(forwarderType, true).FirstOrDefault();
        Assert.That(forwarder, Is.Not.Null, "Repair Pointer Down forwarder was not configured.");
        Invoke(forwarder, "OnPointerDown", new object[] { null });
    }

    private static void PointerDown(object laneInput, string carTypeName)
    {
        Type forwarderType = RequiredType("LaneButtonPointerDownForwarder");
        Component laneInputComponent = laneInput as Component;
        Assert.That(laneInputComponent, Is.Not.Null);
        Component[] forwarders = laneInputComponent.GetComponentsInChildren(forwarderType, true);
        object expectedLane = EnumValue("CarType", carTypeName);
        object forwarder = forwarders.FirstOrDefault(candidate => Equals(GetProperty(candidate, "LaneType"), expectedLane));
        Assert.That(forwarder, Is.Not.Null, $"Pointer Down forwarder for {carTypeName} was not configured.");
        Invoke(forwarder, "OnPointerDown", new object[] { null });
    }

    private static IEnumerator SettleInput()
    {
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.02f);
    }

    private static IEnumerator LoadScene(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        Assert.That(operation, Is.Not.Null, $"Could not start loading scene {sceneName}.");
        while (!operation.isDone)
        {
            yield return null;
        }

        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
    }

    private static IEnumerator WaitForScene(string sceneName, float timeoutSeconds)
    {
        yield return WaitUntil(
            () => SceneManager.GetActiveScene().name == sceneName && SceneManager.GetActiveScene().isLoaded,
            timeoutSeconds,
            $"Scene {sceneName} was not loaded.");
        yield return null;
    }

    private static IEnumerator WaitUntil(Func<bool> predicate, float timeoutSeconds, string failureMessage)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!predicate() && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.That(predicate(), Is.True, failureMessage);
    }

    private static void AssertScore(object scoreManager, int score, int misses)
    {
        Assert.That(GetProperty(scoreManager, "CurrentScore"), Is.EqualTo(score));
        Assert.That(GetProperty(scoreManager, "MissCount"), Is.EqualTo(misses));
    }

    private static Rect GetScreenRect(RectTransform rectTransform, Camera camera)
    {
        Assert.That(rectTransform, Is.Not.Null);
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector2 first = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        float xMin = first.x;
        float yMin = first.y;
        float xMax = first.x;
        float yMax = first.y;
        for (int i = 1; i < corners.Length; i += 1)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
            xMin = Mathf.Min(xMin, point.x);
            yMin = Mathf.Min(yMin, point.y);
            xMax = Mathf.Max(xMax, point.x);
            yMax = Mathf.Max(yMax, point.y);
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static void AssertStoredResult(bool isClear, int expectedScore, int expectedMisses)
    {
        object result = GetStaticProperty(RequiredType("SessionState"), "LastResult");
        Assert.That(result, Is.Not.Null);
        Assert.That(GetProperty(result, "IsClear"), Is.EqualTo(isClear));
        Assert.That(GetProperty(result, "StageNumber"), Is.EqualTo(1));
        Assert.That(GetProperty(result, "Score"), Is.EqualTo(expectedScore));
        Assert.That(GetProperty(result, "MissCount"), Is.EqualTo(expectedMisses));
    }

    private static bool GetBool(object target, string methodName)
    {
        return target != null && (bool)Invoke(target, methodName);
    }

    private static object FindSceneComponent(string typeName)
    {
        Type type = RequiredType(typeName);
        foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
        {
            if (candidate is Component component && component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded)
            {
                return component;
            }
        }

        return null;
    }

    private static Canvas FindSceneCanvas(string objectName)
    {
        return Resources.FindObjectsOfTypeAll<Canvas>()
            .FirstOrDefault(canvas => canvas != null
                && canvas.gameObject.scene.IsValid()
                && canvas.gameObject.scene.isLoaded
                && canvas.gameObject.name == objectName);
    }

    private static Type RequiredType(string typeName)
    {
        Type type = Type.GetType(typeName + ", Assembly-CSharp")
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "Assembly-CSharp")
                ?.GetType(typeName);
        Assert.That(type, Is.Not.Null, $"Expected {typeName} to exist in Assembly-CSharp.");
        return type;
    }

    private static object EnumValue(string typeName, string value)
    {
        return Enum.Parse(RequiredType(typeName), value);
    }

    private static object InvokeStatic(Type type, string methodName, params object[] args)
    {
        MethodInfo method = FindMethod(type, methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, args);
        return method.Invoke(null, args);
    }

    private static object Invoke(object target, string methodName, params object[] args)
    {
        Assert.That(target, Is.Not.Null, $"Cannot invoke {methodName} on a null target.");
        MethodInfo method = FindMethod(target.GetType(), methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, args);
        return method.Invoke(target, args);
    }

    private static MethodInfo FindMethod(Type type, string methodName, BindingFlags flags, object[] args)
    {
        MethodInfo method = type.GetMethods(flags)
            .FirstOrDefault(candidate =>
            {
                if (candidate.Name != methodName)
                {
                    return false;
                }

                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != args.Length)
                {
                    return false;
                }

                for (int i = 0; i < parameters.Length; i += 1)
                {
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i]))
                    {
                        return false;
                    }
                }

                return true;
            });
        Assert.That(method, Is.Not.Null, $"Missing method {type.Name}.{methodName}.");
        return method;
    }

    private static object GetProperty(object target, string propertyName)
    {
        Assert.That(target, Is.Not.Null);
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(property, Is.Not.Null, $"Missing property {target.GetType().Name}.{propertyName}.");
        return property.GetValue(target);
    }

    private static object GetStaticProperty(Type type, string propertyName)
    {
        PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(property, Is.Not.Null, $"Missing static property {type.Name}.{propertyName}.");
        return property.GetValue(null);
    }

    private static object GetField(object target, string fieldName)
    {
        Assert.That(target, Is.Not.Null);
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}.");
        return field.GetValue(target);
    }

    private readonly struct SavedIntPreference
    {
        public SavedIntPreference(bool existed, int value)
        {
            Existed = existed;
            Value = value;
        }

        public bool Existed { get; }
        public int Value { get; }
    }
}
