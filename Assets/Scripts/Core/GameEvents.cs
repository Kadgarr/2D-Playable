using System;

namespace DanceTap
{
    public enum HitGrade { Perfect, Good, Miss }

    /// <summary>Observer hub. Static C# events: subscribe in OnEnable, unsubscribe in OnDisable. No allocations on raise.</summary>
    public static class GameEvents
    {
        /// <summary>Whole beat crossed (song beats, negative before the music starts).</summary>
        public static event Action<int> Beat;
        /// <summary>Raw tap on a zone (0 = left, 1 = right).</summary>
        public static event Action<int> Tap;
        /// <summary>Judged note: grade, side, note index in the beatmap.</summary>
        public static event Action<HitGrade, int, int> Hit;
        /// <summary>Tap with no note close enough - the hero still dances, nothing is scored.</summary>
        public static event Action<int> FreeMove;
        public static event Action<int, int, float> ScoreChanged;   // combo, score, super 0..1
        public static event Action<int> MultiplierChanged;          // x1..x5
        public static event Action SuperReady;
        public static event Action SuperMove;
        public static event Action HintRequested;
        public static event Action<GameStateId> StateChanged;

        public static void RaiseBeat(int beat) => Beat?.Invoke(beat);
        public static void RaiseTap(int side) => Tap?.Invoke(side);
        public static void RaiseHit(HitGrade g, int side, int note) => Hit?.Invoke(g, side, note);
        public static void RaiseFreeMove(int side) => FreeMove?.Invoke(side);
        public static void RaiseScore(int combo, int score, float super) => ScoreChanged?.Invoke(combo, score, super);
        public static void RaiseMultiplier(int m) => MultiplierChanged?.Invoke(m);
        public static void RaiseSuperReady() => SuperReady?.Invoke();
        public static void RaiseSuperMove() => SuperMove?.Invoke();
        public static void RaiseHint() => HintRequested?.Invoke();
        public static void RaiseState(GameStateId s) => StateChanged?.Invoke(s);
    }

    public enum GameStateId { Intro, Tutorial, Play, Super, Victory, Fail, EndCard }
}
