using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DanceTap
{
    /// <summary>Combo, score, super bar and the grade popup. SetText with format args - no string garbage.</summary>
    public sealed class HudView : MonoBehaviour, IBeatTickable
    {
        [SerializeField] BeatClock clock;
        [SerializeField] TMP_Text comboText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text multiplierText;
        [SerializeField] Image superFill;
        [SerializeField] TMP_Text gradeText;
        [SerializeField] Color perfectColor = new Color(1f, 0.85f, 0.2f), goodColor = new Color(0.4f, 1f, 1f), missColor = new Color(1f, 0.35f, 0.5f);
        [SerializeField] float popupTime = 0.45f;

        RectTransform gradeRect;
        float popup;
        float superShown, superTarget;

        void Awake()
        {
            gradeRect = gradeText.rectTransform;
            gradeText.gameObject.SetActive(false);
            comboText.SetText("COMBO {0}", 0);
            scoreText.SetText("{0}", 0);
            multiplierText.SetText("x{0}", 1);
            superFill.fillAmount = 0f;
        }

        void OnEnable()
        {
            clock.Register(this);
            GameEvents.ScoreChanged += OnScore;
            GameEvents.MultiplierChanged += OnMultiplier;
            GameEvents.Hit += OnHit;
        }

        void OnDisable()
        {
            clock.Unregister(this);
            GameEvents.ScoreChanged -= OnScore;
            GameEvents.MultiplierChanged -= OnMultiplier;
            GameEvents.Hit -= OnHit;
        }

        void OnScore(int combo, int score, float super)
        {
            comboText.SetText("COMBO {0}", combo);
            scoreText.SetText("{0}", score);
            superTarget = super;
        }

        void OnMultiplier(int m) => multiplierText.SetText("x{0}", m);

        void OnHit(HitGrade g, int side, int note)
        {
            gradeText.SetText(g == HitGrade.Perfect ? "PERFECT!" : g == HitGrade.Good ? "GOOD!" : "MISS...");
            gradeText.color = g == HitGrade.Perfect ? perfectColor : g == HitGrade.Good ? goodColor : missColor;
            gradeText.gameObject.SetActive(true);
            popup = popupTime;
        }

        public void Tick(double songTime, double songBeat)
        {
            float dt = Time.deltaTime;
            if (!Mathf.Approximately(superShown, superTarget))
            {
                superShown = Mathf.MoveTowards(superShown, superTarget, dt * 1.5f);
                superFill.fillAmount = superShown;
            }

            if (popup <= 0f) return;
            popup -= dt;
            float k = 1f - popup / popupTime;                     // 0..1
            float s = k < 0.2f ? Mathf.Lerp(0.6f, 1.15f, k / 0.2f) : Mathf.Lerp(1.15f, 1f, (k - 0.2f) / 0.8f);
            gradeRect.localScale = new Vector3(s, s, 1f);
            if (popup <= 0f) gradeText.gameObject.SetActive(false);
        }
    }
}
