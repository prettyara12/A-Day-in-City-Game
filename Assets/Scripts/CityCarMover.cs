using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Menggerakkan 2 mobil manual milik pengguna agar berjalan di atas jalan raya kota
/// selayaknya mobil normal pada umumnya:
/// 1. 100% tetap di atas aspal jalan raya kota (tidak keluar kota / tidak ke rumput).
/// 2. Berjalan di dua lajur (Lajur Timur ke Utara, Lajur Barat ke Selatan).
/// 3. Berputar balik (U-Turn) secara mulus di ujung jalan tanpa menabrak trotoar/taman.
/// 4. Menghadap ke depan (kap mobil & lampu depan menghadap arah jalan, bukan mundur).
/// 5. Roda berputar secara alami mengikuti laju mobil.
/// 6. Anti-tabrakan: Menjaga jarak aman jika ada mobil lain di depan.
/// </summary>
public class CityCarMover : MonoBehaviour
{
    [Header("Kecepatan & Kemudi")]
    [Tooltip("Kecepatan laju mobil di jalan kota (meter/detik)")]
    public float speed = 2.1f;

    [Tooltip("Kecepatan belok di tikungan/U-turn (derajat/detik)")]
    public float turnSpeed = 220.0f;

    [Tooltip("Jarak deteksi untuk mencapai waypoint (meter)")]
    public float waypointThreshold = 0.4f;

    [Tooltip("Rute waypoint khusus untuk mobil ini. Jika kosong, menggunakan rute jalan raya standar.")]
    public Vector3[] waypoints;

    /// <summary>
    /// Rute sirkulasi jalan raya kota dua lajur (100% tepat di atas aspal Jalan Raya (3)):
    /// - Lajur Barat (X = 317.65f): Melaju ke Selatan (-Z) tepat di atas lajur kiri tanpa keluar ke trotoar.
    /// - U-Turn Selatan: Berbelok halus dari Barat ke Timur di Z = -10.0 s/d -11.4
    /// - Lajur Timur (X = 318.35f): Melaju ke Utara (+Z)
    /// - U-Turn Utara: Berbelok halus dari Timur ke Barat di Z = 13.5 s/d 14.8
    /// </summary>
    public static readonly Vector3[] MainRoadWaypoints = new Vector3[]
    {
        // 1. Lajur Barat (Melaju ke arah Selatan - tepat di atas aspal jalan, X = 317.65f)
        new Vector3(317.65f, 0f, 14.0f),
        new Vector3(317.65f, 0f, 8.0f),
        new Vector3(317.65f, 0f, 0.0f),
        new Vector3(317.65f, 0f, -6.0f),
        new Vector3(317.65f, 0f, -10.0f),

        // 2. U-Turn Selatan (Berputar halus di atas aspal ujung selatan)
        new Vector3(317.75f, 0f, -10.8f),
        new Vector3(318.00f, 0f, -11.4f),
        new Vector3(318.25f, 0f, -10.8f),

        // 3. Lajur Timur (Melaju ke arah Utara, X = 318.35f)
        new Vector3(318.35f, 0f, -10.0f),
        new Vector3(318.35f, 0f, -6.0f),
        new Vector3(318.35f, 0f, 0.0f),
        new Vector3(318.35f, 0f, 8.0f),
        new Vector3(318.35f, 0f, 14.0f),

        // 4. U-Turn Utara (Berputar halus di atas aspal ujung utara)
        new Vector3(318.25f, 0f, 14.8f),
        new Vector3(318.00f, 0f, 15.2f),
        new Vector3(317.75f, 0f, 14.8f)
    };

