using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class CompetencesModelsExtensions
{
    // DTO
    
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
        => new(competences.Cuisine, competences.Force, competences.Intelligence, competences.Charisme,
        competences.Exploration);
    
    
    // helpers
    
    public static bool GotOneMaxed(this Competences competences) 
        => competences.Cuisine == 10 
           || competences.Force == 10 
           || competences.Intelligence == 10 
           || competences.Charisme == 10 
           || competences.Exploration == 10;
    
    public static bool GotAllMaxed(this Competences competences)
        => competences is { Cuisine: 10, Force: 10, Intelligence: 10, Charisme: 10, Exploration: 10 };
}