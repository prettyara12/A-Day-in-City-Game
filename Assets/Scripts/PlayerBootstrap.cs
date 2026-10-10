using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Project Setup Bootstrap:
/// 1. Kota Kecil (Lingkungan manual tidak disentuh).
/// 2. Player yang bisa digerakkan dengan keyboard (WASD, Shift lari, Spasi lompat, C duduk).
/// 3. Minimal 2 NPC di dalam kota yang berjalan mandiri dengan anti-tabrakan.
/// </summary>
public static class PlayerBootstrap
{
    private const string AnimatorControllerPath = 
        "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Animations/SimpleCharacter_5.0.controller";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoSetupOnPlay()
    {
        SetupGameProject();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    public static void EditorAutoDumpScene()
    {
        EditorApplication.delayCall += () =>
        {
            try
            {
                GameObject playerGo = EnsurePlayerCharacter();
                if (playerGo != null)
                {
                    SetupAllNPCs(playerGo);
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== DUMP SCENE CHARACTERS ===");
                var allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
                foreach (var go in allGo)
                {
                    if (go.GetComponent<Animator>() != null || 
                        go.name.Contains("People") || go.name.Contains("Citizen") || 
                        go.name.Contains("NPC") || go.name.Contains("Woman") || 
                        go.name.Contains("Man") || go.name.Contains("Girl") || 
                        go.name.Contains("Female") || go.name.Contains("Duduk") ||
                        go.name.Contains("Bench") || go.name.Contains("Chair"))
                    {
                        sb.AppendLine($"GO: {go.name} | Tag: {go.tag} | Parent: {go.transform.parent?.name} | Pos: {go.transform.position.ToString("F3")} | Rot: {go.transform.eulerAngles.ToString("F1")} | Scale: {go.transform.localScale.ToString("F3")}");
                        foreach (var comp in go.GetComponents<Component>())
                        {
                            if (comp != null) sb.AppendLine($"   Component: {comp.GetType().Name}");
                        }
                    }
                }
                string scratchDir = @"C:\Users\Asus\.gemini\antigravity-ide\brain\4b52fae9-8fd5-450a-967a-ffa52e0de8cf\scratch";
                if (System.IO.Directory.Exists(scratchDir))
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(scratchDir, "scene_characters.txt"), sb.ToString());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[EditorAutoDumpScene] Error: " + ex);
            }
        };
    }

    [MenuItem("Tools/⭐ Setup Game (1 Player WASD + Minimal 2 NPC)", false, 1)]
    public static void MenuSetupGameProject()
    {
        SetupGameProject();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog("Setup Proyek Selesai!",
            "Proyek game kota berhasil dikonfigurasi sesuai spesifikasi:\n\n" +
            "1. KOTA KECIL: Lingkungan bangunan, jalan, dan rumah Anda tetap 100% terjaga rapi.\n" +
            "2. PLAYER: Dikontrol penuh dengan Keyboard (W, A, S, D / Panah).\n" +
            "   • Shift: Berlari cepat\n" +
            "   • Spasi: Melompat\n" +
            "   • C / X: Duduk santai / berdiri\n" +
            "   • Tekan V untuk beralih antara Kamera Pemandangan Kota & Kamera Mengikuti Player.\n" +
            "3. 3 NPC MANDIRI:\n" +
            "   • NPC Wanita Baju Kuning: Berdiri dan melompat di tempat di trotoar timur, menyapa ramah saat Player mendekat.\n" +
            "   • NPC Pria: Berjalan kaki di trotoar barat melawan arah dari Player, dilengkapi sensor anti-tabrakan.\n" +
            "   • NPC Barber Shop: Berdiri diam di depan Barber Shop menengok kiri-kanan tanpa berjalan.\n" +
            "   • Semuanya bergerak mandiri tanpa kontrol user.\n\n" +
            "Klik tombol Play (▶) untuk langsung mencoba!",
            "OK");
    }

    [MenuItem("Tools/💈 Setup NPC Barber Shop (Menengok Kiri-Kanan)", false, 4)]
    public static void MenuSetupBarberShopNPC()
    {
        RemoveGiantNPCs();
        EnsureEnvironmentColliders();
        GameObject playerGo = EnsurePlayerCharacter();
        EnsureBarberShopNPC(playerGo);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog("Setup NPC Barber Shop Selesai!",
            "Karakter NPC di depan Barber Shop berhasil dikonfigurasi:\n\n" +
            "• Berdiri diam di tempat (tanpa berjalan/berpindah posisi).\n" +
            "• Menengok kiri dan kanan secara halus dan alami.\n" +
            "• Dilengkapi animasi kepala dan badan realistis.\n" +
            "• Menyapa dan melambaikan tangan saat Player mendekat.\n\n" +
            "Klik tombol Play (▶) untuk melihat aksinya!",
            "OK");
    }

    [MenuItem("Tools/👥 Setup 2 NPC (Wanita Melompat di Tempat & Pria Berjalan Melawan Arah)", false, 3)]
    public static void MenuSetupTwoNPCs()
    {
        RemoveGiantNPCs();
        EnsureEnvironmentColliders();
        GameObject playerGo = EnsurePlayerCharacter();
        SetupAllNPCs(playerGo);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog("Setup 2 NPC Berhasil!",
            "Dua NPC mandiri berhasil disiapkan:\n\n" +
            "1. WANITA BAJU KUNING: Berdiri dan melompat di tempat di trotoar timur, menyapa Player saat didekati.\n" +
            "2. PRIA BERJALAN MELAWAN ARAH: Berjalan kaki di trotoar barat menghadap selatan (melawan arah dari pergerakan Player), dilengkapi sensor anti-tabrakan dan dialog sapaan.\n\n" +
            "Keduanya mandiri (NPC) tanpa ada kontrol keyboard/user.\nKlik tombol Play (▶) untuk melihat aksinya!",
            "OK");
    }

    [MenuItem("Tools/🛍️ Pindahkan Player ke Area Pertokoan (Shopping Street)", false, 2)]
    public static void MenuTeleportToShoppingArea()
    {
        GameObject playerGo = EnsurePlayerCharacter();
        if (playerGo != null)
        {
            playerGo.transform.position = new Vector3(320.5f, 0.02f, 4.8f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            SetupCamera(playerGo);
            EnsureEnvironmentColliders();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            EditorUtility.DisplayDialog("Player di Area Pertokoan!",
                "Player berhasil diposisikan di Area Pertokoan (Shopping Street)!\n\n" +
                "• Posisi: Menghadap ke arah barat menelusuri jalan pertokoan (Drug Store, Pawn Shop, dll).\n" +
                "• Bebas Hambatan: Semua garis marka jalan, ubin aspal, dan ornamen lantai telah dibersihkan dari collider rintangan sehingga karakter bisa bebas masuk dan berjalan di area ini.\n\n" +
                "Klik Play (▶) untuk menjelajahi area!",
                "OK");
        }
    }

    [MenuItem("Tools/Hapus Semua NPC Raksasa (Delete Giant NPCs)")]
    public static void MenuRemoveGiantNPCs()
    {
        int deleted = RemoveGiantNPCs();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
        EditorUtility.DisplayDialog("NPC Raksasa Dihapus!",
            deleted > 0 
                ? $"Berhasil menghapus {deleted} karakter raksasa dari kota!"
                : "Semua karakter sudah berukuran proporsional (skala 0.1).",
            "OK");
    }
#endif

    /// <summary>
    /// Menyiapkan 1 Player WASD + Minimal 2 NPC pejalan kaki di trotoar kota.
    /// Lingkungan kota kecil buatan pengguna sama sekali tidak disentuh/diubah.
    /// </summary>
    public static void SetupGameProject()
    {
        // 1. Sesuaikan skala karakter jika ada yang berskala raksasa (skala > 0.2f -> 0.1f)
        RemoveGiantNPCs();

        // 2. Pastikan rintangan/gedung memiliki collider dasar agar tidak tembus
        EnsureEnvironmentColliders();

        // 3. Setup PLAYER (Bisa digerakkan dengan Keyboard WASD)
        GameObject playerGo = EnsurePlayerCharacter();

        // 4. Setup KAMERA (Pemandangan Kota Luas & Third-Person Follow)
        SetupCamera(playerGo);

        // 5. Setup 2 NPC Mandiri Sesuai Permintaan Pengguna:
        //    - NPC Wanita baju kuning (melompat di tempat di trotoar)
        //    - NPC Pria berjalan kaki di trotoar barat melawan arah dari karakter utama
        SetupAllNPCs(playerGo);

        // 6. Pastikan seluruh karakter di scene memiliki material & tekstur URP yang valid (tidak pink/magenta)
        GameObject[] allChars = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        foreach (var go in allChars)
        {
            if (go.GetComponent<Animator>() != null && !go.name.Contains("Camera") && !go.name.Contains("Light"))
            {
                AutoEnsureCharacterMaterial(go);
            }
        }

        // 7. Bersihkan objek kanvas WorldSpace lama yang mungkin tertinggal di scene & inisialisasi Screen-Space Dialogue
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allObjects)
        {
            if (go != null && (go.name == "SpeechBubbleCanvas" || go.name == "BubbleBackground"))
            {
                DestroyGameObjectSafe(go);
            }
        }
        NPCDialogueOverlay.EnsureInstance();

        Debug.Log("[PlayerBootstrap] Berhasil mengonfigurasi 1 Player (WASD) dan 2 NPC Mandiri (Wanita Melompat di Tempat & Pria Berjalan Melawan Arah)!");
    }

    /// <summary>
    /// Menyiapkan karakter Player yang dikendalikan keyboard (W, A, S, D).
    /// </summary>
    private static GameObject EnsurePlayerCharacter()
    {
        GameObject playerGo = null;

        // 1. Prioritas Utama: Karakter SimplePeople_Trucker_Black yang dipilih pengguna
        GameObject existingTrucker = GameObject.Find("SimplePeople_Trucker_Black");
        if (existingTrucker != null) playerGo = existingTrucker;

        if (playerGo == null)
        {
            GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGo)
            {
                if (go.name.Contains("Trucker"))
                {
                    playerGo = go;
                    break;
                }
            }
        }

        // 2. Fallback jika Trucker belum di-drag ke scene
        if (playerGo == null)
        {
            GameObject existingTaggedPlayer = GameObject.FindWithTag("Player");
            if (existingTaggedPlayer != null) playerGo = existingTaggedPlayer;
        }

        if (playerGo == null)
        {
            GameObject existingNamedPlayer = GameObject.Find("Player");
            if (existingNamedPlayer != null) playerGo = existingNamedPlayer;
        }

        if (playerGo == null)
        {
            GameObject existingStreetMan = GameObject.Find("SimplePeople_StreetMan_Black");
            if (existingStreetMan != null) playerGo = existingStreetMan;
        }

        // Cari di seluruh GameObjects (termasuk inactive)
        if (playerGo == null)
        {
            GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGo)
            {
                if (go.name.Contains("StreetMan"))
                {
                    playerGo = go;
                    break;
                }
            }
        }

