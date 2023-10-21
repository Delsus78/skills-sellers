namespace skills_sellers.Models;

public record GiftCodeResponse(
    string Code,
    int NbCards,
    int NbCreatium,
    int NbOr
    );
public record GiftCodeCreationRequest(
    int NbCards,
    int NbCreatium,
    int NbOr
);
    
public record GiftCodeRequest(
    string Code
);