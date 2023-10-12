namespace skills_sellers.Models;

public record CompetencesRequest(int Cuisine, int Force, int Intelligence, int Charisme, int Exploration);

public record CompetencesResponse(int Cuisine, int Force, int Intelligence, int Charisme, int Exploration);