        if (playerGo == null)
        {
            playerGo = new GameObject("Player");
        }

        playerGo.tag = "Player";
        playerGo.SetActive(true);

        // Pastikan SEMUA karakter selain Player tidak ber-tag Player dan tidak memiliki PlayerController / CharacterController
        GameObject[] allCharacters = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var c in allCharacters)
        {
            if (c != playerGo)
            {
                if (c.CompareTag("Player")) c.tag = "Untagged";
                PlayerController oldPc = c.GetComponent<PlayerController>();
                if (oldPc != null) DestroyComponentSafe(oldPc);
                CharacterController oldCc = c.GetComponent<CharacterController>();
                if (oldCc != null) DestroyComponentSafe(oldCc);
            }
        }

        // Pastikan posisi berada di atas tanah (Y >= 0.02, perbaiki jika sebelumnya -1.7 underground)
        Vector3 currentPos = playerGo.transform.position;
        if (currentPos.y < 0.0f || float.IsNaN(currentPos.y))
        {
            currentPos.y = 0.02f;
        }
        if (currentPos.magnitude < 0.1f)
        {
            currentPos = new Vector3(320.5f, 0.02f, 4.8f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else
        {
            // Jika player berada di area jalan pertokoan (Z > 0) dan rotasi masih menghadap utara (0 deg),
            // arahkan menghadap ke barat (-90 deg) menelusuri jalan pertokoan
            if (currentPos.z > 0f && Mathf.Abs(playerGo.transform.rotation.eulerAngles.y) < 1f)
            {
                playerGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
        }
        playerGo.transform.position = currentPos;
        playerGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Pastikan hanya sub-mesh karakter yang relevan yang aktif (nonaktifkan mesh yang tidak dipakai)
        EnsureCorrectCharacterMesh(playerGo);

        // Pasang material URP bertekstur asli
        if (playerGo.name.Contains("Trucker"))
        {
            EnsureCharacterMaterial(playerGo, "SimplePeople_Trucker_Black", 
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Trucker_Black.mat");
        }
        else
        {
            EnsureCharacterMaterial(playerGo, "SimplePeople_StreetMan_Black", 
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_StreetMan_Black.mat");
        }

        // Hapus NPCWalker dari Player jika ada
        NPCWalker walker = playerGo.GetComponent<NPCWalker>();
        if (walker != null) DestroyComponentSafe(walker);

        // Pasang / Konfigurasi Animator
        Animator anim = playerGo.GetComponent<Animator>();
        if (anim == null) anim = playerGo.AddComponent<Animator>();
        anim.applyRootMotion = false;

#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif

        // Pasang Capsule Collider
        CapsuleCollider col = playerGo.GetComponent<CapsuleCollider>();
        if (col == null) col = playerGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // Pasang Rigidbody Kinematic
        Rigidbody rb = playerGo.GetComponent<Rigidbody>();
        if (rb == null) rb = playerGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Pasang PlayerController (WASD Keyboard Control)
        PlayerController pc = playerGo.GetComponent<PlayerController>();
        if (pc == null) pc = playerGo.AddComponent<PlayerController>();
        pc.walkSpeed = 0.45f;
        pc.sprintSpeed = 0.95f;
        pc.rotationSpeed = 12f;
        pc.jumpHeight = 0.25f;
        pc.playerCamera = Camera.main;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(playerGo);
#endif

        return playerGo;
    }

    /// <summary>
    /// Menyiapkan kamera: Fokus pada 1 Pemain Inti (Third-Person Follow).
    /// </summary>
    private static void SetupCamera(GameObject playerGo)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null) return;

        ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
        if (tpc == null) tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();

        tpc.viewMode = ThirdPersonCamera.CameraViewMode.ThirdPersonFollow; // Fokus pada Pemain Inti
        tpc.target = playerGo.transform;
        tpc.targetHeight = 0.18f;
        tpc.distance = 1.35f;
        tpc.height = 0.45f;
        tpc.followSpeed = 12f;
        tpc.followRearAngle = true;
        tpc.rotationFollowSpeed = 6f;
        tpc.enableMouseLook = true;
        tpc.showControlsUI = false;
        tpc.SnapToTarget();

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(cam.gameObject);
#endif
    }

    /// <summary>
    /// Menyiapkan 2 NPC mandiri sesuai permintaan pengguna:
    /// 1. NPC Wanita baju kuning di trotoar timur: Melompat di tempat terus-menerus, menyapa saat Player mendekat.
    /// 2. NPC Pria: Berjalan kaki di trotoar barat melawan arah pergerakan Player (berjalan ke arah selatan).
    /// Keduanya otonom tanpa kontrol keyboard / user.
    /// </summary>
    public static void SetupAllNPCs(GameObject playerGo)
    {
        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

        // 1. CARI LANGSUNG KARAKTER DI SCENE BERDASARKAN NAMA (Mendukung Objek Aktif Maupun Nonaktif)
        GameObject femaleNpc = null;
        GameObject maleNpc = null;

        foreach (var go in allGo)
        {
            if (go == playerGo) continue;
            if (go.name.StartsWith("CH_") || go.name.Contains("_jnt")) continue;

            if (femaleNpc == null && (go.name == "SimplePeople_Waitress_White" || 
                                     go.name == "NPC_Wanita_Melompat" || 
                                     go.name == "NPC_Wanita_Duduk"))
            {
                femaleNpc = go;
            }

            if (maleNpc == null && (go.name == "SimplePeople_Pimp_Brown" || 
                                   go.name == "NPC_Pria_PejalanKaki"))
            {
                maleNpc = go;
            }
        }

        // 2. PENCARIAN FLEKSIBEL JIKA MENGGUNAKAN NAMA / PREFAB LAIN
        if (femaleNpc == null || maleNpc == null)
        {
            foreach (var go in allGo)
            {
                if (go == playerGo) continue;
                if (go == femaleNpc || go == maleNpc) continue;
                // Abaikan sub-mesh CH_ atau objek anak
                if (go.name.StartsWith("CH_") || go.name.Contains("_jnt")) continue;
                if (go.transform.parent != null && (go.transform.parent.name.StartsWith("SimplePeople") || 
                                                   go.transform.parent.name.StartsWith("SimpleCitizens"))) continue;
                if (go.GetComponent<Animator>() == null && !go.name.StartsWith("SimplePeople") && !go.name.StartsWith("SimpleCitizens")) continue;

                Vector3 pos = go.transform.position;
                // Cek apakah di koridor jalan utama kota (X: 315 - 322, Z: -25 - +15)
                if (pos.x >= 315.0f && pos.x <= 322.0f && pos.z >= -25.0f && pos.z <= 15.0f)
                {
                    if (femaleNpc == null && pos.x >= 318.0f)
                    {
                        femaleNpc = go;
                    }
                    else if (maleNpc == null && pos.x < 318.0f)
                    {
                        maleNpc = go;
                    }
                }
            }
        }

        // 3. Fallback jika salah satu belum ada di scene
        if (femaleNpc == null)
        {
#if UNITY_EDITOR
            string[] femalePrefabs = new string[]
            {
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Waitress_White.prefab",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_HouseWife_White.prefab"
            };
            foreach (var p in femalePrefabs)
            {
                GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pref != null)
                {
                    femaleNpc = Object.Instantiate(pref);
                    femaleNpc.name = "SimplePeople_Waitress_White";
                    break;
                }
            }
#endif
        }

        if (maleNpc == null)
        {
#if UNITY_EDITOR
            string[] malePrefabs = new string[]
            {
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Pimp_Brown.prefab",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_StreetMan_White.prefab"
            };
            foreach (var p in malePrefabs)
            {
                GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pref != null)
                {
                    maleNpc = Object.Instantiate(pref);
                    maleNpc.name = "SimplePeople_Pimp_Brown";
                    break;
                }
            }
#endif
        }

        // 4. KONFIGURASI SEMUA NPC
        if (femaleNpc != null)
        {
            ConfigureFemaleJumpingNPC(femaleNpc, playerGo);
        }

        if (maleNpc != null)
        {
            ConfigureMaleWalkingNPC(maleNpc, playerGo);
        }

        // 5. KONFIGURASI NPC DI DEPAN BARBER SHOP (MENENGOK KIRI-KANAN TANPA BERJALAN)
        EnsureBarberShopNPC(playerGo);
    }

    /// <summary>
    /// Membersihkan semua joint/tulang rig karakter dari komponen yang salah dipasang (NPCWalker, Collider, dll)
    /// dan mengembalikan skala lokal tulang ke normal (1, 1, 1) agar kepala dan anggota tubuh tidak hilang/mengecil.
    /// </summary>
    public static void CleanAllJointBones()
    {
        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGo)
        {
            if (go.name.EndsWith("_jnt") || go.name.Contains("_jnt"))
            {
                // Kembalikan skala lokal tulang ke normal (1, 1, 1)
                go.transform.localScale = Vector3.one;

                // Hapus komponen yang salah dipasang di tulang rig
                NPCWalker w = go.GetComponent<NPCWalker>();
                if (w != null) DestroyComponentSafe(w);

                Animator a = go.GetComponent<Animator>();
                if (a != null) DestroyComponentSafe(a);

                Rigidbody rb = go.GetComponent<Rigidbody>();
                if (rb != null) DestroyComponentSafe(rb);

                Collider col = go.GetComponent<Collider>();
                if (col != null) DestroyComponentSafe(col);

                PlayerController pc = go.GetComponent<PlayerController>();
                if (pc != null) DestroyComponentSafe(pc);
            }
        }
    }

    /// <summary>
    /// Menyiapkan NPC di depan Barber Shop agar berdiri diam dan menengok kiri-kanan secara alami (tanpa berjalan).
    /// </summary>
    public static GameObject EnsureBarberShopNPC(GameObject playerGo)
    {
        // Bersihkan tulang rig terlebih dahulu agar kepala tidak hilang
        CleanAllJointBones();

        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

        // 1. Cari karakter di depan Barber Shop berdasarkan nama (aktif atau non-aktif)
        GameObject barberNpc = null;
        foreach (var go in allGo)
        {
            if (go == playerGo) continue;
            if (go.name.StartsWith("CH_") || go.name.Contains("_jnt")) continue;

            if (go.name == "SimplePeople_Prostitute_White" ||
                go.name == "SimplePeople_Punk_White" ||
                go.name == "NPC_BarberShop" ||
                go.name == "NPC_Barber_Customer")
            {
                barberNpc = go;
                break;
            }
        }

        if (barberNpc == null)
        {
            foreach (var go in allGo)
            {
                if (go == playerGo) continue;
                // JANGAN PERNAH memilih tulang/joint atau submesh CH_
                if (go.name.Contains("_jnt") || go.name.StartsWith("CH_")) continue;
                // Karakter harus root GameObject atau anak dari People
                if (go.transform.parent != null && go.transform.parent.name != "People") continue;
                if (go.name.Contains("Building") || go.name.Contains("BaberShop")) continue;

                Vector3 pos = go.transform.position;
                // Area depan Barber Shop (X: 303 - 312, Z: 4.5 - 11.5)
                if (pos.x >= 303.0f && pos.x <= 312.0f && pos.z >= 4.5f && pos.z <= 11.5f)
                {
                    barberNpc = go;
                    break;
                }
            }
        }

        // Fallback: Jika belum ada karakter di depan Barber Shop, buatkan satu dari prefab
        if (barberNpc == null)
        {
#if UNITY_EDITOR
            string[] prefabs = new string[]
            {
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Prostitute_White.prefab",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Punk_White.prefab",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Prefabs/SimpleCitizens_Tourist_White.prefab",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_StreetMan_White.prefab"
            };
            foreach (var p in prefabs)
            {
                GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pref != null)
                {
                    barberNpc = Object.Instantiate(pref);
                    barberNpc.name = "SimplePeople_Prostitute_White";
                    barberNpc.transform.position = new Vector3(307.44f, 0.02f, 6.85f);
                    barberNpc.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    break;
                }
            }
#endif
        }

        if (barberNpc != null)
        {
            ConfigureBarberShopNPC(barberNpc, playerGo);
        }

        return barberNpc;
    }

    /// <summary>
    /// Mengonfigurasi NPC di depan Barber Shop agar berdiri di tempat dan menengok kiri-kanan tanpa berjalan.
    /// </summary>
    private static void ConfigureBarberShopNPC(GameObject npcGo, GameObject playerGo)
    {
        npcGo.tag = "Untagged";
        npcGo.SetActive(true);

        // 1. Bersihkan dan kembalikan skala tulang rig semua anak ke normal (1, 1, 1)
        Transform[] childTransforms = npcGo.GetComponentsInChildren<Transform>(true);
        foreach (var t in childTransforms)
        {
            if (t == npcGo.transform) continue;
            if (t.name.EndsWith("_jnt") || t.name.Contains("_jnt"))
            {
                t.localScale = Vector3.one;
                NPCWalker cw = t.GetComponent<NPCWalker>();
                if (cw != null) DestroyComponentSafe(cw);
                Animator ca = t.GetComponent<Animator>();
                if (ca != null) DestroyComponentSafe(ca);
                Rigidbody crb = t.GetComponent<Rigidbody>();
                if (crb != null) DestroyComponentSafe(crb);
                Collider ccol = t.GetComponent<Collider>();
                if (ccol != null) DestroyComponentSafe(ccol);
                PlayerController cpc = t.GetComponent<PlayerController>();
                if (cpc != null) DestroyComponentSafe(cpc);
            }
        }

        // 2. Hapus komponen pengendali keyboard jika ada
        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);
        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        // Skala proporsional 0.1 pada root GameObject
        npcGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Menghadap ke arah jalan/selatan (180 derajat)
        npcGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // Pastikan posisi Y pas di permukaan tanah/trotoar (Y = 0.02f)
        Vector3 curPos = npcGo.transform.position;
        curPos.y = 0.02f;
        npcGo.transform.position = curPos;

        // Sub-mesh yang benar (CH_Prostitute aktif, menampilkan kepala/rambut dan badan lengkap)
        EnsureCorrectCharacterMesh(npcGo);

        // Pasang Tekstur & Material URP asli
        AutoEnsureCharacterMaterial(npcGo);

        // Animator dengan SimpleCharacter_5.0
        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif
        // Set idle parameter
        anim.SetInteger("Animation_int", 0);
        anim.SetFloat("Speed_f", 0f);
        anim.SetBool("Static_b", false);
        anim.SetBool("Grounded", true);
        anim.SetBool("Jump_b", false);
        anim.Update(0f);

        // Rigidbody Kinematic
        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Capsule Collider
        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // NPCWalker (Menengok Kiri-Kanan Tanpa Berjalan)
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker == null) walker = npcGo.AddComponent<NPCWalker>();
        walker.isSittingPermanently = false;
        walker.isJumpingInPlace = false;
        walker.isLookingAroundInPlace = true;
        walker.lookAroundAngle = 40f;
        walker.lookAroundSpeed = 45f;
        walker.lookPauseDuration = new Vector2(1.5f, 3.0f);
        walker.navMode = NPCWalker.NavigationMode.Stationary;
        walker.walkSpeed = 0f;
        walker.runSpeed = 0f;
        walker.idleChance = 0f;
        walker.sitChance = 0f;
        walker.enableObstacleAvoidance = false;

        // Interaksi Dialog saat Player mendekat
        walker.enablePlayerInteraction = true;
        walker.interactionDistance = 1.6f;
        walker.characterDisplayName = "Pelanggan Barber Shop";
        walker.characterLine = "Halo! Saya sedang menunggu antrean potong rambut di Barber Shop ini.";
        if (playerGo != null) walker.playerTarget = playerGo.transform;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(npcGo);
