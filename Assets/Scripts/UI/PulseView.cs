using UnityEngine;

namespace DanceTap
{
    /// <summary>Beat-synced pulse for the tutorial hand / super button (driven by BeatClock, no own Update).</summary>
    public sealed class PulseView : MonoBehaviour, IBeatTickable
    {
        [SerializeField] float amount = 0.12f;
        [Tooltip("Optional: the hand taps toward the targets")] [SerializeField] Vector2 tapOffset;

        Transform tr;
        Vector3 basePos;

        void Awake() { tr = transform; basePos = tr.localPosition; }

        void OnEnable() { if (BeatClock.Instance != null) BeatClock.Instance.Register(this); }
        void OnDisable() { if (BeatClock.Instance != null) BeatClock.Instance.Unregister(this); }

        public void Tick(double songTime, double songBeat)
        {
            var clk = BeatClock.Instance;
            double b = clk != null && clk.Paused ? Time.unscaledTimeAsDouble / clk.SecondsPerBeat : songBeat;   // frozen song: pulse on real time
            float p = (float)(b - System.Math.Floor(b));   // 0..1 inside the beat
            float k = 1f - p;
            k *= k;                                                      // sharp on the beat
            float s = 1f + amount * k;
            tr.localScale = new Vector3(s, s, 1f);
            tr.localPosition = basePos + (Vector3)(tapOffset * k);
        }
    }
}