    /// <summary>
    /// Rute mobil merah (100% tepat di atas aspal jalan raya & jalan pertokoan):
    /// - Melaju ke selatan di lajur barat Jalan Utama.
    /// - Belok kanan mulus ke jalan pertokoan persis di garis tengah aspal (Z = 4.50f).
    /// - Melaju di jalan pertokoan dengan jarak aman >1.5m dari pertokoan & tembok dinding.
    /// - Berputar balik (U-Turn) mulus di persimpangan jalan aspal terbuka barat (X = 312.0 s/d 312.8f).
    /// - Melaju kembali ke timur di atas aspal jalan pertokoan.
    /// - Belok kanan masuk kembali ke lajur barat Jalan Utama (melaju ke selatan).
    /// - Melakukan U-Turn selatan, melaju di lajur timur ke utara, U-Turn utara, dan berulang terus tanpa henti.
    /// </summary>
    public static readonly Vector3[] RedCarWaypoints = new Vector3[]
    {
        // 1. Datang dari arah utara (Lajur Barat Jalan Raya 3, melaju ke Selatan, X = 317.65f)
        new Vector3(317.65f, 0f, 14.00f),
        new Vector3(317.65f, 0f, 8.50f),
        new Vector3(317.65f, 0f, 6.20f),

        // 2. Belok Kanan Mulus ke Jalan Pertokoan (100% tepat di atas jalur aspal)
        new Vector3(317.40f, 0f, 5.40f),
        new Vector3(316.70f, 0f, 4.75f),
        new Vector3(315.50f, 0f, 4.50f),
        new Vector3(314.50f, 0f, 4.50f),
        new Vector3(313.50f, 0f, 4.50f),
        new Vector3(312.70f, 0f, 4.50f),

        // 3. U-Turn Halus di Persimpangan Aspal Terbuka (Depan West Street)
        new Vector3(312.20f, 0f, 4.85f),
        new Vector3(312.00f, 0f, 5.25f),
        new Vector3(312.30f, 0f, 5.45f),
        new Vector3(312.65f, 0f, 5.20f),
        new Vector3(312.80f, 0f, 4.75f),

        // 4. Melaju Kembali ke Arah Timur di Atas Jalur Aspal Menuju Jalan Utama
        new Vector3(313.30f, 0f, 4.50f),
        new Vector3(314.50f, 0f, 4.50f),
        new Vector3(315.50f, 0f, 4.50f),
        new Vector3(316.70f, 0f, 4.50f),

        // 5. Belok Kanan Mulus Masuk Kembali ke Lajur Barat Jalan Raya 3 (Melaju ke Selatan)
        new Vector3(317.90f, 0f, 4.50f),
        new Vector3(318.10f, 0f, 3.50f),
        new Vector3(317.85f, 0f, 2.30f),
        new Vector3(317.65f, 0f, 1.00f),
        new Vector3(317.65f, 0f, -4.00f),
        new Vector3(317.65f, 0f, -10.00f),

        // 6. U-Turn Selatan (Berputar halus di atas aspal ujung selatan)
        new Vector3(317.75f, 0f, -10.80f),
        new Vector3(318.00f, 0f, -11.40f),
        new Vector3(318.25f, 0f, -10.80f),

        // 7. Lajur Timur (Melaju ke Utara, X = 318.35f)
        new Vector3(318.35f, 0f, -10.00f),
        new Vector3(318.35f, 0f, -4.00f),
        new Vector3(318.35f, 0f, 2.00f),
        new Vector3(318.35f, 0f, 8.00f),
        new Vector3(318.35f, 0f, 14.00f),

        // 8. U-Turn Utara (Berputar halus di atas aspal ujung utara)
        new Vector3(318.25f, 0f, 14.80f),
        new Vector3(318.00f, 0f, 15.20f),
        new Vector3(317.75f, 0f, 14.80f)
    };

    public static Vector3[] CityWaypoints => MainRoadWaypoints;

    private int currentWaypointIndex = 0;
    private float initialY;
    private float modelHeadingOffset = 0f;
    private float currentSpeed = 0f;
    private bool isInitialized = false;
    private List<Transform> wheelTransforms = new List<Transform>();

    public static readonly List<CityCarMover> ActiveMovers = new List<CityCarMover>();

