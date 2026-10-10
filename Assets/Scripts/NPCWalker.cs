using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// NPC Walker & Dialogue Controller
/// Membuat karakter menjadi NPC pejalan kaki interaktif persis seperti pada video challenge:
/// 1. Berjalan / berpatroli di trotoar atau berkeliaran (free wander) di jalanan kota.
/// 2. Melakukan animasi santai (cek jam, silang tangan, duduk, dll.) saat berhenti.
/// 3. Ketika Pemain (Player) mendekat:
///    - NPC berhenti berjalan.
///    - Menoleh menghadap ke Pemain (LookAt Player).
///    - Melambaikan tangan (Animasi Wave).
///    - Menampilkan Speech Bubble (Balon Percakapan / Dialog) di atas kepala menghadap kamera.
/// 4. Ketika Pemain menjauh, balon dialog tertutup dan NPC kembali berpatroli.
/// 5. Disesuaikan khusus untuk skala 0.1 kota.
/// </summary>
[RequireComponent(typeof(Animator))]
public class NPCWalker : MonoBehaviour
{
    // ==================== ENUMS ====================
    public enum NavigationMode
    {
        WaypointPath,   // Mengikuti jalur waypoint trotoar
        FreeWander,     // Bebas berkeliling di sekitar lokasi awal (seperti video challenge)
        Stationary      // Berdiri diam menjaga tempat, melambai & bicara saat player dekat
    }

    public enum EndBehavior
    {
        PingPong,   // Bolak-balik sepanjang jalur
        Loop,       // Berulang dari awal
        Stop        // Berhenti di ujung
    }

    public enum NPCState
    {
        Idle,
        Walking,
        Running,
        Sitting,
        IdleAction,
        InteractingWithPlayer
    }

    // ==================== NAVIGATION SETTINGS ====================
    [Header("=== Pengaturan Navigasi & Jalur ===")]
    [Tooltip("Mode navigasi NPC: WaypointPath (jalur), FreeWander (keliling acak), atau Stationary (penjaga).")]
    public NavigationMode navMode = NavigationMode.FreeWander;

    [Tooltip("Jalur waypoint yang diikuti (jika mode WaypointPath).")]
    public NPCWaypointPath waypointPath;

    [Tooltip("Perilaku saat mencapai ujung jalur.")]
    public EndBehavior endBehavior = EndBehavior.PingPong;

    [Tooltip("Mulai dari titik waypoint acak saat game dimulai.")]
    public bool randomStartPoint = false;

    [Tooltip("Waktu jeda acak maksimal sebelum NPC mulai bergerak (detik).")]
    [Range(0f, 5f)]
    public float maxStartDelay = 2f;

    [Tooltip("Radius keliling bebas di sekitar titik awal (khusus mode FreeWander, skala 0.1).")]
    [Range(0.5f, 15f)]
    public float wanderRadius = 2.5f;

    [Tooltip("Batas jarak maksimal NPC boleh menjauh dari titik awalnya.")]
    [Range(2f, 30f)]
    public float maxDistanceFromStart = 8f;

    // ==================== MOVEMENT SPEEDS ====================
    [Header("=== Kecepatan Gerak (Skala 0.1) ===")]
    [Tooltip("Kecepatan jalan santai.")]
    [Range(0.05f, 1.5f)]
    public float walkSpeed = 0.28f;

    [Tooltip("Kecepatan berlari.")]
    [Range(0.1f, 3f)]
    public float runSpeed = 0.65f;

    [Tooltip("Kecepatan memutar arah badan.")]
    [Range(1f, 20f)]
    public float rotationSpeed = 8f;

    [Tooltip("Jarak toleransi untuk mencapai target waypoint.")]
    [Range(0.02f, 1f)]
    public float waypointReachDistance = 0.15f;

    // ==================== OBSTACLE AVOIDANCE & SAFETY ====================
    [Header("=== Deteksi & Penghindaran Rintangan (Anti Tabrakan) ===")]
    [Tooltip("Aktifkan sensor rintangan otomatis agar NPC tidak menabrak benda di sekitarnya.")]
    public bool enableObstacleAvoidance = true;

    [Tooltip("Jarak deteksi rintangan di depan (skala 0.1).")]
    [Range(0.1f, 1.5f)]
    public float obstacleDetectionDistance = 0.45f;

    [Tooltip("Radius sensor tubuh NPC saat memeriksa rintangan.")]
    [Range(0.02f, 0.15f)]
    public float bodyRadius = 0.05f;

    [Tooltip("Layer rintangan yang dideteksi.")]
    public LayerMask obstacleLayers = ~0;

    [Tooltip("Waktu jeda saat mencapai ujung jalan sebelum berputar balik (detik).")]
    [Range(0f, 3f)]
    public float turnPauseTime = 0.5f;

    // ==================== BEHAVIOR PROBABILITIES ====================
    [Header("=== Perilaku & Animasi Idle ===")]
    [Tooltip("Peluang berhenti santai sejenak (0-1).")]
    [Range(0f, 1f)]
    public float idleChance = 0.35f;

    [Tooltip("Peluang duduk saat berhenti (0-1).")]
    [Range(0f, 1f)]
    public float sitChance = 0.1f;

    [Tooltip("Peluang berlari dibanding berjalan (0-1).")]
    [Range(0f, 1f)]
    public float runChance = 0.15f;

    [Tooltip("Waktu jeda berpikir saat memutuskan arah berikutnya (Detik Min & Max).")]
    public Vector2 decisionPauseTime = new Vector2(1.5f, 4.0f);

    [Header("=== Khusus NPC Duduk (Sitting) ===")]
    [Tooltip("Apakah NPC ini duduk secara permanen di kursi/tempat duduk (tidak akan berdiri atau berjalan).")]
    public bool isSittingPermanently = false;

