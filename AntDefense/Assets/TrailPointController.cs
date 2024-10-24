using System;
using UnityEngine;

public class TrailPointController : MonoBehaviour, ISmellable
{
    public Smell _trailSmell = Smell.Home;
    public Smell Smell => _trailSmell;

    public MeshRenderer Material;

    public override string ToString()
    {
        return "TrailPoint " + Smell;
    }

    internal void SetSmell(Smell trailSmell)
    {
        _trailSmell = trailSmell;
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
