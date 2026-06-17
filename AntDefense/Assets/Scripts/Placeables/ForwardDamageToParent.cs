using System.Linq;
using UnityEngine;

/// <summary>
/// Wires a stump to its parent wall node's HealthController so that:
///   - hits on the stump injure the parent node (ImpactDamageHandlers redirected)
///   - the stump's tinting indicators reflect the parent node's health
///   - the stump plays its death animation when the parent node dies
/// Set Target before the first frame (e.g. immediately after Instantiate).
/// </summary>
[DefaultExecutionOrder(-100)]
public class ForwardDamageToParent : MonoBehaviour
{
    public HealthController Target;

    private void Start()
    {
        if (Target == null) return;

        var existing = Target.HealthIndicators ?? new ProgressIndicatorBehaviour[0];
        Target.HealthIndicators = existing.Concat(GetComponentsInChildren<ProgressIndicatorBehaviour>()).ToArray();

        foreach (var dab in GetComponentsInChildren<DeathActionBehaviour>())
            Target.AdditionalDeathActions.Add(dab);

        foreach (var handler in GetComponentsInChildren<ImpactDamageHandler>())
            handler.HealthController = Target;

        foreach (var hc in GetComponentsInChildren<HealthController>())
            hc.enabled = false;
    }
}
