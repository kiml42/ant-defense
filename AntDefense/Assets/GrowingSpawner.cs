using UnityEngine;

public class GrowingSpawner : MonoBehaviour
{
    public float InitialScale = 0;

    public float GrowTime = 5;

    public float SpawnTime = 6;

    public Transform PrefabToSpawn;

    private float timer = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.transform.localScale = Vector3.one * InitialScale;
    }

    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;

        if(timer >= SpawnTime)
        {
            Instantiate(PrefabToSpawn, this.transform.position, this.transform.rotation, this.transform.parent);
            Destroy(this.gameObject);
        }

    }

    // Graphical only, so can hapen in the UI loop.
    void Update()
    {
        // Handle scale;
        var growProgress = Mathf.Min(0, timer / GrowTime);
        var scale = InitialScale + (1 - InitialScale) * growProgress;
        this.transform.localScale = Vector3.one * scale;


    }
}
