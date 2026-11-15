using System;
using UnityEngine;

namespace DanceTap
{
    [Serializable]
    public sealed class TimingWindows
    {
        [Tooltip("Half window in ms")] public float perfectMs = 90f;
        public float goodMs = 180f;
        [Tooltip("Tutorial: a late note is never a Miss - it is auto-counted as Good and the hint shows again.")]
        public bool forgiving;
    }

    /// <summary>All tuning in one asset (data-driven): timing, scoring, combo tiers, super meter, fail rule.</summary>
    [CreateAssetMenu(menuName = "Dance Tap/Config", fileName = "DanceTapConfig")]
    public sealed class DanceTapConfig : ScriptableObject
    {
        [Header("Timing (Strategy: one reference swapped per state)")]
        public TimingWindows tutorialWindows = new TimingWindows { perfectMs = 120, goodMs = 260, forgiving = true };
        public TimingWindows normalWindows = new TimingWindows { perfectMs = 90, goodMs = 180 };

        [Header("Notes")]
        [Tooltip("The ring appears this many beats before the hit")] public float leadBeats = 2f;
        [Tooltip("Fall distance from spawn to target (world units)")] public float laneHeight = 5.8f;
        [Tooltip("Tutorial: the first N notes stop on the target and wait for the right tap")] public int freezeNotes = 4;
        public float ringStartScale = 2.6f;

        [Header("Score")]
        public int perfectScore = 300;
        public int goodScore = 100;
        [Tooltip("Combo needed for x2, x3, x4, x5")] public int[] multiplierCombo = { 4, 8, 12, 16 };

        [Header("Super meter")]
        public int superMax = 18;
        public int perfectSuper = 2;
        public int goodSuper = 1;

        [Header("Fail")]
        public int missesInRowToFail = 4;

        [Header("Store")]
        public string storeUrl = "https://maksymbozhyk.itch.io/";
    }
}