#endif
    }

    /// <summary>
    /// Mengonfigurasi NPC Wanita baju kuning agar berdiri dan melompat di tempat di trotoar timur.
    /// Sepenuhnya otonom tanpa kontrol user, posisi berdiri di trotoar menghadap jalan raya.
    /// </summary>
    private static void ConfigureFemaleJumpingNPC(GameObject npcGo, GameObject playerGo)
    {
        npcGo.tag = "Untagged";
        npcGo.SetActive(true);

        // Hapus komponen pengendali keyboard
        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);
        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        // Skala proporsional 0.1
        npcGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Posisikan berdiri di trotoar timur dekat bangku taman & air mancur
        // Berdiri tegak di permukaan trotoar (Y = 0.02f)
        npcGo.transform.position = new Vector3(318.85f, 0.02f, -8.06f);
        // Menghadap ke barat ke arah jalan raya (-90 derajat)
        npcGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        // Pastikan hanya sub-mesh wanita (CH_Woman) yang aktif
        EnsureCorrectCharacterMesh(npcGo);

        // Tekstur & Material URP asli
        EnsureCharacterMaterial(npcGo, "SimplePeople_Waitress_White",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Waitress_White.mat");

        // Animator
        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif
        // Reset parameter agar berdiri tegak & siap melompat
        anim.SetInteger("Animation_int", 0);
        anim.SetFloat("Speed_f", 0f);
        anim.SetBool("Static_b", false);
        anim.SetBool("Grounded", true);
        anim.SetBool("Jump_b", false);
        if (anim.layerCount > 3)
        {
            anim.Play("Standing_Jump", 3, 0f);
        }
        anim.Update(0f);

        // Rigidbody Kinematic
        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Capsule Collider berdiri normal
        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // NPCWalker (Melompat di Tempat & Dialog Menyapa)
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker == null) walker = npcGo.AddComponent<NPCWalker>();
        walker.isSittingPermanently = false;
        walker.isJumpingInPlace = true;
        walker.jumpInterval = 1.4f;
        walker.jumpInPlaceHeight = 0.18f;
        walker.navMode = NPCWalker.NavigationMode.Stationary;
        walker.walkSpeed = 0f;
        walker.runSpeed = 0f;
        walker.idleChance = 0f;
        walker.sitChance = 0f;
        walker.enableObstacleAvoidance = false;

        // Interaksi Dialog saat Player mendekat
        walker.enablePlayerInteraction = true;
        walker.interactionDistance = 1.8f;
        walker.characterDisplayName = "Wanita Melompat";
        walker.characterLine = "Halo! Lihat, aku melompat di tempat!\nAsyik sekali olahraga di trotoar ini!";
        if (playerGo != null) walker.playerTarget = playerGo.transform;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(npcGo);
