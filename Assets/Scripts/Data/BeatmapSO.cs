using System;
using UnityEngine;

namespace DanceTap
{
    [Serializable]
    public struct Note
    {
        public float beat;
        [Tooltip("0 = left (pink), 1 = right (blue)")]
        public int side;
    }

    [Serializable]
    public struct Section
    {
        public string name;
        public int fromBeat;
        public int toBeat;
    }

    /// <summary>The level: tempo, offset and notes. Edited in the inspector; can be filled from the JSON export.</summary>
    [CreateAssetMenu(menuName = "Dance Tap/Beatmap", fileName = "BM_DanceTap")]
    public sealed class BeatmapSO : ScriptableObject
    {
        public float bpm = 120f;
        [Tooltip("Positive = notes later. Use it to compensate WebAudio latency.")]
        public int offsetMs;
        public int beatsPerBar = 4;
        public int lengthBeats = 64;
        public Section[] sections = Array.Empty<Section>();
        public Note[] notes = Array.Empty<Note>();
        public TextAsset sourceJson;

        public bool TryGetSection(string sectionName, out Section s)
        {
            for (int i = 0; i < sections.Length; i++)
                if (sections[i].name == sectionName) { s = sections[i]; return true; }
            s = default;
            return false;
        }

        public int SectionStart(string sectionName, int fallback) => TryGetSection(sectionName, out var s) ? s.fromBeat : fallback;

#if UNITY_EDITOR
        [Serializable] struct JsonMap { public float bpm; public int offsetMs; public int beatsPerBar; public int lengthBeats; public Section[] sections; public Note[] notes; }

        [ContextMenu("Import From Source Json")]
        public void ImportFromJson()
        {
            if (sourceJson == null) { Debug.LogWarning("Beatmap: assign Source Json first."); return; }
            var m = JsonUtility.FromJson<JsonMap>(sourceJson.text);
            bpm = m.bpm; offsetMs = m.offsetMs; beatsPerBar = m.beatsPerBar; lengthBeats = m.lengthBeats;
            sections = m.sections ?? Array.Empty<Section>();
            notes = m.notes ?? Array.Empty<Note>();
            Array.Sort(notes, (a, b) => a.beat.CompareTo(b.beat));
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
