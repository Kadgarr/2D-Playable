using System.Collections.Generic;
using UnityEngine;

namespace DanceTap
{
    /// <summary>Anything that needs per-frame work registers here; BeatClock owns the only Update in the game.</summary>
    public interface IBeatTickable
    {
        void Tick(double songTime, double songBeat);
    }

    /// <summary>
    /// Song time from the DSP clock (not Time.time) so hitches never desync notes and music.
    /// Before the first tap the clock free-runs on unscaled time so the rival can dance in silence.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class BeatClock : MonoBehaviour
    {
        [SerializeField] BeatmapSO beatmap;
        [SerializeField] AudioSource music;
        [Tooltip("Delay between the tap and the scheduled music start (s).")]
        [SerializeField] double startDelay = 0.1;

        public static BeatClock Instance { get; private set; }
        public BeatmapSO Beatmap => beatmap;
        public double SongTime { get; private set; }
        public double SongBeat { get; private set; }
        public bool SongStarted { get; private set; }
        public double SecondsPerBeat { get; private set; }
        /// <summary>Song frozen (tutorial waits for the tap). Music is paused, song time holds.</summary>
        public bool Paused { get; private set; }

        static readonly int BeatPhaseId = Shader.PropertyToID("_BeatPhase");

        readonly List<IBeatTickable> tickables = new List<IBeatTickable>(16);
        double dspStart, freeStart, pauseDsp, frozenTime;
        int lastBeat = int.MinValue;

        void Awake()
        {
            Instance = this;
            SecondsPerBeat = 60.0 / beatmap.bpm;
            freeStart = Time.unscaledTimeAsDouble;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Register(IBeatTickable t) { if (!tickables.Contains(t)) tickables.Add(t); }
        public void Unregister(IBeatTickable t) => tickables.Remove(t);

        /// <summary>Called once, from the first tap (sound only after the first user gesture).</summary>
        public void StartSong()
        {
            if (SongStarted) return;
            SongStarted = true;
            dspStart = AudioSettings.dspTime + startDelay;
            if (music != null) music.PlayScheduled(dspStart);
            lastBeat = int.MinValue;
        }

        /// <summary>Freeze the song at holdSongTime (notes stop on their targets).</summary>
        public void Pause(double holdSongTime)
        {
            if (!SongStarted || Paused) return;
            Paused = true;
            pauseDsp = AudioSettings.dspTime;
            frozenTime = holdSongTime;
            if (music != null) music.Pause();
        }

        public void Resume()
        {
            if (!Paused) return;
            Paused = false;
            dspStart += AudioSettings.dspTime - pauseDsp;   // the paused span is cut out of the song timeline
            if (music != null) music.UnPause();
        }

        public double BeatToTime(double beat) => beat * SecondsPerBeat;

        void Update()
        {
            if (SongStarted)
                SongTime = Paused ? frozenTime : AudioSettings.dspTime - dspStart - beatmap.offsetMs * 0.001;
            else
                SongTime = Time.unscaledTimeAsDouble - freeStart - 1000.0;   // far before the song: beats are "intro" beats

            SongBeat = SongTime / SecondsPerBeat;
            Shader.SetGlobalFloat(BeatPhaseId, (float)(SongBeat - System.Math.Floor(SongBeat)));

            int beat = (int)System.Math.Floor(SongBeat);
            if (beat != lastBeat)
            {
                lastBeat = beat;
                GameEvents.RaiseBeat(beat);
            }

            for (int i = 0; i < tickables.Count; i++) tickables[i].Tick(SongTime, SongBeat);
        }
    }
}
