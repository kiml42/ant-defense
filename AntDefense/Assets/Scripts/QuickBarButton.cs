using TMPro;
using UnityEngine;

public class QuickBarButton : ClickableButton
{
    public PlaceableObjectOrGhost Ghost { get; set; }

    public TextMeshPro CostText;

    public float MouseoverScale = 1.1f;
    public float ScaleLerpSpeed = 10f;

    private Vector3 _originalScale;
    private Vector3 _targetScale;
    private bool _isMouseover = false;

    private void Start()
    {
        _originalScale = this.transform.localScale;
        _targetScale = _originalScale;
    }

    private void Update()
    {
        // Smoothly lerp towards target scale
        this.transform.localScale = Vector3.Lerp(
            this.transform.localScale,
            _targetScale,
            Time.deltaTime * this.ScaleLerpSpeed
        );
    }

    private void OnMouseEnter()
    {
        _isMouseover = true;
        _targetScale = _originalScale * this.MouseoverScale;
    }

    private void OnMouseExit()
    {
        _isMouseover = false;
        _targetScale = _originalScale;
    }
}
