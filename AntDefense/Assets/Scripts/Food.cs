using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Food : Carryable
{
    public float FoodValue = 100;
    public List<FoodSmell> Smells;

    public override void Attach(Rigidbody other)
    {
        base.Attach(other);
        foreach (var smell in this.Smells)
        {
            if(smell == null)
                continue;   // TODO - this shouldn't happen.
            smell.enabled = false;
            smell.IsSmellable = false;
        }
    }

    public override void Detach()
    {
        base.Detach();
        foreach (var smell in this.Smells)
        {
            smell.enabled = true;
            smell.IsSmellable = true;
        }
    }
}

public abstract class Carryable : MonoBehaviour
{
    public float Mass {  get; private set; }
    private Rigidbody _rigidbody;
    private Rigidbody _carrier = null;

    private void Start()
    {
        this._rigidbody = this.GetOrAddComponent<Rigidbody>();
        this.Mass = this._rigidbody.mass;
    }

    public virtual void Detach()
    {
        this.transform.parent = null;
        this._rigidbody =this.GetOrAddComponent<Rigidbody>();
        this._rigidbody.mass = this.Mass;

        this._carrier.mass -= this.Mass;
    }

    public virtual void Destroy()
    {
        this.transform.parent = null;
        if (this._carrier != null)
        {
            this._carrier.mass -= this.Mass;
        }
        //Debug.Log($"Decreasing {_carrier.name}'s mass by {Mass} to {_carrier.mass}");
        Destroy(this.gameObject);
    }

    public virtual void Attach(Rigidbody other)
    {
        if (other == null || other.transform == null) return;
        this.transform.parent = other.transform;
        other.mass += this.Mass;
        this._carrier = other;
        //Debug.Log($"Increasing {_carrier.name}'s mass by {Mass} to {_carrier.mass}");
        Destroy(this._rigidbody);
    }
}
