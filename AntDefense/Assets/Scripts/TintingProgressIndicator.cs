using UnityEngine;

public class TintingProgressIndicator : ProgressIndicatorBehaviour
{
    public Color Tint;
    public float TintPoint = 0;
    public float NormalPoint = 1;

    private Renderer _renderer;
    private Color _originalColor;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _originalColor = _renderer.material.color;
    }

    public override void AdjustProgress(float progress)
    {
        float t = NormalPoint == TintPoint
            ? (progress == TintPoint ? 1f : 0f)
            : Mathf.Clamp01((progress - NormalPoint) / (TintPoint - NormalPoint));
        _renderer.material.color = Color.Lerp(_originalColor, Tint, t);
    }
}


