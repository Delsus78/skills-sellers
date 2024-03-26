namespace skills_sellers.Models;

public record CosmeticResponse(int Id, string Name, int Price, int Rarity);

public record UserCosmeticResponse(int Id, string Name, int CosmeticId, int Rarity, int CoordinateX, int CoordinateY, int Size, int ZIndex, int Rotation);
public record CosmeticRequest(int CoordinateX, int CoordinateY, int Size, int ZIndex, int Rotation);
public record CosmeticCreateRequest(string Name, int Price, int Rarity);