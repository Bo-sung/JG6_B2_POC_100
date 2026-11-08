using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Authoring only: actors, UI and reusable effect slots are serialized into scene/prefab assets.
public static class ArenaSceneBuilder
{
    public const string SCENE_PATH = "Assets/Scenes/ArenaPOC.unity";
    const string ROOT = "Assets/POC";
    const int EFFECT_SLOTS = 160;

    [MenuItem("Tools/POC/Build Arena Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Stop Play mode before authoring the arena.");
        }
        var data = TableManager.Instance.RequireValid().Arena;
        Folder(ROOT + "/Art");
        Folder(ROOT + "/Prefabs");
        Folder(ROOT + "/Abilities");
        var sprite = SpriteAsset();
        var font = FontAsset();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var services = new GameObject("Scene Services");
        var session = services.AddComponent<ArenaSession>();
        session.life = services.AddComponent<LifeManager>();
        session.life.session = session;
        session.damage = services.AddComponent<DamageResolver>();
        session.damage.session = session;
        session.effects = services.AddComponent<EffectSystem>();
        session.effects.session = session;
        session.abilities = services.AddComponent<AbilityController>();
        session.abilities.session = session;
        session.actions = services.AddComponent<PlayerActionController>();
        session.actions.session = session;
        session.actions.source = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        session.spawn = new GameObject("Safe Spawn").transform;
        session.spawn.position = new Vector3(-6, data.rules.floorY + 0.8f, 0);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 8.6f;
        camera.backgroundColor = Presentation.ArenaBackground;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0, -.5f, -10);
        cameraObject.AddComponent<AudioListener>();

        var environment = new GameObject("Arena Geometry").transform;
        Solid("Floor Left", new Vector2(-12.25f, data.rules.floorY - .5f), new Vector2(3.5f, 1), sprite, environment, false);
        Solid("Floor Main", new Vector2(2.75f, data.rules.floorY - .5f), new Vector2(22.5f, 1), sprite, environment, false);
        Solid("Left Wall", new Vector2(data.rules.leftWall - .4f, -2), new Vector2(.8f, 12), sprite, environment, true);
        Solid("Right Wall", new Vector2(data.rules.rightWall + .4f, -2), new Vector2(.8f, 12), sprite, environment, true);
        Solid("Platform Left", new Vector2(-3, -2.6f), new Vector2(3.5f, .35f), sprite, environment, false);
        Solid("Platform Right", new Vector2(3, -2.6f), new Vector2(3.5f, .35f), sprite, environment, false);
        // An overhead ledge makes giant-form ceiling risk tangible while leaving the main floor open.
        Solid("Low Ceiling", new Vector2(-11.5f, .8f), new Vector2(3, .4f), sprite, environment, true);

        var playerObject = Actor("Player", sprite, new Vector2(.8f, 1.4f), Presentation.ArenaInk);
        playerObject.layer = GameLayers.PLAYER_LAYER_NUMBER;
        var body = playerObject.AddComponent<Rigidbody2D>();
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var motor = playerObject.AddComponent<PlayerMotor2D>();
        motor.body = body;
        motor.hurtbox = playerObject.GetComponent<BoxCollider2D>();
        motor.visual = playerObject.GetComponentInChildren<SpriteRenderer>();
        playerObject = Prefab(playerObject, ROOT + "/Prefabs/Player.prefab");
        session.motor = playerObject.GetComponent<PlayerMotor2D>();
        session.motor.session = session;
        playerObject.transform.position = session.spawn.position;

        var bossObject = Actor("Boss", sprite, new Vector2(2.3f, 3.6f), Presentation.BossColor(1, false, false));
        bossObject.layer = GameLayers.ENEMY_LAYER_NUMBER;
        var boss = bossObject.AddComponent<BossController>();
        boss.hurtbox = bossObject.GetComponent<BoxCollider2D>();
        boss.hurtbox.isTrigger = true;
        boss.visual = bossObject.GetComponentInChildren<SpriteRenderer>();
        bossObject = Prefab(bossObject, ROOT + "/Prefabs/Boss.prefab");
        session.boss = bossObject.GetComponent<BossController>();
        session.boss.session = session;
        bossObject.transform.position = new Vector3(data.rules.bossRestX, data.rules.bossRestY, 0);

        var effectTemplate = new GameObject("Ability Effect");
        var slot = effectTemplate.AddComponent<EffectSlot>();
        slot.visual = effectTemplate.AddComponent<SpriteRenderer>();
        slot.visual.sprite = sprite;
        slot.visual.sortingOrder = 5;
        effectTemplate.SetActive(false);
        var effectPrefab = PrefabUtility.SaveAsPrefabAsset(effectTemplate, ROOT + "/Prefabs/Effect.prefab");
        UnityEngine.Object.DestroyImmediate(effectTemplate);
        var effectRoot = new GameObject("Reusable Effects").transform;
        session.effects.slots = new EffectSlot[EFFECT_SLOTS];
        for (int i = 0; i < EFFECT_SLOTS; i++)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(effectPrefab, effectRoot);
            instance.name = "Effect " + i.ToString("000");
            session.effects.slots[i] = instance.GetComponent<EffectSlot>();
        }
        session.abilities.definitions = data.abilities.OrderBy(_entry => _entry.id, StringComparer.Ordinal).Select(_entry => Definition(_entry.id)).ToArray();
        BuildUi(session, font);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        EntryScenes(font);
        var paths = new[] { "Assets/Scenes/Preload.unity", "Assets/Scenes/Title.unity", "Assets/Scenes/Game.unity", SCENE_PATH };
        EditorBuildSettings.scenes = paths.Select(_path => new EditorBuildSettingsScene(_path, true)).ToArray();
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = services;
        Debug.Log("ArenaPOC authored: 20 definitions, 7 boss patterns, 160 effect slots, HUD and result panel.");
    }

    static void EntryScenes(TMP_FontAsset _font)
    {
        foreach (string name in new[] { "Preload", "Title" })
        {
            var entryScene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity", OpenSceneMode.Additive);
            SceneManager.SetActiveScene(entryScene);
            var existing = SceneServices.Find<ArenaEntryPresenter>(entryScene);
            if (!existing)
            {
                var presenter = new GameObject("Arena Entry").AddComponent<ArenaEntryPresenter>();
                if (name == "Title")
                {
                    var canvas = new GameObject("Arena Title", typeof(RectTransform), typeof(Canvas),
                        typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                    canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1600, 900);
                    Image("Backdrop", canvas.transform, Vector2.zero, Vector2.one, Presentation.ArenaBackground);
                    presenter.title = Label("Title", canvas.transform, _font, new Vector2(.1f, .58f), new Vector2(.9f, .80f), 64, TextAlignmentOptions.Center);
                    presenter.controls = Label("Controls", canvas.transform, _font, new Vector2(.1f, .35f), new Vector2(.9f, .52f), 25, TextAlignmentOptions.Center);
                    var button = Image("Start", canvas.transform, new Vector2(.34f, .18f), new Vector2(.66f, .30f), Presentation.ArenaAccent);
                    presenter.startButton = button.gameObject.AddComponent<UnityEngine.UI.Button>();
                    presenter.startButton.targetGraphic = button.GetComponent<UnityEngine.UI.Image>();
                    presenter.startButton.targetGraphic.raycastTarget = true;
                    var caption = Label("Start Caption", button, _font, Vector2.zero, Vector2.one, 32, TextAlignmentOptions.Center);
                    caption.text = Presentation.START;
                    caption.color = Presentation.ArenaBackground;
                    if (!SceneServices.Find<UnityEngine.EventSystems.EventSystem>(entryScene))
                    {
                        var input = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
                        input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                    }
                }
            }
            EditorSceneManager.SaveScene(entryScene);
            EditorSceneManager.CloseScene(entryScene, true);
        }
        SceneManager.SetActiveScene(SceneManager.GetSceneByPath(SCENE_PATH));
    }

    static void BuildUi(ArenaSession _session, TMP_FontAsset _font)
    {
        var canvasObject = new GameObject("Arena HUD", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = .5f;
        var hudRoot = Rect("HUD Content", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var hud = canvasObject.AddComponent<ArenaHudView>();
        Panel(hud, hudRoot.gameObject, true);
        var banner = Image("Boss Backdrop", hudRoot, new Vector2(.2f, .86f), new Vector2(.8f, .97f), Presentation.ArenaPanel);
        hud.bossLabel = Label("Boss HP", banner, _font, new Vector2(.03f, .47f), new Vector2(.97f, .95f), 26, TextAlignmentOptions.Center);
        Image("HP Track", banner, new Vector2(.04f, .13f), new Vector2(.96f, .37f), Presentation.ArenaGround);
        var fill = Image("HP Fill", banner, new Vector2(.04f, .13f), new Vector2(.96f, .37f), Presentation.HazardColor(0, false));
        hud.hpFill = fill.GetComponent<UnityEngine.UI.Image>();
        hud.hpFill.type = UnityEngine.UI.Image.Type.Filled;
        hud.hpFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        hud.hpFill.fillOrigin = 0;
        hud.livesLabel = Label("Lives", hudRoot, _font, new Vector2(.025f, .88f), new Vector2(.2f, .96f), 28, TextAlignmentOptions.Left);
        var abilityBack = Image("Ability Backdrop", hudRoot, new Vector2(.02f, .025f), new Vector2(.62f, .20f), Presentation.ArenaPanel);
        hud.abilityLabel = Label("Ability", abilityBack, _font, new Vector2(.025f, .08f), new Vector2(.98f, .94f), 25, TextAlignmentOptions.Left);
        hud.controlsLabel = Label("Controls", hudRoot, _font, new Vector2(.02f, .205f), new Vector2(.97f, .25f), 18, TextAlignmentOptions.Left);
        hud.patternLabel = Label("Pattern Hint", hudRoot, _font, new Vector2(.22f, .79f), new Vector2(.78f, .85f), 23, TextAlignmentOptions.Center);
        var feed = Rect("Kill Feed", hudRoot, new Vector2(.68f, .50f), new Vector2(.98f, .78f), Vector2.zero, Vector2.zero);
        var layout = feed.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 7;
        hud.feedLabels = new TMP_Text[5];
        for (int i = 0; i < hud.feedLabels.Length; i++)
        {
            var label = Label("Death " + i, feed, _font, Vector2.zero, Vector2.one, 21, TextAlignmentOptions.Right);
            label.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 34;
            hud.feedLabels[i] = label;
        }

        var resultRoot = Image("Result Content", canvasObject.transform, new Vector2(.24f, .25f), new Vector2(.76f, .76f), Presentation.ArenaPanel);
        var resultHost = new GameObject("Result View");
        resultHost.transform.SetParent(canvasObject.transform, false);
        var result = resultHost.AddComponent<ArenaResultView>();
        Panel(result, resultRoot.gameObject, false);
        result.summary = Label("Summary", resultRoot, _font, new Vector2(.07f, .28f), new Vector2(.93f, .91f), 30, TextAlignmentOptions.Center);
        var restartRoot = Image("Restart", resultRoot, new Vector2(.26f, .07f), new Vector2(.74f, .23f), Presentation.ArenaAccent);
        result.restart = restartRoot.gameObject.AddComponent<UnityEngine.UI.Button>();
        result.restart.targetGraphic = restartRoot.GetComponent<UnityEngine.UI.Image>();
        result.restart.targetGraphic.raycastTarget = true;
        var restartCaption = Label("Caption", restartRoot, _font, Vector2.zero, Vector2.one, 26, TextAlignmentOptions.Center);
        restartCaption.text = Presentation.RESTART;
        restartCaption.color = Presentation.ArenaBackground;
        var presenter = _session.gameObject.AddComponent<ArenaHudPresenter>();
        presenter.session = _session;
        presenter.hud = hud;
        presenter.result = result;

        var eventObject = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
        var module = eventObject.GetComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    static void Panel(PanelBase _panel, GameObject _root, bool _startOpen)
    {
        var serialized = new SerializedObject(_panel);
        serialized.FindProperty("root").objectReferenceValue = _root;
        serialized.FindProperty("startOpen").boolValue = _startOpen;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static TMP_Text Label(string _name, Transform _parent, TMP_FontAsset _font,
        Vector2 _min, Vector2 _max, int _size, TextAlignmentOptions _alignment)
    {
        var rect = Rect(_name, _parent, _min, _max, Vector2.zero, Vector2.zero);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = _size;
        label.color = Presentation.ArenaInk;
        label.alignment = _alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        label.text = string.Empty;
        return label;
    }

    static RectTransform Image(string _name, Transform _parent, Vector2 _min, Vector2 _max, Color _color)
    {
        var rect = Rect(_name, _parent, _min, _max, Vector2.zero, Vector2.zero);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = SpriteAsset();
        image.color = _color;
        image.raycastTarget = false;
        return rect;
    }

    static RectTransform Rect(string _name, Transform _parent, Vector2 _min, Vector2 _max, Vector2 _offsetMin, Vector2 _offsetMax)
    {
        var rect = new GameObject(_name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(_parent, false);
        rect.anchorMin = _min;
        rect.anchorMax = _max;
        rect.offsetMin = _offsetMin;
        rect.offsetMax = _offsetMax;
        return rect;
    }

    static GameObject Actor(string _name, Sprite _sprite, Vector2 _size, Color _color)
    {
        var actor = new GameObject(_name);
        actor.AddComponent<BoxCollider2D>().size = _size;
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>();
        visual.transform.SetParent(actor.transform, false);
        visual.transform.localScale = _size;
        visual.sprite = _sprite;
        visual.color = _color;
        visual.sortingOrder = 2;
        var eyes = new GameObject("Eyes").AddComponent<SpriteRenderer>();
        eyes.transform.SetParent(actor.transform, false);
        eyes.transform.localPosition = new Vector3(_size.x * .17f, _size.y * .2f, 0);
        eyes.transform.localScale = new Vector3(_size.x * .35f, _size.y * .08f, 1);
        eyes.sprite = _sprite;
        eyes.color = Presentation.ArenaBackground;
        eyes.sortingOrder = 3;
        return actor;
    }

    static void Solid(string _name, Vector2 _position, Vector2 _size, Sprite _sprite, Transform _parent, bool _wall)
    {
        var solid = new GameObject(_name);
        solid.transform.SetParent(_parent);
        solid.transform.position = _position;
        solid.transform.localScale = _size;
        solid.layer = _wall ? GameLayers.OBSTACLE_LAYER_NUMBER : GameLayers.GROUND_LAYER_NUMBER;
        var visual = solid.AddComponent<SpriteRenderer>();
        visual.sprite = _sprite;
        visual.color = _wall ? Presentation.ArenaGround : Presentation.ArenaPlatform;
        solid.AddComponent<BoxCollider2D>();
    }

    static GameObject Prefab(GameObject _object, string _path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(_object, _path);
        UnityEngine.Object.DestroyImmediate(_object);
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    static AbilityDefinition Definition(string _id)
    {
        string path = ROOT + "/Abilities/" + _id + ".asset";
        var definition = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
        if (!definition)
        {
            definition = ScriptableObject.CreateInstance<AbilityDefinition>();
            definition.id = _id;
            AssetDatabase.CreateAsset(definition, path);
        }
        return definition;
    }

    static Sprite SpriteAsset()
    {
        const string PATH = ROOT + "/Art/Square.asset";
        var existing = AssetDatabase.LoadAllAssetsAtPath(PATH).OfType<Sprite>().FirstOrDefault();
        if (existing)
        {
            return existing;
        }
        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        texture.SetPixels(Enumerable.Repeat(UnityEngine.Color.white, 64).ToArray());
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.name = "SquareTexture";
        AssetDatabase.CreateAsset(texture, PATH);
        var sprite = Sprite.Create(texture, new UnityEngine.Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 8);
        sprite.name = "Square";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        return sprite;
    }

    static TMP_FontAsset FontAsset()
    {
        const string PATH = ROOT + "/Art/ArenaFont.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PATH);
        if (existing)
        {
            return existing;
        }
        var source = AssetDatabase.LoadAssetAtPath<Font>(ROOT + "/Art/NotoSansKR.ttf");
        if (!source)
        {
            throw new InvalidOperationException("Import Assets/POC/Art/NotoSansKR.ttf before authoring UI.");
        }
        var font = TMP_FontAsset.CreateFontAsset(source, 40, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            1024, 1024, AtlasPopulationMode.Dynamic, true);
        font.name = "ArenaFont";
        AssetDatabase.CreateAsset(font, PATH);
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var texture in font.atlasTextures)
        {
            AssetDatabase.AddObjectToAsset(texture, font);
        }
        return font;
    }

    static void Folder(string _path)
    {
        if (AssetDatabase.IsValidFolder(_path))
        {
            return;
        }
        string parent = Path.GetDirectoryName(_path).Replace('\\', '/');
        Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(_path));
    }
}
