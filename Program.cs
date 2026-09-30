var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddSingleton<IUserRepository, MockUserRepository>();  // régi
builder.Services.AddScoped<IUserRepository, MySqlUserRepository>();       // új

var app = builder.Build();

app.MapGet("/users", async (IUserRepository repo) =>
    (await repo.GetAll()).Select(u => new { u.Id, u.Username, u.Email }));

app.MapGet("/users/{id}", async (int id, IUserRepository repo) =>
    await repo.GetById(id) is { } u
        ? Results.Ok(new { u.Id, u.Username, u.Email })
        : Results.NotFound());

app.MapPost("/users", async (UserCreateDto dto, IUserRepository repo) =>
{
    var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
    var id = await repo.Create(dto.Username, hash, dto.Email);
    return Results.Created($"/users/{id}", new { Id = id, dto.Username, dto.Email });
});

app.MapPut("/users/{id}", async (int id, UserUpdateDto dto, IUserRepository repo) =>
    await repo.Update(id, dto.Username, dto.Email) ? Results.NoContent() : Results.NotFound());

app.MapDelete("/users/{id}", async (int id, IUserRepository repo) =>
    await repo.Delete(id) ? Results.NoContent() : Results.NotFound());

app.Run();