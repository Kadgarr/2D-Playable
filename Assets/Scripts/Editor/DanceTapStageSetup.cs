#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DanceTap.EditorTools
{
    /// <summary>
    /// Builds the stage (background, crowd, beams) and the VFX as normal scene objects - everything stays editable.
    /// Safe to run again: objects are reused and only the values below are re-applied.
    /// </summary>
    public static class DanceTapStageSetup
    {
        const string Stage = "Assets/Art/Stage/", Fx = "Assets/Art/FX/", Mat = "Assets/Materials/";
        const float ScreenH = 19.2f;                       // ortho size 9.6
        static readonly Color Pink = new Color(1f, 0.33f, 0.75f), Blue = new Color(0.25f, 0.8f, 1f);
        static readonly Color Yellow = new Color(1f, 0.85f, 0.25f), Green = new Color(0.35f, 1f, 0.55f), Violet = new Color(0.7f, 0.45f, 1f);

        [MenuItem("Tools/Dance Tap/Setup Stage and FX")]
        public static void Setup()
        {
            // ---------- import ----------
            SpriteImport(Stage + "bg_stage.png", 1024f / ScreenH, 1024, SpriteMeshType.FullRect, new Vector2(0.5f, 0.5f));
            SpriteImport(Stage + "bg_crowd.png", 384f / 10.8f, 512, SpriteMeshType.Tight, new Vector2(0.5f, 0f));
            SpriteImport(Fx + "fx_beam.png", 64f, 64, SpriteMeshType.FullRect, new Vector2(0.5f, 1f));
            SpriteImport("Assets/Art/UI/ui_logo.png", 100f, 512, SpriteMeshType.FullRect, new Vector2(0.5f, 0.5f));
            TexImport(Stage + "bg_glowmask.png", 128, false);
            TexImport(Fx + "fx_atlas.png", 128, true);

            // ---------- materials (one shader for every beat layer) ----------
            var beat = Shader.Find("DanceTap/Sprite Beat");
            var mStage = MatAsset("M_Stage_Beat", beat);
            mStage.SetTexture("_GlowMask", AssetDatabase.LoadAssetAtPath<Texture2D>(Stage + "bg_glowmask.png"));
            mStage.SetFloat("_GlowBoost", 0.55f); mStage.SetFloat("_BeatAmp", 0f);
            var mCrowd = MatAsset("M_Crowd_Beat", beat);
            mCrowd.SetFloat("_GlowBoost", 0f); mCrowd.SetFloat("_BeatAmp", 0.22f); mCrowd.SetFloat("_WaveFreq", 0.9f); mCrowd.SetFloat("_WaveLag", 0.05f);
            var mBeam = MatAsset("M_FX_Beam", beat);
            mBeam.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); mBeam.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            var mParticles = ParticleMat();
            foreach (var m in new[] { mStage, mCrowd, mBeam }) EditorUtility.SetDirty(m);

            // ---------- stage ----------
            var bg = Find("Background");
            var stage = Sprite(Child(bg, "Stage"), Stage + "bg_stage.png", mStage, -100, Color.white);
            stage.transform.localPosition = Vector3.zero;
            var crowd = Sprite(Child(bg, "Crowd"), Stage + "bg_crowd.png", mCrowd, -90, Color.white);
            crowd.transform.localPosition = new Vector3(0f, -ScreenH * 0.5f - 0.05f, 0f);

            var spots = Child(bg, "Spotlights");
            var beamL = Sprite(Child(spots, "Beam_L"), Fx + "fx_beam.png", mBeam, -80, new Color(Pink.r, Pink.g, Pink.b, 0.18f));
            beamL.transform.localPosition = new Vector3(-4.7f, 9.9f, 0f); beamL.transform.localRotation = Quaternion.Euler(0, 0, 11f); beamL.transform.localScale = new Vector3(5f, 13.5f, 1f);
            var beamR = Sprite(Child(spots, "Beam_R"), Fx + "fx_beam.png", mBeam, -80, new Color(Blue.r, Blue.g, Blue.b, 0.18f));
            beamR.transform.localPosition = new Vector3(4.7f, 9.9f, 0f); beamR.transform.localRotation = Quaternion.Euler(0, 0, -11f); beamR.transform.localScale = new Vector3(5f, 13.5f, 1f);
            var clock = Object.FindFirstObjectByType<BeatClock>();
            var sway = Get<SpotlightSway>(spots);
            Set(sway, "clock", clock);
            SetArr(sway, "beams", new Object[] { beamL, beamR });

            // characters stand in the two spotlight circles of the background
            foreach (var n in new[] { "Gameplay/Hero", "Gameplay/Rival" })
            {
                var t = Find(n).transform;
                t.position = new Vector3(t.position.x, -1.4f, 0f);
            }

            // ---------- VFX ----------
            var vfx = Find("VFX");
            var lanes = Find("Gameplay/NoteLanes").transform;
            var burstL = HitBurst(lanes.Find("Lane_L").gameObject, Pink, mParticles);
            var burstR = HitBurst(lanes.Find("Lane_R").gameObject, Blue, mParticles);

            var heroX = Find("Gameplay/Hero").transform.position.x;
            var perfect = Child(vfx, "PerfectBurst"); perfect.SetActive(true);
            perfect.transform.position = new Vector3(heroX, 2.9f, 0f);
            var sparkle = PerfectSparkle(perfect, mParticles);

            var confetti = Child(vfx, "ComboConfetti");
            confetti.transform.position = new Vector3(0f, 10.4f, 0f);
            Confetti(confetti, mParticles);
            confetti.SetActive(false);

            var super = Child(vfx, "SuperMoveFX");
            super.transform.position = new Vector3(heroX, 2.9f, 0f);
            SuperBurst(super, mParticles);
            super.SetActive(false);

            var hitFx = Get<HitFxView>(vfx);
            Set(hitFx, "leftBurst", burstL); Set(hitFx, "rightBurst", burstR); Set(hitFx, "heroSparkle", sparkle);

            var flow = Object.FindFirstObjectByType<GameFlow>();
            if (flow != null) { flow.perfectFx = null; flow.superFx = super; flow.confettiFx = confetti; EditorUtility.SetDirty(flow); }

            // ---------- end card logo ----------
            var end = Find("UI").transform.Find("EndCard");
            if (end != null)
            {
                var logoGo = Child(end.gameObject, "Logo");
                var rt = logoGo.GetComponent<RectTransform>(); if (rt == null) rt = logoGo.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = new Vector2(0, 700); rt.sizeDelta = new Vector2(820, 184); rt.localScale = Vector3.one;
                var img = Get<Image>(logoGo); img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ui_logo.png"); img.raycastTarget = false; img.preserveAspect = true;
                logoGo.transform.SetSiblingIndex(0);
                end.GetComponent<Image>().color = new Color(0.06f, 0.02f, 0.12f, 0.72f);
            }

            EditorSceneManager.MarkSceneDirty(bg.scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Dance Tap: stage and FX built.");
        }

        // ---------------- particles ----------------

        static ParticleSystem HitBurst(GameObject lane, Color c, Material mat)
        {
            var go = Child(lane, "HitBurst"); go.transform.localPosition = Vector3.zero;
            var ps = PS(go, mat, 32);
            var main = ps.main;
            main.duration = 1f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(c, Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.55f; sh.radiusThickness = 0f;
            Shrink(ps);
            Frames(ps, 0f, 2f);                                     // glow + star
            return ps;
        }

        static ParticleSystem PerfectSparkle(GameObject go, Material mat)
        {
            var ps = PS(go, mat, 33);
            var main = ps.main;
            main.duration = 1f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startColor = new ParticleSystem.MinMaxGradient(Yellow, Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.7f; sh.radiusThickness = 0.3f;
            Shrink(ps);
            Frames(ps, 1f, 1f);                                     // stars
            return ps;
        }

        static void Confetti(GameObject go, Material mat)
        {
            var ps = PS(go, mat, 34);
            var main = ps.main;
            main.duration = 1.5f; main.loop = false; main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.35f;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Pink, 0f), new GradientColorKey(Blue, 0.25f), new GradientColorKey(Yellow, 0.5f), new GradientColorKey(Green, 0.75f), new GradientColorKey(Violet, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 90), new ParticleSystem.Burst(0.5f, 60) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(11f, 0.5f, 0.1f);
            sh.rotation = new Vector3(90f, 0f, 0f);                 // emit downward
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.4f; noise.quality = ParticleSystemNoiseQuality.Low;
            var col = ps.colorOverLifetime; col.enabled = true;
            var fade = new Gradient(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                                                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = fade;
            Frames(ps, 2f, 2f);                                     // confetti strip
        }

        static void SuperBurst(GameObject go, Material mat)
        {
            var ps = PS(go, mat, 35);
            var main = ps.main;
            main.duration = 1f; main.loop = false; main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startColor = new ParticleSystem.MinMaxGradient(Blue, Yellow);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.6f;
            var vel = ps.limitVelocityOverLifetime; vel.enabled = true; vel.limit = 1.5f; vel.dampen = 0.15f;
            Shrink(ps);
            Frames(ps, 0f, 2f);

            var flashGo = Child(go, "Flash"); flashGo.transform.localPosition = Vector3.zero;
            var fl = PS(flashGo, mat, 34);
            var fm = fl.main;
            fm.duration = 0.5f; fm.loop = false; fm.playOnAwake = true;
            fm.startLifetime = 0.45f; fm.startSpeed = 0f; fm.startSize = 9f;
            fm.startColor = new Color(0.6f, 0.9f, 1f, 0.9f);
            fm.maxParticles = 2;
            var fe = fl.emission; fe.enabled = true; fe.rateOverTime = 0f; fe.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            var fs = fl.shape; fs.enabled = false;
            var fc = fl.colorOverLifetime; fc.enabled = true;
            var fg = new Gradient(); fg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                                                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fc.color = fg;
            var fsz = fl.sizeOverLifetime; fsz.enabled = true; fsz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.2f));
            Frames(fl, 0f, 0f);
        }

        static ParticleSystem PS(GameObject go, Material mat, int order)
        {
            var ps = Get<ParticleSystem>(go);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard; r.sortingOrder = order;
            r.minParticleSize = 0f; r.maxParticleSize = 2f;
            var main = ps.main; main.scalingMode = ParticleSystemScalingMode.Hierarchy; main.stopAction = ParticleSystemStopAction.None;
            return ps;
        }

        static void Shrink(ParticleSystem ps)
        {
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }

        // fx_atlas.png is 2x2: 0 glow, 1 star, 2 confetti, 3 beam
        static void Frames(ParticleSystem ps, float first, float last)
        {
            var ts = ps.textureSheetAnimation; ts.enabled = true;
            ts.mode = ParticleSystemAnimationMode.Grid; ts.numTilesX = 2; ts.numTilesY = 2;
            ts.animation = ParticleSystemAnimationType.WholeSheet;
            ts.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            ts.startFrame = first == last ? new ParticleSystem.MinMaxCurve(first) : new ParticleSystem.MinMaxCurve(first, last + 0.999f);
            ts.cycleCount = 1;
        }

        static Material ParticleMat()
        {
            var path = Mat + "M_FX_Particles.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Fx + "fx_atlas.png"));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);   // transparent, additive
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------- helpers ----------------

        static void SpriteImport(string path, float ppu, int maxSize, SpriteMeshType mesh, Vector2 pivot)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning("Missing " + path); return; }
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false; ti.alphaIsTransparency = true; ti.maxTextureSize = maxSize;
            ti.spritePixelsPerUnit = ppu; ti.wrapMode = TextureWrapMode.Clamp;
            var s = new TextureImporterSettings(); ti.ReadTextureSettings(s);
            s.spriteMeshType = mesh; s.spriteAlignment = (int)SpriteAlignment.Custom; s.spritePivot = pivot; s.spriteExtrude = 1;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }

        static void TexImport(string path, int maxSize, bool alpha)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning("Missing " + path); return; }
            ti.textureType = TextureImporterType.Default; ti.mipmapEnabled = false; ti.maxTextureSize = maxSize;
            ti.sRGBTexture = alpha; ti.alphaIsTransparency = alpha; ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }

        static Material MatAsset(string name, Shader sh)
        {
            var path = Mat + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            return m;
        }

        static SpriteRenderer Sprite(GameObject go, string spritePath, Material mat, int order, Color c)
        {
            var sr = Get<SpriteRenderer>(go);
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            sr.sharedMaterial = mat; sr.sortingOrder = order; sr.color = c;
            return sr;
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

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
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
    }
}
#endif
