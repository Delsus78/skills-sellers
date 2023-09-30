using System.ComponentModel.DataAnnotations;
using skills_sellers.Models.Cards;

namespace skills_sellers.Models;

public abstract class ActionRequest
{
    public int CardId { get; set; }
    
    [Required]
    public string ActionName { get; set; }
}

public class ActionExplorerRequest : ActionRequest
{
}

public class ActionEtudierRequest : ActionRequest
{
}

public class ActionCuisinerRequest : ActionRequest
{
}

public class ActionMusclerRequest : ActionRequest
{
}

public class ActionAmeliorerRequest : ActionRequest
{
}

public abstract class ActionResponse
{
    public CardResponse Card { get; set; }
    
    public string ActionName { get; set; }
    
    public DateTime EndTime { get; set; }
}
