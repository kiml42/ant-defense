using UnityEngine;

public class TrailPointController : Smellable
{
    public float RemainingTime;
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
