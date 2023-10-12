using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skills_sellers.Models.Cards;

namespace skills_sellers.Models;

public class ActionRequest
{
    /// <summary>
    /// The name of the action
    /// - ameliorer
    /// - explorer
    /// - cuisiner
    /// - muscler
    /// </summary>
    [Required]
    public string ActionName { get; set; }
    
    /// <summary>
    /// user's cards ids to use for the action
    /// </summary>
    [Required]
    public IEnumerable<int> CardsIds { get; set; }
    
    public string? BatimentToUpgrade { get; set; }
}

[JsonDerivedType(typeof(ActionCuisinerResponse))]
[JsonDerivedType(typeof(ActionExplorerResponse))]
[JsonDerivedType(typeof(ActionAmeliorerResponse))]
[JsonDerivedType(typeof(ActionMusclerResponse))]
public abstract class ActionResponse
{
    public List<UserCardResponse> Cards { get; set; }
    
    public string ActionName { get; set; }
    
    public DateTime EndTime { get; set; }
}

public class ActionCuisinerResponse : ActionResponse
{
    /// <summary>
    /// The name of the plat to cook
    /// </summary>
    [Required]
    public string Plat { get; set; }
}

public class ActionExplorerResponse : ActionResponse
{
    public bool IsReturningToHome { get; set; }
    public string PlanetName { get; set; }
}

public class ActionAmeliorerResponse : ActionResponse
{
    /// <summary>
    /// The name of the batiment to upgrade
    /// </summary>
    [Required]
    public string BatimentToUpgrade { get; set; }
}

public class ActionMusclerResponse : ActionResponse
{
}

public class ActionEstimationResponse : ActionResponse
{
    public Dictionary<string, string> Gains { get; set; } = new();
    
    public Dictionary<string, string> Couts { get; set; } = new();
    public string? Error { get; set; }
}