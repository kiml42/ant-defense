using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay panel displayed before the procedural match begins.
/// Wire up all fields in the Inspector on the ProceduralLevel canvas.
/// </summary>
public class ProceduralLevelConfigUI : MonoBehaviour
{
    [Header("Panel Root")]
    public GameObject Panel;

    [Header("Sliders")]
    public Slider NestCountSlider;
    public Slider ClusterCountSlider;
    public Slider ClusterSizeSlider;
    public Slider WallDensitySlider;

    [Header("Slider Value Labels")]
    public TMP_Text NestCountLabel;
    public TMP_Text ClusterCountLabel;
    public TMP_Text ClusterSizeLabel;
    public TMP_Text WallDensityLabel;

    [Header("Seed & Config String")]
    public TMP_InputField ConfigStringField;

    [Header("Buttons")]
    public Button RegenerateButton;
    public Button ConfirmButton;

    // ── Slider ranges ─────────────────────────────────────────────────────────

    private const int NestMin = 1, NestMax = 5;
    private const int ClusterMin = 1, ClusterMax = 15;
    private const int SizeMin = 1, SizeMax = 10;
    private const int WallMin = 0, WallMax = 10;

    // ── Internal state ────────────────────────────────────────────────────────

    private ProceduralLevelGenerator _generator;
    private bool _ignoreSliderEvents;
    private Coroutine _generateCoroutine;
    private const float GenerateDelay = 0.15f;

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Initialise(ProceduralLevelGenerator generator, ProceduralLevelConfig initialConfig)
    {
        _generator = generator;

        ConfigureSlider(NestCountSlider, NestMin, NestMax);
        ConfigureSlider(ClusterCountSlider, ClusterMin, ClusterMax);
        ConfigureSlider(ClusterSizeSlider, SizeMin, SizeMax);
        ConfigureSlider(WallDensitySlider, WallMin, WallMax);

        NestCountSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        ClusterCountSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        ClusterSizeSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        WallDensitySlider.onValueChanged.AddListener(_ => OnSliderChanged());

        ConfigStringField.onEndEdit.AddListener(OnConfigStringEdited);

        RegenerateButton.onClick.AddListener(OnRegenerateClicked);
        ConfirmButton.onClick.AddListener(OnConfirmClicked);

        ApplyConfigToSliders(initialConfig);
        Panel.SetActive(true);
        SetBuildButtonsVisible(false);
    }

    public void Show()
    {
        Panel.SetActive(true);
        SetBuildButtonsVisible(false);
    }

    public void Hide()
    {
        Panel.SetActive(false);
        SetBuildButtonsVisible(true);
    }

    private static void SetBuildButtonsVisible(bool visible)
    {
        if (UiPlane.Instance != null)
            UiPlane.Instance.QuickBarContainer.gameObject.SetActive(visible);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnSliderChanged()
    {
        if (_ignoreSliderEvents) return;
        var config = ReadConfigFromSliders();
        RefreshLabels(config);
        UpdateConfigStringDisplay(config);

        // Debounce: wait until the slider stops moving before regenerating.
        // WaitForSecondsRealtime works even while the game is paused.
        if (_generateCoroutine != null) StopCoroutine(_generateCoroutine);
        _generateCoroutine = StartCoroutine(GenerateAfterDelay(config));
    }

    private IEnumerator GenerateAfterDelay(ProceduralLevelConfig config)
    {
        yield return new WaitForSecondsRealtime(GenerateDelay);
        _generator.Generate(config);
        _generateCoroutine = null;
    }

    private void OnRegenerateClicked()
    {
        var config = ReadConfigFromSliders();
        _generator.Regenerate(config);
        ApplyConfigToSliders(_generator.CurrentConfig);
        UpdateConfigStringDisplay(_generator.CurrentConfig);
    }

    private void OnConfirmClicked()
    {
        _generator.Confirm();
    }

    private void OnConfigStringEdited(string value)
    {
        if (!ProceduralLevelConfigEncoder.TryDecode(value, out var config))
        {
            // Invalid string — restore the last valid display
            UpdateConfigStringDisplay(_generator.CurrentConfig);
            return;
        }

        ApplyConfigToSliders(config);
        _generator.Generate(config);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ProceduralLevelConfig ReadConfigFromSliders() => new ProceduralLevelConfig(
        Mathf.RoundToInt(NestCountSlider.value),
        Mathf.RoundToInt(ClusterCountSlider.value),
        _generator.CurrentConfig.ClusterRadius,      // no slider yet — preserved from current config
        Mathf.RoundToInt(ClusterSizeSlider.value),   // ClusterDensity, slider wired in Inspector as ClusterSize
        Mathf.RoundToInt(WallDensitySlider.value),
        _generator.CurrentConfig.Seed);

    private void ApplyConfigToSliders(ProceduralLevelConfig config)
    {
        _ignoreSliderEvents = true;
        NestCountSlider.value = config.NestCount;
        ClusterCountSlider.value = config.ClusterCount;
        ClusterSizeSlider.value = config.ClusterDensity;
        WallDensitySlider.value = config.WallDensity;
        _ignoreSliderEvents = false;

        RefreshLabels(config);
        UpdateConfigStringDisplay(config);
    }

    private void RefreshLabels(ProceduralLevelConfig config)
    {
        SetLabel(NestCountLabel, config.NestCount);
        SetLabel(ClusterCountLabel, config.ClusterCount);
        SetLabel(ClusterSizeLabel, config.ClusterDensity);
        SetLabel(WallDensityLabel, config.WallDensity);
    }

    private void UpdateConfigStringDisplay(ProceduralLevelConfig config)
    {
        if (ConfigStringField != null)
            ConfigStringField.SetTextWithoutNotify(ProceduralLevelConfigEncoder.Encode(config));
    }

    private static void ConfigureSlider(Slider s, int min, int max)
    {
        s.minValue = min;
        s.maxValue = max;
        s.wholeNumbers = true;
    }

    private static void SetLabel(TMP_Text label, int value)
    {
        if (label != null) label.text = value.ToString();
    }
}
