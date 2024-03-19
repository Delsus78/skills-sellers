namespace skills_sellers.Entities.Actions;

public class ActionExplorer : Action
{
    public bool IsReturningToHome { get; set; }
    public string PlanetName { get; set; }
    public ExplorationDecision? Decision { get; set; }
    public bool needDecision { get; set; } = false;
}

public enum ExplorationDecision
{
    Pillage,
    Ally
}