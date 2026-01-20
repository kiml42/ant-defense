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

    private Collider _collider;

    private void Start()
    {
        _originalScale = this.transform.localScale;
        _targetScale = _originalScale;
        
        // Find the collider on this object or children
        _collider = this.GetComponent<Collider>();
        if (_collider == null)
        {
            _collider = this.GetComponentInChildren<Collider>();
        }
    }

    private void Update()
    {
        CheckMouseover();
        
        // Smoothly lerp towards target scale
        this.transform.localScale = Vector3.Lerp(
            this.transform.localScale,
            _targetScale,
            Time.deltaTime * this.ScaleLerpSpeed
        );
    }

    private void CheckMouseover()
    {
        if (_collider == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool previousMouseover = _isMouseover;
        
        // Check if raycast hits this button's collider
        if (_collider.Raycast(ray, out RaycastHit hit, 500f))
        {
            _isMouseover = true;
        }
        else
        {
            _isMouseover = false;
        }

        // Call enter/exit when state changes
        if (_isMouseover && !previousMouseover)
        {
            OnMouseEnterButton();
        }
        else if (!_isMouseover && previousMouseover)
        {
            OnMouseExitButton();
        }
    }

    private void OnMouseEnterButton()
    {
        Debug.Log("Mouse Enter QuickBarButton");
        _targetScale = _originalScale * this.MouseoverScale;
    }

    private void OnMouseExitButton()
    {
        Debug.Log("Mouse Exit QuickBarButton");
        _targetScale = _originalScale;
    }
}