    [Header("=== Khusus NPC Melompat di Tempat (Jumping In Place) ===")]
    [Tooltip("Apakah NPC ini melompat-lompat di tempat secara terus-menerus (seperti NPC wanita baju kuning).")]
    public bool isJumpingInPlace = false;

    [Tooltip("Jeda waktu antar lompatan (detik).")]
    public float jumpInterval = 1.4f;

    [Tooltip("Ketinggian lompatan di tempat (skala 0.1).")]
    public float jumpInPlaceHeight = 0.18f;

    [Header("=== Khusus NPC Menengok Kiri-Kanan (Looking Around In Place) ===")]
    [Tooltip("Apakah NPC ini berdiri diam di tempat dan menengok kiri-kanan tanpa berjalan.")]
    public bool isLookingAroundInPlace = false;

    [Tooltip("Sudut tengokan badan ke kiri dan kanan (derajat).")]
    [Range(10f, 80f)]
    public float lookAroundAngle = 40f;

    [Tooltip("Kecepatan menoleh/menengok (derajat per detik).")]
    [Range(10f, 120f)]
    public float lookAroundSpeed = 45f;

    [Tooltip("Durasi jeda saat menatap ke arah kiri, kanan, atau depan (Detik Min & Max).")]
    public Vector2 lookPauseDuration = new Vector2(1.5f, 3.0f);

    // ==================== DIALOGUE & PLAYER INTERACTION ====================
    [Header("=== Interaksi Pemain & Balon Dialog (Fitur Video) ===")]
    [Tooltip("Aktifkan interaksi saat Pemain mendekat.")]
    public bool enablePlayerInteraction = true;

    [Tooltip("Jarak deteksi pemain untuk mulai menyapa & melambaikan tangan.")]
    [Range(0.3f, 5f)]
    public float interactionDistance = 1.4f;

    [Tooltip("Teks dialog yang diucapkan NPC di balon percakapan.")]
    [TextArea(2, 5)]
    public string characterLine = "Halo! Selamat datang di kota kami. Semoga harimu menyenangkan!";

    [Tooltip("Nama panggilan / profesi NPC untuk ditampilkan di dialog overlay.")]
    public string characterDisplayName = "NPC";

    [Tooltip("Referensi ke Canvas Balon Dialog (Otomatis dibuat jika kosong).")]
    public Canvas speechBubbleCanvas;

    [Tooltip("Referensi ke Text dialog di dalam balon percakapan.")]
    public Text speechBubbleText;

    [Tooltip("Tinggi posisi balon dialog di atas kepala NPC (skala 0.1).")]
    public float speechBubbleHeight = 0.22f;

    [Tooltip("Target Pemain yang dideteksi (otomatis dicari jika kosong).")]
    public Transform playerTarget;

    // ==================== PRIVATE STATE ====================
    private Animator animator;
    private NPCState currentState = NPCState.Idle;
    private List<Transform> waypoints;
    private int currentWaypointIndex = 0;
    private int direction = 1;
    private float currentSpeed;
    private bool isInitialized = false;
    private float groundY;
    private Vector3 startLocation;
    private Vector3 currentDestination;
    private bool isDecidingNextMove = false;
    private bool isInteractingWithPlayer = false;

    // Parameter Hashes
    private static readonly int ParamSpeedF = Animator.StringToHash("Speed_f");
    private static readonly int ParamGrounded = Animator.StringToHash("Grounded");
    private static readonly int ParamGroundedB = Animator.StringToHash("Grounded_b");
    private static readonly int ParamStaticB = Animator.StringToHash("Static_b");
    private static readonly int ParamAnimationInt = Animator.StringToHash("Animation_int");
    private static readonly int ParamWave = Animator.StringToHash("Wave");
    private static readonly int ParamSit = Animator.StringToHash("Sit");
    private static readonly int ParamStand = Animator.StringToHash("Stand");
    private static readonly int ParamJumpB = Animator.StringToHash("Jump_b");
    private static readonly int ParamHeadHorizontalF = Animator.StringToHash("Head_Horizontal_f");

    private float jumpTimer = 0f;
    private bool isMidAir = false;
    private float airTime = 0f;
    private const float JumpAirDuration = 0.65f;

    // Looking Around In Place State
    private Quaternion baseFacingRotation = Quaternion.identity;
    private enum LookStage
    {
        CenterToLeft,
        PauseLeft,
        LeftToCenter,
        PauseCenter1,
        CenterToRight,
        PauseRight,
        RightToCenter,
        PauseCenter2
    }
    private LookStage currentLookStage = LookStage.CenterToLeft;
    private float lookStageTimer = 0f;
    private float currentLookAngle = 0f;
    private float targetLookAngle = 0f;
    private float currentHeadTurn = 0f;
    private float targetHeadTurn = 0f;

    // Animation States
    private static readonly int AnimIdle = Animator.StringToHash("Idle");
    private static readonly int AnimWalk = Animator.StringToHash("Walk");
    private static readonly int AnimRun = Animator.StringToHash("Run");
    private static readonly int AnimSitting = Animator.StringToHash("Idle_SittingOnGround");
    private static readonly int AnimCheckWatch = Animator.StringToHash("Idle_CheckWatch");
    private static readonly int AnimCrossedArms = Animator.StringToHash("Idle_CrossedArms");
    private static readonly int AnimHandOnHips = Animator.StringToHash("Idle_HandOnHips");
    private static readonly int AnimWaveClip = Animator.StringToHash("Idle_Wave");

