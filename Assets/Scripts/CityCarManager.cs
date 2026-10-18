using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Mengelola dan mengaktifkan 2 mobil yang telah ditambahkan pengguna secara manual di scene.
/// Secara otomatis membersihkan objek mobil asing buatan AI dan menjamin TIDAK PERNAH
/// men-spawn mobil baru entah dari mana.
/// </summary>
public static class CityCarManager
{
#if UNITY_EDITOR
    [MenuItem("Tools/🚗 Aktifkan 2 Mobil Manual Anda Agar Berjalan", false, 1)]
    public static void MenuSetupUserCars()
    {
        int count = SetupUserManualCars();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        if (count >= 2)
        {
            EditorUtility.DisplayDialog("2 Mobil Anda Berhasil Diaktifkan!",
                $"Berhasil menemukan {count} mobil buatan Anda di scene!\n\n" +
                "Komponen CityCarMover telah dipasang langsung pada mobil milik Anda.\n" +
                "Semua mobil asing yang sebelumnya dibuat otomatis telah dibersihkan.\n\n" +
                "Klik tombol Play (▶) untuk melihat kedua mobil Anda berjalan mengitari jalan kota!",
                "OK");
        }
        else if (count == 1)
        {
            EditorUtility.DisplayDialog("1 Mobil Ditemukan & Diaktifkan!",
                "Berhasil menemukan 1 mobil buatan Anda di jalan kota dan mengaktifkannya.\n\n" +
                "Jika Anda sudah menambahkan 2 mobil, pastikan mobil kedua berada di area jalan kota atau memiliki kata 'car/ute/truck/sedan' agar otomatis terdeteksi.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Mobil Belum Terdeteksi",
                "Tidak menemukan mobil manual di area jalan kota.\n" +
                "Pastikan Anda telah menarik 2 prefab mobil ke jalan kota (antara X: 310 s/d 335, Z: -20 s/d 10).",
                "OK");
        }
    }
#endif

    public static int SetupUserManualCars()
    {
        // 1. HAPUS SEMUA WADAH MOBIL ASING BUATAN SCRIPT SEBELUMNYA
        CleanupAISpawnedCars();

        // 2. DETEKSI MOBIL MANUAL YANG DITARUH PENGGUNA DI SCENE
        List<GameObject> userCars = DetectUserManualCars();

        int configuredCount = 0;
        foreach (var car in userCars)
        {
            if (car == null) continue;

            // Bersihkan Animator Controller asing jika sempat terpasang sebelumnya
            Animator anim = car.GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null && 
                anim.runtimeAnimatorController.name.Contains("CarLoop"))
            {
                anim.runtimeAnimatorController = null;
            }

            // Pasang komponen CityCarMover langsung pada mobil milik pengguna
            CityCarMover mover = car.GetComponent<CityCarMover>();
            if (mover == null)
            {
                mover = car.AddComponent<CityCarMover>();
            }

            // Cek apakah mobil ini adalah mobil merah yang diminta belok ke jalan pertokoan
            string carNameLower = car.name.ToLower();
            bool isRedCar = carNameLower.Contains("red") || (configuredCount == 1 && !carNameLower.Contains("truck"));

            mover.waypoints = isRedCar ? CityCarMover.RedCarWaypoints : CityCarMover.MainRoadWaypoints;

            // Atur kecepatan normal perkotaan yang santai dan natural (2.0 - 2.2 m/detik)
            mover.speed = isRedCar ? 2.0f : 2.1f;
            mover.turnSpeed = 220.0f;
            mover.waypointThreshold = 0.4f;
            mover.InitializePosition();

            configuredCount++;
            Debug.Log($"[CityCarManager] Berhasil mengaktifkan mobil milik Anda: '{car.name}' di posisi {car.transform.position.ToString("F2")}");

            if (configuredCount >= 2) break;
        }

