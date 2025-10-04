using UnityEngine;

public class ScentDetector : MonoBehaviour
{
    public AntStateMachine AntStateMachine;

    void OnTriggerEnter(Collider collider)
    {
        //var smellable = collider;
        //var ViewPoint = transform.GetComponentInParent<AntStateMachine>().ViewPoint;
        //bool hasLineOfSight;
        //Debug.Log("Checking for obstacles betwen " + this + " and " + smellable);
        //if (smellable != null && ViewPoint != null)
        //{
        //    var end = ViewPoint.position;
        //    var start = smellable.transform.position;

        //    Debug.DrawRay(start, end - start, Color.magenta);
        //    var isHit = Physics.Raycast(start, end - start, out var hit, (end - start).magnitude);
        //    if (isHit)
        //    {
        //        Debug.Log("Test3 ray Hit " + hit.transform);
        //        if (hit.transform != this.transform)
        //        {
        //            hasLineOfSight = false;
        //            Debug.Log("3. It's an obstacle!");
        //        }
        //        else
        //        {
        //            hasLineOfSight = true;
        //            Debug.Log("3. Wasn't an obstacle");
        //        }
        //    }
        //    else
        //    {
        //        hasLineOfSight = true;
        //        Debug.Log("3. Didn't hit anything");
        //    }
        //}
        //else
        //{
        //    hasLineOfSight = false;
        //    Debug.Log("3. Not checking for barriers");
        //}

        this.ProcessSmell(collider?.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        this.ProcessSmell(collision.gameObject);
    }

    private void ProcessSmell(GameObject @object)
    {
        var smellable = @object.GetComponentInParent<Smellable>();

        if (smellable != null)
        {
            this.AntStateMachine.ProcessSmell(smellable);
        }

    }
}
