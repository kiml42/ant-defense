using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
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

    public override void Detach(Rigidbody other)
    {
        base.Detach(other);
        // TODO reinstate smells if this ends up being useful
    }
}

public abstract class Carryable : MonoBehaviour
{
    public float Mass {  get; private set; }
    private Rigidbody _rigidbody;

    private void Start()
    {
        _rigidbody = this.GetOrAddComponent<Rigidbody>();
        Mass = _rigidbody.mass;
    }

    public virtual void Detach(Rigidbody other)
    {
        this.transform.parent = null;
        _rigidbody=this.GetOrAddComponent<Rigidbody>();
        _rigidbody.mass = Mass;

        other.mass -= Mass;
    }

    public virtual void Attach(Rigidbody other)
    {
        transform.parent = other.transform;
        other.mass += Mass;
        Destroy(_rigidbody);
    }
}
