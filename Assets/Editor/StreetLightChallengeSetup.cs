#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Lighting
{
    /// <summary>
    /// Implements City Lighting Setup:
    /// - All lamp_mesh objects (3 objects): Spot Light with Range=15, Inner/Outer Angle=10/80, Mode=Baked, Intensity=100, Hard Shadows
    /// - All ParkLight_Single objects (36 objects): Point Light with Range=10, Mode=Baked, Intensity=10
    /// - Performance-focused baked lightmap settings
    /// </summary>
    [InitializeOnLoad]
    public class StreetLightChallengeSetup : EditorWindow
    {
        private const string SpotLightName = "Spot Light";
        private const string PointLightName = "Point Light";
        private static readonly Color SoftYellowWhite = new Color(1.0f, 0.957f, 0.839f, 1.0f); // #FFF4D6

        static StreetLightChallengeSetup()
        {
            EditorApplication.delayCall += RestoreAllLights;
        }

        private static void RestoreAllLights()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene == null || string.IsNullOrEmpty(activeScene.path) || !activeScene.path.Contains("Module 2"))
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Module 2.unity");
            }

            // 1. Setup Spot Lights on all lamp_mesh objects (Range 2, Intensity 1)
            ExecuteLampMeshSpotLights(saveScene: false);

            // 2. Setup Point Lights on all ParkLight_Single objects (Range 2, Intensity 1)
            ExecuteParkLightPointLights(saveScene: false);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[LightingChallenge] Configured Spot Lights on {CountLampMeshSpotLights()} lamp_mesh and Point Lights on {CountParkLightPointLights()} ParkLight_Single.");
        }

        [MenuItem("Tools/Lighting Challenge/Open Control Panel", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<StreetLightChallengeSetup>("Lighting Challenge");
            window.minSize = new Vector2(480, 560);
            window.Show();
        }

        [MenuItem("Tools/Lighting Challenge/1. Setup lamp_mesh Spot Lights (Range 2, Intensity 1)", priority = 1)]
        public static void MenuSetupLampMeshSpotLights()
        {
            ExecuteLampMeshSpotLights(saveScene: true);
        }

        [MenuItem("Tools/Lighting Challenge/2. Setup ParkLight Point Lights (Range 2, Intensity 1)", priority = 2)]
        public static void MenuSetupParkLightPointLights()
        {
            ExecuteParkLightPointLights(saveScene: true);
        }

        [MenuItem("Tools/Lighting Challenge/3. Setup All Lights (Range 2, Intensity 1)", priority = 3)]
        public static void MenuSetupAllLights()
        {
            ExecuteLampMeshSpotLights(saveScene: false);
            ExecuteParkLightPointLights(saveScene: true);
        }

        [MenuItem("Tools/Lighting Challenge/4. Configure Performance Lighting & Bake", priority = 4)]
        public static void MenuBakeLightmap()
        {
            ConfigurePerformanceLightingAndBake();
        }

        [MenuItem("Tools/Lighting Challenge/Clear All Challenge Lights", priority = 20)]
        public static void MenuClearAllLights()
        {
            ClearAllChallengeLights();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Unity City Lighting Control Panel", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Lighting Configuration:\n" +
                "1. lamp_mesh -> Spot Light (Arm Tip / Tiang Ujung, Range: 2, Angle: 15/60, Mode: Baked, Intensity: 1, Hard Shadows)\n" +
                "2. ParkLight_Single -> Point Light (Range: 2, Mode: Baked, Intensity: 1)\n" +
                "3. Performance-focused Baked Lightmap",
                MessageType.Info);

            GUILayout.Space(10);

            int lampMeshCount = CountLampMeshObjects();
            int currentSpotLightCount = CountLampMeshSpotLights();
            int parkLightCount = CountParkLightObjects();
            int currentPointLightCount = CountParkLightPointLights();

            EditorGUILayout.LabelField($"lamp_mesh Objects: {lampMeshCount} (Active Spot Lights: {currentSpotLightCount})");
            EditorGUILayout.LabelField($"ParkLight_Single Objects: {parkLightCount} (Active Point Lights: {currentPointLightCount})");

            GUILayout.Space(15);

            GUI.backgroundColor = new Color(0.35f, 0.75f, 0.35f);
            if (GUILayout.Button("1. Setup lamp_mesh Spot Lights (Range 2, Intensity 1)", GUILayout.Height(34)))
            {
                ExecuteLampMeshSpotLights(saveScene: true);
            }

            GUILayout.Space(5);
            GUI.backgroundColor = new Color(0.95f, 0.75f, 0.25f);
            if (GUILayout.Button("2. Setup ParkLight Point Lights (Range 2, Intensity 1)", GUILayout.Height(34)))
            {
                ExecuteParkLightPointLights(saveScene: true);
            }

            GUILayout.Space(5);
            GUI.backgroundColor = new Color(0.35f, 0.85f, 0.85f);
            if (GUILayout.Button("Setup All Lights (Range 2, Intensity 1)", GUILayout.Height(34)))
            {
                ExecuteLampMeshSpotLights(saveScene: false);
                ExecuteParkLightPointLights(saveScene: true);
            }

            GUILayout.Space(5);
            GUI.backgroundColor = new Color(0.35f, 0.65f, 0.95f);
            if (GUILayout.Button("Configure Performance & Bake Lightmap", GUILayout.Height(34)))
            {
                ConfigurePerformanceLightingAndBake();
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Space(15);

            if (GUILayout.Button("Clear All Challenge Lights", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Clear Lights", 
                    "Are you sure you want to remove Spot Lights and Point Lights from street and park lights?", "Yes", "Cancel"))
                {
                    ClearAllChallengeLights();
                }
            }

            GUILayout.Space(15);
            if (Lightmapping.isRunning)
            {
                EditorGUILayout.HelpBox("Lightmapping bake is currently running in background...", MessageType.Warning);
                Repaint();
            }
        }

        #region Search Helpers
        public static bool IsLampMesh(GameObject go)
        {
            if (go == null) return false;
            if (go.name == SpotLightName || go.name == PointLightName) return false;
            string name = go.name.ToLowerInvariant();
            return name.StartsWith("lamp_mesh");
        }

        public static bool IsParkLight(GameObject go)
        {
            if (go == null) return false;
            if (go.name == SpotLightName || go.name == PointLightName) return false;

            string name = go.name.ToLowerInvariant();
            if (name.Contains("runway") || name.Contains("traffic")) return false;
            if (name.StartsWith("lamp_mesh")) return false;

            return name.StartsWith("lamp") || 
                   name.StartsWith("parklight") || 
                   name.Contains("streetlight") || 
                   name.Contains("street_light") || 
                   name.Contains("street light");
        }

        public static List<GameObject> FindLampMeshObjects()
        {
            var results = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                CheckLampMeshRecursively(root.transform, results);
            }

            return results;
        }

        private static void CheckLampMeshRecursively(Transform current, List<GameObject> results)
        {
            if (IsLampMesh(current.gameObject))
            {
                results.Add(current.gameObject);
            }

            for (int i = 0; i < current.childCount; i++)
            {
                CheckLampMeshRecursively(current.GetChild(i), results);
            }
        }

        public static List<GameObject> FindParkLightObjects()
        {
            var results = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                CheckParkLightRecursively(root.transform, results);
            }

            return results;
        }

        private static void CheckParkLightRecursively(Transform current, List<GameObject> results)
        {
            if (IsParkLight(current.gameObject))
            {
                results.Add(current.gameObject);
            }

            for (int i = 0; i < current.childCount; i++)
            {
                CheckParkLightRecursively(current.GetChild(i), results);
            }
        }

        public static int CountLampMeshObjects() => FindLampMeshObjects().Count;
        public static int CountParkLightObjects() => FindParkLightObjects().Count;

        public static int CountLampMeshSpotLights()
        {
            int count = 0;
            var list = FindLampMeshObjects();
            foreach (var go in list)
            {
                Transform child = go.transform.Find(SpotLightName);
                if (child != null && child.GetComponent<Light>() != null) count++;
            }
            return count;
        }

        public static int CountParkLightPointLights()
        {
            int count = 0;
            var list = FindParkLightObjects();
            foreach (var go in list)
            {
                Transform child = go.transform.Find(PointLightName);
                if (child != null && child.GetComponent<Light>() != null) count++;
            }
            return count;
        }
        #endregion

        #region Light Setup Executions
        public static void ExecuteLampMeshSpotLights(bool saveScene = true)
        {
            var lampMeshes = FindLampMeshObjects();
            if (lampMeshes.Count == 0)
            {
                Debug.LogWarning("[LightingChallenge] No lamp_mesh objects found in the active scene!");
                return;
            }

            Undo.SetCurrentGroupName("Setup lamp_mesh Spot Lights");
            int undoGroup = Undo.GetCurrentGroup();
            int count = 0;

            foreach (var lamp in lampMeshes)
            {
                // Remove Point Light if present
                Transform oldPoint = lamp.transform.Find(PointLightName);
                if (oldPoint != null)
                {
                    Undo.DestroyObjectImmediate(oldPoint.gameObject);
                }

                Transform existingChild = lamp.transform.Find(SpotLightName);
                GameObject spotLightObj;

                if (existingChild != null)
                {
                    spotLightObj = existingChild.gameObject;
                }
                else
                {
                    spotLightObj = new GameObject(SpotLightName);
                    Undo.RegisterCreatedObjectUndo(spotLightObj, "Create Spot Light");
                }

                spotLightObj.transform.SetParent(lamp.transform, false);
                // Position light directly under the fixture head at the tip of the pole arm facing down to the ground
                spotLightObj.transform.localPosition = new Vector3(0f, 7.75f, -1.43f);
                spotLightObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                Light light = spotLightObj.GetComponent<Light>();
                if (light == null)
                {
                    light = spotLightObj.AddComponent<Light>();
                }

                Undo.RecordObject(light, "Configure Spot Light");
                light.type = LightType.Spot;
                light.range = 2f;
                light.innerSpotAngle = 15f;
                light.spotAngle = 60f;
                light.lightmapBakeType = LightmapBakeType.Baked;
                light.intensity = 1f;
                light.shadows = LightShadows.Hard;
                light.color = SoftYellowWhite;

                count++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[LightingChallenge] Configured {count} Spot Lights on lamp_mesh (Range=2, Intensity=1, Hard Shadows).");

            if (saveScene)
            {
                Scene activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"[LightingChallenge] Scene '{activeScene.name}' saved with Spot Lights on lamp_mesh.");
            }
        }

        public static void ExecuteParkLightPointLights(bool saveScene = true)
        {
            var parkLights = FindParkLightObjects();
            if (parkLights.Count == 0)
            {
                Debug.LogWarning("[LightingChallenge] No ParkLight objects found in the active scene!");
                return;
            }

            Undo.SetCurrentGroupName("Setup ParkLight Point Lights");
            int undoGroup = Undo.GetCurrentGroup();
            int count = 0;

            foreach (var parkLight in parkLights)
            {
                // Remove Spot Light if present
                Transform oldSpot = parkLight.transform.Find(SpotLightName);
                if (oldSpot != null)
                {
                    Undo.DestroyObjectImmediate(oldSpot.gameObject);
                }

                Transform existingChild = parkLight.transform.Find(PointLightName);
                GameObject pointLightObj;

                if (existingChild != null)
                {
                    pointLightObj = existingChild.gameObject;
                }
                else
                {
                    pointLightObj = new GameObject(PointLightName);
                    Undo.RegisterCreatedObjectUndo(pointLightObj, "Create Point Light");
                }

                Renderer r = parkLight.GetComponentInChildren<Renderer>();
                Vector3 worldPos;
                if (r != null)
                {
                    Bounds b = r.bounds;
                    float height = b.size.y;
                    float yOffset = Mathf.Clamp(height * 0.08f, 0.05f, 0.35f);
                    worldPos = new Vector3(b.center.x, b.max.y - yOffset, b.center.z);
                }
                else
                {
                    worldPos = parkLight.transform.position + Vector3.up * 3.5f;
                }

                pointLightObj.transform.position = worldPos;
                pointLightObj.transform.rotation = Quaternion.identity;
                pointLightObj.transform.SetParent(parkLight.transform, true);

                Light light = pointLightObj.GetComponent<Light>();
                if (light == null)
                {
                    light = pointLightObj.AddComponent<Light>();
                }

                Undo.RecordObject(light, "Configure Point Light");
                light.type = LightType.Point;
                light.range = 2f;
                light.intensity = 1f;
                light.lightmapBakeType = LightmapBakeType.Baked;
                light.color = SoftYellowWhite;

                count++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[LightingChallenge] Configured {count} Point Lights on ParkLight_Single (Range=2, Intensity=1).");

            if (saveScene)
            {
                Scene activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"[LightingChallenge] Scene '{activeScene.name}' saved with Point Lights on ParkLight_Single.");
            }
        }

        public static void ClearAllChallengeLights()
        {
            int deleted = 0;
            var allLights = new List<GameObject>();
            allLights.AddRange(FindLampMeshObjects());
            allLights.AddRange(FindParkLightObjects());

            foreach (var go in allLights)
            {
                Transform spotChild = go.transform.Find(SpotLightName);
                if (spotChild != null)
                {
                    Undo.DestroyObjectImmediate(spotChild.gameObject);
                    deleted++;
                }

                Transform pointChild = go.transform.Find(PointLightName);
                if (pointChild != null)
                {
                    Undo.DestroyObjectImmediate(pointChild.gameObject);
                    deleted++;
                }
            }

            Scene activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"[LightingChallenge] Removed {deleted} challenge lights from street and park lights.");
        }
        #endregion

        #region Performance Lighting Settings
        public static void ConfigurePerformanceLightingAndBake()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            MarkEnvironmentStaticForGI();
            ConfigureLightingSettingsAsset(activeScene);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[LightingChallenge] Performance Lighting settings applied. Initiating Lightmap Bake...");
            Lightmapping.BakeAsync();
        }

        private static void MarkEnvironmentStaticForGI()
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            int count = 0;
            foreach (var r in renderers)
            {
                GameObject go = r.gameObject;
                string name = go.name.ToLowerInvariant();

                if (name.Contains("player") || name.Contains("car") || name.Contains("npc") || 
                    name.Contains("camera") || name.Contains("canvas") || name.Contains("ui"))
                {
                    continue;
                }

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                if ((flags & StaticEditorFlags.ContributeGI) == 0)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.ContributeGI);
                    count++;
                }
            }
            Debug.Log($"[LightingChallenge] Marked {count} environment objects with ContributeGI for lightmap baking.");
        }

        private static void ConfigureLightingSettingsAsset(Scene activeScene)
        {
            try
            {
                LightingSettings ls;
                if (!Lightmapping.TryGetLightingSettings(out ls) || ls == null)
                {
                    ls = new LightingSettings();
                    ls.name = "Performance_LightingSettings";
                    Lightmapping.lightingSettings = ls;
                }

                Undo.RecordObject(ls, "Set Performance LightingSettings");

                try
                {
                    ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
                }
                catch
                {
                    ls.lightmapper = LightingSettings.Lightmapper.UnityComputeGPU;
                }

                ls.bakedGI = true;
                ls.realtimeGI = false;
                ls.directSampleCount = 32;
                ls.indirectSampleCount = 64;
                ls.environmentSampleCount = 64;
                ls.maxBounces = 2;
                ls.lightmapResolution = 15f;
                ls.lightmapMaxSize = 1024;
                ls.lightmapPadding = 2;
                ls.lightmapCompression = LightmapCompression.HighQuality;
                ls.mixedBakeMode = MixedLightingMode.Subtractive;

                Lightmapping.lightingSettings = ls;
                EditorUtility.SetDirty(ls);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LightingChallenge] Note on LightingSettings: {ex.Message}");
            }
        }
        #endregion
    }
}
#endif
