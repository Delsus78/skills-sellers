using skills_sellers.Entities;

namespace skills_sellers.Models.Extensions;

public static class GiftCodesModelsExtensions
{
    public static GiftCodeResponse ToResponse(this GiftCode giftCode)
        => new GiftCodeResponse(giftCode.Code, giftCode.NbCards, giftCode.NbCreatium, giftCode.NbOr);

    public static GiftCode CreateGiftCode(this GiftCodeCreationRequest giftCode, string code)
        => new GiftCode
        {
            Code = code,
            NbCards = giftCode.NbCards,
            NbCreatium = giftCode.NbCreatium,
            NbOr = giftCode.NbOr,
            Used = false
        };
}