    private bool hasParamSpeedF = false;
    private bool hasParamGrounded = false;
    private bool hasParamGroundedB = false;
    private bool hasParamStaticB = false;
    private bool hasParamAnimationInt = false;
    private bool hasParamWave = false;
    private bool hasParamSit = false;
    private bool hasParamStand = false;
    private bool hasParamJumpB = false;
    private bool hasParamHeadHorizontalF = false;

    private int[] idleActions;

    // ==================== UNITY LIFECYCLE ====================

    private void Awake()
    {
        animator = GetComponent<Animator>();
        CacheAnimatorParameters();

        // Pastikan NPC tidak memiliki InputManager (menghapus komponen yang salah pasang)
        InputManager rogueInput = GetComponent<InputManager>();
        if (rogueInput != null)
        {
            Destroy(rogueInput);
        }

        // Pastikan bukan Player
        if (gameObject.tag == "Player")
        {
            gameObject.tag = "Untagged";
        }

        // Bersihkan objek kanvas WorldSpace lama jika ada
        Transform oldBubble = transform.Find("SpeechBubbleCanvas");
        if (oldBubble != null)
        {
            Destroy(oldBubble.gameObject);
        }

        if (isLookingAroundInPlace)
        {
            navMode = NavigationMode.Stationary;
            currentState = NPCState.Idle;
            isSittingPermanently = false;
            isJumpingInPlace = false;
            if (string.IsNullOrEmpty(characterDisplayName) || characterDisplayName == "NPC")
            {
                characterDisplayName = "Pelanggan Barber Shop";
            }
            if (string.IsNullOrEmpty(characterLine) || characterLine.Contains("patroli") || characterLine.Contains("selamat datang"))
            {
                characterLine = "Halo! Saya sedang menunggu antrean potong rambut di Barber Shop ini.";
            }
        }
        else if (isJumpingInPlace)
        {
            navMode = NavigationMode.Stationary;
            currentState = NPCState.Idle;
            isSittingPermanently = false;
            characterDisplayName = "Wanita Melompat";
            if (string.IsNullOrEmpty(characterLine) || characterLine.Contains("patroli") || characterLine.Contains("selamat datang"))
            {
                characterLine = "Halo! Lihat, aku melompat di tempat!\nAsyik sekali olahraga di trotoar ini!";
            }
        }
        else if (isSittingPermanently)
        {
            navMode = NavigationMode.Stationary;
            currentState = NPCState.Sitting;
            characterDisplayName = "Wanita Duduk";
            if (string.IsNullOrEmpty(characterLine) || characterLine.Contains("patroli") || characterLine.Contains("selamat datang"))
            {
                characterLine = "Halo! Senang sekali bisa bersantai di bangku taman ini.";
            }
        }
        else if (gameObject.name.Contains("Pimp"))
        {
            characterDisplayName = "Pria Pejalan Kaki";
            if (string.IsNullOrEmpty(characterLine) || characterLine.Contains("patroli") || characterLine.Contains("selamat datang"))
            {
                characterLine = "Halo bro! Selamat beraktivitas di kota. Tetap semangat!";
            }
        }

        // Setup Rigidbody & Collider yang aman
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider cc = gameObject.AddComponent<CapsuleCollider>();
            cc.height = 1.8f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.radius = 0.35f;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isDecidingNextMove = false;
        if (isInteractingWithPlayer)
        {
            isInteractingWithPlayer = false;
            ShowSpeechBubble(false);
            currentState = (waypoints != null && waypoints.Count > 0) ? NPCState.Walking : NPCState.Idle;
        }
    }

