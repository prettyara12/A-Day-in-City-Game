using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector Editor for NPCWalker.
/// Provides 1-click configuration buttons directly in the Inspector.
/// </summary>
[CustomEditor(typeof(NPCWalker))]
public class NPCWalkerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        NPCWalker walker = (NPCWalker)target;

        // Banner
        EditorGUILayout.Space(5);
        GUIStyle bannerStyle = new GUIStyle(EditorStyles.helpBox);
        bannerStyle.fontSize = 12;
        bannerStyle.fontStyle = FontStyle.Bold;
        bannerStyle.alignment = TextAnchor.MiddleCenter;
        bannerStyle.normal.textColor = new Color(0.1f, 0.4f, 0.8f);

        EditorGUILayout.LabelField("🎮 NPC Controller (Video Challenge Mode)", bannerStyle, GUILayout.Height(30));
        EditorGUILayout.Space(5);

        // Warning if InputManager is attached to this NPC
        InputManager rogueInput = walker.GetComponent<InputManager>();
        if (rogueInput != null)
        {
            EditorGUILayout.HelpBox("PERINGATAN: Ada komponen 'Input Manager (Script)' terpasang di NPC ini! Komponen ini hanya untuk pemain/sistem dan membuat kontrol bertabrakan.", MessageType.Error);
            if (GUILayout.Button("❌ Hapus InputManager dari Karakter Ini Sekarang", GUILayout.Height(32)))
            {
                Undo.DestroyObjectImmediate(rogueInput);
                EditorUtility.DisplayDialog("Sukses", "Komponen InputManager berhasil dihapus dari NPC!", "OK");
            }
            EditorGUILayout.Space(5);
        }

        // Action Buttons
        GUI.backgroundColor = new Color(0.2f, 0.75f, 0.35f);
        if (GUILayout.Button("⭐ Jadikan Karakter Ini NPC Seperti Video Challenge", GUILayout.Height(36)))
        {
            PlayerBootstrap.SetupPoliceAsVideoNPC(walker.gameObject);
            EditorUtility.SetDirty(walker.gameObject);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        // Speech Bubble generator button
        if (GUILayout.Button("💬 Buat / Perbarui Balon Percakapan (Speech Bubble)", GUILayout.Height(28)))
        {
            walker.EnsureSpeechBubbleUI();
            EditorUtility.SetDirty(walker.gameObject);
            EditorUtility.DisplayDialog("Balon Dialog Siap!", "Speech Bubble Canvas berhasil dibuat di atas kepala NPC dan siap menampilkan teks dialog saat didekati pemain!", "OK");
        }

        EditorGUILayout.Space(10);

        // Default inspector fields
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
    }
}
