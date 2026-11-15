using UnityEngine;

namespace DanceTap
{
    /// <summary>
    /// Falling notes on two lanes. Object pool: notes already live in the NoteLanes prefab (no Instantiate at runtime).
    /// A note spawns leadBeats before its hit, laneHeight above the target, and falls onto it,
    /// so the player sees 2-3 notes ahead.
    /// </summary>
    public sealed class NoteSpawner : MonoBehaviour, IBeatTickable
    {
        [SerializeField] BeatClock clock;
        [SerializeField] Judge judge;
        [SerializeField] DanceTapConfig config;
        [Tooltip("Pool for the left lane")] [SerializeField] NoteRing[] leftPool;
        [Tooltip("Pool for the right lane")] [SerializeField] NoteRing[] rightPool;
        [SerializeField] Transform leftTarget, rightTarget;
        [Tooltip("Lane strips that light up on tap (optional)")] [SerializeField] SpriteRenderer leftTrack, rightTrack;
        [SerializeField] float hitPunch = 1.25f;
        [SerializeField, Range(0f, 1f)] float trackAlpha = 0.09f, trackFlash = 0.35f;
        [Tooltip("Note scale while falling: far .. on the target")] [SerializeField] float farScale = 0.75f;
        [Tooltip("Note size on the target (matches the target ring)")] [SerializeField] float noteSize = 1.6f;

        Note[] notes;
        int nextSpawn;
        bool spawning;
        int[] ringOfNote;
        float leftPunch, rightPunch;
        Color leftTrackColor, rightTrackColor;
        Vector3 leftTargetScale, rightTargetScale;

        public void SetSpawning(bool on) => spawning = on;

        void Awake()
        {
            notes = clock.Beatmap.notes;
            ringOfNote = new int[notes.Length];
            for (int i = 0; i < ringOfNote.Length; i++) ringOfNote[i] = -1;
            if (leftTrack != null) leftTrackColor = leftTrack.color;
            if (rightTrack != null) rightTrackColor = rightTrack.color;
            leftTargetScale = leftTarget.localScale;
            rightTargetScale = rightTarget.localScale;
        }

        void OnEnable() { clock.Register(this); GameEvents.Hit += OnHit; GameEvents.Tap += OnTap; }
        void OnDisable() { clock.Unregister(this); GameEvents.Hit -= OnHit; GameEvents.Tap -= OnTap; }

        void OnTap(int side) { if (side == 0) leftPunch = 1f; else rightPunch = 1f; }

        void OnHit(HitGrade grade, int side, int note)
        {
            int r = ringOfNote[note];
            if (r < 0) return;
            Pool(side)[r].Hide();
            ringOfNote[note] = -1;
        }

        NoteRing[] Pool(int side) => side == 0 ? leftPool : rightPool;

        public void Tick(double songTime, double songBeat)
        {
            // tap feedback: target punch + lane flash, decays without allocations (real time: works while frozen)
            float decay = Time.unscaledDeltaTime * 6f;
            leftPunch = Mathf.Max(0f, leftPunch - decay);
            rightPunch = Mathf.Max(0f, rightPunch - decay);
            leftTarget.localScale = leftTargetScale * (1f + (hitPunch - 1f) * leftPunch);
            rightTarget.localScale = rightTargetScale * (1f + (hitPunch - 1f) * rightPunch);
            Flash(leftTrack, leftTrackColor, leftPunch);
            Flash(rightTrack, rightTrackColor, rightPunch);

            if (!clock.SongStarted) return;
            float lead = config.leadBeats;

            while (spawning && nextSpawn < notes.Length && songBeat >= notes[nextSpawn].beat - lead)
            {
                if (judge.IsPlayable(nextSpawn)) Spawn(nextSpawn);
                nextSpawn++;
            }

            Animate(leftPool, songBeat, lead);
            Animate(rightPool, songBeat, lead);
        }

        void Flash(SpriteRenderer track, Color baseColor, float punch)
        {
            if (track == null) return;
            var c = baseColor; c.a = trackAlpha + trackFlash * punch;
            track.color = c;
        }

        void Spawn(int note)
        {
            var pool = Pool(notes[note].side);
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Busy) continue;
                pool[i].Show(note);
                ringOfNote[note] = i;
                return;
            }
        }

        void Animate(NoteRing[] pool, double songBeat, float lead)
        {
            float h = config.laneHeight;
            for (int i = 0; i < pool.Length; i++)
            {
                var ring = pool[i];
                if (!ring.Busy) continue;
                float t = (float)(1.0 - (notes[ring.NoteIndex].beat - songBeat) / lead);   // 0 spawn .. 1 on target
                if (t > 1.5f) { ringOfNote[ring.NoteIndex] = -1; ring.Hide(); continue; }  // missed: Judge already graded it
                float y = h * (1f - t);                                                  // keeps falling past the target
                float scale = (t <= 1f ? Mathf.Lerp(farScale, 1f, t) : 1f) * noteSize;
                float alpha = t <= 1f ? Mathf.Clamp01(t * 6f) : Mathf.Clamp01((1.5f - t) / 0.5f);
                ring.SetPose(y, scale, alpha);
            }
        }
    }
}
