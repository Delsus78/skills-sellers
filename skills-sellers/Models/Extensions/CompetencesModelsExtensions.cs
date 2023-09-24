using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class CompetencesModelsExtensions
{

    public static Competences CreateCompetences(this CompetencesRequest model) 
        => new()
        {
            Cuisine = model.Cuisine,
            Force = model.Force,
            Intelligence = model.Intelligence,
            Charisme = model.Charisme,
            Exploration = model.Exploration
        };

    public static CompetencesResponse ToResponse(this Competences competences) 
        => new(competences.Id, competences.Cuisine, competences.Force, competences.Intelligence, competences.Charisme,
        competences.Exploration);
}