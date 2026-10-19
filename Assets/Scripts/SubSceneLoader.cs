using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SubSceneLoader (Modul 6 - Managing Multiple Scenes)
/// Bertanggung jawab atas dynamic additive streaming sub-scene 3x3 (grid 1.5 x 1.5).
/// Memuat kotak baru di hadapan player dan melepas kotak di belakang saat player menjelajah.
/// </summary>
public class SubSceneLoader : MonoBehaviour
{
    [Tooltip("Drag your player object here.")]
    public Transform player;

    [Tooltip("Drag all of your Scriptable Objects for the sub-levels here.")]
    public SubSceneData[] subScenes;

    [Tooltip("What is the name of your primary scene? (Leave empty to use active scene)")]
    public string baseSceneName = "";

    [Tooltip("How many sub-scenes do you have in the Z direction? (Columns)")]
    public int numberOfColumns = 3;

    [Tooltip("How many sub-scenes do you have in the X direction? (Rows)")]
    public int numberOfRows = 3;

    [Tooltip("The rate in seconds that we will check what scenes need to be loaded/unloaded.")]
    public float tickTime = 0.1f;

    [Tooltip("How many sub-scenes should be loaded next to each side of our current one.")]
    public int loadRadius = 1;

    [Header("Runtime Status")]
    [SerializeField] private SubSceneData currentSubScene;

    public SubSceneData CurrentSubScene => currentSubScene;

    private readonly HashSet<string> scenesLoading = new HashSet<string>();
    private readonly HashSet<string> scenesUnloading = new HashSet<string>();

    void Awake()
    {
        if (string.IsNullOrEmpty(baseSceneName))
        {
            baseSceneName = SceneManager.GetActiveScene().name;
        }

        EnsureConfigured();
    }

    void Start()
    {
        EnsureConfigured();

        // Cek posisi awal player
        SubSceneData initialScene = GetCurrentSubScene();
        if (initialScene != null)
        {
            currentSubScene = initialScene;
            Debug.Log($"[SubSceneLoader] Posisi awal player di sub-scene: {initialScene.sceneName} (Grid {initialScene.gridLocation.x}, {initialScene.gridLocation.y})");
            UpdateSubScenes(initialScene.gridLocation);
        }

        CancelInvoke(nameof(CheckPosition));
        InvokeRepeating(nameof(CheckPosition), 0.1f, tickTime);
    }

