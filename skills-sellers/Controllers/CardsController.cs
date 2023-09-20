using Microsoft.AspNetCore.Mvc;
using skills_sellers.Entities;
using skills_sellers.Models.Cards;
using skills_sellers.Services;

namespace skills_sellers.Controllers;

[ApiController]
[Route("[controller]")]
public class CardsController : ControllerBase
{
    private ICardService _cardService;

    public CardsController(
        ICardService cardService)
    {
        _cardService = cardService;
    }
    
    [HttpGet]
    public IEnumerable<Card> GetAll()
        => _cardService.GetAll();

    [HttpGet("{id}")]
    public Card GetById(int id)
        => _cardService.GetById(id);

    [HttpPost]
    public IActionResult Create(CreateRequest model)
    {
        _cardService.Create(model);
        return Ok(new { message = "Card created" });
    }
}