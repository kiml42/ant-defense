using UnityEngine;

public abstract class BaseGhostable : MonoBehaviour
{
    public abstract void Ghostify();
    public abstract void UnGhostify();
}

public class Ghostable : BaseGhostable
{
    public Material GhostMaterial;
    private Material originalMaterial;
    public Renderer Renderer;

    public override void Ghostify()
    {
        Debug.Log("Ghostifying " + this);
        if (this.Renderer != null)
        {
            if (this.originalMaterial == null)
            {
                this.originalMaterial = this.Renderer.material;
            }
            this.Renderer.material = this.GhostMaterial;
        }
    }

    public override void UnGhostify()
    {
        if (this.Renderer != null)
        {
            this.Renderer.material = this.originalMaterial;
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
