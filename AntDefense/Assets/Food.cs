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
            smell.enabled = false;
            Destroy(smell);
        }
    }

    public override void Detach()
    {
        base.Detach();
        // TODO reinstate smells if this ends up being useful
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
        _carrier.mass -= Mass;
        Destroy(this.gameObject);
    }

    public virtual void Attach(Rigidbody other)
    {
        transform.parent = other.transform;
        other.mass += Mass;
        _carrier = other;
        Destroy(_rigidbody);
    }
}
