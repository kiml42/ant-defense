using UnityEngine;

public class TrailPointController : Smellable
{
    public Smell _trailSmell = Smell.Home;
    private float _distance;

    public Transform Transform => this.transform;

    public override Smell Smell => _trailSmell;

    public override float Distance => _distance;

    public MeshRenderer Material;

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
            switch (_trailSmell)
            {
                case Smell.Home:
                    Material.material.color = Color.white; break;
                case Smell.Food:
                    Material.material.color = Color.red; break;
            }
        }
    }
}
