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
    private NoSpawnZone[] _noSpawnZones;

    // Start is called before the first frame update
    void Start()
    {
        // TODO see where it's actually useful to set this.
        this.SetNoSpawnZoneEnabled(false);
    }

    private void SetNoSpawnZoneEnabled(bool enabled)
    {
        // TODO check if this is correctly activating the no spawn zone at the right time. and calculating the intersects only once it's placed.
        // TODO: consider keeping the same NoSpawnZone instances, when switching from the ghost to the real thing.
        _noSpawnZones ??= GetComponentsInChildren<NoSpawnZone>();
        foreach (var noSpawnZone in _noSpawnZones)
        {
            noSpawnZone.enabled = enabled;
        }
    }

    public void Place()
    {
        _isPlaced = true;

        this.SetNoSpawnZoneEnabled(true);
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
            this.SetNoSpawnZoneEnabled(true);
        }
        else
        {
            this.SetNoSpawnZoneEnabled(false);
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

        var placeables = newObject.GetComponents<PlaceableMonoBehaviour>();

        foreach(var placeable in placeables)
        {
            placeable.OnPlace(this);
        }

        Destroy(gameObject);
    }
}
