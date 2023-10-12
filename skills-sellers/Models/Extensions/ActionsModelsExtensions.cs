using skills_sellers.Entities.Actions;

namespace skills_sellers.Models.Extensions;
using Action = skills_sellers.Entities.Action;

public static class ActionsModelsExtensions
{
    public static ActionResponse ToResponse(this Action action)
        => action switch
        {
            ActionCuisiner actionCuisiner => new ActionCuisinerResponse
            {
                ActionName = "cuisiner",
                EndTime = actionCuisiner.DueDate,
                Plat = actionCuisiner.Plat,
                Cards = actionCuisiner.UserCards.Select(uc => uc.ToUserCardInActionResponse()).ToList()
            },
            ActionExplorer actionExplorer => new ActionExplorerResponse
            {
                ActionName = "explorer",
                EndTime = actionExplorer.DueDate,
                Cards = actionExplorer.UserCards.Select(uc => uc.ToUserCardInActionResponse()).ToList(),
                IsReturningToHome = actionExplorer.IsReturningToHome,
                PlanetName = actionExplorer.PlanetName
            },
            ActionAmeliorer actionAmeliorer => new ActionAmeliorerResponse
            {
                ActionName = "ameliorer",
                EndTime = actionAmeliorer.DueDate,
                BatimentToUpgrade = actionAmeliorer.BatimentToUpgrade,
                Cards = actionAmeliorer.UserCards.Select(uc => uc.ToUserCardInActionResponse()).ToList()
            },
            ActionMuscler actionMuscler => new ActionMusclerResponse
            {
                ActionName = "muscler",
                EndTime = actionMuscler.DueDate,
                Cards = actionMuscler.UserCards.Select(uc => uc.ToUserCardInActionResponse()).ToList()
            },
            _ => throw new ArgumentException("Action inconnue")
        };
}