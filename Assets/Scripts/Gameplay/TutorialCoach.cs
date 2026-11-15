using TMPro;
using UnityEngine;

namespace DanceTap
{
    /// <summary>
    /// Freeze tutorial: the first notes stop on their target, the song pauses and the hand shows which side to tap.
    /// The right tap resumes the song (Judge grades that note Perfect). Runs only in the Tutorial state.
    /// </summary>
    public sealed class TutorialCoach : MonoBehaviour, IBeatTickable
    {
        [SerializeField] BeatClock clock;
        [SerializeField] DanceTapConfig config;
        [SerializeField] Judge judge;
        [Tooltip("Root of the hand + label (UI)")] [SerializeField] RectTransform handRoot;
        [SerializeField] TMP_Text label;
        [Tooltip("Hand position over the left / right target (canvas units, 1080x1920)")]
        [SerializeField] Vector2 leftPos = new Vector2(-230, -820), rightPos = new Vector2(310, -820);
        [SerializeField] Vector2 introLabelPos = new Vector2(0, 520), tapLabelPos = new Vector2(-40, 330);
        [SerializeField] string introText = "TAP TO THE BEAT!", tapText = "TAP!";

        Note[] notes;
        double[] times;
        bool[] done;   // already graded (an early tap inside the window) - never freeze on those
        int next, waitingSide = -1;
        bool on;

        void Awake()
        {
            notes = clock.Beatmap.notes;
            times = new double[notes.Length];
            done = new bool[notes.Length];
            for (int i = 0; i < notes.Length; i++) times[i] = notes[i].beat * (60.0 / clock.Beatmap.bpm);
        }

        void OnEnable() { clock.Register(this); GameEvents.Tap += OnTap; GameEvents.StateChanged += OnState; GameEvents.Hit += OnHit; }
        void OnDisable() { clock.Unregister(this); GameEvents.Tap -= OnTap; GameEvents.StateChanged -= OnState; GameEvents.Hit -= OnHit; }

        void OnHit(HitGrade g, int side, int note) => done[note] = true;

        void OnState(GameStateId s)
        {
            on = s == GameStateId.Tutorial;
            if (judge != null) judge.HoldNotes = on ? config.freezeNotes : 0;
            if (s == GameStateId.Intro) Show(0, introText, true);
            else if (s == GameStateId.Tutorial) handRoot.gameObject.SetActive(false);
            if (!on && clock.Paused) { clock.Resume(); waitingSide = -1; }
        }

        public void Tick(double songTime, double songBeat)
        {
            if (!on || !clock.SongStarted || clock.Paused) return;
            while (next < notes.Length && next < config.freezeNotes && done[next]) next++;
            if (next >= notes.Length || next >= config.freezeNotes) return;
            if (songTime < times[next]) return;
            clock.Pause(times[next]);
            waitingSide = notes[next].side;
            Show(waitingSide, tapText, false);
            next++;
        }

        void OnTap(int side)
        {
            if (!clock.Paused || side != waitingSide) return;
            clock.Resume();
            waitingSide = -1;
            handRoot.gameObject.SetActive(false);
        }

        void Show(int side, string text, bool intro)
        {
            var pos = side == 0 ? leftPos : rightPos;
            handRoot.anchoredPosition = pos;
            label.SetText(text);
            label.rectTransform.anchoredPosition = intro ? introLabelPos - pos : tapLabelPos;
            handRoot.gameObject.SetActive(true);
        }
    }
}
