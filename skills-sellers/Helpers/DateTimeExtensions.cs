namespace skills_sellers.Helpers;

public static class DateTimeExtensions
{
    public static bool EstDansSemaineActuelle(this DateTime date)
    {
        var today = DateTime.Today;

        var debutSemaine = today.AddDays(-(int)today.DayOfWeek).Date;

        var finSemaine = debutSemaine.AddDays(6).Date;
        finSemaine = finSemaine.AddHours(23).AddMinutes(59).AddSeconds(59);

        return date >= debutSemaine && date <= finSemaine;
    }
}