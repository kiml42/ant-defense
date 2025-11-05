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
        this.transform.localScale = Vector3.one * this.InitialScale;
    }

    private void FixedUpdate()
    {
        this.timer += Time.deltaTime;

        if(this.timer >= this.SpawnTime)
        {
            Instantiate(this.PrefabToSpawn, this.transform.position, this.transform.rotation, this.transform.parent);
            Destroy(this.gameObject);
        }

    }

    // Graphical only, so can hapen in the UI loop.
    void Update()
    {
        // Handle scale;
        var growProgress = Mathf.Min(1, this.timer / this.GrowTime);
        var scale = this.InitialScale + ((1 - this.InitialScale) * growProgress);
        this.transform.localScale = Vector3.one * scale;


    }
}
