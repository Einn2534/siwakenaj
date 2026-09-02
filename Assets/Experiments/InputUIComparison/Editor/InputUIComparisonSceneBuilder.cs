using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Siwakenja.Experiments.InputUIComparison.Editor
{
    public static class InputUIComparisonSceneBuilder
    {
        public const string ScenePath = "Assets/Experiments/InputUIComparison/Scenes/InputUIComparison.unity";
        private const string SportsSpritePath = "Assets/Art/Sprites/Vehicles/sportscar.png";
        private const string CompactSpritePath = "Assets/Art/Sprites/Vehicles/car.png";
        private const string LightTruckSpritePath = "Assets/Art/Sprites/Vehicles/truck.png";

        [MenuItem("Tools/しわ賢者/Input UI Comparison/Rebuild Experiment Scene")]
        public static void RebuildScene()
        {
            Scene previousActive = SceneManager.GetActiveScene();
            string directory = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            Scene loadedExperiment = default;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene candidate = SceneManager.GetSceneAt(i);
                if (string.Equals(candidate.path, ScenePath, System.StringComparison.OrdinalIgnoreCase))
                {
                    loadedExperiment = candidate;
                    break;
                }
            }

            bool reuseLoadedExperiment = loadedExperiment.IsValid();
            bool replaceDisposableUntitled = !reuseLoadedExperiment
                && string.IsNullOrEmpty(previousActive.path)
                && !previousActive.isDirty;
            Scene experiment = reuseLoadedExperiment
                ? loadedExperiment
                : EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    replaceDisposableUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            experiment.name = "InputUIComparison";
            SceneManager.SetActiveScene(experiment);

            if (reuseLoadedExperiment)
            {
                foreach (GameObject root in experiment.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            var bootstrap = new GameObject("InputUIComparisonBootstrap");
            InputUIComparisonController controller = bootstrap.AddComponent<InputUIComparisonController>();
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_sportsVehicleSprite").objectReferenceValue = LoadFirstSprite(SportsSpritePath);
            serializedController.FindProperty("_compactVehicleSprite").objectReferenceValue = LoadFirstSprite(CompactSpritePath);
            serializedController.FindProperty("_lightTruckVehicleSprite").objectReferenceValue = LoadFirstSprite(LightTruckSpritePath);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(experiment);
            if (!EditorSceneManager.SaveScene(experiment, ScenePath))
            {
                throw new IOException("Failed to save experiment scene: " + ScenePath);
            }

            if (previousActive.IsValid() && previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(previousActive);
            }
            if (!reuseLoadedExperiment && !replaceDisposableUntitled)
            {
                EditorSceneManager.CloseScene(experiment, true);
            }
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[InputUIComparison] Rebuilt isolated scene: " + ScenePath);
        }

        private static Sprite LoadFirstSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) throw new FileNotFoundException("Vehicle sprite was not found.", path);
            return sprite;
        }

        [MenuItem("Tools/しわ賢者/Input UI Comparison/Open Experiment Scene")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) RebuildScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/しわ賢者/Input UI Comparison/Open Results Folder")]
        public static void OpenResultsFolder()
        {
            string path = Path.Combine(
                Application.persistentDataPath,
                "InputUIComparison",
                "v2",
                InputUIExperimentSchema.CurrentExperimentVersion);
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }
    }
}
