using System;
using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;

/// <summary>
/// Triggers a cutscene Timeline when the player enters the trigger zone (Module 8).
/// Disables player movement during cinematic playback and restores it on completion.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CutsceneTrigger : MonoBehaviour
{
    [Header("=== Cutscene Configuration ===")]
    [Tooltip("PlayableDirector that controls the cutscene Timeline.")]
    public PlayableDirector cutsceneDirector;

    [Tooltip("Whether to automatically start the cutscene when the game starts (as shown in the YouTube video).")]
    public bool playOnStart = false;

    [Tooltip("Whether the cutscene should only trigger once.")]
    public bool playOnce = true;

    [Tooltip("Tag to identify the player GameObject.")]
    public string playerTag = "Player";

    [Header("=== Optional UI Elements ===")]
    [Tooltip("Optional banner or canvas GameObject to deactivate when cutscene finishes.")]
    public GameObject introBanner;

    private bool hasPlayed = false;
    private GameObject playerObject;
    private PlayerController playerController;
    private MovableObjectMotor objectMotor;
    private ThirdPersonCamera cachedThirdPersonCamera;
    private CinemachineBrain cachedCinemachineBrain;

    private void Awake()
    {
        if (cutsceneDirector == null)
        {
            cutsceneDirector = GetComponent<PlayableDirector>();
        }

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        EnsurePlayerHasController();

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain != null && !playOnStart)
            {
                brain.enabled = false;
            }
        }

        if (playOnStart && !hasPlayed)
        {
            StartCutscene();
        }
    }

    /// <summary>
    /// Ensures the player GameObject has the PlayerController component for WASD/Space navigation.
    /// </summary>
    public void EnsurePlayerHasController()
    {
        if (playerObject == null)
        {
            playerObject = GameObject.FindWithTag(playerTag);
        }

        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = playerObject.AddComponent<PlayerController>();
                playerController.walkSpeed = 4.5f;
                playerController.sprintSpeed = 8.0f;
                playerController.jumpHeight = 1.2f;
                playerController.bodyRadius = 0.35f;
                playerController.bodyHeight = 1.8f;
                playerController.maxStepHeight = 0.45f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other != null ? other.gameObject : null);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTrigger(collision != null ? collision.gameObject : null);
    }

    /// <summary>
    /// Mencoba memicu cutscene saat player menyentuh toko donat atau area trigger.
    /// </summary>
    public void TryTrigger(GameObject hitGo)
    {
        if (hasPlayed && playOnce) return;
        if (hitGo == null) return;

        bool isPlayer = hitGo.CompareTag(playerTag) ||
                        hitGo.GetComponent<PlayerAvatarController>() != null ||
                        hitGo.GetComponent<PlayerController>() != null ||
                        hitGo.GetComponentInParent<PlayerController>() != null ||
                        (hitGo.transform.root != null && hitGo.transform.root.CompareTag(playerTag)) ||
                        (hitGo.transform.root != null && hitGo.transform.root.GetComponent<PlayerController>() != null);

        if (isPlayer)
        {
            GameObject targetPlayer = hitGo.transform.root != null ? hitGo.transform.root.gameObject : hitGo;
            StartCutscene(targetPlayer);
        }
    }

    private Coroutine cutsceneRoutine;

    /// <summary>
    /// Starts the cutscene and freezes player controls.
    /// </summary>
    public void StartCutscene(GameObject player = null)
    {
        if (hasPlayed && playOnce) return;

        if (player != null)
        {
            playerObject = player;
        }

        EnsurePlayerHasController();

        if (playerObject != null)
        {
            // Disable PlayerController during cutscene
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // Hentikan langkah kaki animasi player agar berdiri tegak saat cutscene
            Animator playerAnim = playerObject.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetFloat("Speed_f", 0f);
            }

            // Disable workshop avatar motor during cutscene if present
            objectMotor = playerObject.GetComponent<MovableObjectMotor>();
            if (objectMotor != null)
            {
                objectMotor.enabled = false;
            }
        }

        // Freeze workshop InputManager
        InputManager.bCanPlayerMove = false;

        // Temporarily disable ThirdPersonCamera so Cinemachine has full smooth control
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cachedThirdPersonCamera = mainCam.GetComponent<ThirdPersonCamera>();
            if (cachedThirdPersonCamera != null)
            {
                cachedThirdPersonCamera.enabled = false;
            }

            cachedCinemachineBrain = mainCam.GetComponent<CinemachineBrain>();
            if (cachedCinemachineBrain != null)
            {
                cachedCinemachineBrain.enabled = true;
            }
        }

        if (cutsceneDirector != null)
        {
            hasPlayed = true;
            cutsceneDirector.stopped += OnCutsceneStopped;
            if (cutsceneDirector.state != PlayState.Playing)
            {
                cutsceneDirector.time = 0.0;
                cutsceneDirector.Play();
            }

            if (cutsceneRoutine != null)
            {
                StopCoroutine(cutsceneRoutine);
            }
            cutsceneRoutine = StartCoroutine(CutsceneWatchdog());
        }
        else
        {
            Debug.LogWarning("[CutsceneTrigger] No PlayableDirector assigned to cutscene trigger.", this);
            RestorePlayerControls();
        }
    }

    private System.Collections.IEnumerator CutsceneWatchdog()
    {
        float waitTime = cutsceneDirector != null ? (float)cutsceneDirector.duration : 4.5f;
        yield return new WaitForSeconds(waitTime);
        OnCutsceneFinished();
    }

    private void OnCutsceneStopped(PlayableDirector director)
    {
        if (director == cutsceneDirector)
        {
            OnCutsceneFinished();
        }
    }

    private void OnCutsceneFinished()
    {
        if (cutsceneRoutine != null)
        {
            StopCoroutine(cutsceneRoutine);
            cutsceneRoutine = null;
        }

        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped -= OnCutsceneStopped;
        }

        RestorePlayerControls();

        if (introBanner != null)
        {
            introBanner.SetActive(false);
        }
    }

    private void RestorePlayerControls()
    {
        EnsurePlayerHasController();

        InputManager.bCanPlayerMove = true;

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Disable CinemachineBrain so standard ThirdPersonCamera resumes full control
        if (cachedCinemachineBrain != null)
        {
            cachedCinemachineBrain.enabled = false;
            cachedCinemachineBrain = null;
        }
        else
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var brain = mainCam.GetComponent<CinemachineBrain>();
                if (brain != null)
                {
                    brain.enabled = false;
                }
            }
        }

        // Restore ThirdPersonCamera
        if (cachedThirdPersonCamera != null)
        {
            cachedThirdPersonCamera.enabled = true;
            cachedThirdPersonCamera.SnapToTarget();
            cachedThirdPersonCamera = null;
        }
        else
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var tpc = mainCam.GetComponent<ThirdPersonCamera>();
                if (tpc != null)
                {
                    tpc.enabled = true;
                    tpc.SnapToTarget();
                }
            }
        }
    }

    private void OnDisable()
    {
        OnCutsceneFinished();
    }
}

/// <summary>
/// Proxy untuk meneruskan event sentuhan/tabrakan (misal pada fisik gedung Toko Donut)
/// langsung ke CutsceneTrigger utama.
/// </summary>
public class CutsceneTriggerProxy : MonoBehaviour
{
    [Tooltip("Komponen CutsceneTrigger utama yang akan dipicu saat objek ini disentuh.")]
    public CutsceneTrigger targetTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (targetTrigger != null && other != null)
        {
            targetTrigger.TryTrigger(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (targetTrigger != null && collision != null)
        {
            targetTrigger.TryTrigger(collision.gameObject);
        }
    }
}
