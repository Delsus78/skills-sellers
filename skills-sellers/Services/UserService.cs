using skills_sellers.Entities;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd.Contexts;
using skills_sellers.Models;
using skills_sellers.Models.Extensions;

namespace skills_sellers.Services;

public interface IUserService
{
    IEnumerable<User> GetAll();
    User GetById(int id);
    void Create(CreateRequest model);
    void Update(int id, UpdateRequest model);
    void Delete(int id);
}

public class UserService : IUserService
{
    private readonly UserContext _context;

    public UserService(
        UserContext context)
    {
        _context = context;
    }

    public IEnumerable<User> GetAll()
    {
        return _context.Users;
    }

    public User GetById(int id)
    {
        return getUser(id);
    }

    public void Create(CreateRequest model)
    {
        // validate
        if (_context.Users.Any(x => x.Pseudo == model.Pseudo))
            throw new AppException("User with the pseudo '" + model.Pseudo + "' already exists", 400);

        // map model to new user object
        var user = model.CreateUser();

        // hash password
        //user.PasswordHash = BCrypt.HashPassword(model.Password);

        // save user
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    public void Update(int id, UpdateRequest model)
    {
        var user = getUser(id);

        // validate
        if (model.Pseudo != user.Pseudo && _context.Users.Any(x => x.Pseudo == model.Pseudo))
            throw new AppException("User with the pseudo '" + model.Pseudo + "' already exists", 400);

        // hash password if it was entered
        //if (!string.IsNullOrEmpty(model.Password))
        //    user.PasswordHash = BCrypt.HashPassword(model.Password);

        // copy model to user and save
        model.UpdateUser(user);
        
        _context.Users.Update(user);
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var user = getUser(id);
        _context.Users.Remove(user);
        _context.SaveChanges();
    }

    // helper methods

    private User getUser(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null) throw new KeyNotFoundException("User not found");
        return user;
    }
}