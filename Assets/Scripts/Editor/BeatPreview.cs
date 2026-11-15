#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DanceTap.EditorTools
{
    /// <summary>
    /// Edit-mode preview of the beat shaders: drives the global _BeatPhase at 120 BPM so the crowd bounce and the
    /// neon flash play in the Scene view without entering Play Mode. Editor only - nothing of this goes into a build.
    /// In Play Mode BeatClock owns _BeatPhase, the preview steps aside.
    /// </summary>
    [InitializeOnLoad]
    public static class BeatPreview
    {
        const string Menu = "Tools/Dance Tap/Preview Beat In Scene";
        const string Pref = "DanceTap.BeatPreview";
        static readonly int BeatPhaseId = Shader.PropertyToID("_BeatPhase");
        static double nextRepaint;

        static BeatPreview()
        {
            EditorApplication.update -= Update;
            if (EditorPrefs.GetBool(Pref, false)) EditorApplication.update += Update;
        }

        [MenuItem(Menu)]
        static void Toggle()
        {
            bool on = !EditorPrefs.GetBool(Pref, false);
            EditorPrefs.SetBool(Pref, on);
            EditorApplication.update -= Update;
            if (on) EditorApplication.update += Update;
            else { Shader.SetGlobalFloat(BeatPhaseId, 0f); SceneView.RepaintAll(); }
        }

        [MenuItem(Menu, true)]
        static bool ToggleValidate()
        {
            Menu.ToString();
            UnityEditor.Menu.SetChecked(Menu, EditorPrefs.GetBool(Pref, false));
            return true;
        }

        static void Update()
        {
            if (EditorApplication.isPlaying) return;
            double t = EditorApplication.timeSinceStartup;
            double beat = t * 2.0;                                   // 120 BPM
            Shader.SetGlobalFloat(BeatPhaseId, (float)(beat - System.Math.Floor(beat)));
            if (t < nextRepaint) return;                             // ~30 fps is enough for a preview
            nextRepaint = t + 1.0 / 30.0;
            SceneView.RepaintAll();
        }
    }
}
#endif
