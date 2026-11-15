using UnityEngine;

namespace DanceTap
{
    /// <summary>Hero: turns gameplay events into Animator triggers. Hashes cached, no per-frame code.</summary>
    public sealed class DancerController : MonoBehaviour
    {
        static readonly int StartId = Animator.StringToHash("Start");
        static readonly int MoveL = Animator.StringToHash("MoveL");
        static readonly int MoveR = Animator.StringToHash("MoveR");
        static readonly int PerfectId = Animator.StringToHash("Perfect");
        static readonly int StumbleId = Animator.StringToHash("Stumble");
        static readonly int SuperId = Animator.StringToHash("Super");
        static readonly int VictoryId = Animator.StringToHash("Victory");
        static readonly int SadId = Animator.StringToHash("Sad");
        static readonly int FallId = Animator.StringToHash("Fall");

        [SerializeField] Animator animator;
        [Tooltip("Off for the rival: it is driven by RivalController")]
        [SerializeField] bool reactToPlayer = true;

        bool locked;   // after the finale only end poses play

        void Reset() => animator = GetComponent<Animator>();

        void OnEnable()
        {
            if (!reactToPlayer) return;
            GameEvents.Hit += OnHit;
            GameEvents.FreeMove += OnFreeMove;
        }

        void OnDisable()
        {
            GameEvents.Hit -= OnHit;
            GameEvents.FreeMove -= OnFreeMove;
        }

        void OnHit(HitGrade grade, int side, int note)
        {
            if (locked) return;
            if (grade == HitGrade.Miss) { animator.SetTrigger(StumbleId); return; }
            Move(side, grade == HitGrade.Perfect);
        }

        void OnFreeMove(int side) { if (!locked) Move(side, false); }

        public void Move(int side, bool perfect)
        {
            animator.SetBool(PerfectId, perfect);
            animator.SetTrigger(side == 0 ? MoveL : MoveR);
        }

        public void StartDancing() => animator.SetTrigger(StartId);
        public void SetSpeed(float bpm) => animator.speed = bpm / 120f;   // clips are authored at 120 BPM

        public void PlaySuper() { locked = true; ResetMoves(); animator.SetTrigger(SuperId); }
        public void PlayVictory() { locked = true; ResetMoves(); animator.SetTrigger(VictoryId); }
        public void PlaySad() { locked = true; ResetMoves(); animator.SetTrigger(SadId); }
        public void PlayFall() { locked = true; ResetMoves(); animator.SetTrigger(FallId); }

        void ResetMoves()
        {
            animator.ResetTrigger(MoveL);
            animator.ResetTrigger(MoveR);
            animator.ResetTrigger(StumbleId);
        }
    }
}
