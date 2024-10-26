using UnityEngine;

public class TrailPointController : Smellable
{
    public MeshRenderer Material;

    private Smell _trailSmell;
    private float _timeFromTarget;

    public Transform Transform => this.transform;

    public override Smell Smell => _trailSmell;

    public override float TimeFromTarget => _timeFromTarget;

    public override bool IsActual => false;

    public override string ToString()
    {
        return "TrailPoint " + Smell;
    }

    internal void SetSmell(Smell trailSmell, float timeFromTarget)
    {
        _trailSmell = trailSmell;
        _timeFromTarget = timeFromTarget;

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
