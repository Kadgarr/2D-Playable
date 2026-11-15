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
        [Tooltip("World targets the hand points at (any screen aspect). Empty = fixed canvas positions below")]
        [SerializeField] Transform leftTarget, rightTarget;
        [SerializeField] Camera worldCamera;
        [Tooltip("Hand offset from the target centre (canvas units)")]
        [SerializeField] Vector2 handOffset = new Vector2(40, -80);
        [Tooltip("Fallback hand position over the left / right target (canvas units, 1080x1920)")]
        [SerializeField] Vector2 leftPos = new Vector2(-230, -820), rightPos = new Vector2(310, -820);
        [SerializeField] Vector2 introLabelPos = new Vector2(0, 520), tapLabelPos = new Vector2(-40, 330);
        [SerializeField] string introText = "TAP TO THE BEAT!", tapText = "TAP!";

        Note[] notes;
        double[] times;
        bool[] done;   // already graded (an early tap inside the window) - never freeze on those
        int next, waitingSide = -1, shownSide;
        bool on, shownIntro;
        RectTransform handParent;

        void Awake()
        {
            notes = clock.Beatmap.notes;
            times = new double[notes.Length];
            done = new bool[notes.Length];
            for (int i = 0; i < notes.Length; i++) times[i] = notes[i].beat * (60.0 / clock.Beatmap.bpm);
            handParent = handRoot.parent as RectTransform;
            if (worldCamera == null) worldCamera = Camera.main;
        }

        void OnEnable() { clock.Register(this); GameEvents.Tap += OnTap; GameEvents.StateChanged += OnState; GameEvents.Hit += OnHit; CameraFitter.ViewChanged += OnViewChanged; }
        void OnDisable() { clock.Unregister(this); GameEvents.Tap -= OnTap; GameEvents.StateChanged -= OnState; GameEvents.Hit -= OnHit; CameraFitter.ViewChanged -= OnViewChanged; }

        void OnHit(HitGrade g, int side, int note) => done[note] = true;

        // screen resized while the hand is visible: follow the target again
        void OnViewChanged(Camera _) { if (handRoot.gameObject.activeSelf) Place(shownSide, shownIntro); }

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
            label.SetText(text);
            Place(side, intro);
            handRoot.gameObject.SetActive(true);
        }

        void Place(int side, bool intro)
        {
            shownSide = side;
            shownIntro = intro;
            var pos = HandPosition(side);
            handRoot.anchoredPosition = pos;
            label.rectTransform.anchoredPosition = intro ? introLabelPos - pos : tapLabelPos;
        }

        Vector2 HandPosition(int side)
        {
            var target = side == 0 ? leftTarget : rightTarget;
            if (target == null || worldCamera == null || handParent == null) return side == 0 ? leftPos : rightPos;
            Vector2 screen = worldCamera.WorldToScreenPoint(target.position);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(handParent, screen, null, out local);  // overlay canvas: no camera
            return local + handOffset;
        }
    }
}
