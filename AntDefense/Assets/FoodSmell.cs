using UnityEngine;

public class FoodSmell : MonoBehaviour, ISmellable
{
    public Smell Smell => Smell.Food;

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
