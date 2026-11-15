#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DanceTap.EditorTools
{
    /// <summary>
    /// UI look: one shared neon text material for every TMP text (outline + soft glow = still one draw-call material),
    /// end card layout with the buttons near the centre, pulsing store CTA. Re-running re-applies these values.
    /// </summary>
    public static class DanceTapUISetup
    {
        const string NeonPath = "Assets/Materials/M_TMP_Neon.mat";

        [MenuItem("Tools/Dance Tap/Setup UI Style")]
        public static void Setup()
        {
            var ui = GameObject.Find("UI");
            var neon = NeonMaterial();

            foreach (var t in ui.GetComponentsInChildren<TMP_Text>(true))
            {
                t.fontSharedMaterial = neon;
                t.fontStyle = FontStyles.Bold;
                EditorUtility.SetDirty(t);
            }

            // ---------- HUD ----------
            var bar = ui.transform.Find("HUD/SuperBar");
            if (bar != null)
            {
                bar.GetComponent<Image>().color = new Color(0.1f, 0.03f, 0.2f, 0.8f);
                var fill = bar.Find("Fill").GetComponent<Image>(); fill.color = new Color(1f, 0.85f, 0.25f);
                bar.GetComponent<Image>().pixelsPerUnitMultiplier = 2f;
                fill.type = Image.Type.Sliced; fill.pixelsPerUnitMultiplier = 2.2f;   // pill grows in width (HudView), ends stay round
                var frt = fill.rectTransform; frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f); frt.pivot = new Vector2(0f, 0.5f); frt.anchoredPosition = new Vector2(10f, 0f); frt.sizeDelta = new Vector2(600f, 40f);
                var label = bar.Find("Label").GetComponent<TMP_Text>(); label.color = Color.white; label.fontSize = 32;
            }

            // ---------- end card: everything grouped around the centre ----------
            var end = ui.transform.Find("EndCard");
            if (end != null)
            {
                Place(end, "Logo", new Vector2(0, 560));
                Place(end, "Title", new Vector2(0, 340));
                Place(end, "Score", new Vector2(0, 210));
                var store = Place(end, "StoreButton", new Vector2(0, -40));
                var replay = Place(end, "ReplayButton", new Vector2(0, -230));
                if (store != null)
                {
                    var pulse = store.GetComponent<PulseView>(); if (pulse == null) pulse = store.gameObject.AddComponent<PulseView>();
                    var so = new SerializedObject(pulse); so.FindProperty("amount").floatValue = 0.06f; so.ApplyModifiedPropertiesWithoutUndo();
                    store.GetComponent<Image>().color = new Color(0.25f, 0.85f, 0.4f);
                }
                if (replay != null) replay.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.16f);
            }

            EditorSceneManager.MarkSceneDirty(ui.scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Dance Tap: UI style applied.");
        }

        static RectTransform Place(Transform parent, string name, Vector2 pos)
        {
            var t = parent.Find(name) as RectTransform;
            if (t == null) return null;
            t.anchoredPosition = pos;
            return t;
        }

        /// <summary>Created once from the font's own material (same atlas), then it is yours to tweak.</summary>
        static Material NeonMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(NeonPath);
            if (m != null) return m;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            m = new Material(font.material) { name = "M_TMP_Neon" };
            m.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            m.SetFloat("_FaceDilate", 0.15f);
            m.EnableKeyword("OUTLINE_ON");
            m.SetColor("_OutlineColor", new Color(0.12f, 0.03f, 0.22f, 1f));
            m.SetFloat("_OutlineWidth", 0.22f);
            m.EnableKeyword("UNDERLAY_ON");
            m.SetColor("_UnderlayColor", new Color(1f, 0.2f, 0.85f, 0.55f));
            m.SetFloat("_UnderlayOffsetX", 0f); m.SetFloat("_UnderlayOffsetY", 0f);
            m.SetFloat("_UnderlayDilate", 0.5f); m.SetFloat("_UnderlaySoftness", 0.75f);
            AssetDatabase.CreateAsset(m, NeonPath);
            return m;
        }
    }
}
#endif
