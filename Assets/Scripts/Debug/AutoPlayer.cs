#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace DanceTap.DebugTools
{
    /// <summary>
    /// Dev-only bot: plays the level by itself (QA, recording a portfolio video, smoke test).
    /// Ticks through BeatClock (no own Update). Off by default; enable the component to use it.
    /// </summary>
    public sealed class AutoPlayer : MonoBehaviour, IBeatTickable
    {
        public enum Mode { Perfect, Sloppy, MissAll }

        [SerializeField] BeatClock clock;
        [SerializeField] GameFlow flow;
        [SerializeField] Mode mode = Mode.Perfect;
        [Tooltip("Seconds before the bot makes the first tap (starts the music)")] [SerializeField] float startAfter = 1.5f;
        [Tooltip("Sloppy: every Nth note is late (Good), every Mth is skipped")] [SerializeField] int goodEvery = 3, skipEvery = 7;
        [Tooltip("Optional: save a screenshot at these song beats (Game view)")] [SerializeField] float[] shotBeats = { };
        [SerializeField] string shotFolder = "";

        Note[] notes;
        int next, nextShot;
        float started;
        bool tappedIn, pressedSuper;
        float pausedAt = -1f;

        void OnEnable() { clock.Register(this); notes = clock.Beatmap.notes; started = Time.unscaledTime; }
        void OnDisable() => clock.Unregister(this);

        public void Tick(double songTime, double songBeat)
        {
            if (!tappedIn)
            {
                if (Time.unscaledTime - started >= startAfter) { tappedIn = true; GameEvents.RaiseTap(0); }
                return;
            }
            if (!clock.SongStarted) return;

            if (clock.Paused) { if (pausedAt < 0f) pausedAt = Time.unscaledTime; }
            else pausedAt = -1f;

            while (next < notes.Length)
            {
                var n = notes[next];
                if (flow != null && next < flow.config.freezeNotes && !(clock.Paused && Time.unscaledTime - pausedAt > 0.8f)) break;   // tutorial freeze: tap like a human, after a moment
                double offsetBeats = mode == Mode.Sloppy && goodEvery > 0 && next % goodEvery == 1 ? 0.25 : 0.0;   // 125 ms late
                if (songBeat < n.beat + offsetBeats) break;
                bool frozenNote = flow != null && next < flow.config.freezeNotes;   // the tutorial waits for these - even a bad player taps them
                bool skip = !frozenNote && (mode == Mode.MissAll || (mode == Mode.Sloppy && skipEvery > 0 && next % skipEvery == skipEvery - 1));
                if (!skip) GameEvents.RaiseTap(n.side);
                if (frozenNote) pausedAt = -1f;   // the next freeze waits again
                next++;
            }

            if (!pressedSuper && flow != null && flow.superButton != null && flow.superButton.activeInHierarchy && songBeat >= flow.superStartBeat + 2)
            {
                pressedSuper = true;
                flow.OnSuperButton();
            }

            if (shotBeats != null && nextShot < shotBeats.Length && songBeat >= shotBeats[nextShot])
            {
                if (!string.IsNullOrEmpty(shotFolder))
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(shotFolder, "shot_" + nextShot.ToString("00") + "_b" + shotBeats[nextShot].ToString("0") + ".png"));
                nextShot++;
            }
        }
    }
}
#endif
