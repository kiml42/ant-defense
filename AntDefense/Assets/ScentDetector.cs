using UnityEngine;

public class ScentDetector : MonoBehaviour
{
    public Smell SeekingSmell = Smell.Food;
    private ISmellable currentSmell;

    void OnTriggerEnter(Collider collider)
    {
        ProcessSmell(collider.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessSmell(collision.gameObject);
    }

    private void ProcessSmell(GameObject @object)
    {
        if (@object.TryGetComponent<ISmellable>(out var smellable))
        {
            if (smellable.Smell == SeekingSmell)
            {
                currentSmell = smellable;
                print(currentSmell);
            }
        }
    }
}
