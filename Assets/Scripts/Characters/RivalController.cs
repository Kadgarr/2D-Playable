using UnityEngine;

namespace DanceTap
{
    /// <summary>Rival dances on the beat by itself (hook before the first tap and during the battle).</summary>
    public sealed class RivalController : MonoBehaviour
    {
        [SerializeField] DancerController dancer;
        [Tooltip("Move every N beats")] [SerializeField] int everyBeats = 2;
        [Tooltip("Every Nth move is the wide Perfect version")] [SerializeField] int perfectEvery = 3;

        bool dancing = true;
        int moves;

        void OnEnable() => GameEvents.Beat += OnBeat;
        void OnDisable() => GameEvents.Beat -= OnBeat;

        void Start() => dancer.StartDancing();

        public void StopDancing() => dancing = false;

        void OnBeat(int beat)
        {
            if (!dancing || ((beat % everyBeats) + everyBeats) % everyBeats != 0) return;
            moves++;
            dancer.Move(moves & 1, perfectEvery > 0 && moves % perfectEvery == 0);
        }
    }
}
