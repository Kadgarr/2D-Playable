using UnityEngine;

namespace DanceTap
{
    /// <summary>
    /// State machine: Intro → Tutorial → Play → Super → Victory → EndCard, with a Fail branch.
    /// Each state is a class with Enter/Exit and reacts to beats/taps; GameFlow has no Update.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        [Header("Systems")]
        public BeatClock clock;
        public Judge judge;
        public NoteSpawner spawner;
        public ComboModel combo;
        public DanceTapConfig config;
        public AdAdapter ads;

        [Header("Characters")]
        public DancerController hero;
        public DancerController rivalDancer;
        public RivalController rival;

        [Header("Scene objects (enabled/disabled by states)")]
        public GameObject tutorialHand;
        public GameObject superButton;
        public GameObject hud;
        public EndCardView endCard;
        public GameObject perfectFx, superFx, confettiFx;

        [Header("Fail")]
        [Tooltip("Beats between the fall and the end card")] public int failToEndCardBeats = 4;

        // sections (from the beatmap, with fallbacks)
        internal int tutorialEnd, superStart, superMoveStart, victoryStart, outroStart;
        public int superStartBeat => superStart;

        GameState current;
        internal readonly IntroState intro = new IntroState();
        internal readonly TutorialState tutorial = new TutorialState();
        internal readonly PlayState play = new PlayState();
        internal readonly SuperState super = new SuperState();
        internal readonly VictoryState victory = new VictoryState();
        internal readonly FailState fail = new FailState();
        internal readonly EndCardState endCardState = new EndCardState();

        void Awake()
        {
            var bm = clock.Beatmap;
            tutorialEnd = bm.SectionStart("Main", 16);
            superStart = bm.SectionStart("SuperBuild", 36);
            superMoveStart = bm.SectionStart("SuperMove", 44);
            victoryStart = bm.SectionStart("Victory", 48);
            outroStart = bm.SectionStart("Outro", 56);
            judge.SetLastBeat(superStart);
            hero.SetSpeed(bm.bpm);
            rivalDancer.SetSpeed(bm.bpm);
            if (endCard != null) endCard.gameObject.SetActive(false);
            if (superButton != null) superButton.SetActive(false);
        }

        void OnEnable()
        {
            GameEvents.Beat += OnBeat;
            GameEvents.Tap += OnTap;
            GameEvents.HintRequested += OnHint;
            GameEvents.Hit += OnHit;
            combo.FailReached += OnFail;
        }

        void OnDisable()
        {
            GameEvents.Beat -= OnBeat;
            GameEvents.Tap -= OnTap;
            GameEvents.HintRequested -= OnHint;
            GameEvents.Hit -= OnHit;
            combo.FailReached -= OnFail;
        }

        void Start() => Go(intro);

        internal void Go(GameState next)
        {
            current?.Exit(this);
            current = next;
            current.Enter(this);
            GameEvents.RaiseState(current.Id);
        }

        void OnBeat(int beat) => current?.OnBeat(this, beat);
        void OnTap(int side) => current?.OnTap(this, side);
        void OnHint() { if (tutorialHand != null) tutorialHand.SetActive(true); }
        void OnFail() { if (current == play) Go(fail); }

        void OnHit(HitGrade g, int side, int note)
        {
            if (g == HitGrade.Perfect) Pulse(perfectFx);
        }

        /// <summary>Super button OnClick.</summary>
        public void OnSuperButton() => current?.OnSuperPressed(this);

