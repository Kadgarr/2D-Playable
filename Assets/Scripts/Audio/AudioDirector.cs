using UnityEngine;

namespace DanceTap
{
    /// <summary>SFX on events. Music is scheduled by BeatClock on the first tap. Sources live in the scene.</summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField] AudioSource music;
        [SerializeField] AudioSource endLoop;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip hit, perfect, miss, whoosh, cheer;
        [SerializeField, Range(0f, 1f)] float endCardMusicVolume = 0.5f;

        void OnEnable()
        {
            GameEvents.Hit += OnHit;
            GameEvents.SuperReady += OnSuperReady;
            GameEvents.StateChanged += OnState;
        }

        void OnDisable()
        {
            GameEvents.Hit -= OnHit;
            GameEvents.SuperReady -= OnSuperReady;
            GameEvents.StateChanged -= OnState;
        }

        void OnHit(HitGrade g, int side, int note)
        {
            var clip = g == HitGrade.Perfect ? perfect : g == HitGrade.Good ? hit : miss;
            if (clip != null) sfx.PlayOneShot(clip);
        }

        void OnSuperReady() { if (whoosh != null) sfx.PlayOneShot(whoosh); }

        void OnState(GameStateId s)
        {
            if (s == GameStateId.Victory && cheer != null) sfx.PlayOneShot(cheer);
            if (s == GameStateId.EndCard)
            {
                music.Stop();
                if (endLoop != null) { endLoop.volume = endCardMusicVolume; endLoop.loop = true; endLoop.Play(); }
            }
        }

        /// <summary>Platform mute button (IronSource etc.) goes through here.</summary>
        public static void SetMuted(bool muted) => AudioListener.volume = muted ? 0f : 1f;
    }
}
