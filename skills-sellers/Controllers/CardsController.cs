using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skills_sellers.Models.Cards;
using skills_sellers.Services;

namespace skills_sellers.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class CardsController : ControllerBase
{
    private ICardService _cardService;

    public CardsController(
        ICardService cardService) => _cardService = cardService;

    [HttpGet]
    public IEnumerable<CardResponse> GetAll()
        => _cardService.GetAll();

    [HttpGet("{id}")]
    public CardResponse GetById(int id)
        => _cardService.GetById(id);

    [Authorize(Roles = "admin")]
    [HttpPost]
    public CardResponse Create(CreateRequest model)
        => _cardService.Create(model);
}