    public void EnsureConfigured()
    {
        EnsurePlayerReference();

        numberOfColumns = 3;
        numberOfRows = 3;
        loadRadius = 1;

        if (subScenes == null || subScenes.Length < 9)
        {
            var loadedList = new List<SubSceneData>();

            // 1. Coba dari Resources/SubScenes
            SubSceneData[] fromRes = Resources.LoadAll<SubSceneData>("SubScenes");
            if (fromRes != null && fromRes.Length > 0)
            {
                loadedList.AddRange(fromRes);
            }

#if UNITY_EDITOR
            // 2. Fallback Editor: AssetDatabase
            if (loadedList.Count < 9)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SubSceneData", new[] { "Assets/ScriptableObjects/SubScenes", "Assets/Resources/SubScenes" });
                if (guids != null)
                {
                    foreach (var g in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                        SubSceneData data = UnityEditor.AssetDatabase.LoadAssetAtPath<SubSceneData>(path);
                        if (data != null && !loadedList.Exists(x => x.sceneName == data.sceneName))
                        {
                            loadedList.Add(data);
                        }
                    }
                }
            }
#endif

            loadedList.Sort((a, b) => string.Compare(a.sceneName, b.sceneName, System.StringComparison.Ordinal));
            if (loadedList.Count > 0)
            {
                subScenes = loadedList.ToArray();
            }
        }
    }

    private void EnsurePlayerReference()
    {
        if (player != null) return;

        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;
            return;
        }

        var go = GameObject.FindWithTag("Player");
        if (go != null)
        {
            player = go.transform;
            return;
        }

        var trucker = GameObject.Find("SimplePeople_Trucker_Black");
        if (trucker != null)
        {
            player = trucker.transform;
            return;
        }
    }

    public void ForceUpdatePosition()
    {
        CheckPosition();
    }

    void CheckPosition()
    {
        EnsurePlayerReference();
        if (player == null || subScenes == null || subScenes.Length == 0) return;

        SubSceneData target = GetCurrentSubScene();

        if (target == null)
        {
            // Player berada di luar area grid multi-scene (di kota)
            if (currentSubScene != null)
            {
                currentSubScene = null;
                UnloadAllSubScenes();
            }
            return;
        }

        if (target != currentSubScene)
        {
            Debug.Log($"[SubSceneLoader] Player berpindah ke {target.sceneName} (Grid {target.gridLocation.x}, {target.gridLocation.y})");
            currentSubScene = target;
            UpdateSubScenes(target.gridLocation);
        }
    }

    public SubSceneData GetCurrentSubScene()
    {
        EnsurePlayerReference();
        if (player == null || subScenes == null || subScenes.Length == 0) return null;

        Vector3 pos = player.position;

        // 1. Cek batas kotak sub-scene (ukuran 1.5 x 1.5)
        foreach (var s in subScenes)
        {
            if (s == null) continue;
            float halfSize = s.sceneSize / 2f;
            if (pos.x >= s.worldLocation.x - halfSize &&
                pos.x <= s.worldLocation.x + halfSize &&
                pos.z >= s.worldLocation.z - halfSize &&
                pos.z <= s.worldLocation.z + halfSize)
            {
                return s;
            }
        }

        // 2. Toleransi kedekatan: Jika berada di dekat area grid multi-scene (radius 6m dari pusat grid (1.5, 0, 1.5))
        Vector3 gridCenter = new Vector3(1.5f, 0f, 1.5f);
        if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(gridCenter.x, gridCenter.z)) <= 6.0f)
        {
            SubSceneData closest = null;
            float minDist = float.MaxValue;
            foreach (var s in subScenes)
            {
                if (s == null) continue;
                float d = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(s.worldLocation.x, s.worldLocation.z));
                if (d < minDist)
                {
                    minDist = d;
                    closest = s;
                }
            }
            return closest;
        }

        // Di luar grid (di kota)
        return null;
    }

    public void UpdateSubScenes(Vector2 newGrid)
    {
        if (newGrid.x < 0 || newGrid.x >= numberOfRows) return;
        if (newGrid.y < 0 || newGrid.y >= numberOfColumns) return;

        foreach (SubSceneData scene in subScenes)
        {
            if (scene == null) continue;

            if (IsWithinLoadRadius(scene, newGrid))
            {
                // Kotak baru yang berada dalam radius jangkauan dimuat (muncul)
                if (!IsSceneLoaded(scene.sceneName) && !scenesLoading.Contains(scene.sceneName))
                {
                    StartCoroutine(LoadSceneRoutine(scene.sceneName));
                }
            }
            else
            {
                // Kotak lama yang sudah di luar radius jangkauan dilepas (hilang)
                if (IsSceneLoaded(scene.sceneName) && !scenesUnloading.Contains(scene.sceneName))
                {
                    StartCoroutine(UnloadSceneRoutine(scene.sceneName));
                }
            }
        }
    }

    public void UnloadAllSubScenes()
    {
        if (subScenes == null) return;
        foreach (var s in subScenes)
        {
            if (s != null && IsSceneLoaded(s.sceneName) && !scenesUnloading.Contains(s.sceneName))
            {
                StartCoroutine(UnloadSceneRoutine(s.sceneName));
            }
        }
    }

    bool IsWithinLoadRadius(SubSceneData scene, Vector2 center)
    {
        return scene.gridLocation.x >= center.x - loadRadius
            && scene.gridLocation.x <= center.x + loadRadius
            && scene.gridLocation.y >= center.y - loadRadius
            && scene.gridLocation.y <= center.y + loadRadius;
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (scenesLoading.Contains(sceneName) || IsSceneLoaded(sceneName)) yield break;
        scenesLoading.Add(sceneName);

        Debug.Log($"[SubSceneLoader] + Loading SubScene '{sceneName}' (Kotak Baru Muncul)");
        AsyncOperation op = null;
        try
        {
            op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SubSceneLoader] Catatan saat LoadSceneAsync '{sceneName}': {ex.Message}");
        }

        if (op != null)
        {
            yield return op;
        }

        scenesLoading.Remove(sceneName);
        EnsureBaseSceneActive();
    }

    IEnumerator UnloadSceneRoutine(string sceneName)
    {
        if (scenesUnloading.Contains(sceneName) || !IsSceneLoaded(sceneName)) yield break;
        scenesUnloading.Add(sceneName);

        Debug.Log($"[SubSceneLoader] - Unloading SubScene '{sceneName}' (Kotak Lama Terlepas)");
        AsyncOperation op = null;
        int buildIndex = GetSceneBuildIndex(sceneName);
        if (buildIndex >= 0)
        {
            op = SceneManager.UnloadSceneAsync(buildIndex, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        }
        else
        {
            op = SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        }

        if (op != null)
        {
            yield return op;
        }

        scenesUnloading.Remove(sceneName);
        EnsureBaseSceneActive();
    }

    private void EnsureBaseSceneActive()
    {
        if (!string.IsNullOrEmpty(baseSceneName))
        {
            Scene baseScene = SceneManager.GetSceneByName(baseSceneName);
            if (baseScene.IsValid() && baseScene.isLoaded)
                SceneManager.SetActiveScene(baseScene);
        }
    }

    bool IsSceneLoaded(string sceneName)
    {
        Scene s = SceneManager.GetSceneByName(sceneName);
        return s.IsValid() && s.isLoaded;
    }

    int GetSceneBuildIndex(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
                return i;
        }
        return -1;
    }
}