    private void Start()
    {
        startLocation = transform.position;
        groundY = transform.position.y;
        baseFacingRotation = transform.rotation;

        idleActions = new int[]
        {
            AnimCheckWatch,
            AnimCrossedArms,
            AnimHandOnHips,
            AnimWaveClip
        };

        // Buat UI Balon Percakapan jika belum ada
        EnsureSpeechBubbleUI();

        // Cari Pemain di scene
        FindPlayerTarget();

        // Inisialisasi waypoint jika mode WaypointPath
        if (navMode == NavigationMode.WaypointPath && waypointPath != null)
        {
            waypoints = waypointPath.GetWaypoints();
            if (waypoints == null || waypoints.Count < 2)
            {
                // Fallback ke FreeWander jika waypoint kurang
                navMode = NavigationMode.FreeWander;
            }
            else if (randomStartPoint)
            {
                currentWaypointIndex = Random.Range(0, waypoints.Count);
                transform.position = new Vector3(waypoints[currentWaypointIndex].position.x, groundY, waypoints[currentWaypointIndex].position.z);
            }
        }
        else if (navMode == NavigationMode.WaypointPath && waypointPath == null)
        {
            // Jika waypoint belum dipasang, otomatis keliling bebas (FreeWander)
            navMode = NavigationMode.FreeWander;
        }

        isInitialized = true;

        if (isLookingAroundInPlace)
        {
            currentState = NPCState.Idle;
            SetSpeed(0f);
            if (animator != null)
            {
                if (hasParamAnimationInt) animator.SetInteger(ParamAnimationInt, 0);
                if (hasParamStaticB) animator.SetBool(ParamStaticB, false);
                if (hasParamGrounded) animator.SetBool(ParamGrounded, true);
                if (hasParamJumpB) animator.SetBool(ParamJumpB, false);
            }
            return;
        }

        if (isJumpingInPlace)
        {
            currentState = NPCState.Idle;
            SetSpeed(0f);
            if (animator != null)
            {
                if (hasParamAnimationInt) animator.SetInteger(ParamAnimationInt, 0);
                if (hasParamStaticB) animator.SetBool(ParamStaticB, false);
                if (hasParamGrounded) animator.SetBool(ParamGrounded, true);
                if (hasParamJumpB) animator.SetBool(ParamJumpB, false);
            }
            return;
        }

        if (isSittingPermanently)
        {
            currentState = NPCState.Sitting;
            SetSpeed(0f);
            if (animator != null)
            {
                if (hasParamAnimationInt) animator.SetInteger(ParamAnimationInt, 9);
                if (hasParamStaticB) animator.SetBool(ParamStaticB, true);
                if (hasParamGrounded) animator.SetBool(ParamGrounded, true);
                if (animator.layerCount > 2) animator.Play("Idle_SittingOnGround", 2, 0f);
            }
            return;
        }

        // Jika mode WaypointPath, tentukan waypoint tujuan awal dan mulai bergerak
        if (navMode == NavigationMode.WaypointPath && waypoints != null && waypoints.Count > 1)
        {
            currentWaypointIndex = 1;
            direction = 1;
            currentDestination = waypoints[1].position;
            currentState = NPCState.Walking;
            currentSpeed = walkSpeed;
            SetSpeed(2.5f);
        }
        else
        {
            // Mulai dengan sedikit jeda acak untuk mode keliling bebas
            float initialDelay = Random.Range(0.1f, Mathf.Max(0.2f, maxStartDelay));
            StartCoroutine(StartInitialRoutine(initialDelay));
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 1. Cek Interaksi dengan Pemain
        if (enablePlayerInteraction)
        {
            CheckPlayerProximity();
        }

        if (isJumpingInPlace)
        {
            UpdateJumpingInPlace();
            return;
        }

        if (isLookingAroundInPlace)
        {
            UpdateLookingAroundInPlace();
            return;
        }

        // Jika sedang berinteraksi dengan pemain, abaikan pergerakan patroli
        if (isInteractingWithPlayer)
        {
            HandlePlayerInteraction();
            return;
        }

        if (isSittingPermanently)
        {
            if (animator != null && hasParamAnimationInt)
            {
                if (animator.GetInteger(ParamAnimationInt) != 9)
                {
                    animator.SetInteger(ParamAnimationInt, 9);
                }
            }
            return;
        }

        // 2. Pergerakan Berdasarkan Mode Navigasi
        if (currentState == NPCState.Walking || currentState == NPCState.Running)
        {
            MoveTowardsDestination();
        }
    }

    private void LateUpdate()
    {
        // Posisi dan tampilan dialog ditangani secara presisi di layar oleh NPCDialogueOverlay
    }

    // ==================== PLAYER PROXIMITY & DIALOGUE ====================

    private void FindPlayerTarget()
    {
        if (playerTarget != null) return;

        GameObject pObj = GameObject.FindWithTag("Player");
        if (pObj == null)
        {
            PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null) pObj = pc.gameObject;
        }
        if (pObj == null)
        {
            pObj = GameObject.Find("SimplePeople_StreetMan_Black") ?? GameObject.Find("Player");
        }

        if (pObj != null)
        {
            playerTarget = pObj.transform;
        }
    }

    private void CheckPlayerProximity()
    {
        if (playerTarget == null)
        {
            FindPlayerTarget();
            if (playerTarget == null) return;
        }

        float dist = Vector3.Distance(transform.position, playerTarget.position);

        if (!isInteractingWithPlayer && dist <= interactionDistance)
        {
            // Pemain masuk area interaksi!
            StartInteraction();
        }
        else if (isInteractingWithPlayer && dist > interactionDistance + 0.4f)
        {
            // Pemain menjauh dari area interaksi!
            StopInteraction();
        }
    }

    private void StartInteraction()
    {
        isInteractingWithPlayer = true;
        currentState = isSittingPermanently ? NPCState.Sitting : 
                       (isJumpingInPlace || isLookingAroundInPlace) ? NPCState.Idle : NPCState.InteractingWithPlayer;

        // Berhenti bergerak
        SetSpeed(0f);

        if (isSittingPermanently)
        {
            if (animator != null && hasParamAnimationInt)
            {
                animator.SetInteger(ParamAnimationInt, 9);
            }
        }
        else if (!isJumpingInPlace)
        {
            // Lambaikan tangan (Wave animation)
            TriggerWaveAnimation();
        }

        // Tampilkan balon dialog
        ShowSpeechBubble(true);
    }

    private void StopInteraction()
    {
        isInteractingWithPlayer = false;

        // Sembunyikan balon dialog
        ShowSpeechBubble(false);

        if (isJumpingInPlace)
        {
            currentState = NPCState.Idle;
            SetSpeed(0f);
        }
        else if (isLookingAroundInPlace)
        {
            currentState = NPCState.Idle;
            SetSpeed(0f);
        }
        else if (isSittingPermanently)
        {
            currentState = NPCState.Sitting;
            SetSpeed(0f);
            if (animator != null && hasParamAnimationInt)
            {
                animator.SetInteger(ParamAnimationInt, 9);
            }
        }
        else
        {
            if (gameObject.activeInHierarchy)
            {
                // Lambaikan tangan perpisahan sejenak lalu lanjutkan patroli
                TriggerWaveAnimation();
                StartCoroutine(ResumePatrolAfterDelay(1.2f));
            }
            else
            {
                currentState = (waypoints != null && waypoints.Count > 0) ? NPCState.Walking : NPCState.Idle;
            }
        }
    }