        internal static void Pulse(GameObject fx)
        {
            if (fx == null) return;
            fx.SetActive(false);   // re-enable restarts the particle system set to Play On Awake
            fx.SetActive(true);
        }
    }

    // ---------------- States ----------------

    public abstract class GameState
    {
        public abstract GameStateId Id { get; }
        public virtual void Enter(GameFlow f) { }
        public virtual void Exit(GameFlow f) { }
        public virtual void OnBeat(GameFlow f, int beat) { }
        public virtual void OnTap(GameFlow f, int side) { }
        public virtual void OnSuperPressed(GameFlow f) { }
    }

    /// <summary>Silent hook: rival dances, hero is shy, hand pulses. The first tap starts the music.</summary>
    public sealed class IntroState : GameState
    {
        public override GameStateId Id => GameStateId.Intro;

        public override void Enter(GameFlow f)
        {
            f.judge.SetActive(false);
            f.spawner.SetSpawning(false);
            if (f.tutorialHand != null) f.tutorialHand.SetActive(true);
        }

        public override void OnTap(GameFlow f, int side)
        {
            f.clock.StartSong();
            f.hero.StartDancing();
            f.hero.Move(side, false);
            f.Go(f.tutorial);
        }
    }

    public sealed class TutorialState : GameState
    {
        public override GameStateId Id => GameStateId.Tutorial;

        public override void Enter(GameFlow f)
        {
            f.judge.SetWindows(f.config.tutorialWindows);
            f.judge.SetActive(true);
            f.spawner.SetSpawning(true);
        }


        public override void OnBeat(GameFlow f, int beat)
        {
            if (f.clock.SongStarted && beat >= f.tutorialEnd) f.Go(f.play);
        }
    }

    public sealed class PlayState : GameState
    {
        public override GameStateId Id => GameStateId.Play;

        public override void Enter(GameFlow f)
        {
            f.judge.SetWindows(f.config.normalWindows);
            if (f.tutorialHand != null) f.tutorialHand.SetActive(false);
        }

        public override void OnBeat(GameFlow f, int beat)
        {
            if (beat >= f.superStart) f.Go(f.super);
        }
    }

    /// <summary>Meter full → big TAP! button. Pressing it (or waiting until the SuperMove beat) plays the finale.</summary>
    public sealed class SuperState : GameState
    {
        bool done;
        public override GameStateId Id => GameStateId.Super;

        public override void Enter(GameFlow f)
        {
            done = false;
            f.judge.SetActive(false);
            f.spawner.SetSpawning(false);
            f.combo.ForceSuperFull();
            if (f.superButton != null) f.superButton.SetActive(true);
        }

        public override void OnSuperPressed(GameFlow f) => Fire(f);

        public override void OnBeat(GameFlow f, int beat)
        {
            if (!done && beat >= f.superMoveStart) Fire(f);
            if (beat >= f.victoryStart) f.Go(f.victory);
        }

        void Fire(GameFlow f)
        {
            if (done) return;
            done = true;
            if (f.superButton != null) f.superButton.SetActive(false);
            f.hero.PlaySuper();
            f.rival.StopDancing();
            f.rivalDancer.PlayFall();
            GameFlow.Pulse(f.superFx);
            GameFlow.Pulse(f.confettiFx);
            GameEvents.RaiseSuperMove();
        }
    }

    public sealed class VictoryState : GameState
    {
        public override GameStateId Id => GameStateId.Victory;

        public override void Enter(GameFlow f)
        {
            f.hero.PlayVictory();
            f.rivalDancer.PlaySad();
        }

        public override void OnBeat(GameFlow f, int beat)
        {
            if (beat >= f.outroStart) f.Go(f.endCardState.With(true));
        }
    }

    /// <summary>4 misses in a row: hero falls, rival celebrates, early end card ("Can you do better?").</summary>
    public sealed class FailState : GameState
    {
        int endBeat = int.MaxValue;
        public override GameStateId Id => GameStateId.Fail;

        public override void Enter(GameFlow f)
        {
            f.judge.SetActive(false);
            f.spawner.SetSpawning(false);
            f.hero.PlayFall();
            f.rival.StopDancing();
            f.rivalDancer.PlayVictory();
            endBeat = (int)System.Math.Floor(f.clock.SongBeat) + f.failToEndCardBeats;
        }

        public override void OnBeat(GameFlow f, int beat)
        {
            if (beat >= endBeat) f.Go(f.endCardState.With(false));
        }
    }

    public sealed class EndCardState : GameState
    {
        bool win;
        public override GameStateId Id => GameStateId.EndCard;

        public EndCardState With(bool won) { win = won; return this; }

        public override void Enter(GameFlow f)
        {
            if (f.hud != null) f.hud.SetActive(false);
            if (f.superButton != null) f.superButton.SetActive(false);
            if (f.tutorialHand != null) f.tutorialHand.SetActive(false);
            f.endCard.Show(win, f.combo.Score);
            f.ads.GameEnd();   // Mintegral: Game End when the end card is shown
        }
    }
}