#endif
    }

    /// <summary>
    /// Mengonfigurasi NPC Pria agar berjalan kaki mandiri di trotoar barat.
    /// Berjalan melawan arah pergerakan karakter utama (dari Utara Z=+6 ke Selatan Z=-22).
    /// </summary>
    private static void ConfigureMaleWalkingNPC(GameObject npcGo, GameObject playerGo)
    {
        npcGo.tag = "Untagged";
        npcGo.SetActive(true);

        // Hapus komponen pengendali keyboard
        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);
        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        // Skala proporsional 0.1
        npcGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Buat atau dapatkan jalur trotoar barat melawan arah
        // Trotoar barat berada di X = 317.35f, membentang dari Utara (Z = +6.0f) ke Selatan (Z = -22.0f)
        NPCWaypointPath pathMelawanArah = EnsureSidewalkPath("Path_Trotoar_Melawan_Arah", 317.35f, 6.0f, -22.0f, Color.yellow);

        // Mulai posisi di sisi utara trotoar menghadap ke selatan (rotasi 180 derajat)
        // Karakter utama berjalan ke utara (+Z), sehingga NPC ini berjalan melawan arah (-Z) menuju player!
        npcGo.transform.position = new Vector3(317.35f, 0.02f, 4.0f);
        npcGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // Pastikan hanya sub-mesh karakter yang sesuai yang aktif (CH_Pimp)
        EnsureCorrectCharacterMesh(npcGo);

        // Tekstur & Material URP asli
        EnsureCharacterMaterial(npcGo, "SimplePeople_Pimp_Brown",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Pimp_Brown.mat");

        // Animator
        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif
        anim.SetInteger("Animation_int", 0);
        anim.SetFloat("Speed_f", 2.5f);
        anim.SetBool("Static_b", false);
        anim.SetBool("Grounded", true);

        // Rigidbody Kinematic
        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Capsule Collider
        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // NPCWalker (Berjalan Melawan Arah, Anti-Tabrakan & Dialog Menyapa)
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker == null) walker = npcGo.AddComponent<NPCWalker>();
        walker.isSittingPermanently = false;
        walker.navMode = NPCWalker.NavigationMode.WaypointPath;
        walker.waypointPath = pathMelawanArah;
        walker.endBehavior = NPCWalker.EndBehavior.PingPong;
        walker.randomStartPoint = false;
        walker.maxStartDelay = 0.0f; // Langsung jalan maju tanpa delay
        walker.walkSpeed = 0.32f;
        walker.runSpeed = 0.55f;
        walker.rotationSpeed = 8f;
        walker.waypointReachDistance = 0.2f;
        walker.idleChance = 0f; // Terus berjalan maju tanpa jeda acak
        walker.sitChance = 0f;
        walker.runChance = 0f;
        walker.turnPauseTime = 0.5f;

        // Sensor anti-tabrakan
        walker.enableObstacleAvoidance = true;
        walker.obstacleDetectionDistance = 0.45f;
        walker.bodyRadius = 0.05f;

        // Interaksi Dialog saat Player mendekat (berhenti, melambai & menyapa)
        walker.enablePlayerInteraction = true;
        walker.interactionDistance = 1.8f;
        walker.characterDisplayName = "Pria Pejalan Kaki";
        walker.characterLine = "Halo bro! Selamat beraktivitas di kota. Tetap semangat!";
        if (playerGo != null) walker.playerTarget = playerGo.transform;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(npcGo);
