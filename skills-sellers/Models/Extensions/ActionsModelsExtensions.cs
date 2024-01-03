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
                Id = actionCuisiner.Id,
                EndTime = actionCuisiner.DueDate,
                CreatedAt = actionCuisiner.CreatedAt ?? DateTime.Now,
                Plat = actionCuisiner.Plat,
                Cards = actionCuisiner.UserCards.Select(uc => uc.ToUserCardInActionResponse()).ToList()
            },
            ActionExplorer actionExplorer => new ActionExplorerResponse
            {
                ActionName = "explorer",
                Id = actionExplorer.Id,
                EndTime = actionExplorer.DueDate,
                CreatedAt = actionExplorer.CreatedAt ?? DateTime.Now,
                Cards = actionExplorer.UserCards.Select(uc => uc.ToUserCardInActionResponse()).ToList(),
                IsReturningToHome = actionExplorer.IsReturningToHome,
                PlanetName = actionExplorer.PlanetName,
                Decision = actionExplorer.Decision,
                needDecision = actionExplorer.needDecision
            },
            ActionAmeliorer actionAmeliorer => new ActionAmeliorerResponse
            {
                ActionName = "ameliorer",
                Id = actionAmeliorer.Id,
                EndTime = actionAmeliorer.DueDate,
                CreatedAt = actionAmeliorer.CreatedAt ?? DateTime.Now,
                BatimentToUpgrade = actionAmeliorer.BatimentToUpgrade,
                WeaponToUpgradeId = actionAmeliorer.WeaponToUpgradeId,
                Cards = actionAmeliorer.UserCards.Select(uc => uc.ToUserCardInActionResponse()).ToList()
            },
            ActionMuscler actionMuscler => new ActionMusclerResponse
            {
                ActionName = "muscler",
                Id = actionMuscler.Id,
                EndTime = actionMuscler.DueDate,
                CreatedAt = actionMuscler.CreatedAt ?? DateTime.Now,
                Cards = actionMuscler.UserCards.Select(uc => uc.ToUserCardInActionResponse()).ToList(),
                Muscle = actionMuscler.Muscle
            },
            ActionReparer actionReparer => new ActionReparerResponse
            {
                ActionName = "reparer",
                Id = actionReparer.Id,
                EndTime = actionReparer.DueDate,
                CreatedAt = actionReparer.CreatedAt ?? DateTime.Now,
                Cards = actionReparer.UserCards.Select(uc => uc.ToUserCardInActionResponse()).ToList(),
                RepairChances = actionReparer.RepairChances
            },
            ActionSatellite actionSatellite => new ActionSatelliteResponse
            {
                ActionName = "satellite",
                Id = actionSatellite.Id,
                EndTime = actionSatellite.DueDate,
                CreatedAt = actionSatellite.CreatedAt ?? DateTime.Now,
                Cards = actionSatellite.UserCards.Select(uc => uc.ToUserCardInActionResponse()).ToList()
            },
            _ => throw new ArgumentException("Action inconnue")
        };
}