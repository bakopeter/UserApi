using Dapper;
using MySqlConnector;

public class MySqlUserRepository(IConfiguration config) : IUserRepository
{
    private readonly string _cs = config.GetConnectionString("Default")!;

    private const string Select =
        "SELECT id AS Id, username AS Username, password AS PasswordHash, email AS Email FROM users";

    public async Task<IEnumerable<User>> GetAll()
    {
        await using var c = new MySqlConnection(_cs);
        return await c.QueryAsync<User>(Select);
    }

    public async Task<User?> GetById(int id)
    {
        await using var c = new MySqlConnection(_cs);
        return await c.QuerySingleOrDefaultAsync<User>(Select + " WHERE id = @id", new { id });
    }

    public async Task<int> Create(string username, string passwordHash, string email)
    {
        await using var c = new MySqlConnection(_cs);
        return await c.ExecuteScalarAsync<int>(
            "INSERT INTO users (username, password, email) VALUES (@username, @passwordHash, @email); SELECT LAST_INSERT_ID();",
            new { username, passwordHash, email });
    }

    public async Task<bool> Update(int id, string username, string email)
    {
        await using var c = new MySqlConnection(_cs);
        return await c.ExecuteAsync(
            "UPDATE users SET username = @username, email = @email WHERE id = @id",
            new { id, username, email }) > 0;
    }

    public async Task<bool> Delete(int id)
    {
        await using var c = new MySqlConnection(_cs);
        return await c.ExecuteAsync("DELETE FROM users WHERE id = @id", new { id }) > 0;
    }
}