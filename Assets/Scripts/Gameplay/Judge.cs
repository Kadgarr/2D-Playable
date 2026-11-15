using UnityEngine;

namespace DanceTap
{
    /// <summary>
    /// Grades taps against the beatmap. Timing windows are a Strategy (TimingWindows) swapped by the game state.
    /// Also expires notes nobody tapped (Miss, or auto-Good in the forgiving tutorial).
    /// </summary>
    public sealed class Judge : MonoBehaviour, IBeatTickable
    {
        [SerializeField] BeatClock clock;
        [SerializeField] DanceTapConfig config;

        Note[] notes;
        double[] noteTime;
        bool[] judged;
        readonly int[] nextOnSide = new int[2];
        int nextAny;
        TimingWindows windows;
        bool active;
        int lastJudgeableBeat = int.MaxValue;

        public void SetWindows(TimingWindows w) => windows = w;

        /// <summary>Notes with index below this are never auto-expired: the tutorial coach freezes the song on them.</summary>
        public int HoldNotes { get; set; }

        /// <summary>Judging on/off (off in Intro, Super, Victory...).</summary>
        public void SetActive(bool on) => active = on;

        /// <summary>Notes at or after this beat are not played (e.g. the super section).</summary>
        public void SetLastBeat(int beat) => lastJudgeableBeat = beat;

        void Awake()
        {
            notes = clock.Beatmap.notes;
            noteTime = new double[notes.Length];
            judged = new bool[notes.Length];
            for (int i = 0; i < notes.Length; i++) noteTime[i] = clock.BeatToTime(notes[i].beat);
            windows = config.normalWindows;
        }

        void OnEnable() { GameEvents.Tap += OnTap; clock.Register(this); }
        void OnDisable() { GameEvents.Tap -= OnTap; clock.Unregister(this); }

        public bool IsPlayable(int i) => notes[i].beat < lastJudgeableBeat;

        void OnTap(int side)
        {
            if (!active || !clock.SongStarted) return;
            int i = FindNext(side);
            if (i >= 0)
            {
                double dt = System.Math.Abs(clock.SongTime - noteTime[i]) * 1000.0;
                if (dt <= windows.goodMs)
                {
                    Resolve(i, dt <= windows.perfectMs ? HitGrade.Perfect : HitGrade.Good);
                    return;
                }
            }
            GameEvents.RaiseFreeMove(side);   // forgiving: an early/ghost tap is never a Miss
        }

        int FindNext(int side)
        {
            for (int i = nextOnSide[side]; i < notes.Length; i++)
            {
                if (notes[i].side != side || judged[i]) continue;
                nextOnSide[side] = i;
                return IsPlayable(i) ? i : -1;
            }
            return -1;
        }

        void Resolve(int i, HitGrade grade)
        {
            judged[i] = true;
            GameEvents.RaiseHit(grade, notes[i].side, i);
        }

        public void Tick(double songTime, double songBeat)
        {
            if (!active || !clock.SongStarted) return;
            double late = windows.goodMs * 0.001;
            while (nextAny < notes.Length)
            {
                if (judged[nextAny] || !IsPlayable(nextAny)) { nextAny++; continue; }
                if (nextAny < HoldNotes) break;   // a lag spike must not expire a tutorial note before the coach freezes on it
                if (songTime <= noteTime[nextAny] + late) break;
                if (windows.forgiving) { Resolve(nextAny, HitGrade.Good); GameEvents.RaiseHint(); }
                else Resolve(nextAny, HitGrade.Miss);
                nextAny++;
            }
        }
    }
}
