using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PortalExit : MonoBehaviour {
    [SerializeField]
    [Tooltip("Add the Animator from the Fade Panel here. It is under the Main Cameras Canvas.")]
    private Animator fadePanelAnimator;

    [Tooltip("What is Build Number of the next scene?")]
    [SerializeField]
    private int nextSceneNumber = 1;

    [Tooltip("How many seconds is your fade out animation? Mostly this should stay at 1.")]
    [SerializeField]
    public float secondsOfFade = 1f;

    private bool isTransitioning = false;

    [Tooltip("Sudut rotasi portal agar menghadap ke jalan (Y = 90)")]
    [SerializeField]
    private Vector3 targetEulerRotation = new Vector3(0f, 90f, 0f);

    private void Start()
    {
        fadePanelAnimator = FindFadePanelAnimator();
        transform.rotation = Quaternion.Euler(targetEulerRotation);
    }

    private Animator FindFadePanelAnimator()
    {
        if (fadePanelAnimator != null) return fadePanelAnimator;

        GameObject fadeObj = GameObject.Find("Fade Panel");
        if (fadeObj != null)
        {
            Animator anim = fadeObj.GetComponent<Animator>();
            if (anim != null) return anim;
        }

        Animator[] anims = FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var anim in anims)
        {
            if (anim.gameObject.name.Contains("Fade"))
            {
                return anim;
            }
        }
        return null;
    }

    // ********* Method is called when an object intersects this object's trigger collider *********
    private void OnTriggerEnter(Collider other)
    {
        TryTriggerPortal(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTriggerPortal(other);
    }

    private void TryTriggerPortal(Collider other)
    {
        if (isTransitioning || other == null) return;

        bool isPlayer = other.CompareTag("Player") ||
                        (other.transform.root != null && other.transform.root.CompareTag("Player")) ||
                        other.GetComponentInParent<PlayerController>() != null ||
                        other.GetComponent<PlayerController>() != null ||
                        other.gameObject.name.Contains("Trucker") ||
                        other.gameObject.name.Contains("Player");

        if (isPlayer)
        {
            EnterPortal(other);
        }
    }

    // ********* Method is called when Player enters the portal trigger **********
    private void EnterPortal(Collider playerCollider)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // Turn off player control
        InputManager.bCanPlayerMove = false;

        if (playerCollider != null)
        {
            PlayerController pc = playerCollider.GetComponentInParent<PlayerController>() ?? playerCollider.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.enabled = false;
            }
        }

        // Auto-find fadePanelAnimator if not set
        if (fadePanelAnimator == null)
        {
            fadePanelAnimator = FindFadePanelAnimator();
        }

        if (fadePanelAnimator != null)
        {
            fadePanelAnimator.SetTrigger("FadeTransitionOut");
        }

        StartCoroutine(WaitForFade());
    }

    // ********* Wait for Fade animation **********
    private IEnumerator WaitForFade()
    {
        yield return new WaitForSeconds(secondsOfFade);

        // Determine destination scene
        int targetScene = nextSceneNumber;
        int activeIndex = SceneManager.GetActiveScene().buildIndex;

        // If targetScene is the same as current active scene, fallback to next available scene in Build Settings
        if (targetScene == activeIndex && SceneManager.sceneCountInBuildSettings > 1)
        {
            targetScene = (activeIndex + 1) % SceneManager.sceneCountInBuildSettings;
        }

        Debug.Log($"[PortalExit] Memuat scene berikutnya pada build index: {targetScene}");
        SceneManager.LoadScene(targetScene);
    }
}