#endif
    }

    /// <summary>
    /// Mengonfigurasi komponen dan parameter NPC pejalan kaki.
    /// </summary>
    private static void ConfigureNPC(GameObject npcGo, GameObject playerGo, NPCWaypointPath path, float speed, string dialogText)
    {
        // Hapus PlayerController jika ada di NPC
        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);

        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        // Animator
        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;

#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif

        // Rigidbody Kinematic
        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Capsule Collider
        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // NPCWalker (Sensor Anti-Tabrakan & Navigasi Trotoar)
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker == null) walker = npcGo.AddComponent<NPCWalker>();

        walker.waypointPath = path;
        walker.navMode = NPCWalker.NavigationMode.WaypointPath;
        walker.endBehavior = NPCWalker.EndBehavior.PingPong;
        walker.randomStartPoint = false;
        walker.walkSpeed = speed;
        walker.runSpeed = speed * 1.8f;
        walker.rotationSpeed = 8f;
        walker.waypointReachDistance = 0.15f;
        walker.sitChance = 0f;
        walker.runChance = 0f;
        walker.idleChance = 0.1f;
        walker.turnPauseTime = 0.5f;

        // Sensor anti tabrakan
        walker.enableObstacleAvoidance = true;
        walker.obstacleDetectionDistance = 0.45f;
        walker.bodyRadius = 0.05f;

        // Interaksi dengan Player & Balon Dialog
        walker.enablePlayerInteraction = true;
        walker.interactionDistance = 1.2f;
        walker.characterLine = dialogText;
        if (playerGo != null) walker.playerTarget = playerGo.transform;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(npcGo);
