using UnityEngine;

public class TrailPointController : Smellable
{
    public float RemainingTime = 30;
    public MeshRenderer Material;

    private Smell _trailSmell;
    private float _distance;

    public Transform Transform => this.transform;

    public override Smell Smell => _trailSmell;

    public override float Distance => _distance;

    public override bool IsActual => false;


    private void FixedUpdate()
    {
        RemainingTime -= Time.deltaTime;
        if (RemainingTime <= 0)
        {
            Destroy(this.gameObject);
        }
        if(RemainingTime < 2)
        {
            this.transform.localScale = Vector3.one * RemainingTime / 2;
        }
    }

    public override string ToString()
    {
        return "TrailPoint " + Smell;
    }

    internal void SetSmell(Smell trailSmell, float distance)
    {
        _trailSmell = trailSmell;
        _distance = distance;

        if(Material != null)
        {
            var a = Material.material.color.a;
            switch (_trailSmell)
            {
                case Smell.Home:
                    Material.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, a); break;
                case Smell.Food:
                    Material.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, a); break;
            }
        }
    }
}
