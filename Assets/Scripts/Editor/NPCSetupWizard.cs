using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor script to automatically set up NPC walkers and align camera in the scene.
/// Accessible from the Unity top menu: Tools > ...
/// </summary>
public class NPCSetupWizard : EditorWindow
{
    /// <summary>
    /// Align Main Camera to the current Scene View angle (like Ctrl+Shift+F).
    /// </summary>
    [MenuItem("Tools/1. Samakan Kamera Game dengan Scene View (Align Camera)")]
    public static void AlignCameraToSceneView()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            EditorUtility.DisplayDialog("Error", "Main Camera tidak ditemukan di Hierarchy!", "OK");
            return;
        }

        if (SceneView.lastActiveSceneView == null)
        {
            EditorUtility.DisplayDialog("Info", "Buka dan arahkan jendela Scene View terlebih dahulu.", "OK");
            return;
        }

        Undo.RecordObject(cam.transform, "Align Camera to Scene View");
        cam.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
        cam.transform.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;

        EditorUtility.SetDirty(cam);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        Debug.Log($"[NPCSetup] Main Camera berhasil disamakan dengan Scene View! Posisi: {cam.transform.position}, Rotasi: {cam.transform.eulerAngles}");

        EditorUtility.DisplayDialog("Berhasil!",
            "Kamera Game (Main Camera) sekarang sudah sama persis dengan sudut pandang Scene View!\n\n" +
            "Ketika Anda menekan tombol Play (▶), tampilannya akan persis dari jarak dekat seperti di Scene View.",
            "OK");
    }

    /// <summary>
    /// Spawn NPCs with scale 0.1 and create walking paths along roads/sidewalks.
    /// </summary>
    [MenuItem("Tools/2. Buat Semua NPC Berjalan di Jalanan (Skala 0.1)")]
    public static void SetupNPCWalkers()
    {
        // Find or create People parent
        GameObject peopleParent = GameObject.Find("People");
        if (peopleParent == null)
        {
            peopleParent = new GameObject("People");
            Undo.RegisterCreatedObjectUndo(peopleParent, "Create People Parent");
        }

        // Find or create NPC_Paths parent
        GameObject pathsParent = GameObject.Find("NPC_Paths");
        if (pathsParent == null)
        {
            pathsParent = new GameObject("NPC_Paths");
            Undo.RegisterCreatedObjectUndo(pathsParent, "Create NPC_Paths Parent");
        }

        // Determine road center coordinates (Module 2 has roads around X=316.4, Y=0.05, Z=-45 to 15)
        GameObject jalanRaya = GameObject.Find("Jalan Raya") ?? GameObject.Find("Jalan Raya (1)") ?? GameObject.Find("Jalan Raya (2)");
        Vector3 centerPos;
        bool isModule2 = (jalanRaya != null);

        if (isModule2)
        {
            centerPos = new Vector3(316.4f, 0.05f, -15f);
        }
        else if (peopleParent.transform.position != Vector3.zero)
        {
            centerPos = peopleParent.transform.position;
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            centerPos = SceneView.lastActiveSceneView.pivot;
            centerPos.y = 0.05f;
        }
        else
        {
            centerPos = Vector3.zero;
        }

        // ============================================
        // Create Waypoint Paths along sidewalks
        // ============================================
        Vector3[] p1, p2, p3, p4;

        if (isModule2)
        {
            // Jalan Raya Utama (X = 316.4)
            // Trotoar Kiri (West)
            p1 = new Vector3[]
            {
                new Vector3(314.5f, 0.05f, -42f),
                new Vector3(314.5f, 0.05f, -25f),
                new Vector3(314.5f, 0.05f, -10f),
                new Vector3(314.5f, 0.05f, 5f),
                new Vector3(314.5f, 0.05f, 15f)
            };

            // Trotoar Kanan (East)
            p2 = new Vector3[]
            {
                new Vector3(318.3f, 0.05f, 15f),
                new Vector3(318.3f, 0.05f, 5f),
                new Vector3(318.3f, 0.05f, -10f),
                new Vector3(318.3f, 0.05f, -25f),
                new Vector3(318.3f, 0.05f, -42f)
            };

            // Jalan Silang Atas (Z = 18.5)
            p3 = new Vector3[]
            {
                new Vector3(306f, 0.05f, 18.5f),
                new Vector3(314.5f, 0.05f, 18.5f),
                new Vector3(325f, 0.05f, 18.5f),
                new Vector3(345f, 0.05f, 18.5f)
            };

            // Jalan Silang Bawah (Z = 15.5)
            p4 = new Vector3[]
            {
                new Vector3(345f, 0.05f, 15.5f),
                new Vector3(325f, 0.05f, 15.5f),
                new Vector3(318.3f, 0.05f, 15.5f),
                new Vector3(306f, 0.05f, 15.5f)
            };
        }
        else
        {
            p1 = new Vector3[]
            {
                centerPos + new Vector3(-3f, 0f, -20f),
                centerPos + new Vector3(-3f, 0f, -10f),
                centerPos + new Vector3(-3f, 0f, 0f),
                centerPos + new Vector3(-3f, 0f, 10f),
                centerPos + new Vector3(-3f, 0f, 20f)
            };
            p2 = new Vector3[]
            {
                centerPos + new Vector3(3f, 0f, 20f),
                centerPos + new Vector3(3f, 0f, 10f),
                centerPos + new Vector3(3f, 0f, 0f),
                centerPos + new Vector3(3f, 0f, -10f),
                centerPos + new Vector3(3f, 0f, -20f)
            };
            p3 = new Vector3[]
            {
                centerPos + new Vector3(-20f, 0f, -3f),
                centerPos + new Vector3(-10f, 0f, -3f),
                centerPos + new Vector3(0f, 0f, -3f),
                centerPos + new Vector3(10f, 0f, -3f),
                centerPos + new Vector3(20f, 0f, -3f)
            };
            p4 = new Vector3[]
            {
                centerPos + new Vector3(20f, 0f, 3f),
                centerPos + new Vector3(10f, 0f, 3f),
                centerPos + new Vector3(0f, 0f, 3f),
                centerPos + new Vector3(-10f, 0f, 3f),
                centerPos + new Vector3(-20f, 0f, 3f)
            };
        }

        GameObject path1 = CreateWaypointPath("SidewalkPath_01", pathsParent.transform, p1);
        GameObject path2 = CreateWaypointPath("SidewalkPath_02", pathsParent.transform, p2);
        GameObject path3 = CreateWaypointPath("SidewalkPath_03", pathsParent.transform, p3);
        GameObject path4 = CreateWaypointPath("SidewalkPath_04", pathsParent.transform, p4);

        // Find available character prefabs
        string[] prefabSearchPaths = new string[]
        {
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Prefabs"
        };

        List<GameObject> availablePrefabs = new List<GameObject>();
        foreach (string searchPath in prefabSearchPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    availablePrefabs.Add(prefab);
                }
            }
        }

        if (availablePrefabs.Count == 0)
        {
            Debug.LogError("[NPCSetup] Karakter prefab tidak ditemukan!");
            return;
        }

        // Find the animation controller
        string controllerPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Animations/SimpleCharacter_5.0.controller";
        RuntimeAnimatorController animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);

        // ============================================
        // Spawn 8 NPCs (Scale 0.1)
        // ============================================
        GameObject[] paths = { path1, path2, path3, path4 };
        string[] npcNames = { "NPC_Walker_01", "NPC_Walker_02", "NPC_Walker_03", "NPC_Walker_04",
                              "NPC_Walker_05", "NPC_Walker_06", "NPC_Walker_07", "NPC_Walker_08" };

        // Configuration: path index, walk speed, run chance, idle chance, sit chance
        float[][] npcConfigs = new float[][]
        {
            new float[] { 0, 0.25f, 0.15f, 0.3f, 0.1f },
            new float[] { 0, 0.22f, 0.1f, 0.4f, 0.15f },
            new float[] { 1, 0.26f, 0.2f, 0.25f, 0.05f },
            new float[] { 1, 0.24f, 0.1f, 0.35f, 0.1f },
            new float[] { 2, 0.28f, 0.25f, 0.2f, 0.1f },
            new float[] { 2, 0.20f, 0.05f, 0.5f, 0.2f },
            new float[] { 3, 0.26f, 0.3f, 0.15f, 0.05f },
            new float[] { 3, 0.23f, 0.1f, 0.3f, 0.1f }
        };

        for (int i = 0; i < npcNames.Length; i++)
        {
            GameObject existingNPC = GameObject.Find(npcNames[i]);
            if (existingNPC != null)
            {
                DestroyImmediate(existingNPC);
            }

            int prefabIndex = Random.Range(0, availablePrefabs.Count);
            GameObject prefab = availablePrefabs[prefabIndex];

            int pathIndex = (int)npcConfigs[i][0];
            NPCWaypointPath waypointPath = paths[pathIndex].GetComponent<NPCWaypointPath>();
            Vector3 startPos = waypointPath.GetWaypoint(0).position;

            GameObject npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            npc.name = npcNames[i];
            npc.transform.position = startPos;
            npc.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // Skala kecil 0.1
            npc.transform.SetParent(peopleParent.transform);
            Undo.RegisterCreatedObjectUndo(npc, "Create NPC " + npcNames[i]);

            // Setup Animator
            Animator animator = npc.GetComponent<Animator>();
            if (animator == null)
            {
                animator = npc.AddComponent<Animator>();
            }
            if (animController != null)
            {
                animator.runtimeAnimatorController = animController;
            }

            // Setup NPCWalker
            NPCWalker walker = npc.GetComponent<NPCWalker>();
            if (walker == null)
            {
                walker = npc.AddComponent<NPCWalker>();
            }

            walker.waypointPath = waypointPath;
            walker.walkSpeed = npcConfigs[i][1];
            walker.runSpeed = walker.walkSpeed * 2.5f;
            walker.waypointReachDistance = 0.12f;
            walker.runChance = npcConfigs[i][2];
            walker.idleChance = npcConfigs[i][3];
            walker.sitChance = npcConfigs[i][4];
            walker.randomStartPoint = true;
            walker.maxStartDelay = Random.Range(0f, 3f);
            walker.endBehavior = NPCWalker.EndBehavior.PingPong;

            Debug.Log($"[NPCSetup] Dibuat: {npcNames[i]} ({prefab.name}) di {paths[pathIndex].name}, Skala: 0.1");
        }

        // Align camera with Scene View if user has Scene View open
        if (SceneView.lastActiveSceneView != null && Camera.main != null)
        {
            Undo.RecordObject(Camera.main.transform, "Align Camera With Scene View");
            Camera.main.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
            Camera.main.transform.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        Debug.Log("=== NPC Setup Selesai! 8 NPC dibuat dengan skala 0.1 di sepanjang jalan. ===");

        EditorUtility.DisplayDialog("NPC Setup Selesai!",
            "8 NPC (Skala 0.1) telah dibuat di sepanjang jalan & trotoar kota!\n\n" +
            "• Karakter sudah diperkecil menjadi 0.1 sesuai skala kota.\n" +
            "• Kecepatan jalan sudah disesuaikan agar natural.\n" +
            "• Main Camera otomatis disamakan dengan sudut pandang Scene View Anda.\n\n" +
            "Tekan tombol Play (▶) untuk melihat NPC berjalan!",
            "OK");
    }

    /// <summary>
    /// Set up a character already placed in the scene (such as the Doctor).
    /// </summary>
    [MenuItem("Tools/3. Aktifkan NPC yang Dipilih (Make Selected NPC Walk)")]
    public static void SetupSelectedNPC()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Pilih Karakter",
                "Silakan klik salah satu karakter NPC di Hierarchy terlebih dahulu (misal: Doctor atau karakter yang ada di jalan).",
                "OK");
            return;
        }

        Undo.RecordObject(selected.transform, "Setup Selected NPC");
        selected.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // Set skala 0.1

        // Setup Animator
        Animator animator = selected.GetComponent<Animator>();
        if (animator == null)
        {
            animator = selected.AddComponent<Animator>();
        }

        string controllerPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Animations/SimpleCharacter_5.0.controller";
        RuntimeAnimatorController animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (animController != null)
        {
            animator.runtimeAnimatorController = animController;
        }

        // Setup NPCWalker
        NPCWalker walker = selected.GetComponent<NPCWalker>();
        if (walker == null)
        {
            walker = selected.AddComponent<NPCWalker>();
        }

        // Find closest waypoint path
        NPCWaypointPath[] allPaths = Object.FindObjectsByType<NPCWaypointPath>(FindObjectsInactive.Exclude);
        if (allPaths.Length > 0)
        {
            NPCWaypointPath closest = allPaths[0];
            float closestDist = float.MaxValue;
            foreach (var p in allPaths)
            {
                if (p.GetWaypoints().Count > 0)
                {
                    float dist = Vector3.Distance(selected.transform.position, p.GetWaypoint(0).position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = p;
                    }
                }
            }
            walker.waypointPath = closest;
        }

        walker.walkSpeed = 0.25f;
        walker.runSpeed = 0.65f;
        walker.waypointReachDistance = 0.12f;
        walker.endBehavior = NPCWalker.EndBehavior.PingPong;

        EditorUtility.SetDirty(selected);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog("Karakter Berhasil Diaktifkan!",
            $"Karakter '{selected.name}' sekarang sudah:\n\n" +
            "• Diperkecil menjadi skala 0.1\n" +
            "• Dipasangi animasi gerak (SimpleCharacter_5.0)\n" +
            "• Dipasangi script NPCWalker untuk berjalan!\n\n" +
            "Tekan Play (▶) untuk mencobanya.",
            "OK");
    }

    /// <summary>
    /// Spawns a playable character with PlayerController, CharacterController, and ThirdPersonCamera.
    /// </summary>
    [MenuItem("Tools/4. Buat Karakter Pemain & Kamera (Spawn Player & Camera)")]
    public static void SetupPlayerAndCamera()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Matikan Play Mode",
                "Silakan matikan Play Mode terlebih dahulu (klik tombol Play) sebelum membuat Player agar perubahannya tersimpan!",
                "OK");
            return;
        }

        // 1. Determine spawn position
        Vector3 spawnPos = new Vector3(316.4f, 0.05f, -15f);
        if (Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            Vector3 camFwd = Camera.main.transform.forward;
            camFwd.y = 0f;
            camFwd.Normalize();
            spawnPos = camPos + camFwd * 1.5f;
            spawnPos.y = 0.05f;
        }

        // 2. Find or create Player GameObject
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            // Pick a character prefab
            string[] searchPaths = new string[]
            {
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Prefabs"
            };

            GameObject playerPrefab = null;
            foreach (string p in searchPaths)
            {
                string[] guids = AssetDatabase.FindAssets("SimplePeople_StreetMan_White t:Prefab", new[] { p });
                if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Prefab", new[] { p });
                if (guids.Length > 0)
                {
                    playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    if (playerPrefab != null) break;
                }
            }

            if (playerPrefab != null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            }
            else
            {
                player = new GameObject("Player");
            }

            player.name = "Player";
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
        }

        player.tag = "Player";
        player.transform.position = spawnPos;
        player.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // 3. Configure Animator
        string controllerPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Animations/SimpleCharacter_5.0.controller";
        RuntimeAnimatorController animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);

        Animator animator = player.GetComponent<Animator>();
        if (animator == null)
        {
            animator = player.AddComponent<Animator>();
        }
        if (animController != null)
        {
            animator.runtimeAnimatorController = animController;
        }

        // 4. Configure CharacterController
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = player.AddComponent<CharacterController>();
        }
        cc.height = 1.8f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.radius = 0.35f;
        cc.stepOffset = 0.3f;
        cc.skinWidth = 0.01f;
        cc.minMoveDistance = 0.001f;

        // 5. Configure PlayerController
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null)
        {
            pc = player.AddComponent<PlayerController>();
        }
        pc.walkSpeed = 0.35f;
        pc.sprintSpeed = 0.75f;
        pc.jumpHeight = 0.25f;
        pc.playerCamera = Camera.main;

        // 6. Setup ThirdPersonCamera on Main Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            ThirdPersonCamera tpc = mainCam.GetComponent<ThirdPersonCamera>();
            if (tpc == null)
            {
                tpc = mainCam.gameObject.AddComponent<ThirdPersonCamera>();
            }
            tpc.target = player.transform;
            tpc.targetHeight = 0.18f;
            tpc.distance = 1.2f;
            tpc.height = 0.45f;
            tpc.followSpeed = 12f;
            tpc.SnapToTarget();
            EditorUtility.SetDirty(mainCam);
        }

        EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        Selection.activeGameObject = player;

        EditorUtility.DisplayDialog("Karakter Pemain Siap!",
            "Karakter Pemain (Player) berhasil dibuat dan dipasangi kamera Third-Person!\n\n" +
            "CARA BERMAIN:\n" +
            "• W, A, S, D / Tombol Panah: Berjalan menjelajah kota\n" +
            "• Tahan Left Shift: Berlari cepat (Sprint)\n" +
            "• Spasi: Melompat (Jump)\n" +
            "• Gerakkan Mouse: Melihat sekeliling kota\n\n" +
            "Sekarang silakan klik tombol Play (▶) untuk langsung memainkannya!",
            "OK");
    }

    private static GameObject CreateWaypointPath(string name, Transform parent, Vector3[] positions)
    {
        // Recreate path if already exists to ensure fresh points
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        GameObject pathObj = new GameObject(name);
        pathObj.transform.SetParent(parent);
        pathObj.transform.position = Vector3.zero;

        NPCWaypointPath waypointPath = pathObj.AddComponent<NPCWaypointPath>();
        waypointPath.isLoop = false;

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject wp = new GameObject($"WP_{i + 1:D2}");
            wp.transform.SetParent(pathObj.transform);
            wp.transform.position = positions[i];
        }

        Undo.RegisterCreatedObjectUndo(pathObj, "Create " + name);
        return pathObj;
    }
}
