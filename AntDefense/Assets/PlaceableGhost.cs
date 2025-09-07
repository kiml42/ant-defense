using UnityEngine;

public class PlaceableGhost : MonoBehaviour
{
    public Transform RealObject;

    public Transform FloorPoint;

    public float TimeOut = 3;

    private bool _isPlaced = false;

    public bool Rotatable = true;

    public Vector3 SpawnOffset = Vector3.zero;

    // TODO just make a button version.
    public Transform Icon;
    public float ScaleForButton = 1;
    public Vector3 OffsetForButton = Vector3.zero;
    public Quaternion RotationForButton = Quaternion.identity;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Place()
    {
        _isPlaced = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (_isPlaced)
        {
            TimeOut -= Time.deltaTime;
            if(TimeOut < 0)
            {
                SpawnRealObject();
            }
        }
    }

    private void SpawnRealObject()
    {
        var newObject = Instantiate(RealObject, transform.position + SpawnOffset, transform.rotation);

        var foodSmells = newObject.GetComponentsInChildren<FoodSmell>();
        foreach (var foodSmell in foodSmells)
        {
            foodSmell.MarkAsPermanant(false);
        }

        Destroy(gameObject);
    }
}
