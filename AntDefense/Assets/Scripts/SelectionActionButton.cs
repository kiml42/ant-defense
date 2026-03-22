using UnityEngine;

/// <summary>
/// Base class for world-space buttons that appear in the selection actions panel.
/// Attach to a GameObject on the UI layer with a Collider.
/// </summary>
public abstract class SelectionActionButton : ClickableButton
{
    public float MouseoverScale = 1.1f;
    public float ScaleLerpSpeed = 10f;

    private Vector3 _originalScale;
    private Vector3 _targetScale;
    private bool _isMouseover = false;
    private Collider _collider;

    protected virtual void Start()
    {
        this._originalScale = this.transform.localScale;
        this._targetScale = this._originalScale;
        this._collider = this.GetComponent<Collider>() ?? this.GetComponentInChildren<Collider>();
    }

    protected virtual void Update()
    {
        this.CheckMouseover();
        this.transform.localScale = Vector3.Lerp(this.transform.localScale, this._targetScale, Time.deltaTime * this.ScaleLerpSpeed);
    }

    private void CheckMouseover()
    {
        if (this._collider == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool wasMouseover = this._isMouseover;
        this._isMouseover = this._collider.Raycast(ray, out _, 500f);

        if (this._isMouseover && !wasMouseover)
            this._targetScale = this._originalScale * this.MouseoverScale;
        else if (!this._isMouseover && wasMouseover)
            this._targetScale = this._originalScale;
    }

    public abstract void Execute();
}