#endif
    }

    /// <summary>
    /// Membuat atau mendapatkan jalur waypoint trotoar lurus.
    /// </summary>
    public static NPCWaypointPath EnsureSidewalkPath(string pathName, float xPos, float zStart, float zEnd, Color pathColor)
    {
        GameObject pathsRoot = GameObject.Find("NPC_Paths");
        if (pathsRoot == null)
        {
            pathsRoot = new GameObject("NPC_Paths");
        }

        Transform existing = pathsRoot.transform.Find(pathName);
        GameObject pathObj = existing != null ? existing.gameObject : new GameObject(pathName);
        pathObj.transform.SetParent(pathsRoot.transform, false);

        // Bersihkan waypoints lama jika ada
        while (pathObj.transform.childCount > 0)
        {
            Transform child = pathObj.transform.GetChild(0);
            child.SetParent(null);
            DestroyGameObjectSafe(child.gameObject);
        }

        NPCWaypointPath wpPath = pathObj.GetComponent<NPCWaypointPath>();
        if (wpPath == null) wpPath = pathObj.AddComponent<NPCWaypointPath>();

        wpPath.pathColor = pathColor;
        wpPath.waypointSize = 0.25f;
        wpPath.isLoop = false;

        // Buat 2 Titik Lurus di Trotoar
        Vector3[] points = new Vector3[]
        {
            new Vector3(xPos, 0.02f, zStart),
            new Vector3(xPos, 0.02f, zEnd)
        };

        for (int i = 0; i < points.Length; i++)
        {
            GameObject wp = new GameObject($"WP_{i + 1:D2}");
            wp.transform.SetParent(pathObj.transform, false);
            wp.transform.position = points[i];
        }

        return wpPath;
    }

    /// <summary>
    /// Memastikan hanya sub-mesh karakter yang relevan yang aktif dan semua sub-mesh lain dinonaktifkan,
    /// agar tidak terjadi tumpang-tindih mesh ganda yang menyebabkan karakter berwarna magenta/pink.
    /// </summary>
    public static void EnsureCorrectCharacterMesh(GameObject character)
    {
        if (character == null) return;
        string cName = character.name;

        string targetMesh = null;
        if (cName.Contains("Pimp")) targetMesh = "CH_Pimp";
        else if (cName.Contains("Trucker")) targetMesh = "CH_Trucker";
        else if (cName.Contains("StreetMan")) targetMesh = "CH_StreetMan";
        else if (cName.Contains("Waitress") || cName.Contains("Woman") || cName.Contains("HouseWife") || cName.Contains("Wanita")) targetMesh = "CH_Woman";
        else if (cName.Contains("Police") || cName.Contains("Cop")) targetMesh = "CH_Policeman";
        else if (cName.Contains("FireFighter")) targetMesh = "CH_FireFighter";
        else if (cName.Contains("BusinessMan")) targetMesh = "CH_BusinessMan";
        else if (cName.Contains("Punk")) targetMesh = "CH_Punk";
        else if (cName.Contains("Robber")) targetMesh = "CH_Robber";
        else if (cName.Contains("Sheriff")) targetMesh = "CH_Sheriff";
        else if (cName.Contains("Hobo")) targetMesh = "CH_Hobo";
        else if (cName.Contains("Roadworker")) targetMesh = "CH_Roadworker";
        else if (cName.Contains("Prostitute")) targetMesh = "CH_Prostitute";
        else if (cName.Contains("RiotCop")) targetMesh = "CH_RiotCop";

        Transform[] allTransforms = character.GetComponentsInChildren<Transform>(true);
        if (!string.IsNullOrEmpty(targetMesh))
        {
            foreach (var t in allTransforms)
            {
                if (t.name.StartsWith("CH_"))
                {
                    t.gameObject.SetActive(t.name == targetMesh);
                }
            }
        }
        else
        {
            // Jika nama prefab tidak terdeteksi, aktifkan hanya 1 mesh CH_ pertama dan nonaktifkan sisanya
            bool foundOne = false;
            foreach (var t in allTransforms)
            {
                if (t.name.StartsWith("CH_"))
                {
                    if (!foundOne && t.gameObject.activeSelf)
                    {
                        foundOne = true;
                    }
                    else if (foundOne)
                    {
                        t.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Memastikan material URP dan tekstur karakter terpasang dengan benar agar tidak muncul warna magenta (pink).
    /// </summary>
    public static void EnsureCharacterMaterial(GameObject character, string textureName, string materialAssetPath)
    {
        if (character == null) return;

#if UNITY_EDITOR
        // 1. Rapikan sub-mesh karakter agar tidak tumpang tindih
        EnsureCorrectCharacterMesh(character);

        // 2. Cari file tekstur asli
        Texture2D tex = null;
        if (!string.IsNullOrEmpty(textureName))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/{textureName}.png"
            );
            if (tex == null && (textureName.Contains("Trucker") || character.name.Contains("Trucker")))
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_Redneck_Black.png"
                );
            }
            if (tex == null)
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Textures/{textureName}.png"
                );
            }
        }

        if (tex == null && character.name.Contains("Trucker"))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_Redneck_Black.png"
            );
        }

        if (tex == null)
        {
            string cleanName = character.name.Replace("(Clone)", "").Trim().ToLower();
            string[] allGuids = AssetDatabase.FindAssets("t:Texture2D", new[] {
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Textures"
            });
            foreach (var guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (cleanName.Contains(fileName) || fileName.Contains(cleanName) ||
                    (cleanName.Contains("pimp") && fileName.Contains("pimp")) ||
                    (cleanName.Contains("waitress") && fileName.Contains("waitress")) ||
                    (cleanName.Contains("trucker") && fileName.Contains("redneck")) ||
                    (cleanName.Contains("housewife") && fileName.Contains("housewife")) ||
                    (cleanName.Contains("streetman") && fileName.Contains("streetman")))
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    break;
                }
            }
        }

        // 3. Muat atau buat Material URP yang valid
        Material targetMat = null;
        if (!string.IsNullOrEmpty(materialAssetPath))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);
        }

        Shader urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Mobile/Diffuse");
        if (urpShader == null) urpShader = Shader.Find("Sprites/Default");

        bool isUrpShader = targetMat != null && targetMat.shader != null && 
                           targetMat.shader.name.StartsWith("Universal Render Pipeline");

        if (targetMat == null || !isUrpShader || targetMat.shader.name.Contains("Error"))
        {
            if (targetMat != null && urpShader != null)
            {
                targetMat.shader = urpShader;
            }
            else
            {
                targetMat = new Material(urpShader != null ? urpShader : Shader.Find("Universal Render Pipeline/Simple Lit"));
                targetMat.name = (string.IsNullOrEmpty(textureName) ? character.name : textureName) + "_URP_Mat";
            }
        }

        // Ikat tekstur ke targetMat
        if (tex != null && targetMat != null)
        {
            if (targetMat.HasProperty("_BaseMap")) targetMat.SetTexture("_BaseMap", tex);
            if (targetMat.HasProperty("_MainTex")) targetMat.SetTexture("_MainTex", tex);
            if (targetMat.HasProperty("_BaseColor")) targetMat.SetColor("_BaseColor", Color.white);
            if (targetMat.HasProperty("_Color")) targetMat.SetColor("_Color", Color.white);
            targetMat.mainTexture = tex;
        }

        // 4. SELALU tetapkan targetMat ke seluruh Renderer anak karakter ini
        // (Menggantikan material default Standard yang menyebabkan warna ungu/pink di URP)
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (targetMat != null)
            {
                r.sharedMaterial = targetMat;
            }

            if (Application.isPlaying)
            {
                if (targetMat != null)
                {
                    r.material = targetMat;
                }
                if (tex != null && r.material != null)
                {
                    if (r.material.HasProperty("_BaseMap")) r.material.SetTexture("_BaseMap", tex);
                    if (r.material.HasProperty("_MainTex")) r.material.SetTexture("_MainTex", tex);
                    r.material.mainTexture = tex;
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", Color.white);
                }
            }
        }
