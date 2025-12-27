using TMPro;
using UnityEngine;

public class BubblingText : MonoBehaviour
{
    public TextMeshPro Text;
    public Vector3 Velocity;
    public float Lifetime = 1.0f;
    public float FadeDuration = 0.5f;

    public void Initialise(string text, Color? colour = null)
    {
        this.Text.text = text;
        if (colour.HasValue)
        {
            Debug.Log("Setting bubbling text colour to " + colour.Value);
            this.Text.color = colour.Value;
            return;
        }
    }


    // Update is called once per frame
    void Update()
    {
        this.transform.position += this.Velocity * Time.unscaledDeltaTime;
        this.Lifetime -= Time.unscaledDeltaTime;

        this.Text.alpha = Mathf.Clamp01(this.Lifetime / this.FadeDuration);

        if (this.Lifetime <= 0)
        {
            Destroy(this.gameObject);
        }
    }
}
