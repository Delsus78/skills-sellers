using System.ComponentModel.DataAnnotations;

namespace skills_sellers.Models;

public record LinkCreateRequest(string Role, int FirstCardId);
public record ResetPasswordLinkResponse(string Link);
public record ResetPasswordLinkRequest([Required] string Pseudo);