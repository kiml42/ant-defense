using UnityEngine;

public abstract class TurretTurner : MonoBehaviour
{
    protected Vector3 _targetDirection;

    public void TurnTo(Vector3 direction)
    {
        this._targetDirection = direction;
    }
}

public class BallTurretTurner : TurretTurner
{
    void FixedUpdate()
    {
        if(this._targetDirection == Vector3.zero) { return; }
        this.transform.rotation = Quaternion.LookRotation(this._targetDirection, Vector3.up);
    }
}
