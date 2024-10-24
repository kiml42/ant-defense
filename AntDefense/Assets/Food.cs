using UnityEngine;

public class Food : MonoBehaviour, ISmellable
{
    public Smell Smell => Smell.Food;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override string ToString()
    {
        return "Actual Food";
    }
}

public interface ISmellable
{
    public Smell Smell { get; }
}

public enum Smell
{
    Food, Home
}
