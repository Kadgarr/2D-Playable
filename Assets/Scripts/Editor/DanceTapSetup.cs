#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DanceTap.EditorTools
{
    /// <summary>
    /// One-time scene wiring: creates the editable objects (lanes, UI, systems) and links references.
    /// Safe to run again: existing objects are reused, nothing is duplicated. Everything stays editable in the scene.
    /// </summary>
    public static class DanceTapSetup
    {
        const string ArtUI = "Assets/Art/UI/";
        static readonly Color Pink = new Color(1f, 0.33f, 0.75f), Blue = new Color(0.25f, 0.8f, 1f);

        [MenuItem("Tools/Dance Tap/Setup Scene")]
        public static void Setup()
        {
            ImportSettings();
            var config = LoadOrCreate<DanceTapConfig>("Assets/ScriptableObjects/DanceTapConfig.asset");
            var beatmap = LoadOrCreate<BeatmapSO>("Assets/ScriptableObjects/BM_DanceTap.asset");
            beatmap.sourceJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ScriptableObjects/DanceTap_Beatmap.json");
            beatmap.ImportFromJson();
            EditorUtility.SetDirty(config);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            // ---------- audio listener (no listener = no sound at all) ----------
            var camGo = Camera.main.gameObject; if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();

            // ---------- systems ----------
            var systems = Find("Systems");
            var clockGo = Child(systems, "BeatClock");
            var music = Get<AudioSource>(clockGo); music.playOnAwake = false; music.loop = false;
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/DanceTap_Music.mp3");
            var clock = Get<BeatClock>(clockGo);
            Set(clock, "beatmap", beatmap); Set(clock, "music", music);

            var judge = Get<Judge>(Child(systems, "Judge"));
            Set(judge, "clock", clock); Set(judge, "config", config);
            var combo = Get<ComboModel>(Child(systems, "Combo"));
            Set(combo, "config", config);
            var ads = Get<AdAdapter>(Child(systems, "AdAdapter"));
            Set(ads, "config", config);

            var audioGo = Child(systems, "Audio");
            var audio = Get<AudioDirector>(audioGo);
            var endLoop = Get<AudioSource>(Child(audioGo, "EndLoop"));
            endLoop.playOnAwake = false; endLoop.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/DanceTap_EndcardLoop.mp3");
            var sfx = Get<AudioSource>(Child(audioGo, "Sfx")); sfx.playOnAwake = false;
            Set(audio, "music", music); Set(audio, "endLoop", endLoop); Set(audio, "sfx", sfx);
            Set(audio, "hit", Sfx("Hit")); Set(audio, "perfect", Sfx("Perfect")); Set(audio, "miss", Sfx("Miss"));
            Set(audio, "whoosh", Sfx("Whoosh")); Set(audio, "cheer", Sfx("Cheer"));

            // ---------- lanes (prefab) ----------
            var spawner = BuildLanes(clock, judge, config);

            // ---------- characters (components go into the prefab assets) ----------
            AddDancer("Assets/Prefabs/Characters/Hero.prefab", true, false);
            AddDancer("Assets/Prefabs/Characters/Rival.prefab", false, true);
            var heroGo = Find("Gameplay/Hero"); var rivalGo = Find("Gameplay/Rival");

            // ---------- UI ----------
            var ui = Find("UI");
            var canvas = ui.GetComponent<Canvas>();
            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;

            var tapZones = Rct(ui, "TapZones", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            tapZones.SetAsFirstSibling();
            MakeTapZone(tapZones, "Left", 0, new Vector2(0, 0), new Vector2(0.5f, 1));
            MakeTapZone(tapZones, "Right", 1, new Vector2(0.5f, 0), new Vector2(1, 1));

            var hud = Rct(ui, "HUD", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var score = Txt(hud, "Score", font, "0", 72, new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(600, 90), Color.white);
            var comboT = Txt(hud, "Combo", font, "x0", 54, new Vector2(0.5f, 1), new Vector2(-150, -200), new Vector2(300, 70), Pink);
            var mult = Txt(hud, "Multiplier", font, "x1", 54, new Vector2(0.5f, 1), new Vector2(150, -200), new Vector2(300, 70), Blue);
            var barFrame = Img(hud, "SuperBar", Spr("ui_round.png"), new Vector2(0.5f, 1), new Vector2(0, -290), new Vector2(620, 54), new Color(1, 1, 1, 0.25f));
            barFrame.type = Image.Type.Sliced;
            var fill = Img(barFrame.rectTransform, "Fill", Spr("ui_round.png"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 40), new Color(1f, 0.85f, 0.2f));
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0;
            var superLabel = Txt(barFrame.rectTransform, "Label", font, "SUPER", 30, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 50), new Color(0.15f, 0.05f, 0.25f));
            var grade = Txt(hud, "Grade", font, "PERFECT!", 96, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(900, 140), Color.yellow);
            foreach (var t in new[] { score, comboT, mult, superLabel, grade }) t.raycastTarget = false;
            barFrame.raycastTarget = false; fill.raycastTarget = false;
            var hudView = Get<HudView>(hud.gameObject);
            Set(hudView, "clock", clock); Set(hudView, "comboText", comboT); Set(hudView, "scoreText", score);
            Set(hudView, "multiplierText", mult); Set(hudView, "superFill", fill); Set(hudView, "gradeText", grade);

            var handRoot = Rct(ui, "TutorialHand", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-230, -820), new Vector2(160, 160));
            var hand = Img(handRoot, "Hand", Spr("ui_hand.png"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150), Color.white);
            hand.raycastTarget = false;
            var handPulse = Get<PulseView>(hand.gameObject); SetV2(handPulse, "tapOffset", new Vector2(0, -40));
            var tapText = Txt(handRoot, "Label", font, "TAP TO THE BEAT!", 64, new Vector2(0.5f, 0.5f), new Vector2(260, 1080), new Vector2(900, 100), Color.white);
            tapText.raycastTarget = false;

            var superBtnRt = Rct(ui, "SuperButton", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(520, 220));
            var superImg = Get<Image>(superBtnRt.gameObject); superImg.sprite = Spr("ui_round.png"); superImg.type = Image.Type.Sliced; superImg.color = new Color(1f, 0.35f, 0.75f);
            var superBtn = Get<Button>(superBtnRt.gameObject);
            Txt(superBtnRt, "Label", font, "TAP!", 110, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 200), Color.white).raycastTarget = false;
            Get<PulseView>(superBtnRt.gameObject);

            var endRt = Rct(ui, "EndCard", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var endBg = Get<Image>(endRt.gameObject); endBg.color = new Color(0.08f, 0.03f, 0.16f, 0.82f);
            var endTitle = Txt(endRt, "Title", font, "YOU WON THE BATTLE!", 80, new Vector2(0.5f, 0.5f), new Vector2(0, 340), new Vector2(980, 220), Color.white);
            var endScore = Txt(endRt, "Score", font, "SCORE 0", 60, new Vector2(0.5f, 0.5f), new Vector2(0, 210), new Vector2(900, 90), Blue);
            var storeRt = Rct(endRt, "StoreButton", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(640, 170));
            var storeImg = Get<Image>(storeRt.gameObject); storeImg.sprite = Spr("ui_round.png"); storeImg.type = Image.Type.Sliced; storeImg.color = new Color(0.3f, 0.85f, 0.35f);
            var storeBtn = Get<Button>(storeRt.gameObject);
            Txt(storeRt, "Label", font, "GO TO STORE", 64, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 160), Color.white).raycastTarget = false;
            var endView = Get<EndCardView>(endRt.gameObject);
            Set(endView, "title", endTitle); Set(endView, "finalScore", endScore);
            ClearListeners(storeBtn.onClick); UnityEventTools.AddPersistentListener(storeBtn.onClick, endView.OnStoreButton);
            var replayRt = Rct(endRt, "ReplayButton", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -230), new Vector2(420, 120));
            var replayImg = Get<Image>(replayRt.gameObject); replayImg.sprite = Spr("ui_round.png"); replayImg.type = Image.Type.Sliced; replayImg.color = new Color(1f, 1f, 1f, 0.18f);
            var replayBtn = Get<Button>(replayRt.gameObject);
            Txt(replayRt, "Label", font, "PLAY AGAIN", 48, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 110), Color.white).raycastTarget = false;
            ClearListeners(replayBtn.onClick); UnityEventTools.AddPersistentListener(replayBtn.onClick, endView.OnReplayButton);

            // draw order: tap zones, HUD, hand, super button, end card
            tapZones.SetSiblingIndex(0); hud.SetSiblingIndex(1); handRoot.SetSiblingIndex(2); superBtnRt.SetSiblingIndex(3); endRt.SetSiblingIndex(4);

            // ---------- flow ----------
            var flow = Get<GameFlow>(Child(systems, "GameFlow"));
            flow.clock = clock; flow.judge = judge; flow.spawner = spawner; flow.combo = combo; flow.config = config; flow.ads = ads;
            flow.hero = heroGo.GetComponent<DancerController>();
            flow.rivalDancer = rivalGo.GetComponent<DancerController>();
            flow.rival = rivalGo.GetComponent<RivalController>();
            flow.tutorialHand = handRoot.gameObject; flow.superButton = superBtnRt.gameObject; flow.hud = hud.gameObject; flow.endCard = endView;
            var vfx = Find("VFX");
            flow.perfectFx = vfx.transform.Find("PerfectBurst")?.gameObject;
            flow.superFx = vfx.transform.Find("SuperMoveFX")?.gameObject;
            flow.confettiFx = vfx.transform.Find("ComboConfetti")?.gameObject;
            EditorUtility.SetDirty(flow);
            var coach = Get<TutorialCoach>(Child(systems, "TutorialCoach"));
            Set(coach, "clock", clock); Set(coach, "config", config); Set(coach, "judge", judge); Set(coach, "handRoot", handRoot); Set(coach, "label", tapText);
            ClearListeners(superBtn.onClick); UnityEventTools.AddPersistentListener(superBtn.onClick, flow.OnSuperButton);

            superBtnRt.gameObject.SetActive(false);
            endRt.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(heroGo.scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Dance Tap: scene wired.");
        }

        // ---------------- helpers ----------------

        static NoteSpawner BuildLanes(BeatClock clock, Judge judge, DanceTapConfig config)
        {
            const string path = "Assets/Prefabs/Gameplay/NoteLanes.prefab";
            var lanesGo = Find("Gameplay/NoteLanes");
            if (PrefabUtility.IsPartOfPrefabInstance(lanesGo)) PrefabUtility.UnpackPrefabInstance(lanesGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Sprite_Unlit.mat");
            var left = Lane(lanesGo.transform, "Lane_L", new Vector3(-2.7f, -7.4f, 0), Pink, mat, out var lt, out var lp);
            var right = Lane(lanesGo.transform, "Lane_R", new Vector3(2.7f, -7.4f, 0), Blue, mat, out var rt, out var rp);
            lanesGo.transform.position = Vector3.zero;
            var spawner = Get<NoteSpawner>(lanesGo);
            Set(spawner, "clock", clock); Set(spawner, "judge", judge); Set(spawner, "config", config);
            SetArr(spawner, "leftPool", lp); SetArr(spawner, "rightPool", rp);
            Set(spawner, "leftTarget", lt); Set(spawner, "rightTarget", rt);
            Set(spawner, "leftTrack", Track(left, Pink, mat, config.laneHeight)); Set(spawner, "rightTrack", Track(right, Blue, mat, config.laneHeight));
            System.IO.Directory.CreateDirectory("Assets/Prefabs/Gameplay");
            PrefabUtility.SaveAsPrefabAssetAndConnect(lanesGo, path, InteractionMode.AutomatedAction);
            return spawner;
        }

        static GameObject Lane(Transform parent, string name, Vector3 pos, Color c, Material mat, out Transform target, out NoteRing[] pool)
        {
            var lane = Child(parent.gameObject, name);
            lane.transform.localPosition = pos;
            var glow = SpriteObj(lane, "Glow", Spr("ui_glow.png"), new Color(c.r, c.g, c.b, 0.35f), mat, 0, 2.6f);
            var tgt = SpriteObj(lane, "Target", Spr("ui_target.png"), c, mat, 1, 1.8f);
            target = tgt.transform;
            pool = new NoteRing[5];
            for (int i = 0; i < pool.Length; i++)
            {
                var ring = SpriteObj(lane, "Ring_" + i, Spr("ui_ring.png"), c, mat, 2, 1f);
                var nr = Get<NoteRing>(ring);
                Set(nr, "sprite", ring.GetComponent<SpriteRenderer>());
                pool[i] = nr;
            }
            return lane;
        }

        static SpriteRenderer Track(GameObject lane, Color c, Material mat, float h)
        {
            var go = Child(lane, "Track");
            var sr = Get<SpriteRenderer>(go);
            sr.sprite = Spr("ui_round.png"); sr.drawMode = SpriteDrawMode.Sliced; sr.size = new Vector2(1.3f, h + 1.2f);
            sr.color = new Color(c.r, c.g, c.b, 0.12f); sr.sharedMaterial = mat; sr.sortingOrder = 19;
            go.transform.localPosition = new Vector3(0, h * 0.5f, 0); go.transform.localScale = Vector3.one;
            return sr;
        }

        static GameObject SpriteObj(GameObject parent, string name, Sprite s, Color c, Material mat, int order, float scale)
        {
            var go = Child(parent, name);
            var sr = Get<SpriteRenderer>(go);
            sr.sprite = s; sr.color = c; sr.sharedMaterial = mat; sr.sortingOrder = 20 + order;
            go.transform.localScale = new Vector3(scale, scale, 1);
            go.transform.localPosition = Vector3.zero;
            return go;
        }

        static void AddDancer(string prefabPath, bool hero, bool rival)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var dc = Get<DancerController>(root);
            Set(dc, "animator", root.GetComponent<Animator>());
            var so = new SerializedObject(dc); so.FindProperty("reactToPlayer").boolValue = hero; so.ApplyModifiedPropertiesWithoutUndo();
            if (rival) { var rc = Get<RivalController>(root); Set(rc, "dancer", dc); }
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void MakeTapZone(RectTransform parent, string name, int side, Vector2 min, Vector2 max)
        {
            var rt = Rct(parent, name, min, max, Vector2.zero, Vector2.zero);
            var img = Get<Image>(rt.gameObject); img.color = new Color(1, 1, 1, 0); img.raycastTarget = true;
            var z = Get<DanceTap.TapZone>(rt.gameObject);
            var so = new SerializedObject(z); so.FindProperty("side").intValue = side; so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ImportSettings()
        {
            foreach (var n in new[] { "ui_ring.png", "ui_target.png", "ui_glow.png", "ui_round.png", "ui_hand.png" })
            {
                var ti = AssetImporter.GetAtPath(ArtUI + n) as TextureImporter;
                if (ti == null) continue;
                bool changed = ti.textureType != TextureImporterType.Sprite || ti.mipmapEnabled;
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false; ti.maxTextureSize = n == "ui_round.png" || n == "ui_hand.png" || n == "ui_glow.png" ? 128 : 256;
                ti.alphaIsTransparency = true; ti.spritePixelsPerUnit = n == "ui_round.png" || n == "ui_hand.png" ? 100 : 256;   // ppu follows the source size; max size 128 only lowers the glow's resolution
                if (n == "ui_round.png") { var mts = new TextureImporterSettings(); ti.ReadTextureSettings(mts); if (mts.spriteMeshType != SpriteMeshType.FullRect) { mts.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(mts); changed = true; } if (ti.spriteBorder != new Vector4(44, 44, 44, 44)) changed = true; ti.spriteBorder = new Vector4(44, 44, 44, 44); }
                if (changed) ti.SaveAndReimport();
            }
        }

        static Sprite Spr(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtUI + n);
        static AudioClip Sfx(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_" + n + ".mp3");

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static GameObject Find(string path)
        {
            var parts = path.Split('/');
            foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (r.name == parts[0]) return parts.Length == 1 ? r : r.transform.Find(path.Substring(parts[0].Length + 1)).gameObject;
            return new GameObject(parts[0]);
        }

        static GameObject Child(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = parent.layer;
            return go;
        }

        static T Get<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }

        static RectTransform Rct(Object parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var pgo = parent as GameObject ?? ((Component)parent).gameObject;
            var go = Child(pgo, name);
            var rt = go.GetComponent<RectTransform>(); if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            return rt;
        }

        static Image Img(RectTransform parent, string name, Sprite s, Vector2 anchor, Vector2 pos, Vector2 size, Color c)
        {
            var rt = Rct(parent, name, anchor, anchor, pos, size);
            var img = Get<Image>(rt.gameObject); img.sprite = s; img.color = c;
            return img;
        }

        static TextMeshProUGUI Txt(RectTransform parent, string name, TMP_FontAsset font, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box, Color c)
        {
            var rt = Rct(parent, name, anchor, anchor, pos, box);
            var t = Get<TextMeshProUGUI>(rt.gameObject);
            if (font != null) t.font = font;
            if (t.font != null && t.fontSharedMaterial == null) t.fontSharedMaterial = t.font.material;   // fresh component: material not set until Awake
            t.text = text; t.fontSize = size; t.color = c; t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold; t.textWrappingMode = TextWrappingModes.NoWrap;   // no outlineWidth: it makes a material instance per text (extra draw calls); outline goes into one shared font material later
            return t;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetV2(Object target, string field, Vector2 v)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).vector2Value = v;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArr(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ClearListeners(UnityEventBase e)
        {
            for (int i = e.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(e, i);
        }
    }
}
#endif
