public record User(int Id, string Username, string PasswordHash, string Email);
public record UserCreateDto(string Username, string Password, string Email);
public record UserUpdateDto(string Username, string Email);

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAll();
    Task<User?> GetById(int id);
    Task<int> Create(string username, string passwordHash, string email);
    Task<bool> Update(int id, string username, string email);
    Task<bool> Delete(int id);
}