    private void OnEnable()
    {
        if (!ActiveMovers.Contains(this))
        {
            ActiveMovers.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveMovers.Remove(this);
    }

    private void Awake()
    {
        InitializePosition();
    }

    private void Start()
    {
        InitializePosition();
    }

    public void InitializePosition()
    {
        if (isInitialized) return;

        if (waypoints == null || waypoints.Length == 0)
        {
            waypoints = MainRoadWaypoints;
        }

        // 1. Kunci ketinggian Y asli agar roda menempel pas di atas aspal
        initialY = transform.position.y;

        // 2. Deteksi orientasi model bawaan FBX (banyak model SimpleCars menghadap -Z)
        DetectModelOrientation();

        // 3. Kumpulkan transform roda untuk animasi putaran roda
        CollectWheels();

        // 4. Pastikan Rigidbody diatur kinematic agar tidak terpental/terguling
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 5. Pilih waypoint awal yang sesuai dengan lajur tempat mobil berada
        SelectInitialWaypoint();

        currentSpeed = speed;
        isInitialized = true;
    }

    /// <summary>
    /// Mengembalikan arah hadap visual bagian depan kendaraan di sumbu XZ (dunia nyata).
    /// Memperhitungkan modelHeadingOffset (misalnya 180 derajat untuk model Synty SimpleCars).
    /// </summary>
    public Vector3 GetVisualForward()
    {
        Vector3 fwd = transform.rotation * Quaternion.Euler(0f, -modelHeadingOffset, 0f) * Vector3.forward;
        fwd.y = 0f;
        return fwd.sqrMagnitude > 0.001f ? fwd.normalized : transform.forward;
    }

    /// <summary>
    /// Menentukan apakah model 3D kendaraan menghadap +Z atau -Z secara lokal.
    /// Prefab Synty SimpleCars secara standar memiliki roda depan di Z negatif (-3.6m),
    /// sehingga membutuhkan offset rotasi 180 derajat agar bagian depan menghadap arah jalan.
    /// </summary>
    private void DetectModelOrientation()
    {
        Transform flWheel = null;
        Transform rlWheel = null;

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren)
        {
            if (t == transform) continue;
            string lName = t.name.ToLower();
            if (lName.Contains("fl_wheel") || lName.Contains("fr_wheel"))
            {
                if (flWheel == null) flWheel = t;
            }
            else if (lName.Contains("rl_wheel") || lName.Contains("rr_wheel"))
            {
                if (rlWheel == null) rlWheel = t;
            }
        }

        if (flWheel != null && rlWheel != null)
        {
            // Jika roda depan berada di koordinat Z lokal lebih kecil dibanding roda belakang,
            // berarti bagian depan mobil menghadap sumbu -Z lokal.
            if (flWheel.localPosition.z < rlWheel.localPosition.z)
            {
                modelHeadingOffset = 180.0f;
            }
            else
            {
                modelHeadingOffset = 0.0f;
            }
        }
        else
        {
            // Default untuk seri SimpleCars jika wheel tidak terdeteksi
            modelHeadingOffset = 180.0f;
        }
    }

