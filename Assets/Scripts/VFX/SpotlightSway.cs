using UnityEngine;

namespace DanceTap
{
    /// <summary>Stage beams sway slowly and flash on the beat. Driven by BeatClock (no own Update).</summary>
    public sealed class SpotlightSway : MonoBehaviour, IBeatTickable
    {
        [SerializeField] BeatClock clock;
        [SerializeField] SpriteRenderer[] beams;
        [SerializeField] float amplitude = 6f;
        [Tooltip("One full sway takes this many beats")] [SerializeField] float periodBeats = 8f;
        [SerializeField, Range(0f, 1f)] float baseAlpha = 0.18f, beatFlash = 0.16f;

        Transform[] tr;
        float[] baseZ;
        Color[] baseColor;

        void Awake()
        {
            tr = new Transform[beams.Length];
            baseZ = new float[beams.Length];
            baseColor = new Color[beams.Length];
            for (int i = 0; i < beams.Length; i++)
            {
                tr[i] = beams[i].transform;
                baseZ[i] = tr[i].localEulerAngles.z;
                baseColor[i] = beams[i].color;
            }
        }

        void OnEnable() => clock.Register(this);
        void OnDisable() => clock.Unregister(this);

        public void Tick(double songTime, double songBeat)
        {
            // before the music (and while frozen) the clock still runs, so the stage is alive from frame one
            double b = clock.Paused ? Time.unscaledTimeAsDouble / clock.SecondsPerBeat : songBeat;
            float k = 1f - (float)(b - System.Math.Floor(b)); k *= k;
            float s = (float)System.Math.Sin(b / periodBeats * 6.283185307);
            for (int i = 0; i < tr.Length; i++)
            {
                float dir = (i & 1) == 0 ? 1f : -1f;                       // mirrored pair
                tr[i].localRotation = Quaternion.Euler(0f, 0f, baseZ[i] + amplitude * s * dir);
                var c = baseColor[i]; c.a = baseAlpha + beatFlash * k;
                beams[i].color = c;
            }
        }
    }
}
