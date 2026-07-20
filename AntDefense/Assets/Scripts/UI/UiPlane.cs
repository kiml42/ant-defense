using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiPlane : SingletonMonoBehaviour<UiPlane>
{
    public QuickBarButton QuickBarButton;
    public Transform QuickBarContainer;
    private List<QuickBarButton> _buttons = null;

    private static readonly List<ProtectMeBarObject> ProtectMes = new List<ProtectMeBarObject>();
    private bool _anythingEverRegistered = false;

    private void Start()
    {
        this.InitialiseQuickBar();
    }

    private void Update()
    {
        ProtectMes.RemoveAll(p => p.ProtectMe == null);
        if (!_anythingEverRegistered) return;
        if (ProtectMes.Count == 0)
        {
            Debug.Log("GAME OVER");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    private void InitialiseQuickBar()
    {
        if (this._buttons != null) return;

        this._buttons = new List<QuickBarButton>();
        var quickBarObjects = ObjectPlacer.Instance.QuickBarObjects;

        foreach (var ghost in quickBarObjects)
        {
            var newButton = Instantiate(this.QuickBarButton, this.QuickBarContainer);
            newButton.Ghost = ghost;

            if (newButton.NameText != null)
                newButton.NameText.text = ghost.DisplayName;

            if (newButton.CostText != null)
                newButton.CostText.text = $"£{ghost.BaseCost:F2}";

            if (newButton.Icon != null && ghost.ButtonIcon != null)
                newButton.Icon.sprite = ghost.ButtonIcon;

            var captured = ghost;
            var btn = newButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => ObjectPlacer.Instance.StartPlacingGhost(captured));

            this._buttons.Add(newButton);
        }
    }

    internal static void RegisterProtectMe(ProtectMe protectMe)
    {
        if (!ProtectMes.Any(p => p.ProtectMe == protectMe))
        {
            ProtectMes.Add(new ProtectMeBarObject(protectMe));
            Instance._anythingEverRegistered = true;
        }
    }

    private class ProtectMeBarObject
    {
        public ProtectMe ProtectMe;
        public ProtectMeBarObject(ProtectMe protectMe)
        {
            this.ProtectMe = protectMe;
        }
    }
}
