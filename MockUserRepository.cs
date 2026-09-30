public class MockUserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    private int _nextId = 1;

    public Task<IEnumerable<User>> GetAll() =>
        Task.FromResult<IEnumerable<User>>(_users.ToList());

    public Task<User?> GetById(int id) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<int> Create(string username, string passwordHash, string email)
    {
        var id = _nextId++;
        _users.Add(new User(id, username, passwordHash, email));
        return Task.FromResult(id);
    }

    public Task<bool> Update(int id, string username, string email)
    {
        var i = _users.FindIndex(u => u.Id == id);
        if (i < 0) return Task.FromResult(false);
        _users[i] = _users[i] with { Username = username, Email = email };
        return Task.FromResult(true);
    }

    public Task<bool> Delete(int id) =>
        Task.FromResult(_users.RemoveAll(u => u.Id == id) > 0);
}