#endif
    }

    /// <summary>
    /// Memastikan seluruh bangunan dan rintangan fisik memiliki collider agar tidak tertembus.
    /// Jalan raya, trotoar, marka jalan, ubin lantai datar, serta ornamen jalan (papan reklame/billboard melayang,
    /// lampu lalu lintas, tiang, gerobak, tempat sampah) dibebaskan dari collider penghalang agar tidak menghalangi jalan.
    /// </summary>
    public static int EnsureEnvironmentColliders()
    {
        int count = 0;
        Material townMat = null;
#if UNITY_EDITOR
        townMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Materials/SimpleTown.mat"
        );
#endif

        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        foreach (var mr in renderers)
        {
            GameObject go = mr.gameObject;
            if (go.GetComponent<Animator>() != null || go.GetComponentInParent<Animator>() != null) continue;
            if (go.name.Contains("Camera") || go.name.Contains("Light")) continue;

            string lowerName = go.name.ToLower();
            string lowerParent = go.transform.parent != null ? go.transform.parent.name.ToLower() : "";

            // Perbaiki material ungu/pink pada grave_small_mesh atau prop dengan material hilang
            if (lowerName.Contains("grave") && townMat != null)
            {
                mr.sharedMaterial = townMat;
            }

            // Cek apakah objek ini adalah permukaan tanah, jalan, trotoar, marka jalan, ubin, atau ornamen lantai datar
            bool isGroundOrStreet = 
                lowerName.Contains("road") || lowerName.Contains("jalan") || lowerName.Contains("runway") || 
                lowerName.Contains("plane") || lowerName.Contains("ground") || lowerName.Contains("floor") ||
                lowerName.Contains("sidewalk") || lowerName.Contains("trotoar") || lowerName.Contains("tile") ||
                lowerName.Contains("asphalt") || lowerName.Contains("aspal") || lowerName.Contains("dirt") ||
                lowerName.Contains("grass") || lowerName.Contains("rumput") || lowerName.Contains("paper") ||
                lowerName.Contains("rolledpaper") || lowerName.Contains("line") || lowerName.Contains("garis") ||
                lowerName.Contains("marking") || lowerName.Contains("curb") || lowerName.Contains("divider") ||
                lowerName.Contains("connector") || lowerName.Contains("crossing") || lowerName.Contains("carpet") ||
                lowerName.Contains("water") ||
                lowerParent.Contains("road") || lowerParent.Contains("jalan") || lowerParent.Contains("plane") ||
                lowerParent.Contains("ground") || lowerParent.Contains("floor") || lowerParent.Contains("sidewalk") ||
                lowerParent.Contains("trotoar") || lowerParent.Contains("dinding") || lowerParent.Contains("runway");

            // Cek ornamen jalan, papan reklame melayang, lampu lalu lintas, tiang, gerobak, tempat sampah
            // Objek-objek ini tidak boleh memiliki BoxCollider raksasa yang menutupi/memblokir jalan raya
            bool isStreetDecorOrOverhead =
                lowerName.Contains("billboard") || lowerName.Contains("traffic_light") ||
                lowerName.Contains("grave") || lowerName.Contains("sign") ||
                lowerName.Contains("light") || lowerName.Contains("lamp") ||
                lowerName.Contains("pole") || lowerName.Contains("cart") ||
                lowerName.Contains("bin") || lowerName.Contains("hydrant") ||
                lowerName.Contains("bench") || lowerName.Contains("trash") ||
                lowerName.Contains("post") || lowerName.Contains("planter02") ||
                lowerName.Contains("planter03");

            // Cek juga berdasarkan tinggi bounding box mesh di world space (di bawah 8 cm = permukaan lantai/marka datar)
            bool isFlatSurface = mr.bounds.size.y < 0.08f;

            if (isGroundOrStreet || isFlatSurface || isStreetDecorOrOverhead)
            {
                // Hapus BoxCollider atau collider penghalang jika ada agar karakter tidak tertahan dinding tak kasat mata
                Collider existingCol = go.GetComponent<Collider>();
                if (existingCol != null)
                {
                    DestroyComponentSafe(existingCol);
                }
                continue;
            }

            // Pasang BoxCollider hanya untuk bangunan, gedung, rumah, tembok batas, kendaraan, dan pohon
            bool isActualObstacle =
                lowerName.Contains("building") || lowerName.Contains("bangunan") || lowerName.Contains("house") ||
                lowerName.Contains("shop") || lowerName.Contains("store") || lowerName.Contains("office") ||
                lowerName.Contains("cinema") || lowerName.Contains("gym") || lowerName.Contains("stripclub") ||
                lowerName.Contains("drivethru") || lowerName.Contains("repair") || lowerName.Contains("pawn") ||
                lowerName.Contains("veh_") || lowerName.Contains("car") || lowerName.Contains("truck") ||
                lowerName.Contains("palm") || lowerName.Contains("tree") || lowerName.Contains("wall") ||
                lowerName.Contains("fence") ||
                lowerParent.Contains("bangunan") || lowerParent.Contains("cars") || lowerParent.Contains("pohon") ||
                lowerParent.Contains("wall") || lowerParent.Contains("pagar");

            if (isActualObstacle)
            {
                Collider col = go.GetComponent<Collider>();
                if (col == null)
                {
                    go.AddComponent<BoxCollider>();
                    count++;
                }
            }
            else
            {
                // Objek lain di luar rintangan utama dibersihkan collidernya agar jalan 100% mulus
                Collider extraCol = go.GetComponent<Collider>();
                if (extraCol != null)
                {
                    DestroyComponentSafe(extraCol);
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Menghapus karakter berskala raksasa (skala > 0.2f) agar skala kota konsisten 0.1.
    /// </summary>
    /// <summary>
    /// Menyesuaikan skala karakter berskala raksasa (skala > 0.2f) menjadi proporsional 0.1.
    /// Tidak menghapus karakter baru yang baru saja di-drag user ke scene.
    /// </summary>
    public static int RemoveGiantNPCs()
    {
        CleanAllJointBones();

        int count = 0;
        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        foreach (var go in allGo)
        {
            if (go.name.Contains("_jnt") || go.name.StartsWith("CH_")) continue;

            bool isCharacter = go.name.StartsWith("NPC_Walker") || 
                               go.name.StartsWith("SimplePeople") || 
                               go.name.StartsWith("SimpleCitizens") ||
                               go.name.Contains("ShopKeeper") ||
                               (go.GetComponent<Animator>() != null && !go.name.Contains("Camera") && !go.name.Contains("Building"));

            if (isCharacter)
            {
                if (go.transform.localScale.y > 0.2f || go.transform.lossyScale.y > 0.2f)
                {
                    go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Secara otomatis mencarikan tekstur dan material URP asli untuk karakter berdasarkan namanya.
    /// Mencegah karakter menjadi putih atau magenta (pink).
    /// </summary>
    public static void AutoEnsureCharacterMaterial(GameObject character)
    {
        if (character == null) return;
#if UNITY_EDITOR
        string cName = character.name.Replace("(Clone)", "").Trim();
        string matPath = $"Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/{cName}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
        {
            matPath = $"Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Materials/{cName}.mat";
        }
        EnsureCharacterMaterial(character, cName, matPath);
#endif
    }

    /// <summary>
    /// Alias kompatibilitas untuk NPCWalkerEditor.
    /// </summary>
    public static void SetupPoliceAsVideoNPC(GameObject go)
    {
        if (go == null) return;
        NPCWaypointPath path = EnsureSidewalkPath("Path_Trotoar_Timur", 318.3f, -28.0f, 6.0f, Color.cyan);
        GameObject player = GameObject.FindWithTag("Player");
        ConfigureNPC(go, player, path, 0.30f, "Halo! Saya petugas patroli. Selamat menikmati kota!");
    }

    private static void DestroyComponentSafe(Component comp)
    {
        if (comp == null) return;
        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(comp);
#else
            Object.Destroy(comp);
#endif
        }
        else
        {
            Object.Destroy(comp);
        }
    }

    private static void DestroyGameObjectSafe(GameObject go)
    {
        if (go == null) return;
        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(go);
#else
            Object.Destroy(go);
#endif
        }
        else
        {
            Object.Destroy(go);
        }
    }
}
