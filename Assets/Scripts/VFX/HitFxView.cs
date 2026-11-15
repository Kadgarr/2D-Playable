using UnityEngine;

namespace DanceTap
{
    /// <summary>
    /// Hit feedback particles. The systems live in the scene (editable); this only calls Emit on events,
    /// so nothing is instantiated and nothing runs per frame.
    /// </summary>
    public sealed class HitFxView : MonoBehaviour
    {
        [SerializeField] ParticleSystem leftBurst, rightBurst;
        [Tooltip("Stars around the hero on Perfect (optional)")] [SerializeField] ParticleSystem heroSparkle;
        [SerializeField] int goodCount = 8, perfectCount = 18, sparkleCount = 10;

        void OnEnable() => GameEvents.Hit += OnHit;
        void OnDisable() => GameEvents.Hit -= OnHit;

        void OnHit(HitGrade grade, int side, int note)
        {
            if (grade == HitGrade.Miss) return;
            var ps = side == 0 ? leftBurst : rightBurst;
            bool perfect = grade == HitGrade.Perfect;
            if (ps != null) ps.Emit(perfect ? perfectCount : goodCount);
            if (perfect && heroSparkle != null) heroSparkle.Emit(sparkleCount);
        }
    }
}
