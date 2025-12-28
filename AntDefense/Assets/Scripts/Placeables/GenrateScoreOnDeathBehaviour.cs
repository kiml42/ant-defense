using Assets.Scripts;

public class GenrateScoreOnDeathBehaviour: DeathActionBehaviour
{
    public ScoreGenerator ScoreGenerator;
    public override void OnDeath()
    {
        ScoreGenerator.IncrementScore();
    }
}