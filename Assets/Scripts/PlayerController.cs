using UnityEngine;

/// <summary>
/// Robust Player Controller for exploring the city.
/// Supports W, A, S, D and Arrow keys for movement, Shift to sprint.
/// Works reliably without physics/CharacterController bugs.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("=== Movement Settings ===")]
    [Tooltip("Walking speed in world units (Skala 0.1).")]
    public float walkSpeed = 0.45f;

    [Tooltip("Sprint speed when holding Left Shift (Skala 0.1).")]
    public float sprintSpeed = 0.95f;

    [Tooltip("How fast the character rotates towards movement direction.")]
    public float rotationSpeed = 12f;

    [Tooltip("Jump height.")]
    public float jumpHeight = 0.25f;

    [Header("=== References ===")]
    [Tooltip("Reference to the camera to move relative to view direction.")]
    public Camera playerCamera;

    private Animator animator;
    private float verticalVelocity = 0f;
    private bool isGrounded = true;
    private const float gravity = -9.81f;

    // Animator parameter hashes for SimpleCharacter_5.0
    private static readonly int ParamSpeedF = Animator.StringToHash("Speed_f");
    private static readonly int ParamGrounded = Animator.StringToHash("Grounded");
    private static readonly int ParamStaticB = Animator.StringToHash("Static_b");
    private static readonly int ParamJumpB = Animator.StringToHash("Jump_b");
    private static readonly int ParamAnimationInt = Animator.StringToHash("Animation_int");

    [Header("=== Collision & Obstacle Settings ===")]
    [Tooltip("Radius kapsul tabrakan badan karakter.")]
    public float bodyRadius = 0.035f;

    [Tooltip("Tinggi kapsul tabrakan badan karakter.")]
    public float bodyHeight = 0.18f;

    [Tooltip("Tinggi maksimal undakan/trotoar yang bisa dinaiki otomatis.")]
    public float maxStepHeight = 0.06f;

    [Tooltip("Jarak aman permukaan untuk mencegah rounding error.")]
    public float skinWidth = 0.003f;

    [Tooltip("Layer rintangan yang tidak bisa ditembus.")]
    public LayerMask obstacleMask = ~0;

    [Header("=== State ===")]
    [Tooltip("Is the character currently sitting (Toggle with C or X key).")]
    public bool isSitting = false;

    private CapsuleCollider ownCapsule;
    private Collider[] ownColliders;
    private string lastBlockedObstacle = null;
    private float lastBlockedTime = -999f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ownCapsule = GetComponent<CapsuleCollider>();
        ownColliders = GetComponentsInChildren<Collider>();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Matikan CharacterController bawaan jika ada agar tidak bentrok
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        // Sesuaikan ukuran kapsul otomatis dengan skala karakter
        if (ownCapsule != null)
        {
            float scaleX = Mathf.Abs(transform.lossyScale.x);
            float scaleY = Mathf.Abs(transform.lossyScale.y);
            bodyRadius = Mathf.Max(0.035f, ownCapsule.radius * scaleX);
            bodyHeight = Mathf.Max(0.16f, ownCapsule.height * scaleY);
        }

        if (animator != null)
        {
            animator.SetBool(ParamGrounded, true);
            animator.SetBool(ParamStaticB, true);
        }
    }

    private void Start()
    {
        SnapToGround();
        ResolveDepenetration();
    }

    private void Update()
    {
        HandleMovement();
    }

    private void OnDisable()
    {
        if (animator != null)
        {
            animator.SetFloat(ParamSpeedF, 0f);
        }
    }

    private void HandleMovement()
    {
        // Kunci input jika sedang kuis atau transisi portal
        if (!InputManager.bCanPlayerMove)
        {
            if (animator != null)
            {
                animator.SetFloat(ParamSpeedF, 0f);
            }
            return;
        }

        // 1. Read input axes (with direct KeyCode fallback for 100% responsiveness)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical = -1f;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 2. Calculate movement direction relative to camera
        Vector3 moveDirection = Vector3.zero;

        if (playerCamera != null)
        {
            Vector3 camForward = playerCamera.transform.forward;
            Vector3 camRight = playerCamera.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = (camForward * vertical + camRight * horizontal).normalized;
        }
        else
        {
            moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        }

        // 0. Sitting toggle (Key C or Key X)
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.X))
        {
            isSitting = !isSitting;
            if (isSitting)
            {
                verticalVelocity = 0f;
            }
            if (animator != null)
            {
                animator.SetInteger(ParamAnimationInt, isSitting ? 9 : 0);
            }
        }

        float inputMagnitude = new Vector2(horizontal, vertical).magnitude;
        inputMagnitude = Mathf.Clamp01(inputMagnitude);

        // If sitting and player presses movement or jump, automatically stand up
        if (isSitting && (inputMagnitude > 0.05f || Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)))
        {
            isSitting = false;
            if (animator != null)
            {
                animator.SetInteger(ParamAnimationInt, 0);
            }
        }

        // If currently sitting, stay in place and keep sitting animation
        if (isSitting)
        {
            if (animator != null)
            {
                animator.SetFloat(ParamSpeedF, 0f);
                animator.SetInteger(ParamAnimationInt, 9);
                animator.SetBool(ParamGrounded, true);
                animator.SetBool(ParamStaticB, true);
            }
            return;
        }

        // 3. Determine current speed
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        float currentMoveSpeed = targetSpeed * inputMagnitude;

        // 4. Smooth rotation towards movement direction
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Vector3 facingDirection = moveDirection;

            // Jika berjalan mundur (tombol S), tetap menghadap ke depan (searah pandang kamera)
            // agar kamera di belakang tetap stabil dan tidak berputar bolak-balik 180 derajat
            if (vertical < -0.1f && playerCamera != null)
            {
                Vector3 forwardRef = playerCamera.transform.forward;
                forwardRef.y = 0f;
                if (forwardRef.sqrMagnitude > 0.001f)
                {
                    Vector3 sideOffset = playerCamera.transform.right * horizontal * 0.5f;
                    facingDirection = (forwardRef + sideOffset).normalized;
                }
            }

            Quaternion targetRot = Quaternion.LookRotation(facingDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // 5. Jump & Gravity
        if (isGrounded)
        {
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                isGrounded = false;
                if (animator != null) animator.SetBool(ParamJumpB, true);
            }
            else
            {
                if (animator != null) animator.SetBool(ParamJumpB, false);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 6. Anti-Tembus Dinding: Selesaikan penetrasi sebelum bergerak
        ResolveDepenetration();

        // 7. Pergerakan Horizontal dengan Swept Capsule & Wall Sliding
        Vector3 horizontalStep = moveDirection * currentMoveSpeed * Time.deltaTime;
        if (horizontalStep.sqrMagnitude > 0.0000001f)
        {
            MoveWithCollision(horizontalStep);
        }

        // 8. Pergerakan Vertikal (Lompat / Gravitasi) dengan cek benturan kepala
        if (Mathf.Abs(verticalVelocity) > 0.0001f)
        {
            if (verticalVelocity > 0f)
            {
                // Cek jika kepala membentur atap
                if (Physics.SphereCast(transform.position + Vector3.up * (bodyHeight * 0.5f), bodyRadius, Vector3.up, out RaycastHit ceilHit, (bodyHeight * 0.5f) + 0.01f, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    if (!ceilHit.collider.isTrigger && !ceilHit.collider.transform.IsChildOf(transform))
                    {
                        verticalVelocity = 0f;
                    }
                }
            }
            transform.position += Vector3.up * (verticalVelocity * Time.deltaTime);
        }

        // 9. Ground Snap (Menempel di tanah/trotoar secara akurat)
        SnapToGround();

        // 10. Anti-Tembus Dinding: Selesaikan penetrasi setelah bergerak (100% aman)
        ResolveDepenetration();

        // 11. Update Animator parameters (Speed_f controls Walk & Run)
        if (animator != null)
        {
            animator.SetBool(ParamGrounded, isGrounded);
            animator.SetBool(ParamStaticB, true);

            float animSpeed = 0f;
            if (inputMagnitude > 0.05f)
            {
                animSpeed = isSprinting ? 1.0f : 0.45f;
            }

            animator.SetFloat(ParamSpeedF, animSpeed);
        }
    }

    /// <summary>
    /// Menggerakkan karakter dengan swept-capsule cast dan pantulan geser (wall slide).
    /// Menjamin 100% karakter tidak bisa menembus dinding, planter, pagar, pohon, atau bangunan apapun.
    /// </summary>
    private void MoveWithCollision(Vector3 horizontalStep)
    {
        Vector3 moveRemaining = horizontalStep;
        int maxBounces = 3;

        for (int b = 0; b < maxBounces; b++)
        {
            if (moveRemaining.sqrMagnitude < 0.0000001f) break;

            float moveDist = moveRemaining.magnitude;
            Vector3 moveDir = moveRemaining / moveDist;

            // Titik kapsul bawah & atas
            float r = bodyRadius;
            float bottomY = maxStepHeight + r;
            float topY = Mathf.Max(bottomY + 0.02f, bodyHeight - r);

            Vector3 pt1 = transform.position + Vector3.up * bottomY;
            Vector3 pt2 = transform.position + Vector3.up * topY;

            RaycastHit hit;
            bool hasHit = Physics.CapsuleCast(
                pt1, pt2, r, moveDir, out hit,
                moveDist + skinWidth, obstacleMask, QueryTriggerInteraction.Ignore
            );

            // Abaikan collider milik diri sendiri atau trigger
            if (hasHit && (hit.collider.isTrigger || hit.collider == ownCapsule || hit.collider.transform.IsChildOf(transform)))
            {
                hasHit = false;
            }

            if (hasHit)
            {
                lastBlockedObstacle = hit.collider.gameObject.name;
                lastBlockedTime = Time.time;

                // Maju sampai batas aman tepat sebelum dinding
                float safeDist = Mathf.Max(0f, hit.distance - skinWidth);
                transform.position += moveDir * safeDist;

                float distLeft = moveDist - safeDist;

                // Normal dinding di bidang horizontal
                Vector3 wallNormal = hit.normal;
                wallNormal.y = 0f;
                if (wallNormal.sqrMagnitude > 0.001f)
                {
                    wallNormal.Normalize();
                }
                else
                {
                    wallNormal = -moveDir;
                }

                // Geser ke samping permukaan dinding (sliding)
                Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, wallNormal);
                slideDir.y = 0f;

                if (slideDir.sqrMagnitude > 0.001f)
                {
                    slideDir.Normalize();
                    float forwardProjection = Vector3.Dot(moveDir, slideDir);
                    if (forwardProjection > 0.1f)
                    {
                        moveRemaining = slideDir * (distLeft * forwardProjection);
                    }
                    else
                    {
                        moveRemaining = Vector3.zero;
                    }
                }
                else
                {
                    moveRemaining = Vector3.zero;
                }
            }
            else
            {
                transform.position += moveRemaining;
                break;
            }
        }
    }

    /// <summary>
    /// Mendeteksi jika karakter sedikit menembus/bersentuhan dengan objek lain
    /// dan secara matematis mendorong keluar ke batas luar (0% tembus dinding/planter/gedung).
    /// </summary>
    private void ResolveDepenetration()
    {
        if (ownCapsule == null) ownCapsule = GetComponent<CapsuleCollider>();
        if (ownCapsule == null) return;

        Vector3 worldCenter = transform.TransformPoint(ownCapsule.center);
        float worldHeight = ownCapsule.height * Mathf.Abs(transform.lossyScale.y);

        Collider[] overlaps = Physics.OverlapSphere(worldCenter, worldHeight * 0.75f, obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider other = overlaps[i];
            if (other == null || other.isTrigger || other == ownCapsule || other.transform.IsChildOf(transform))
                continue;

            // Cek apakah collider berada di level lantai/tanah/trotoar rendah
            bool isGroundLevel = other.bounds.max.y <= transform.position.y + maxStepHeight + 0.015f;

            if (Physics.ComputePenetration(
                ownCapsule, transform.position, transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 direction, out float distance))
            {
                if (distance > 0.0001f)
                {
                    // Penetrasi ke lantai / trotoar / tanah / undakan rendah -> dorong ke atas
                    if (isGroundLevel || direction.y > 0.65f)
                    {
                        transform.position += Vector3.up * distance;
                        if (verticalVelocity < 0f) verticalVelocity = 0f;
                        isGrounded = true;
                    }
                    else if (Mathf.Abs(direction.y) < 0.65f)
                    {
                        // Dorong keluar secara horizontal jika menabrak dinding / planter tinggi / bangunan
                        Vector3 push = new Vector3(direction.x, 0f, direction.z).normalized * (distance + 0.001f);
                        transform.position += push;
                        lastBlockedObstacle = other.gameObject.name;
                        lastBlockedTime = Time.time;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Memastikan kaki karakter selalu menempel sempurna pada permukaan tanah / trotoar (tidak melayang).
    /// Tidak menempel pada dinding vertikal atau objek tinggi seperti planter / pagar.
    /// </summary>
    private void SnapToGround()
    {
        if (verticalVelocity > 0.01f)
        {
            isGrounded = false;
            return;
        }

        if (ownColliders == null || ownColliders.Length == 0)
        {
            ownColliders = GetComponentsInChildren<Collider>();
        }

        float rayStartOffset = Mathf.Max(0.08f, maxStepHeight + 0.04f);
        float rayDistance = rayStartOffset + 0.15f;

        RaycastHit[] hits = Physics.RaycastAll(
            transform.position + Vector3.up * rayStartOffset,
            Vector3.down, rayDistance, obstacleMask, QueryTriggerInteraction.Ignore
        );

        bool foundGround = false;
        float highestGroundY = -9999f;

        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;

            // Abaikan collider tubuh sendiri
            bool isSelf = false;
            for (int i = 0; i < ownColliders.Length; i++)
            {
                if (h.collider == ownColliders[i]) { isSelf = true; break; }
            }
            if (isSelf) continue;

            // Hanya anggap permukaan tanah jika cukup datar (normal.y > 0.55f)
            // Ini mencegah karakter 'menempel' di dinding vertikal planter atau tembok
            if (h.normal.y < 0.55f) continue;

            if (h.point.y > highestGroundY)
            {
                highestGroundY = h.point.y;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            float currentY = transform.position.y;
            float diff = highestGroundY - currentY;

            // Naik trotoar atau turun undakan
            if (diff > -0.1f && diff <= maxStepHeight + 0.01f)
            {
                transform.position = new Vector3(transform.position.x, highestGroundY, transform.position.z);
                verticalVelocity = 0f;
                isGrounded = true;
            }
            else if (diff <= -0.1f)
            {
                isGrounded = false;
            }
        }
        else
        {
            // Fallback permukaan aspal jalan raya kota (Y = 0.0f)
            if (transform.position.y <= 0.08f && verticalVelocity <= 0f)
            {
                transform.position = new Vector3(transform.position.x, 0.0f, transform.position.z);
                verticalVelocity = 0f;
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }
    }

    private void OnGUI()
    {
        if (Time.time - lastBlockedTime < 1.5f && !string.IsNullOrEmpty(lastBlockedObstacle))
        {
            GUIStyle blockStyle = new GUIStyle(GUI.skin.box);
            blockStyle.fontSize = 14;
            blockStyle.fontStyle = FontStyle.Bold;
            blockStyle.normal.textColor = new Color(1f, 0.45f, 0.45f);
            blockStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Box(new Rect(12, 92, 390, 32), $"⛔ Menabrak Rintangan: {lastBlockedObstacle}", blockStyle);
        }
    }
}
