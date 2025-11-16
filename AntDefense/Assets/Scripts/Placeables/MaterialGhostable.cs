using UnityEngine;

public class MaterialGhostable : BaseGhostableMonobehaviour
{
    public Material GhostMaterial;
    public Material OriginalMaterial;
    public Renderer Renderer;

    public override void Ghostify()
    {
        if (this.Renderer != null)
        {
            if (this.OriginalMaterial == null)
            {
                this.OriginalMaterial = this.Renderer.material;
            }
            this.Renderer.material = this.GhostMaterial;
        }
    }

    public override void UnGhostify()
    {
        if (this.Renderer != null)
        {
            this.Renderer.material = this.OriginalMaterial;
        }
    }

    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        this.Ghostify();
    //    }
    //    if (Input.GetKeyDown(KeyCode.Return))
    //    {
    //        this.UnGhostify();
    //    }
    //}
}