    /// <summary>
    /// Mencatat semua objek roda pada mobil untuk diputar saat melaju.
    /// </summary>
    private void CollectWheels()
    {
        wheelTransforms.Clear();
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren)
        {
            if (t == transform) continue;
            if (t.name.ToLower().Contains("wheel"))
            {
                wheelTransforms.Add(t);
            }
        }
    }

    /// <summary>
    /// Menentukan waypoint awal berdasarkan posisi mobil saat ini pada rutenya.
    /// </summary>
    private void SelectInitialWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 curPos = transform.position;
        float bestDist = float.MaxValue;
        int bestIdx = 0;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 wp = waypoints[i];
            float dist = Vector2.Distance(new Vector2(curPos.x, curPos.z), new Vector2(wp.x, wp.z));
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIdx = i;
            }
        }

        currentWaypointIndex = (bestIdx + 1) % waypoints.Length;

        // Arahkan rotasi awal mobil langsung menghadap waypoint tujuan
        Vector3 initialDir = waypoints[currentWaypointIndex] - transform.position;
        initialDir.y = 0;
        if (initialDir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(initialDir);
            transform.rotation = lookRot * Quaternion.Euler(0f, modelHeadingOffset, 0f);
        }
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // 1. Tentukan target waypoint saat ini
        Vector3 target = waypoints[currentWaypointIndex];
        target.y = initialY; // Pertahankan tinggi mobil di atas aspal

        Vector3 moveDir = target - transform.position;
        moveDir.y = 0f;

        Vector3 visualFwd = GetVisualForward();

        // 2. Sensor Anti-Tabrakan Cerdas (Menjaga jarak aman dengan mobil searah di depan)
        float desiredSpeed = speed;
        for (int i = 0; i < ActiveMovers.Count; i++)
        {
            var other = ActiveMovers[i];
            if (other == null || other == this) continue;

            Vector3 toOther = other.transform.position - transform.position;
            toOther.y = 0f;
            float distToOther = toOther.magnitude;

            // Hanya periksa mobil yang berada dalam radius deteksi 4.5 meter
            if (distToOther < 4.5f)
            {
                Vector3 otherFwd = other.GetVisualForward();
                float headingDot = Vector3.Dot(visualFwd, otherFwd);

                // Abaikan jika mobil lain bergerak berlawanan arah (lajur lawan) atau tegak lurus.
                // Mobil HANYA mengerem jika mobil lain di depannya melaju searah (headingDot > 0.4f).
                // Ini mencegah deadlock/macet permanen di tikungan U-turn ujung jalan!
                if (headingDot > 0.4f)
                {
                    // Hitung jarak longitudinal (depan) dan lateral (samping)
                    float forwardDist = Vector3.Dot(visualFwd, toOther);
                    Vector3 lateralDir = Vector3.Cross(Vector3.up, visualFwd).normalized;
                    float lateralDist = Mathf.Abs(Vector3.Dot(lateralDir, toOther));

                    // Hanya tanggapi jika mobil lain berada di depan kita dalam lajur yang sama
                    if (forwardDist > 0.3f && lateralDist < 0.65f)
                    {
                        if (forwardDist < 2.0f)
                        {
                            desiredSpeed = 0f; // Berhenti jika terlalu dekat dengan ekor mobil depan
                        }
                        else
                        {
                            desiredSpeed = Mathf.Min(desiredSpeed, other.currentSpeed * 0.8f);
                        }
                    }
                }
            }
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, 4.0f * Time.deltaTime);

        // 3. Rotasi halus mobil agar bagian depan menghadap arah gerak ke waypoint
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetLook = Quaternion.LookRotation(moveDir);
            Quaternion targetRot = targetLook * Quaternion.Euler(0f, modelHeadingOffset, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // 4. Majukan mobil:
        // Selaraskan laju translasi dengan orientasi bodi mobil agar tidak meluncur miring (anti crab-walking).
        // Jika bodi mobil belum sejajar dengan arah gerak (sedang belok), perlambat translasi sesaat.
        float align = (moveDir.sqrMagnitude > 0.001f) ? Mathf.Clamp01(Vector3.Dot(visualFwd, moveDir.normalized)) : 1f;
        float speedMultiplier = Mathf.Lerp(0.20f, 1.0f, align * align);
        float step = currentSpeed * speedMultiplier * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, step);

        // 5. Animasi putaran roda (Rolling Wheels)
        if (step > 0.0001f && wheelTransforms.Count > 0)
        {
            float rollAngle = (step / (Mathf.PI * 0.7f)) * 360.0f;
            float rollSign = (modelHeadingOffset > 90.0f) ? -1.0f : 1.0f;

            foreach (var w in wheelTransforms)
            {
                if (w != null)
                {
                    w.Rotate(Vector3.right, rollAngle * rollSign, Space.Self);
                }
            }
        }

        // 6. Cek apakah sudah mencapai waypoint untuk lanjut ke waypoint berikutnya
        float distToWp = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(target.x, target.z)
        );

        if (distToWp < waypointThreshold)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 p1 = waypoints[i];
            Vector3 p2 = waypoints[(i + 1) % waypoints.Length];
            p1.y = transform.position.y + 0.1f;
            p2.y = transform.position.y + 0.1f;
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawSphere(p1, 0.2f);
        }
    }
}
