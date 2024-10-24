using UnityEngine;

public class ScentDetector : MonoBehaviour
{
    public Smell SeekingSmell = Smell.Food;
    private ISmellable currentSmell;

    void OnTriggerEnter(Collider col)
    {
        if(col.gameObject.TryGetComponent<ISmellable>(out var smellable))
        {
            if (smellable.Smell == SeekingSmell)
            {
                currentSmell = smellable;
                print(currentSmell);
            }
        }
    }
}
