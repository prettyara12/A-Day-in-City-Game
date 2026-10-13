using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistem Balon Dialog & Subtitle Overlay Beresolusi Tinggi (Screen-Space Overlay).
/// 1. Balon percakapan melayang tepat di atas kepala karakter di layar (mengikuti pergerakan kepala secara dinamis).
/// 2. Subtitle dialog elegan di bagian bawah layar agar selalu 100% terbaca dengan jelas dalam kondisi apapun.
/// Robust terhadap Unity domain reload & scene transitions (Enter Play Mode Options).
/// </summary>
public class NPCDialogueOverlay : MonoBehaviour
{
    private static NPCDialogueOverlay instance;
    private static bool isQuitting = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnSubsystemRegistration()
    {
        instance = null;
        isQuitting = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStaticsBeforeSceneLoad()
    {
        instance = null;
        isQuitting = false;
    }

    public static NPCDialogueOverlay Instance
    {
        get
        {
            if (instance == null || instance.overheadText == null)
            {
                EnsureInstance();
            }
            return instance;
        }
    }

    private Canvas overlayCanvas;
    private CanvasScaler canvasScaler;

    // Overhead Speech Bubble Elements
    private GameObject overheadBubbleObj;
    private RectTransform overheadBubbleRect;
    private Text overheadText;
    private Image overheadBg;
    private RectTransform arrowRect;

    // Bottom Subtitle Elements
    private GameObject subtitleBarObj;
    private Text subtitleSpeakerText;
    private Text subtitleMessageText;

    // Active Dialogue Tracking
    private NPCWalker activeSpeaker;
    private Transform activeSpeakerTransform;
    private float activeSpeakerHeightOffset = 0.20f;
    private string activeSpeakerName;
    private string activeMessage;

    public static void EnsureInstance()
    {
        if (!Application.isPlaying || isQuitting) return;

        if (instance != null && instance.overheadText != null && instance.subtitleMessageText != null)
        {
            return;
        }

        GameObject go = GameObject.Find("NPCDialogueOverlaySystem");
        if (go == null)
        {
            if (isQuitting) return;
            go = new GameObject("NPCDialogueOverlaySystem");
            instance = go.AddComponent<NPCDialogueOverlay>();
        }
        else
        {
            instance = go.GetComponent<NPCDialogueOverlay>();
            if (instance == null) instance = go.AddComponent<NPCDialogueOverlay>();
            if (instance.overheadText == null || instance.subtitleMessageText == null)
            {
                instance.BuildOverlayUI();
            }
        }
    }

    private void Awake()
    {
        isQuitting = false;
        if (instance == null) instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        BuildOverlayUI();
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private static Font GetBestDialogueFont()
    {
        Font font = null;
#if UNITY_EDITOR
        font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/Develop 3D Mobile Games in Unity/Assets/MobileUI/fonts/Roboto-Bold.ttf");
        if (font == null)
        {
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
        }
#endif
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont(new string[] { "Segoe UI Bold", "Segoe UI", "Arial Bold", "Arial", "Roboto Bold", "Roboto" }, 32);
        }
        return font;
    }

    public void BuildOverlayUI()
    {
        // 1. Setup ScreenSpaceOverlay Canvas
        overlayCanvas = GetComponent<Canvas>();
        if (overlayCanvas == null) overlayCanvas = gameObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 999; // Prioritas paling atas di layar

        canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null) canvasScaler = gameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1280, 720); // Resolusi 720p HD seimbang
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasScaler.dynamicPixelsPerUnit = 2f; // Supersampling 2x untuk teks tajam tanpa pixelated

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Hapus UI anak lama jika ada
        while (transform.childCount > 0)
        {
            Transform ch = transform.GetChild(0);
            ch.SetParent(null);
            Destroy(ch.gameObject);
        }

        Font mainFont = GetBestDialogueFont();

        // 2. OVERHEAD SPEECH BUBBLE (Proporsional, Rapi, Tebal & Tetap Sangat Jelas)
        overheadBubbleObj = new GameObject("OverheadSpeechBubble");
        overheadBubbleObj.transform.SetParent(transform, false);

        overheadBubbleRect = overheadBubbleObj.AddComponent<RectTransform>();
        overheadBubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
        overheadBubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        overheadBubbleRect.pivot = new Vector2(0.5f, 0f); // Pivot di bagian bawah tengah balon
        overheadBubbleRect.sizeDelta = new Vector2(440f, 88f); // Ukuran proporsional

        overheadBg = overheadBubbleObj.AddComponent<Image>();
        overheadBg.color = new Color(1f, 1f, 1f, 0.98f); // Putih bersih solid

        // Outline gelap tegas berkualitas tinggi
        Outline bubbleOutline = overheadBubbleObj.AddComponent<Outline>();
        bubbleOutline.effectColor = new Color(0.1f, 0.15f, 0.25f, 1f);
        bubbleOutline.effectDistance = new Vector2(2.5f, -2.5f);

