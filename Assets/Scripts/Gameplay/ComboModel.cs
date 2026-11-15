using UnityEngine;

namespace DanceTap
{
    /// <summary>Score, combo, multiplier, super meter and the miss streak. Listens to Hit, publishes results.</summary>
    public sealed class ComboModel : MonoBehaviour
    {
        [SerializeField] DanceTapConfig config;

        public int Combo { get; private set; }
        public int Score { get; private set; }
        public int MissStreak { get; private set; }
        public int Multiplier { get; private set; } = 1;
        public float Super01 => Mathf.Clamp01((float)super / config.superMax);
        public bool SuperFull => super >= config.superMax;

        int super;
        bool superAnnounced;
        public System.Action FailReached;

        void OnEnable() => GameEvents.Hit += OnHit;
        void OnDisable() => GameEvents.Hit -= OnHit;

        void OnHit(HitGrade grade, int side, int note)
        {
            if (grade == HitGrade.Miss)
            {
                Combo = 0;
                MissStreak++;
                SetMultiplier(1);
                Publish();
                if (MissStreak >= config.missesInRowToFail) FailReached?.Invoke();
                return;
            }

            MissStreak = 0;
            Combo++;
            UpdateMultiplier();
            Score += (grade == HitGrade.Perfect ? config.perfectScore : config.goodScore) * Multiplier;
            super += grade == HitGrade.Perfect ? config.perfectSuper : config.goodSuper;
            Publish();
            if (SuperFull) AnnounceSuper();
        }

        /// <summary>Guarantees the finale even for weak players (it's an ad): fills the meter at the super section.</summary>
        public void ForceSuperFull()
        {
            super = config.superMax;
            Publish();
            AnnounceSuper();
        }

        void AnnounceSuper()
        {
            if (superAnnounced) return;
            superAnnounced = true;
            GameEvents.RaiseSuperReady();
        }

        void UpdateMultiplier()
        {
            int m = 1;
            var tiers = config.multiplierCombo;
            for (int i = 0; i < tiers.Length; i++) if (Combo >= tiers[i]) m = i + 2;
            SetMultiplier(m);
        }

        void SetMultiplier(int m)
        {
            if (m == Multiplier) return;
            Multiplier = m;
            GameEvents.RaiseMultiplier(m);
        }

        void Publish() => GameEvents.RaiseScore(Combo, Score, Super01);
    }
}
