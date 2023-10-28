using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using skills_sellers.Entities;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;

namespace skills_sellers.Services;

public interface IAuthService
{
    Task<(int,string)> Registeration(User user, string password, string role);
    Task<(int,string)> Login(User user, AuthenticateRequest model);
    Task<(int,string)> ResetPassword(int userId, string password);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly DataContext _context;
    
    public AuthService(
        IConfiguration configuration, DataContext context)
    {
        _configuration = configuration;
        _context = context;
    }
    
    public async Task<(int,string)> Registeration(User user, string password, string role)
    {
        var userAuthExist = _context.AuthUsers.FirstOrDefault(u => u.UserId == user.Id);
        if (userAuthExist != null)
            return (0, "User auth instance is already created");
        
        var authUser = new AuthUser{
            UserId = user.Id,
            Hash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, 13), 
            Role = role
        };
        
        _context.AuthUsers.Add(authUser);
        await _context.SaveChangesAsync();
        
        return (1,"User created successfully!");
    }
    
    public async Task<(int,string)> Login(User user, AuthenticateRequest model)
    {
        var userAuth = await _context.AuthUsers.FirstOrDefaultAsync(u => u.UserId == user.Id);
        if (userAuth == null)
            return (0, "User auth instance not found");
        
        
        
        if (!BCrypt.Net.BCrypt.EnhancedVerify(model.Password, userAuth.Hash))
            return (0, "Invalid password");
            
        var userRoles = userAuth.Role.Split(",");
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Pseudo),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var userRole in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, userRole));
        }
        string token = GenerateToken(authClaims);
        return (1, token);
    }

    public async Task<(int, string)> ResetPassword(int userId, string password)
    {
        var userAuth = _context.AuthUsers.FirstOrDefault(u => u.UserId == userId);
        if (userAuth == null)
            return (0, "User auth instance not found");
        
        userAuth.Hash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, 13);
        
        _context.AuthUsers.Update(userAuth);
        await _context.SaveChangesAsync();
        
        return (1, "Password updated successfully!");
    }

    private string GenerateToken(IEnumerable<Claim> claims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["jwt:secret"]));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(claims)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
}