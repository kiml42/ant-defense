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
        foreach (var smell in Smells)
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
        foreach (var smell in Smells)
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
        _rigidbody = this.GetOrAddComponent<Rigidbody>();
        Mass = _rigidbody.mass;
    }

    public virtual void Detach()
    {
        this.transform.parent = null;
        _rigidbody=this.GetOrAddComponent<Rigidbody>();
        _rigidbody.mass = Mass;

        _carrier.mass -= Mass;
    }

    public virtual void Destroy()
    {
        this.transform.parent = null;
        if (_carrier != null)
        {
            _carrier.mass -= Mass;
        }
        //Debug.Log($"Decreasing {_carrier.name}'s mass by {Mass} to {_carrier.mass}");
        Destroy(this.gameObject);
    }

    public virtual void Attach(Rigidbody other)
    {
        if (other == null || other.transform == null) return;
        transform.parent = other.transform;
        other.mass += Mass;
        _carrier = other;
        //Debug.Log($"Increasing {_carrier.name}'s mass by {Mass} to {_carrier.mass}");
        Destroy(_rigidbody);
    }
}
