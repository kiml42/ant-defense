using UnityEngine;

/// <summary>
/// Redirects all child ImpactDamageHandlers to a target HealthController and disables
/// the stump's own HealthControllers, so hits on the stump injure the parent wall node.
/// Set Target before the first frame (e.g. immediately after Instantiate).
/// </summary>
[DefaultExecutionOrder(-100)]
public class ForwardDamageToParent : MonoBehaviour
{
    public HealthController Target;

    private void Start()
    {
        if (Target == null) return;

        foreach (var handler in GetComponentsInChildren<ImpactDamageHandler>())
            handler.HealthController = Target;

        foreach (var hc in GetComponentsInChildren<HealthController>())
            hc.enabled = false;
    }
}
