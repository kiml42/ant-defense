using TMPro;

public abstract class ValueTracker<TValue, TSelf> : SingletonMonoBehaviour<TSelf> where TSelf : ValueTracker<TValue, TSelf>
{
    public TextMeshProUGUI Text;
    public static TValue CurrentValue { get; protected set; }

    public abstract string FormattedValue { get; }

    // Update is called once per frame
    void Update()
    {
        if (this.Text != null)
        {
            this.Text.text = this.FormattedValue;
        }
    }
}
