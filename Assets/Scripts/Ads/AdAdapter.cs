using UnityEngine;

namespace DanceTap
{
    /// <summary>Adapter target: what the game needs from an ad network / host page.</summary>
    public interface IAdNetwork
    {
        void OpenStore(string url);
        void GameEnd();
    }

    /// <summary>itch.io / plain WebGL: open the page in a new tab, Game End is a no-op.</summary>
    public sealed class WebNetwork : IAdNetwork
    {
        public void OpenStore(string url) => Application.OpenURL(url);
        public void GameEnd() { }
    }

    /// <summary>MRAID-style networks (IronSource, AppLovin...). In a network build these two calls are bound
    /// to the page bridge (mraid.open / gameEnd) through a .jslib; until then it falls back to OpenURL.</summary>
    public sealed class MraidNetwork : IAdNetwork
    {
        public void OpenStore(string url) => Application.OpenURL(url);
        public void GameEnd() { }
    }

    /// <summary>One place for store clicks, Game End and platform rules. The platform is chosen in the inspector / per build.</summary>
    public sealed class AdAdapter : MonoBehaviour
    {
        public enum Platform { WebItch, Mraid }

        [SerializeField] Platform platform = Platform.WebItch;
        [SerializeField] DanceTapConfig config;

        public static AdAdapter Instance { get; private set; }
        IAdNetwork network;
        bool gameEndSent;

        void Awake()
        {
            Instance = this;
            network = platform == Platform.Mraid ? new MraidNetwork() : (IAdNetwork)new WebNetwork();
        }

        /// <summary>Only from a tap on a button (store redirect by tap - platform rule).</summary>
        public void OpenStore() => network.OpenStore(config.storeUrl);

        public void GameEnd()
        {
            if (gameEndSent) return;
            gameEndSent = true;
            network.GameEnd();
        }
    }
}