    private void HandlePlayerInteraction()
    {
        if (playerTarget == null) return;

        // Jika duduk permanen di kursi taman, jangan putar badan secara drastis
        if (isSittingPermanently) return;

        // Menoleh menghadap ke Pemain secara halus
        Vector3 dir = (playerTarget.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 6f * Time.deltaTime);
        }
    }

    private IEnumerator ResumePatrolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isInteractingWithPlayer)
        {
            PickNextMove();
        }
    }

    // ==================== MOVEMENT & NAVIGATION ====================

    private void MoveTowardsDestination()
    {
        Vector3 targetPos = new Vector3(currentDestination.x, groundY, currentDestination.z);
        Vector3 currentPos = transform.position;

        Vector3 moveDirection = (targetPos - currentPos).normalized;
        float dist = Vector2.Distance(new Vector2(currentPos.x, currentPos.z), new Vector2(targetPos.x, targetPos.z));

        if (moveDirection == Vector3.zero)
        {
            OnDestinationReached();
            return;
        }

        Vector3 finalMoveDirection = moveDirection;
        bool isPathBlocked = false;

        // 1. Deteksi Rintangan di Depan (Raycast / SphereCast Sensor)
        if (enableObstacleAvoidance)
        {
            Vector3 sensorOrigin = currentPos + Vector3.up * 0.09f;
            Collider myCol = GetComponent<Collider>();

            // Sensor Depan
            RaycastHit hitForward;
            if (Physics.SphereCast(sensorOrigin, bodyRadius, moveDirection, out hitForward, obstacleDetectionDistance, obstacleLayers))
            {
                if (hitForward.collider != myCol && !hitForward.collider.transform.IsChildOf(transform) && !hitForward.collider.isTrigger)
                {
                    // Cek jika rintangan adalah sesama NPC
                    NPCWalker otherWalker = hitForward.collider.GetComponent<NPCWalker>();
                    if (otherWalker != null && hitForward.distance < 0.22f)
                    {
                        StartCoroutine(DoYieldPause(0.8f));
                        return;
                    }

                    // Cek sensor samping (Whiskers 30 derajat) untuk menghindari rintangan
                    Vector3 leftDir = Quaternion.Euler(0, -30f, 0) * moveDirection;
                    Vector3 rightDir = Quaternion.Euler(0, 30f, 0) * moveDirection;

                    bool blockedLeft = Physics.SphereCast(sensorOrigin, bodyRadius, leftDir, out _, obstacleDetectionDistance * 0.75f, obstacleLayers);
                    bool blockedRight = Physics.SphereCast(sensorOrigin, bodyRadius, rightDir, out _, obstacleDetectionDistance * 0.75f, obstacleLayers);

                    if (!blockedRight)
                    {
                        finalMoveDirection = Vector3.Slerp(moveDirection, rightDir, 0.6f).normalized;
                    }
                    else if (!blockedLeft)
                    {
                        finalMoveDirection = Vector3.Slerp(moveDirection, leftDir, 0.6f).normalized;
                    }
                    else if (hitForward.distance < 0.12f)
                    {
                        isPathBlocked = true;
                    }
                }
            }
        }

        if (isPathBlocked)
        {
            SetSpeed(0f);
            if (navMode == NavigationMode.WaypointPath && waypoints != null && waypoints.Count > 1)
            {
                // Putar balik arah jika terhalang rintangan solid
                direction *= -1;
                AdvanceWaypoint();
                currentDestination = waypoints[currentWaypointIndex].position;
            }
            else
            {
                StartCoroutine(DoIdlePause());
            }
            return;
        }

        // Putar badan ke arah tujuan secara halus
        if (finalMoveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 2. Physics Sweep Test Sebelum Melangkah (Mencegah Menembus/Menabrak Benda di Sekitar)
        float stepDist = currentSpeed * Time.deltaTime;
        Vector3 step = finalMoveDirection * stepDist;
        Vector3 rayStart = transform.position + Vector3.up * 0.09f;

        RaycastHit sweepHit;
        if (Physics.SphereCast(rayStart, bodyRadius * 0.85f, finalMoveDirection, out sweepHit, stepDist + 0.02f, obstacleLayers))
        {
            if (sweepHit.collider != GetComponent<Collider>() && !sweepHit.collider.transform.IsChildOf(transform) && !sweepHit.collider.isTrigger)
            {
                // Geser / slide sepanjang permukaan rintangan
                Vector3 slideDir = Vector3.ProjectOnPlane(finalMoveDirection, sweepHit.normal).normalized;
                if (Vector3.Dot(slideDir, finalMoveDirection) > 0.1f)
                {
                    step = slideDir * stepDist * 0.5f;
                }
                else
                {
                    step = Vector3.zero; // Berhenti jika tegak lurus
                }
            }
        }

        // Geser posisi maju
        transform.position += step;

        // Jaga ketinggian kaki tetap pas di permukaan tanah / trotoar (tidak melayang)
        RaycastHit[] gHits = Physics.RaycastAll(transform.position + Vector3.up * 0.05f, Vector3.down, 0.25f);
        bool foundNpcGround = false;
        float npcGroundHeight = 0f;
        foreach (var gh in gHits)
        {
            if (gh.collider == null || gh.collider.isTrigger || gh.collider == GetComponent<Collider>() || gh.collider.transform.IsChildOf(transform)) continue;
            if (!foundNpcGround || gh.point.y > npcGroundHeight)
            {
                npcGroundHeight = gh.point.y;
                foundNpcGround = true;
            }
        }
        if (foundNpcGround) groundY = npcGroundHeight;
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);

        // Cek apakah sudah sampai di tujuan waypoint
        if (dist <= waypointReachDistance)
        {
            OnDestinationReached();
        }
    }

    private void OnDestinationReached()
    {
        SetSpeed(0f);
        currentState = NPCState.Idle;

        if (navMode == NavigationMode.Stationary)
        {
            StartCoroutine(DoIdlePause());
            return;
        }

        // Jika mode WaypointPath di ujung jalur PingPong: jeda sejenak lalu berputar balik
        if (navMode == NavigationMode.WaypointPath && waypoints != null && waypoints.Count > 1)
        {
            bool isAtEndpoint = (direction == 1 && currentWaypointIndex >= waypoints.Count - 1) ||
                                (direction == -1 && currentWaypointIndex <= 0);

            if (isAtEndpoint && turnPauseTime > 0.05f)
            {
                StartCoroutine(DoTurnAroundPause(turnPauseTime));
                return;
            }
        }

        // Tentukan apakah duduk, berhenti sejenak, atau langsung jalan lagi
        float roll = Random.value;
        if (roll < sitChance)
        {
            StartCoroutine(DoSitPause());
        }
        else if (roll < sitChance + idleChance)
        {
            StartCoroutine(DoIdlePause());
        }
        else
        {
            PickNextMove();
        }
    }

    private IEnumerator DoTurnAroundPause(float delay)
    {
        if (isDecidingNextMove) yield break;
        isDecidingNextMove = true;
        SetSpeed(0f);
        currentState = NPCState.Idle;
        yield return new WaitForSeconds(delay);
        isDecidingNextMove = false;
        PickNextMove();
    }

    private IEnumerator DoYieldPause(float duration)
    {
        if (isDecidingNextMove) yield break;
        isDecidingNextMove = true;
        SetSpeed(0f);
        currentState = NPCState.Idle;
        yield return new WaitForSeconds(duration);
        isDecidingNextMove = false;
    }

    private void PickNextMove()
    {
        if (isInteractingWithPlayer) return;

        if (navMode == NavigationMode.Stationary || isSittingPermanently)
        {
            SetSpeed(0f);
            if (isSittingPermanently)
            {
                currentState = NPCState.Sitting;
                if (hasParamAnimationInt && animator != null) animator.SetInteger(ParamAnimationInt, 9);
            }
            else
            {
                currentState = NPCState.Idle;
            }
            return;
        }

        // Reset parameter animasi santai saat mulai bergerak
        if (hasParamAnimationInt && animator != null)
        {
            animator.SetInteger(ParamAnimationInt, 0);
        }

        if (navMode == NavigationMode.WaypointPath && waypoints != null && waypoints.Count > 1)
        {
            // Ambil waypoint berikutnya
            AdvanceWaypoint();
            currentDestination = waypoints[currentWaypointIndex].position;
        }
        else
        {
            // Free Wander: Pilih titik acak di sekitar lokasi awal
            currentDestination = SelectRandomWanderPosition();
        }

        // Tentukan jalan atau lari
        float runRoll = Random.value;
        if (runRoll < runChance)
        {
            currentState = NPCState.Running;
            currentSpeed = runSpeed;
            SetSpeed(8.0f); // Nilai Speed_f untuk lari (seperti di video)
        }
        else
        {
            currentState = NPCState.Walking;
            currentSpeed = walkSpeed;
            SetSpeed(2.5f); // Nilai Speed_f untuk jalan santai
        }
    }

    private void AdvanceWaypoint()
    {
        bool reachedEnd = false;
        if (direction == 1 && currentWaypointIndex >= waypoints.Count - 1) reachedEnd = true;
        else if (direction == -1 && currentWaypointIndex <= 0) reachedEnd = true;

        if (reachedEnd)
        {
            switch (endBehavior)
            {
                case EndBehavior.PingPong:
                    direction *= -1;
                    break;
                case EndBehavior.Loop:
                    currentWaypointIndex = 0;
                    return;
                case EndBehavior.Stop:
                    navMode = NavigationMode.Stationary;
                    return;
            }
        }

        currentWaypointIndex += direction;
        currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Count - 1);
    }

    private Vector3 SelectRandomWanderPosition()
    {
        // Jika sudah terlalu jauh dari titik awal, arahkan kembali pulang
        if (Vector3.Distance(transform.position, startLocation) > maxDistanceFromStart)
        {
            return startLocation;
        }

        // Pilih arah acak dalam radius
        Vector2 circle = Random.insideUnitCircle * wanderRadius;
        Vector3 target = transform.position + new Vector3(circle.x, 0, circle.y);

        // Pastikan tidak melebihi maxDistanceFromStart dari lokasi awal
        if (Vector3.Distance(target, startLocation) > maxDistanceFromStart)
        {
            target = startLocation + (target - startLocation).normalized * (wanderRadius * 0.8f);
        }

        target.y = groundY;
        return target;
    }

    private IEnumerator DoIdlePause()
    {
        if (isDecidingNextMove) yield break;
        isDecidingNextMove = true;

        SetSpeed(0f);
        currentState = NPCState.Idle;

        // Pilih variasi animasi santai (cek jam, silang tangan, dll.)
        PlayRandomIdleAction();

        float pauseTime = Random.Range(decisionPauseTime.x, decisionPauseTime.y);
        yield return new WaitForSeconds(pauseTime);

        if (hasParamAnimationInt && animator != null)
        {
            animator.SetInteger(ParamAnimationInt, 0);
        }

        isDecidingNextMove = false;

        if (!isInteractingWithPlayer)
        {
            PickNextMove();
        }
    }

    private IEnumerator DoSitPause()
    {
        if (isDecidingNextMove) yield break;
        isDecidingNextMove = true;

        SetSpeed(0f);
        currentState = NPCState.Sitting;

        // Trigger duduk
        if (hasParamSit)
        {
            animator.SetTrigger(ParamSit);
        }
        else if (hasParamAnimationInt)
        {
            animator.SetInteger(ParamAnimationInt, 9);
        }
        else if (animator.HasState(0, AnimSitting))
        {
            animator.CrossFade(AnimSitting, 0.3f, 0);
        }

        float sitDuration = Random.Range(4f, 10f);
        yield return new WaitForSeconds(sitDuration);

        // Berdiri kembali
        if (hasParamStand)
        {
            animator.SetTrigger(ParamStand);
        }
        else if (hasParamAnimationInt)
        {
            animator.SetInteger(ParamAnimationInt, 0);
        }
        else if (animator.HasState(0, AnimIdle))
        {
            animator.CrossFade(AnimIdle, 0.3f, 0);
        }

        yield return new WaitForSeconds(1f);
        isDecidingNextMove = false;

        if (!isInteractingWithPlayer)
        {
            PickNextMove();
        }
    }

    private IEnumerator StartInitialRoutine(float delay)
    {
        SetSpeed(0f);
        if (isJumpingInPlace)
        {
            currentState = NPCState.Idle;
            yield break;
        }
        if (isSittingPermanently)
        {
            currentState = NPCState.Sitting;
            if (animator != null && hasParamAnimationInt) animator.SetInteger(ParamAnimationInt, 9);
            yield break;
        }
        currentState = NPCState.Idle;
        yield return new WaitForSeconds(delay);
        PickNextMove();
    }

    // ==================== ANIMATION HELPERS ====================

    private void CacheAnimatorParameters()
    {
        if (animator == null) return;

        foreach (var p in animator.parameters)
        {
            if (p.nameHash == ParamSpeedF) hasParamSpeedF = true;
            else if (p.nameHash == ParamGrounded) hasParamGrounded = true;
            else if (p.nameHash == ParamGroundedB) hasParamGroundedB = true;
            else if (p.nameHash == ParamStaticB) hasParamStaticB = true;
            else if (p.nameHash == ParamAnimationInt) hasParamAnimationInt = true;
            else if (p.nameHash == ParamWave) hasParamWave = true;
            else if (p.nameHash == ParamSit) hasParamSit = true;
            else if (p.nameHash == ParamStand) hasParamStand = true;
            else if (p.nameHash == ParamJumpB) hasParamJumpB = true;
            else if (p.nameHash == ParamHeadHorizontalF) hasParamHeadHorizontalF = true;
        }

        if (hasParamGrounded) animator.SetBool(ParamGrounded, true);
        if (hasParamGroundedB) animator.SetBool(ParamGroundedB, true);
        if (hasParamStaticB) animator.SetBool(ParamStaticB, isSittingPermanently);
    }

    /// <summary>
    /// Logika melompat di tempat terus-menerus dengan animasi Standing_Jump dan kurva loncatan parabola.
    /// </summary>
    private void UpdateJumpingInPlace()
    {
        // Jika sedang berinteraksi dengan pemain, hadapkan badan ke arah pemain
        if (isInteractingWithPlayer && playerTarget != null)
        {
            Vector3 lookDir = playerTarget.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
            }
        }

        // Timer siklus lompatan
        if (!isMidAir)
        {
            jumpTimer += Time.deltaTime;
            if (jumpTimer >= jumpInterval)
            {
                jumpTimer = 0f;
                isMidAir = true;
                airTime = 0f;

                if (animator != null)
                {
                    if (hasParamGrounded) animator.SetBool(ParamGrounded, false);
                    if (hasParamGroundedB) animator.SetBool(ParamGroundedB, false);
                    if (hasParamJumpB) animator.SetBool(ParamJumpB, true);
                    if (animator.layerCount > 3)
                    {
                        animator.Play("Standing_Jump", 3, 0f);
                    }
                }
            }
        }
        else
        {
            airTime += Time.deltaTime;
            if (airTime < JumpAirDuration)
            {
                // Parabola lompatan: t bernilai 0..1
                float t = airTime / JumpAirDuration;
                float hop = 4f * jumpInPlaceHeight * t * (1f - t);
                transform.position = new Vector3(startLocation.x, groundY + hop, startLocation.z);
            }
            else
            {
                // Mendarat kembali ke trotoar
                isMidAir = false;
                transform.position = new Vector3(startLocation.x, groundY, startLocation.z);

                if (animator != null)
                {
                    if (hasParamGrounded) animator.SetBool(ParamGrounded, true);
                    if (hasParamGroundedB) animator.SetBool(ParamGroundedB, true);
                    if (hasParamJumpB) animator.SetBool(ParamJumpB, false);
                }
            }
        }
    }

    /// <summary>
    /// Logika karakter berdiri diam di tempat dan menengok kiri-kanan secara alami tanpa berjalan.
    /// Memadukan perputaran halus badan dan perputaran kepala (Head_Horizontal_f) pada Animator.
    /// </summary>
    private void UpdateLookingAroundInPlace()
    {
        // 1. Kunci posisi agar 100% diam di tempat (tidak berjalan / bergeser)
        transform.position = new Vector3(startLocation.x, groundY, startLocation.z);
        SetSpeed(0f);

        if (baseFacingRotation == Quaternion.identity)
        {
            baseFacingRotation = transform.rotation;
        }

        // 2. Jika sedang berinteraksi dengan pemain: hadapkan badan ke pemain
        if (isInteractingWithPlayer && playerTarget != null)
        {
            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 6f * Time.deltaTime);
            }

            // Kembalikan arah kepala ke tengah (lurus ke pemain)
            currentHeadTurn = Mathf.MoveTowards(currentHeadTurn, 0f, 3f * Time.deltaTime);
            if (animator != null && hasParamHeadHorizontalF)
            {
                animator.SetFloat(ParamHeadHorizontalF, currentHeadTurn);
            }
            return;
        }

        // 3. Siklus menengok: Tengah -> Kiri -> Tengah -> Kanan -> Tengah
        switch (currentLookStage)
        {
            case LookStage.CenterToLeft:
                targetLookAngle = -lookAroundAngle;
                targetHeadTurn = -1.0f;
                if (Mathf.Abs(currentLookAngle - targetLookAngle) < 0.5f)
                {
                    currentLookStage = LookStage.PauseLeft;
                    lookStageTimer = Random.Range(lookPauseDuration.x, lookPauseDuration.y);
                }
                break;

            case LookStage.PauseLeft:
                lookStageTimer -= Time.deltaTime;
                if (lookStageTimer <= 0f)
                {
                    currentLookStage = LookStage.LeftToCenter;
                }
                break;

            case LookStage.LeftToCenter:
                targetLookAngle = 0f;
                targetHeadTurn = 0f;
                if (Mathf.Abs(currentLookAngle) < 0.5f)
                {
                    currentLookStage = LookStage.PauseCenter1;
                    lookStageTimer = Random.Range(lookPauseDuration.x, lookPauseDuration.y);
                }
                break;

            case LookStage.PauseCenter1:
                lookStageTimer -= Time.deltaTime;
                if (lookStageTimer <= 0f)
                {
                    currentLookStage = LookStage.CenterToRight;
                }
                break;

            case LookStage.CenterToRight:
                targetLookAngle = lookAroundAngle;
                targetHeadTurn = 1.0f;
                if (Mathf.Abs(currentLookAngle - targetLookAngle) < 0.5f)
                {
                    currentLookStage = LookStage.PauseRight;
                    lookStageTimer = Random.Range(lookPauseDuration.x, lookPauseDuration.y);
                }
                break;

            case LookStage.PauseRight:
                lookStageTimer -= Time.deltaTime;
                if (lookStageTimer <= 0f)
                {
                    currentLookStage = LookStage.RightToCenter;
                }
                break;

            case LookStage.RightToCenter:
                targetLookAngle = 0f;
                targetHeadTurn = 0f;
                if (Mathf.Abs(currentLookAngle) < 0.5f)
                {
                    currentLookStage = LookStage.PauseCenter2;
                    lookStageTimer = Random.Range(lookPauseDuration.x, lookPauseDuration.y);
                }
                break;

            case LookStage.PauseCenter2:
                lookStageTimer -= Time.deltaTime;
                if (lookStageTimer <= 0f)
                {
                    currentLookStage = LookStage.CenterToLeft;
                }
                break;
        }

        // Pergerakan rotasi badan halus
        currentLookAngle = Mathf.MoveTowards(currentLookAngle, targetLookAngle, lookAroundSpeed * Time.deltaTime);
        transform.rotation = baseFacingRotation * Quaternion.Euler(0f, currentLookAngle, 0f);

        // Pergerakan kepala halus
        float headSpeed = (lookAroundSpeed / Mathf.Max(10f, lookAroundAngle)) * 1.5f;
        currentHeadTurn = Mathf.MoveTowards(currentHeadTurn, targetHeadTurn, headSpeed * Time.deltaTime);

        if (animator != null && hasParamHeadHorizontalF)
        {
            animator.SetFloat(ParamHeadHorizontalF, currentHeadTurn);
        }
    }

    private void SetSpeed(float speedValue)
    {
        if (animator != null && hasParamSpeedF)
        {
            animator.SetFloat(ParamSpeedF, speedValue);
        }
    }

    public void TriggerWaveAnimation()
    {
        if (animator == null) return;

        if (hasParamWave)
        {
            animator.SetTrigger(ParamWave);
        }
        else if (animator.HasState(0, AnimWaveClip))
        {
            animator.CrossFade(AnimWaveClip, 0.2f, 0);
        }
        else if (animator.HasState(0, AnimCrossedArms))
        {
            animator.CrossFade(AnimCrossedArms, 0.2f, 0);
        }
    }

    private void PlayRandomIdleAction()
    {
        if (animator == null) return;

        // Jika animator memiliki parameter Animation_int (seperti SimpleCharacter_5.0):
        // 0: Normal Idle, 1: Crossed Arms, 2: Hands on Hips, 3: Check Watch, 7: Wipe Mouth, 8: Lean against wall
        if (hasParamAnimationInt)
        {
            int[] idleInts = new int[] { 0, 1, 2, 3, 7, 8 };
            int chosen = idleInts[Random.Range(0, idleInts.Length)];
            animator.SetInteger(ParamAnimationInt, chosen);
            return;
        }

        if (idleActions == null || idleActions.Length == 0) return;

        // Hanya jalankan animasi yang benar-benar ada di Controller untuk menghindari warning
        List<int> validActions = new List<int>();
        for (int i = 0; i < idleActions.Length; i++)
        {
            if (animator.HasState(0, idleActions[i]))
            {
                validActions.Add(idleActions[i]);
            }
        }

        if (validActions.Count > 0)
        {
            int randomAction = validActions[Random.Range(0, validActions.Count)];
            animator.CrossFade(randomAction, 0.25f, 0);
        }
    }

    // ==================== SPEECH BUBBLE UI BUILDER ====================

    public void EnsureSpeechBubbleUI()
    {
        // Bersihkan child kanvas 3D lama agar tidak mengganggu pemandangan di scene
        Transform existing = transform.Find("SpeechBubbleCanvas");
        if (existing != null)
        {
            Destroy(existing.gameObject);
            speechBubbleCanvas = null;
            speechBubbleText = null;
        }

        // Pastikan overlay dialog beresolusi tinggi di layar telah aktif
        NPCDialogueOverlay.EnsureInstance();
    }

    private void ShowSpeechBubble(bool show)
    {
        // Tampilkan balon dialog di atas kepala dan subtitle di layar secara tajam dan jernih
        NPCDialogueOverlay.ShowDialogue(this, characterDisplayName, characterLine, show);
    }

    private void OnDestroy()
    {
        if (isInteractingWithPlayer)
        {
            isInteractingWithPlayer = false;
            ShowSpeechBubble(false);
        }
    }
}