        return configuredCount;
    }

    /// <summary>
    /// Menghapus GameObject 'Animated_Vehicles' dan mobil yang sebelumnya di-spawn oleh AI,
    /// serta membersihkan komponen CityCarMover yang tidak sengaja menempel pada gerobak/props kota.
    /// </summary>
    public static void CleanupAISpawnedCars()
    {
        string[] spawnedNames = new string[] { "Animated_Vehicles", "Car_BlueTruck", "Car_RedSedan" };
        foreach (var name in spawnedNames)
        {
            GameObject obj = GameObject.Find(name);
            while (obj != null)
            {
                DestroyObjectSafe(obj);
                obj = GameObject.Find(name);
            }
        }

        // Bersihkan komponen CityCarMover jika menempel pada gerobak makanan atau objek non-kendaraan
        CityCarMover[] allMovers = Object.FindObjectsByType<CityCarMover>(FindObjectsInactive.Include);
        foreach (var mover in allMovers)
        {
            if (mover == null) continue;
            if (!IsValidVehicle(mover.gameObject))
            {
                Debug.Log($"[CityCarManager] Membersihkan CityCarMover dari objek non-kendaraan: '{mover.gameObject.name}'");
                DestroyComponentSafe(mover);
            }
        }
    }

    /// <summary>
    /// Mendeteksi 2 mobil milik pengguna yang diletakkan di jalan kota.
    /// Memastikan objek non-kendaraan seperti gerobak makanan (Cart_HotDog, Cart_IceCream, Cart_Donut)
    /// sama sekali tidak pernah dianggap sebagai mobil.
    /// </summary>
    public static List<GameObject> DetectUserManualCars()
    {
        List<GameObject> foundCars = new List<GameObject>();
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

        // Prioritas 1: Objek yang sudah memiliki CityCarMover DAN benar-benar kendaraan valid
        foreach (var go in allObjects)
        {
            if (go == null) continue;
            CityCarMover mover = go.GetComponent<CityCarMover>();
            if (mover != null)
            {
                if (IsValidVehicle(go))
                {
                    if (!foundCars.Contains(go)) foundCars.Add(go);
                }
                else
                {
                    DestroyComponentSafe(mover);
                }
            }
        }

        if (foundCars.Count >= 2) return foundCars;

        // Prioritas 2: Cari objek kendaraan valid yang diletakkan di scene kota
        List<GameObject> candidates = new List<GameObject>();
        foreach (var go in allObjects)
        {
            if (go == null) continue;
            if (foundCars.Contains(go)) continue;

            // Abaikan folder 'Cars' (parkiran koleksi 95 mobil) pada tahap prioritas pertama
            if (go.transform.parent != null && go.transform.parent.name == "Cars") continue;
            if (go.name == "Cars") continue;

            // Abaikan objek AI buatan script lama
            if (go.name == "Animated_Vehicles" || go.name == "Car_BlueTruck" || go.name == "Car_RedSedan") continue;

            if (IsValidVehicle(go))
            {
                // Ambil root kendaraan jika child mesh yang terdeteksi
                GameObject root = go;
                while (root.transform.parent != null && 
                       root.transform.parent.name != "Cars" &&
                       root.transform.parent.name != "Environment" &&
                       root.transform.parent.name != "Jalan Raya" &&
                       IsValidVehicle(root.transform.parent.gameObject))
                {
                    root = root.transform.parent.gameObject;
                }

                if (!candidates.Contains(root) && !foundCars.Contains(root))
                {
                    candidates.Add(root);
                }
            }
        }

        // Urutkan kandidat berdasarkan letak terdekat ke jalan raya kota (X: 317.75)
        Vector3 roadCenter = new Vector3(317.75f, 0f, 0f);
        candidates.Sort((a, b) =>
        {
            // Mobil yang sudah berada di jalan raya kota (X: 316.5 s/d 319.0, Z: -15 s/d 16) diprioritaskan nomor satu
            bool aOnRoad = a.transform.position.x >= 316.5f && a.transform.position.x <= 319.0f &&
                           a.transform.position.z >= -15.0f && a.transform.position.z <= 16.0f;
            bool bOnRoad = b.transform.position.x >= 316.5f && b.transform.position.x <= 319.0f &&
                           b.transform.position.z >= -15.0f && b.transform.position.z <= 16.0f;

            if (aOnRoad && !bOnRoad) return -1;
            if (!aOnRoad && bOnRoad) return 1;

            float distA = Vector3.Distance(a.transform.position, roadCenter);
            float distB = Vector3.Distance(b.transform.position, roadCenter);
            return distA.CompareTo(distB);
        });

        foreach (var c in candidates)
        {
            if (foundCars.Count >= 2) break;
            foundCars.Add(c);
        }

        // Prioritas 3 (Fallback): Jika pengguna belum menarik mobil ke luar folder Cars,
        // ambil kendaraan dari folder Cars yang paling dekat dengan jalan raya
        if (foundCars.Count < 2)
        {
            List<GameObject> fallbackCandidates = new List<GameObject>();
            foreach (var go in allObjects)
            {
                if (go == null || foundCars.Contains(go)) continue;
                if (go.transform.parent != null && go.transform.parent.name == "Cars" && IsValidVehicle(go))
                {
                    fallbackCandidates.Add(go);
                }
            }

            fallbackCandidates.Sort((a, b) =>
                Vector3.Distance(a.transform.position, roadCenter).CompareTo(Vector3.Distance(b.transform.position, roadCenter))
            );

            foreach (var fb in fallbackCandidates)
            {
                if (foundCars.Count >= 2) break;
                foundCars.Add(fb);
            }
        }

        return foundCars;
    }

    /// <summary>
    /// Memastikan objek adalah kendaraan valid dan BUKAN gerobak makanan/properti kota.
    /// </summary>
    public static bool IsValidVehicle(GameObject go)
    {
        if (go == null) return false;

        string lName = go.name.ToLower();

        // 1. DAFTAR KATA TERLARANG: Gerobak, Kardus, Trotoar, Tanaman, Karakter, dsb.
        string[] forbidden = new string[]
        {
            "cart", "box", "cardboard", "carrot", "carpet",
            "planter", "bench", "lamp", "light", "pole", "tree", "palm", "bush",
            "grass", "flower", "foliage", "table", "chair", "seat", "fountain",
            "billboard", "sign", "poster", "barrier", "fence",
            "building", "house", "shop", "store", "office", "roof", "wall", "dinding",
            "road", "runway", "street", "tile", "asphalt", "sidewalk", "trotoar",
            "bin", "trash", "rubbish", "dumpster", "barrel",
            "people", "character", "steve", "citizen", "pedestrian", "player",
            "trucker", "waitress", "prostitute", "pimp", "man", "woman", "girl", "boy",
            "camera", "canvas", "audio", "event", "manager", "system", "portal"
        };

        foreach (var bad in forbidden)
        {
            if (lName.Contains(bad)) return false;
        }

        // 2. Jika parent adalah folder lingkungan atau properti non-kendaraan
        if (go.transform.parent != null)
        {
            string pName = go.transform.parent.name.ToLower();
            if (pName == "environment" || pName.Contains("bangunan") || 
                pName.Contains("pohon") || pName.Contains("dinding") || 
                pName.Contains("plane") || pName.Contains("runway") ||
                pName.Contains("jalan raya"))
            {
                // Harus benar-benar nama prefab mobil yang sangat spesifik
                if (!IsSpecificVehicleName(lName)) return false;
            }
        }

        // 3. Cek nama spesifik kendaraan atau struktur roda kendaraan
        return IsSpecificVehicleName(lName) || HasVehicleWheels(go);
    }

    private static bool IsSpecificVehicleName(string lName)
    {
        if (lName.Contains("truck") && !lName.Contains("trucker")) return true;
        if (lName.Contains("sedan")) return true;
        if (lName.Contains("wagon")) return true;
        if (lName.Contains("limo")) return true;
        if (lName.Contains("ute")) return true;
        if (lName.Contains("pickup")) return true;
        if (lName.Contains("taxi")) return true;
        if (lName.Contains("van") && !lName.Contains("vane")) return true;
        if (lName.Contains("bus") && !lName.Contains("bush")) return true;
        if (lName.Contains("automobile") || lName.Contains("mobil")) return true;
        if (lName.Contains("st_veh_") || lName.Contains("sk_veh_")) return true;

        if (lName.Contains("car_") || lName.Contains("_car") || lName.Contains("car ") || 
            lName.Contains("old_car") || lName.Contains("sports_car") || lName.Contains("police_car"))
        {
            return true;
        }

        return false;
    }

    private static bool HasVehicleWheels(GameObject go)
    {
        Transform[] children = go.GetComponentsInChildren<Transform>(true);
        int wheelCount = 0;
        foreach (var t in children)
        {
            if (t == go.transform) continue;
            string tName = t.name.ToLower();
            if (tName.Contains("wheel") && !tName.Contains("wheelie"))
            {
                wheelCount++;
            }
        }
        return wheelCount >= 2;
    }

    private static void DestroyComponentSafe(Component comp)
    {
        if (comp == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(comp);
            return;
        }
#endif
        Object.Destroy(comp);
    }

    private static void DestroyObjectSafe(GameObject go)
    {
        if (go == null) return;
#if UNITY_EDITOR
        if (Selection.activeGameObject == go || (Selection.activeTransform != null && Selection.activeTransform.IsChildOf(go.transform)))
        {
            Selection.activeObject = null;
        }
#endif
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
