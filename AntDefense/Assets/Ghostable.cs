using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ghostable : MonoBehaviour
{
    public Material GhostMaterial;
    private (Renderer renderer, Material[] originalMaterial)[] _renderers;
    private (Renderer renderer, Material[] originalMaterial)[] Renderers
    {
        get
        {
            return this._renderers ??= this.GetComponentsInChildren<Renderer>().Select(r => (r, r.materials.ToArray())).ToArray();
        }
    }
    public List<MonoBehaviour> ComponentsToDisable;
    private Collider[] _collidersToDisable;
    private Collider[] CollidersToDisable
    {
        get
        {
            return this._collidersToDisable ??= this.GetComponentsInChildren<Collider>();
        }
    }

    public void Ghostify()
    {
        Debug.Log("Ghostifying " + this);
        foreach (var (renderer, originalMaterial) in this.Renderers)
        {
            Debug.Log("Changing material on " + renderer + " from (original) " + originalMaterial.First() + " to (ghost) " + this.GhostMaterial);
            renderer.materials = new[] { this.GhostMaterial };
            Debug.Log("original is now " + originalMaterial.First());
        }
        foreach (var component in this.ComponentsToDisable)
        {
            component.enabled = false;
        }
        foreach (var collider in this.CollidersToDisable)
        {
            collider.enabled = false;
        }
    }

    public void UnGhostify()
    {
        foreach (var (renderer, originalMaterial) in this.Renderers)
        {
            Debug.Log("Restoring material on " + renderer + " to (original) " + originalMaterial.First());
            renderer.materials = originalMaterial;
        }
        foreach (var component in this.ComponentsToDisable)
        {
            component.enabled = true;
        }
        foreach (var collider in this.CollidersToDisable)
        {
            Debug.Log("Re-enabling collider " + collider);
            collider.enabled = true;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.Ghostify();
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            this.UnGhostify();
        }
    }
}
