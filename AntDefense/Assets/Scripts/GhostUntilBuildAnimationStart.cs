using UnityEngine;

/// <summary>
/// Re-ghostifies an object after placement and restores it to normal
/// the moment its build animation begins progressing.
/// Add this component to the root of a placed object to keep it ghosted
/// until its delayed build animation actually starts.
/// </summary>
public class GhostUntilBuildAnimationStart : MonoBehaviour
{
    private BaseBuildAnimation[] _animations;
    private BaseGhostableMonobehaviour[] _ghostables;

    private void Awake()
    {
        _animations = this.GetComponentsInChildren<BaseBuildAnimation>();
        _ghostables = this.GetComponentsInChildren<BaseGhostableMonobehaviour>(includeInactive: true);
    }

    private void Update()
    {
        foreach (var anim in this._animations)
        {
            if (anim.HasStartedAnimating)
            {
                foreach (var ghostable in this._ghostables)
                    ghostable.UnGhostify();
                Destroy(this);
                return;
            }
        }
    }
}
