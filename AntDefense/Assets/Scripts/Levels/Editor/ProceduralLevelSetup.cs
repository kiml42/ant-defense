#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-shot setup tool. Open ProceduralLevel.unity, then run
/// AntDefense > Setup Procedural Level Scene from the menu.
/// </summary>
public static class ProceduralLevelSetup
{
    [MenuItem("AntDefense/Setup Procedural Level Scene")]
    static void Run()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!scene.name.Contains("ProceduralLevel"))
        {
            Debug.LogError("Open ProceduralLevel.unity first.");
            return;
        }

        // ── Remove old hand-placed objects ──────────────────────────────────
        foreach (var go in scene.GetRootGameObjects())
        {
            var name = go.name;
            if (name.StartsWith("BerryBush") || name.StartsWith("BiscuitPlate") || name.StartsWith("AntNest"))
                Object.DestroyImmediate(go);
        }

        // ── Load prefabs ─────────────────────────────────────────────────────
        var antNestPrefab      = LoadPrefab("Assets/Prefabs/Ants/AntNest.prefab");
        var biscuitPlatePrefab = LoadPrefab("Assets/Prefabs/BiscuitPlate.prefab");
        var berryBushPrefab    = LoadPrefab("Assets/Prefabs/BerryBush.prefab");
        var wallPrefab         = LoadPrefab("Assets/Prefabs/EnvironmentWall.prefab");

        // ── PlayArea ─────────────────────────────────────────────────────────
        var playAreaGO = new GameObject("PlayArea");
        var playAreaCol = playAreaGO.AddComponent<BoxCollider>();
        // Sized to match BasicScenery's inner ground plane
        playAreaGO.transform.position = new Vector3(1.6f, 0f, 7.85f);
        playAreaCol.size = new Vector3(249.2f, 1f, 152.7f);
        playAreaCol.isTrigger = false;
        playAreaGO.SetActive(true);

        // ── EventSystem ───────────────────────────────────────────────────────
        EnsureEventSystem();

        // ── Canvas ────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("ProceduralConfigCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Config panel ──────────────────────────────────────────────────────
        var panel = MakePanel(canvasGO.transform);

        // ── Title ─────────────────────────────────────────────────────────────
        MakeLabel(panel, "Title", "Procedural Level Config", 18, TextAlignmentOptions.Center,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), new Vector2(-20, 25));

        // ── Slider rows ───────────────────────────────────────────────────────
        float rowY = -45;
        const float rowStep = 35f;

        var nestSlider    = MakeSliderRow(panel, "Nests",        1, 5,  2, rowY); rowY -= rowStep;
        var clusterSlider = MakeSliderRow(panel, "Clusters",     1, 15, 6, rowY); rowY -= rowStep;
        var sizeSlider    = MakeSliderRow(panel, "Cluster Size", 1, 10, 4, rowY); rowY -= rowStep;
        var wallSlider    = MakeSliderRow(panel, "Walls",        0, 10, 3, rowY); rowY -= rowStep;

        // ── Config string ─────────────────────────────────────────────────────
        rowY -= 5;
        MakeLabel(panel, "ConfigLabel", "Config string:", 12, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, rowY), new Vector2(-20, 18));
        rowY -= 22;
        var configField = MakeInputField(panel, rowY);
        rowY -= 38;

        // ── Buttons ───────────────────────────────────────────────────────────
        rowY -= 5;
        var regenBtn    = MakeButton(panel, "Regenerate", new Vector2(10, rowY), new Vector2(165, 35));
        var confirmBtn  = MakeButton(panel, "Confirm",    new Vector2(185, rowY), new Vector2(165, 35));

        // ── ProceduralLevelConfigUI ───────────────────────────────────────────
        var configUI = panel.GetComponent<ProceduralLevelConfigUI>()
                    ?? panel.AddComponent<ProceduralLevelConfigUI>();

        configUI.Panel            = panel;
        configUI.NestCountSlider   = nestSlider.slider;
        configUI.ClusterCountSlider = clusterSlider.slider;
        configUI.ClusterSizeSlider  = sizeSlider.slider;
        configUI.WallDensitySlider  = wallSlider.slider;
        configUI.NestCountLabel    = nestSlider.label;
        configUI.ClusterCountLabel  = clusterSlider.label;
        configUI.ClusterSizeLabel   = sizeSlider.label;
        configUI.WallDensityLabel   = wallSlider.label;
        configUI.ConfigStringField  = configField;
        configUI.RegenerateButton   = regenBtn;
        configUI.ConfirmButton      = confirmBtn;

        // ── Generator GO ─────────────────────────────────────────────────────
        var generatorGO = new GameObject("ProceduralLevelGenerator");
        var gen = generatorGO.AddComponent<ProceduralLevelGenerator>();
        gen.AntNestPrefab        = antNestPrefab;
        gen.BiscuitPlatePrefab   = biscuitPlatePrefab;
        gen.BerryBushPrefabs     = new[] { berryBushPrefab };
        gen.EnvironmentWallPrefab = wallPrefab;
        gen.GroundCollider        = playAreaCol;
        gen.ConfigUI              = configUI;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ProceduralLevel setup complete. Press Play to test.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject LoadPrefab(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) Debug.LogWarning($"Prefab not found: {path}");
        return prefab;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static GameObject MakePanel(Transform canvasT)
    {
        var go = new GameObject("ConfigPanel");
        go.transform.SetParent(canvasT, false);
        go.layer = 5;
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-10, -10);
        rt.sizeDelta = new Vector2(370, 480);
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        img.type = Image.Type.Sliced;
        return go;
    }

    static TMP_Text MakeLabel(Transform parent, string name, string text, float fontSize,
        TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = 5;
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    struct SliderRow { public Slider slider; public TMP_Text label; }

    static SliderRow MakeSliderRow(Transform parent, string rowName,
        float min, float max, float val, float topY)
    {
        // Row label
        var labelGO = new GameObject(rowName + "Label");
        labelGO.transform.SetParent(parent, false);
        labelGO.layer = 5;
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 1);
        labelRT.anchorMax = new Vector2(0, 1);
        labelRT.pivot = new Vector2(0, 1);
        labelRT.anchoredPosition = new Vector2(10, topY);
        labelRT.sizeDelta = new Vector2(105, 22);
        labelGO.AddComponent<CanvasRenderer>();
        var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = rowName;
        labelTmp.fontSize = 14;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.raycastTarget = false;

        // Slider
        var sliderGO = new GameObject(rowName + "Slider");
        sliderGO.transform.SetParent(parent, false);
        sliderGO.layer = 5;
        var sliderRT = sliderGO.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0, 1);
        sliderRT.anchorMax = new Vector2(0, 1);
        sliderRT.pivot = new Vector2(0, 1);
        sliderRT.anchoredPosition = new Vector2(118, topY - 4);
        sliderRT.sizeDelta = new Vector2(205, 30);

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        bg.layer = 5;
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero; bgRT.anchoredPosition = Vector2.zero;
        bg.AddComponent<CanvasRenderer>();
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        bgImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bgImg.type = Image.Type.Sliced;

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        fillArea.layer = 5;
        var fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1, 0.75f);
        fillAreaRT.anchoredPosition = new Vector2(-5, 0);
        fillAreaRT.sizeDelta = new Vector2(-20, 0);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        fill.layer = 5;
        var fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0); fillRT.anchorMax = new Vector2(0, 1);
        fillRT.sizeDelta = new Vector2(10, 0);
        fill.AddComponent<CanvasRenderer>();
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 0.9f, 1f);
        fillImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fillImg.type = Image.Type.Sliced;

        // Handle Slide Area
        var hsa = new GameObject("Handle Slide Area");
        hsa.transform.SetParent(sliderGO.transform, false);
        hsa.layer = 5;
        var hsaRT = hsa.AddComponent<RectTransform>();
        hsaRT.anchorMin = Vector2.zero; hsaRT.anchorMax = Vector2.one;
        hsaRT.anchoredPosition = Vector2.zero;
        hsaRT.sizeDelta = new Vector2(-20, 0);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(hsa.transform, false);
        handle.layer = 5;
        var handleRT = handle.AddComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0, 0); handleRT.anchorMax = new Vector2(0, 1);
        handleRT.sizeDelta = new Vector2(20, 0);
        handle.AddComponent<CanvasRenderer>();
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.value = val;

        // Value label
        var valGO = new GameObject(rowName + "ValueLabel");
        valGO.transform.SetParent(parent, false);
        valGO.layer = 5;
        var valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0, 1);
        valRT.anchorMax = new Vector2(0, 1);
        valRT.pivot = new Vector2(0, 1);
        valRT.anchoredPosition = new Vector2(328, topY);
        valRT.sizeDelta = new Vector2(32, 22);
        valGO.AddComponent<CanvasRenderer>();
        var valTmp = valGO.AddComponent<TextMeshProUGUI>();
        valTmp.text = ((int)val).ToString();
        valTmp.fontSize = 14;
        valTmp.alignment = TextAlignmentOptions.MidlineRight;
        valTmp.raycastTarget = false;

        return new SliderRow { slider = slider, label = valTmp };
    }

    static TMP_InputField MakeInputField(Transform parent, float topY)
    {
        var go = new GameObject("ConfigInputField");
        go.transform.SetParent(parent, false);
        go.layer = 5;
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(0, topY);
        rt.sizeDelta = new Vector2(-20, 30);
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        img.type = Image.Type.Sliced;

        // Text Area
        var area = new GameObject("Text Area");
        area.transform.SetParent(go.transform, false);
        area.layer = 5;
        var areaRT = area.AddComponent<RectTransform>();
        areaRT.anchorMin = Vector2.zero; areaRT.anchorMax = Vector2.one;
        areaRT.anchoredPosition = new Vector2(5, 0);
        areaRT.sizeDelta = new Vector2(-10, 0);
        area.AddComponent<RectMask2D>();

        // Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(area.transform, false);
        textGO.layer = 5;
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        textGO.AddComponent<CanvasRenderer>();
        var textTmp = textGO.AddComponent<TextMeshProUGUI>();
        textTmp.text = "";
        textTmp.fontSize = 12;
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;
        textTmp.raycastTarget = false;

        // Placeholder
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(area.transform, false);
        phGO.layer = 5;
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.sizeDelta = Vector2.zero;
        phGO.AddComponent<CanvasRenderer>();
        var phTmp = phGO.AddComponent<TextMeshProUGUI>();
        phTmp.text = "Paste config string here...";
        phTmp.fontSize = 12;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.raycastTarget = false;

        var field = go.AddComponent<TMP_InputField>();
        field.targetGraphic = img;
        field.textViewport = areaRT;
        field.textComponent = textTmp;
        field.placeholder = phTmp;
        field.lineType = TMP_InputField.LineType.SingleLine;

        return field;
    }

    static Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(label + "Button");
        go.transform.SetParent(parent, false);
        go.layer = 5;
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        labelGO.layer = 5;
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.sizeDelta = Vector2.zero;
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }
}
#endif
