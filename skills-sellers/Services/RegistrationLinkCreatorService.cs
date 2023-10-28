namespace skills_sellers.Services;

public interface IRegistrationLinkCreatorService
{
    string CreateRegistrationLink(string role, int firstCardId);
    string CreateResetPasswordLink(int userId);
    (bool valid, string role, int firstCardId) GetLink(string link);
    (bool valid, int userId) GetResetPasswordLink(string link);
    void DeleteLink(string link);
    void DeletePasswordLink(string link);
}
public class RegistrationLinkCreatorService : IRegistrationLinkCreatorService
{
    private readonly Dictionary<string, (string role, int firstCardId)> _validLinks = new();
    private readonly Dictionary<string, int> _validResetPasswordLinks = new();
    

    public string CreateRegistrationLink(string role, int firstCardId)
    {
        // generate a random string of 20 characters
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var link = new string(Enumerable.Repeat(chars, 20)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        _validLinks.Add(link, (role, firstCardId));
        return link;
    }
    
    public string CreateResetPasswordLink(int userId)
    {
        // generate a random string of 20 characters
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var link = new string(Enumerable.Repeat(chars, 20)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        _validResetPasswordLinks.Add(link, userId);
        return link;
    }

    public (bool valid, string role, int firstCardId) GetLink(string link) =>
        !_validLinks.ContainsKey(link)
            ? (false, "", 0) : (true, _validLinks[link].role, _validLinks[link].firstCardId);

    public (bool valid, int userId) GetResetPasswordLink(string link) => 
        !_validResetPasswordLinks.ContainsKey(link)
                ? (false, 0) : (true, _validResetPasswordLinks[link]);

    public void DeleteLink(string link) 
        => _validLinks.Remove(link);
    
    public void DeletePasswordLink(string link)
        => _validResetPasswordLinks.Remove(link);
}