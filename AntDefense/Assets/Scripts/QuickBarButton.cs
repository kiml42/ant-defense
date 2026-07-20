using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickBarButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public PlaceableObjectOrGhost Ghost { get; set; }

    public TMP_Text NameText;
    public TMP_Text CostText;
    public Image Icon;

    public float MouseoverScale = 1.1f;
    public float ScaleLerpSpeed = 10f;

    private Vector3 _originalScale;
    private Vector3 _targetScale;

    private void Start()
    {
        _originalScale = this.transform.localScale;
        _targetScale = _originalScale;
    }

    private void Update()
    {
        this.transform.localScale = Vector3.Lerp(
            this.transform.localScale,
            _targetScale,
            Time.deltaTime * this.ScaleLerpSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetScale = _originalScale * this.MouseoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _originalScale;
    }
}
