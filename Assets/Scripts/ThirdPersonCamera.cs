using UnityEngine;

/// <summary>
/// Third Person Camera - Fokus mengikuti 1 Pemain Inti (Player).
/// 1. Mengikuti pergerakan pemain (berjalan, berlari, melompat, duduk) secara halus.
/// 2. Geser Mouse untuk memutar kamera 360 derajat mengelilingi pemain.
/// 3. Scroll Mouse untuk zoom in / zoom out.
/// 4. Tekan tombol [V] jika ingin beralih ke kamera pemandangan kota luas.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    public enum CameraViewMode
    {
        ThirdPersonFollow,  // Default: Fokus mengikuti Pemain Inti dari belakang
        CityOverview        // Sudut pandang kota luas
    }

    [Header("=== Mode Kamera ===")]
    [Tooltip("Default: ThirdPersonFollow (Kamera fokus mengikuti 1 Pemain Inti).")]
    public CameraViewMode viewMode = CameraViewMode.ThirdPersonFollow;

    [Tooltip("Tekan tombol ini di keyboard untuk beralih mode kamera (opsional).")]
    public KeyCode toggleCameraKey = KeyCode.V;

    [Header("=== Target Pemain Inti ===")]
    [Tooltip("Target Transform Player yang diikuti.")]
    public Transform target;

    [Tooltip("Tinggi titik fokus di atas posisi target (dada/kepala).")]
    public float targetHeight = 0.18f;

    [Header("=== Jarak & Ketinggian (Skala 0.1) ===")]
    [Tooltip("Jarak kamera di belakang pemain.")]
    public float distance = 1.35f;

    [Tooltip("Tinggi kamera di atas target.")]
    public float height = 0.45f;

    [Tooltip("Kecepatan kamera mengikuti pergerakan pemain.")]
    public float followSpeed = 12f;

    [Header("=== Kontrol Putaran Mouse ===")]
    public bool enableMouseLook = true;
    public float mouseSensitivityX = 3f;
    public float mouseSensitivityY = 2f;
    public float minPitch = -10f;
    public float maxPitch = 60f;

    [Header("=== Sudut Belakang Pemain (Rear Follow) ===")]
    [Tooltip("Apakah kamera otomatis mengikuti sudut rotasi belakang pemain?")]
    public bool followRearAngle = true;

    [Tooltip("Kecepatan kamera menyelaraskan sudut pandang ke belakang punggung pemain.")]
    public float rotationFollowSpeed = 6f;

    [Header("=== Deteksi Tabrakan Dinding (Camera Collision) ===")]
    [Tooltip("Mencegah kamera menembus tembok atau bangunan di belakang pemain.")]
    public bool enableCameraCollision = true;
    public LayerMask collisionLayers = ~0;
    public float collisionRadius = 0.06f;

    [Header("=== Posisi Sudut Pandang Kota (City Overview) ===")]
    public Vector3 cityOverviewPosition = new Vector3(322.056f, 1.582f, -10.228f);
    public Vector3 cityOverviewRotationEuler = new Vector3(8.458f, 270f, 0f);

    [Header("=== Tampilan UI Panduan Kontrol ===")]
    [Tooltip("Tampilkan kotak panduan kontrol pemain di pojok layar.")]
    public bool showControlsUI = false;

    private float currentYaw = 0f; // Menghadap ke arah jalan kota (+Z) secara default
    private float currentPitch = 15f;

    private void Start()
    {
        FindPlayerTarget();

        if (target != null)
        {
            // Mulai dari belakang pemain
            currentYaw = target.eulerAngles.y;
            currentPitch = 15f;
            SnapToTarget();
        }
    }

    private void Update()
    {
        // Tombol V untuk beralih mode kamera (opsional)
        if (Input.GetKeyDown(toggleCameraKey))
        {
            if (viewMode == CameraViewMode.ThirdPersonFollow)
            {
                viewMode = CameraViewMode.CityOverview;
                Debug.Log("[Camera] Mode Kamera: City Overview (Pemandangan Kota Luas)");
            }
            else
            {
                viewMode = CameraViewMode.ThirdPersonFollow;
                currentYaw = target != null ? target.eulerAngles.y : 0f;
                currentPitch = 15f;
                SnapToTarget();
                Debug.Log("[Camera] Mode Kamera: Third-Person Follow (Fokus pada Pemain Inti)");
            }
        }

        // Zoom in / zoom out dengan scroll mouse
        if (viewMode == CameraViewMode.ThirdPersonFollow)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                distance = Mathf.Clamp(distance - scroll * 1.5f, 0.4f, 4.0f);
                height = Mathf.Clamp(height - scroll * 0.5f, 0.15f, 1.8f);
            }
        }
    }

    private void LateUpdate()
    {
        if (viewMode == CameraViewMode.CityOverview)
        {
            ApplyCityOverview();
            return;
        }

        // Mode Third-Person Follow (Fokus pada Pemain)
        if (target == null)
        {
            FindPlayerTarget();
            if (target == null) return;
        }

        // 1. Mouse orbit (putar kamera mengelilingi pemain jika tombol mouse ditekan)
        bool isManualOrbiting = false;
        if (enableMouseLook)
        {
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || !Input.mousePresent)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                if (Mathf.Abs(mouseX) > 0.0001f || Mathf.Abs(mouseY) > 0.0001f)
                {
                    currentYaw += mouseX * mouseSensitivityX;
                    currentPitch -= mouseY * mouseSensitivityY;
                    currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
                    isManualOrbiting = true;
                }
            }
        }

        // 2. Selalu mengikuti sudut belakang pemain
        if (followRearAngle && !isManualOrbiting)
        {
            float targetYaw = target.eulerAngles.y;
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotationFollowSpeed * Time.deltaTime);
        }

        // 3. Posisi fokus target pemain
        Vector3 targetFocusPos = target.position + Vector3.up * targetHeight;

        // 4. Rotasi dan posisi kamera yang diinginkan (selalu di belakang pemain)
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 direction = rotation * new Vector3(0f, height, -distance);
        Vector3 desiredPosition = targetFocusPos + direction;

        // 5. Cek tabrakan kamera dengan dinding/bangunan agar tidak tembus
        if (enableCameraCollision)
        {
            Vector3 camRay = desiredPosition - targetFocusPos;
            float camDist = camRay.magnitude;
            if (camDist > 0.05f)
            {
                Vector3 rayDir = camRay / camDist;
                if (Physics.SphereCast(targetFocusPos, collisionRadius, rayDir, out RaycastHit hit, camDist, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null && !hit.collider.transform.IsChildOf(target))
                    {
                        desiredPosition = targetFocusPos + rayDir * Mathf.Max(0.25f, hit.distance - 0.03f);
                    }
                }
            }
        }

        // 6. Interpolasi posisi kamera secara halus
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // 7. Selalu menatap ke arah titik fokus Pemain
        transform.LookAt(targetFocusPos);
    }

    /// <summary>
    /// Menempatkan kamera pada sudut pemandangan kota luas.
    /// </summary>
    public void ApplyCityOverview()
    {
        transform.position = Vector3.Lerp(transform.position, cityOverviewPosition, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(cityOverviewRotationEuler), followSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Mencari target karakter Player utama.
    /// </summary>
    public void FindPlayerTarget()
    {
        GameObject trucker = GameObject.Find("SimplePeople_Trucker_Black");
        if (trucker != null)
        {
            target = trucker.transform;
            return;
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            return;
        }

        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            target = pc.transform;
            return;
        }

        GameObject streetMan = GameObject.Find("SimplePeople_StreetMan_Black");
        if (streetMan != null)
        {
            target = streetMan.transform;
            return;
        }
    }

    /// <summary>
    /// Mengarahkan kamera secara instan ke belakang pemain.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        currentYaw = target.eulerAngles.y;
        Vector3 targetFocusPos = target.position + Vector3.up * targetHeight;
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        transform.position = targetFocusPos + rotation * new Vector3(0f, height, -distance);
        transform.LookAt(targetFocusPos);
    }

    private void OnGUI()
    {
        if (!showControlsUI) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleLeft;

        GUI.Box(new Rect(12, 12, 390, 75), 
            "🎮 KONTROL PEMAIN:\n" +
            "• Gerak: [W, A, S, D] | Lari: [Shift]\n" +
            "• Lompat: [Spasi] | Duduk / Berdiri: [C] atau [X]\n" +
            "• Putar Kamera: [Geser Mouse] | Zoom: [Scroll]", 
            style);
    }
}