        // Shadow lembut elegan di bawah balon
        Shadow bubbleShadow = overheadBubbleObj.AddComponent<Shadow>();
        bubbleShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        bubbleShadow.effectDistance = new Vector2(0f, -4f);

        // Segitiga penunjuk (Pointer Arrow) di bawah balon mengarah ke kepala NPC
        GameObject arrowObj = new GameObject("BubblePointerArrow");
        arrowObj.transform.SetParent(overheadBubbleObj.transform, false);
        arrowRect = arrowObj.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 0f);
        arrowRect.anchorMax = new Vector2(0.5f, 0f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.sizeDelta = new Vector2(20f, 20f);
        arrowRect.anchoredPosition = new Vector2(0f, 0f);
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, 45f); // Belah ketupat miring

        Image arrowImg = arrowObj.AddComponent<Image>();
        arrowImg.color = new Color(1f, 1f, 1f, 0.98f);

        Outline arrowOutline = arrowObj.AddComponent<Outline>();
        arrowOutline.effectColor = new Color(0.1f, 0.15f, 0.25f, 1f);
        arrowOutline.effectDistance = new Vector2(1.8f, -1.8f);

        // Teks di dalam Balon (Ukuran Seimbang, Bold, Tajam & Mudah Dibaca)
        GameObject textObj = new GameObject("BubbleText");
        textObj.transform.SetParent(overheadBubbleObj.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(16f, 10f);
        textRT.offsetMax = new Vector2(-16f, -10f);

        overheadText = textObj.AddComponent<Text>();
        overheadText.font = mainFont;
        overheadText.fontSize = 19; // Ukuran proporsional, rapi dan tetap sangat jelas
        overheadText.fontStyle = FontStyle.Bold;
        overheadText.lineSpacing = 1.2f;
        overheadText.alignment = TextAnchor.MiddleCenter;
        overheadText.color = new Color(0.06f, 0.08f, 0.14f, 1f); // Hitam pekat kontras tinggi
        overheadText.horizontalOverflow = HorizontalWrapMode.Wrap;
        overheadText.verticalOverflow = VerticalWrapMode.Overflow;

        // 3. SUBTITLE BAR (Panel Dialog Bawah Layar Berukuran Pas & Elegan)
        subtitleBarObj = new GameObject("SubtitleDialogueBar");
        subtitleBarObj.transform.SetParent(transform, false);

        RectTransform subRT = subtitleBarObj.AddComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.035f);
        subRT.anchorMax = new Vector2(0.5f, 0.035f);
        subRT.pivot = new Vector2(0.5f, 0f);
        subRT.sizeDelta = new Vector2(740f, 85f); // Ukuran seimbang

        Image subBg = subtitleBarObj.AddComponent<Image>();
        subBg.color = new Color(0.04f, 0.07f, 0.14f, 0.96f); // Panel gelap elegan kontras tinggi

        // Garis aksen vertikal di tepi kiri (Gaya RPG modern)
        GameObject accentObj = new GameObject("AccentBar");
        accentObj.transform.SetParent(subtitleBarObj.transform, false);
        RectTransform accentRT = accentObj.AddComponent<RectTransform>();
        accentRT.anchorMin = new Vector2(0f, 0f);
        accentRT.anchorMax = new Vector2(0f, 1f);
        accentRT.pivot = new Vector2(0f, 0.5f);
        accentRT.sizeDelta = new Vector2(5f, 0f);
        accentRT.anchoredPosition = Vector2.zero;
        Image accentImg = accentObj.AddComponent<Image>();
        accentImg.color = new Color(0.3f, 0.7f, 1f, 1f); // Cyan elektrik modern

        // Outline border bercahaya biru modern
        Outline subOutline = subtitleBarObj.AddComponent<Outline>();
        subOutline.effectColor = new Color(0.35f, 0.55f, 0.95f, 0.85f);
        subOutline.effectDistance = new Vector2(2f, -2f);

        // Shadow lembut di sekeliling panel
        Shadow subShadow = subtitleBarObj.AddComponent<Shadow>();
        subShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        subShadow.effectDistance = new Vector2(0f, -4f);

        // Nama Pembicara
        GameObject spkObj = new GameObject("SpeakerName");
        spkObj.transform.SetParent(subtitleBarObj.transform, false);
        RectTransform spkRT = spkObj.AddComponent<RectTransform>();
        spkRT.anchorMin = new Vector2(0f, 0.52f);
        spkRT.anchorMax = new Vector2(1f, 1f);
        spkRT.offsetMin = new Vector2(24f, 0f);
        spkRT.offsetMax = new Vector2(-22f, -3f);

        subtitleSpeakerText = spkObj.AddComponent<Text>();
        subtitleSpeakerText.font = mainFont;
        subtitleSpeakerText.fontSize = 18; // Ukuran pas & jelas
        subtitleSpeakerText.fontStyle = FontStyle.Bold;
        subtitleSpeakerText.alignment = TextAnchor.MiddleLeft;
        subtitleSpeakerText.color = new Color(1f, 0.85f, 0.18f, 1f); // Kuning keemasan cerah

        Shadow spkShadow = spkObj.AddComponent<Shadow>();
        spkShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
        spkShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // Isi Dialog Subtitle
        GameObject msgObj = new GameObject("SpeakerMessage");
        msgObj.transform.SetParent(subtitleBarObj.transform, false);
        RectTransform msgRT = msgObj.AddComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0f, 0f);
        msgRT.anchorMax = new Vector2(1f, 0.52f);
        msgRT.offsetMin = new Vector2(24f, 8f);
        msgRT.offsetMax = new Vector2(-22f, 0f);

        subtitleMessageText = msgObj.AddComponent<Text>();
        subtitleMessageText.font = mainFont;
        subtitleMessageText.fontSize = 18; // Ukuran proporsional, tajam & mudah dibaca
        subtitleMessageText.fontStyle = FontStyle.Bold; // Dibuat tebal agar tetap sangat tegas terbaca
        subtitleMessageText.lineSpacing = 1.2f;
        subtitleMessageText.alignment = TextAnchor.MiddleLeft;
        subtitleMessageText.color = Color.white; // Putih bersih
        subtitleMessageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleMessageText.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow msgShadow = msgObj.AddComponent<Shadow>();
        msgShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
        msgShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // Default sembunyikan saat mulai
        overheadBubbleObj.SetActive(false);
        subtitleBarObj.SetActive(false);
    }

    public static void ShowDialogue(NPCWalker speaker, string speakerName, string line, bool show)
    {
        float offset = (speaker != null && speaker.isSittingPermanently) ? 0.11f : 0.20f;
        ShowDialogue(speaker != null ? speaker.transform : null, speakerName, line, show, offset);
        if (instance != null) instance.activeSpeaker = show ? speaker : null;
    }

    public static void ShowDialogue(Transform speakerTransform, string speakerName, string line, bool show, float heightOffset = 0.20f)
    {
        // 1. Jika hanya ingin menyembunyikan dialog, jangan pernah panggil EnsureInstance() atau buat objek baru
        if (!show)
        {
            if (instance != null && (instance.activeSpeakerTransform == speakerTransform || speakerTransform == null))
            {
                instance.activeSpeaker = null;
                instance.activeSpeakerTransform = null;
                if (instance.overheadBubbleObj != null) instance.overheadBubbleObj.SetActive(false);
                if (instance.subtitleBarObj != null) instance.subtitleBarObj.SetActive(false);
            }
            return;
        }

        if (!Application.isPlaying || isQuitting) return;

        EnsureInstance();
        if (instance == null) return;

        instance.activeSpeakerTransform = speakerTransform;
        instance.activeSpeakerHeightOffset = heightOffset;
        instance.activeSpeakerName = speakerName;
        instance.activeMessage = line;

        if (instance.overheadText != null) instance.overheadText.text = line;
        if (instance.subtitleSpeakerText != null) instance.subtitleSpeakerText.text = "◆  " + speakerName;
        if (instance.subtitleMessageText != null) instance.subtitleMessageText.text = "\"" + line.Replace("\n", " ") + "\"";

        if (instance.overheadBubbleObj != null) instance.overheadBubbleObj.SetActive(true);
        if (instance.subtitleBarObj != null) instance.subtitleBarObj.SetActive(true);

        instance.UpdateOverheadPosition();
    }

    private void LateUpdate()
    {
        if (activeSpeakerTransform != null && overheadBubbleObj != null && overheadBubbleObj.activeSelf)
        {
            UpdateOverheadPosition();
        }
    }

    private void UpdateOverheadPosition()
    {
        if (activeSpeakerTransform == null) return;

        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null) return;

        // Posisi kepala di dunia 3D:
        Vector3 headWorldPos = activeSpeakerTransform.position + Vector3.up * activeSpeakerHeightOffset;

        Vector3 screenPos = cam.WorldToScreenPoint(headWorldPos);

        // Hanya tampilkan jika berada di depan kamera (Z > 0)
        if (screenPos.z > 0.1f)
        {
            overheadBubbleObj.SetActive(true);

            // Letakkan balon tepat di atas kepala di layar
            // Jarak vertikal di layar: 35px di atas posisi kepala
            overheadBubbleRect.position = new Vector3(screenPos.x, screenPos.y + 35f, 0f);
        }
        else
        {
            overheadBubbleObj.SetActive(false);
        }
    }
}
