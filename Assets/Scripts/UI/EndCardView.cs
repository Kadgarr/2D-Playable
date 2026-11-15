using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DanceTap
{
    /// <summary>End card. The store button calls AdAdapter (store redirect only by tap). Replay restarts the playable.</summary>
    public sealed class EndCardView : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text finalScore;
        [SerializeField] string winTitle = "YOU WON THE BATTLE!";
        [SerializeField] string failTitle = "CAN YOU DO BETTER?";

        public void Show(bool win, int score)
        {
            title.SetText(win ? winTitle : failTitle);
            finalScore.SetText("SCORE {0}", score);
            gameObject.SetActive(true);
        }

        // UI Button OnClick
        public void OnStoreButton() { if (AdAdapter.Instance != null) AdAdapter.Instance.OpenStore(); }

        /// <summary>UI Button OnClick: reload the scene. Every system subscribes in OnEnable / unsubscribes in OnDisable,
        /// so the static GameEvents are clean after the reload. Audio is already unlocked by the earlier taps.</summary>
        public void OnReplayButton() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
