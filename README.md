# MySQL Dockerben + CRUD API (.NET)
Útmutató: a mock adatbázis lecserélése egy valódi MySQL adatbázisra (Visual Studio + Docker, hibaelhárítással)

**Cél:** a meglévő minimal API projektben a mock adatbázist egy Dockerben futó MySQL-re cseréled. Egy `users` táblával (username, password, email) készül el a CRUD API. A végén a bátrabbak Entity Frameworkkel is kipróbálhatják.

**Az útmutató felépítése:**
- 0. Előfeltételek: WSL 2, Docker Desktop, .NET SDK, Visual Studio, a kiinduló projekt mock adatbázissal
- 1-7. A MySQL beállítása és az API átírása
- Bónusz: Entity Framework Core
- Hibaelhárítás: a leggyakoribb problémák és megoldásaik

---

## 0. lépés: Előfeltételek

A végére mind működnie kell: WSL 2 (csak Windowson), Docker Desktop, .NET SDK, kódszerkesztő és egy futó minimal API projekt a mock adatbázissal.

### 0.1 WSL 2 telepítése (csak Windows)

**Miért kell?** A Docker Desktop Windowson a WSL 2-re (Windows Subsystem for Linux) épülő Linux-motort használja. Egy átlagos, frissen telepített Windowson ez általában **nincs telepítve vagy nincs frissítve**, és ez a leggyakoribb oka annak, hogy a Docker nem indul el. Ezért ezt a lépést a Docker előtt csináld meg.

**Követelmények:** Windows 10 (2004-es, 19041-es build vagy újabb) vagy Windows 11, 64 bites rendszer, bekapcsolt virtualizáció. A Windows verzióját a `Win + R` → `winver` paranccsal nézheted meg. A Home kiadás is megfelel.

1. **Virtualizáció ellenőrzése.** Feladatkezelő → Teljesítmény → CPU → a *Virtualizáció* sornak **Engedélyezve** értéket kell mutatnia. Ha *Letiltva*, kapcsold be a BIOS/UEFI-ben (Intel: *VT-x*, AMD: *SVM Mode* vagy *AMD-V*), majd indítsd újra a gépet.
2. **PowerShell megnyitása rendszergazdaként.** Start menü → keress rá: *PowerShell* → jobb klikk → *Futtatás rendszergazdaként*.
3. **WSL telepítése.** Futtasd az alábbi parancsot:

```powershell
wsl --install --no-distribution
```

Ez telepíti a WSL-t és a hozzá szükséges Windows-szolgáltatásokat (*Virtual Machine Platform*, *Windows Subsystem for Linux*). A `--no-distribution` kapcsoló miatt nem települ Ubuntu, mert a Dockernek nincs rá szüksége. Ha saját Linux-környezetet is szeretnél, a `wsl --install` parancs az Ubuntut is felteszi.

4. **Indítsd újra a gépet.**
5. **Frissítés.** Újra nyiss egy rendszergazdai PowerShellt, és futtasd:

```powershell
wsl --update
wsl --set-default-version 2
```

6. **Ellenőrzés:**

```powershell
wsl --version
wsl --status
```

Ha a `wsl --version` kiír egy verziószámot, a `wsl --status` pedig 2-es alapértelmezett verziót mutat, a WSL kész.

**Ha a telepítés nem sikerül:**
- *"A művelet rendszergazdai jogosultságot igényel"*: nem rendszergazdai PowerShellből futtattad.
- A parancs csak a súgót listázza ki: régi a Windows, futtass Windows Update-et.
- `0x80370102` hibakód: a virtualizáció ki van kapcsolva (lásd az 1. pontot).
- Kézi alternatíva: Start menü → *Windows-szolgáltatások be- és kikapcsolása* → jelöld be a **Virtual Machine Platform** és a **Windows Subsystem for Linux** elemeket, majd indítsd újra a gépet.

### 0.2 Docker Desktop telepítése

**Mire kell?** Ebben fut majd a MySQL, így nem kell közvetlenül a gépedre telepítened. (Ha nem szeretnéd használni a Docker Desktopot, nézd meg a Hibaelhárítás fejezet utolsó pontját.)

**Windows** (előtte legyen kész a 0.1 lépés):
1. Töltsd le a Docker Desktopot: https://www.docker.com/products/docker-desktop/
2. Futtasd a telepítőt. Hagyd bepipálva a **WSL 2** használatát.
3. Ha kéri, indítsd újra a gépet.
4. Indítsd el a Docker Desktopot. A bejelentkezést kihagyhatod (*Skip*). Várd meg, amíg a bal alsó sarokban zöld lesz az állapotjelző (*Engine running*).

**macOS:** töltsd le a megfelelő verziót (Apple Silicon vagy Intel), húzd az alkalmazást az Applications mappába, majd indítsd el.

**Linux:** a Docker Desktop helyett a Docker Engine-t és a Compose plugint is telepítheted a disztribúciód csomagkezelőjével: https://docs.docker.com/engine/install/

**Ellenőrzés:**

```bash
docker --version
docker compose version
docker run hello-world
```

Ha az utolsó parancs kiírja, hogy "Hello from Docker!", minden rendben van. Ha hibát kapsz (például `500 Internal Server Error`), nézd meg a Hibaelhárítás fejezetet.

### 0.3 .NET SDK telepítése

**Mire kell?** Ezzel tudsz .NET projektet létrehozni, lefordítani és futtatni. A `dotnet` parancs is ennek a része.

**Melyik verziót válaszd?** Az útmutató kódja (például a primary constructorok) legalább a **.NET 8-at** igényli. A legújabb hosszú távú támogatású (LTS) kiadás a **.NET 10**. **Vigyázat:** a **Visual Studio 2022 (17.x) nem tud .NET 10-re fordítani**, ahhoz a Visual Studio 2026 (18.0 vagy újabb) kell. Ha Visual Studio 2022-t használsz, a projekt célverziója legyen .NET 9 vagy 8 (lásd a 0.5 lépést).

- **Minden rendszeren:** töltsd le az **SDK** telepítőt (nem a Runtime-ot!): https://dotnet.microsoft.com/download
- **Windows, parancssorból:** `winget install Microsoft.DotNet.SDK.10` (vagy `...SDK.9`, ha a .NET 9-re van szükséged)
- **macOS, Homebrew-val:** `brew install --cask dotnet-sdk`
- **Linux:** a letöltőoldalon a *Package manager instructions* linknél találod a parancsokat.

**Ellenőrzés** (a telepítés után nyiss egy **új** terminált):

```bash
dotnet --version
```

Ha kiír egy verziószámot (például `10.0.xxx`), az SDK működik. Egy gépen több SDK is lehet egyszerre, ezeket a `dotnet --list-sdks` paranccsal listázhatod.

### 0.4 Kódszerkesztő

Bármelyik megfelel, ami C#-ot támogat:
- **Visual Studio** (Windows, a Community verzió ingyenes). Telepítéskor jelöld be az **ASP.NET and web development** workloadot. Ha már telepítve van: Visual Studio Installer → *Modify* → jelöld be ugyanezt.
- **Visual Studio Code** a *C# Dev Kit* bővítménnyel (ingyenes, minden rendszeren működik).
- **JetBrains Rider**.

Az útmutató példái Visual Studióra épülnek, de a parancssoros lépések bármelyik szerkesztővel működnek.

### 0.5 A projekt létrehozása

Ha az előző órákon már készítettél egy projektet mock adatbázissal, azzal dolgozz tovább, és ugorj a **Célplatform ellenőrzése** részre. Különben hozz létre egy újat.

**A) Visual Studióban:**
1. *Create a new project* → keresd: `ASP.NET Core Empty` → válaszd a **C#** nyelvű sablont.
2. Név: például `UserApi`.
3. **Framework:** .NET 10, ha a Visual Studio támogatja (2026), különben **.NET 9 vagy 8**.
4. *Additional information*: az **HTTPS-t vedd ki**, a Docker/container support maradjon **kikapcsolva**, a *top-level statements* legyen **bekapcsolva** (vagyis ne pipáld be a *Do not use top-level statements* opciót).

**Melyik sablont NE válaszd?**
- **Empty Project (.NET Framework)**: régi, csak Windowson futó technológia, az útmutató kódja abban nem működik.
- **Blazor Server App Empty**: weboldal-keretrendszer, nem minimal API.
- **Aspire Empty App**: több szolgáltatást összefogó orkesztrációs projekt.
- Az *ASP.NET Core Web API* csak tartalékként jó: vedd ki a pipát a *Use controllers* elől, és töröld a mintaként generált `weatherforecast` végpontot.

**B) Parancssorból (ez mindig működik, ha a sablon nem jelenik meg):**

```bash
dotnet new web -n UserApi
cd UserApi
```

Ezután Visual Studióban: **File → Open → Project/Solution** (nem *Folder*!), és válaszd ki a `UserApi/UserApi.csproj` fájlt.

**Ellenőrzés:** a `.csproj` fájl (dupla kattintás a projekten) első sora ez kell legyen:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```

**Célplatform ellenőrzése.** Ugyanebben a fájlban keresd ezt a sort:

```xml
<TargetFramework>net10.0</TargetFramework>
```

Ha Visual Studio 2022-t használsz, és F5-re a *"The current Visual Studio version does not support targeting .NET 10.0"* hibát kapod, írd át `net9.0`-ra (vagy `net8.0`-ra régebbi Visual Studio esetén), majd mentsd a fájlt. Ha a futtatáskor hiányzó runtime miatt szól, telepítsd a megfelelő SDK-t (lásd 0.3).

### 0.6 A kiinduló kód: modellek, mock, Program.cs

**Fájlok létrehozása.** Hozz létre két fájlt a projekt mappájában (a `.csproj` mellett): `Models.cs` és `MockUserRepository.cs`.
- Visual Studióban: jobb klikk a **projekten** → *Add → Class...* (vagy *Add → New Item...*, és keresd a *Class* elemet).
- **Ha nem kínál fel osztályt** (például csak *Assembly Information File*-t): ez a sablonkezelés hibája, a projektnek nincs baja. Hozd létre a fájlokat kézzel: jobb klikk a projekten → *Open Folder in File Explorer*, hozz létre két szövegfájlt, és nevezd át őket `Models.cs` és `MockUserRepository.cs` névre. **Kapcsold be a fájlnévkiterjesztések megjelenítését** (Intéző → Nézet → Megjelenítés → Fájlnévkiterjesztések), különben `Models.cs.txt` lesz belőle. A Visual Studio automatikusan felveszi a fájlokat a projektbe.
- Ha a Visual Studio beszúr egy `namespace ...;` sort vagy egy üres osztályt, **töröld ki az egész tartalmat**, és másold be az alábbi kódot. Névtér nélkül a `Program.cs` közvetlenül látja a típusokat.

**Models.cs**

```csharp
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
```

**MockUserRepository.cs**

```csharp
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
```

**Program.cs** (az egész fájl tartalmát cseréld erre; a regisztráció most még a mockra mutat):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IUserRepository, MockUserRepository>();

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
    await repo.Update(id, dto.Username, dto.Email)
        ? Results.NoContent()
        : Results.NotFound());

app.MapDelete("/users/{id}", async (int id, IUserRepository repo) =>
    await repo.Delete(id) ? Results.NoContent() : Results.NotFound());

app.Run();
```

**A BCrypt csomag.** A `POST` végpont a BCrypt csomagot használja, ezért már most telepítened kell. A parancsot **a projekt mappájában** futtasd (ahol a `.csproj` van):

```bash
dotnet add package BCrypt.Net-Next
```

**Futtatás és tesztelés.**
1. Indítsd el az alkalmazást: Visual Studióban F5, vagy a projekt mappájában a terminálban `dotnet run`. **Csak az egyiket használd**, mert két példány ütközne a porton. Ha a Visual Studio nem tud a projekt verziójával futtatni, használd a `dotnet run` parancsot.
2. A kimenetben keresd ezt a sort: `Now listening on: http://localhost:5200`. A port nálad más lehet, ezt használd a példákban. Ez a sor jelzi, hogy az alkalmazás tényleg elindult.
3. Hagyd nyitva azt a terminált, ahol az alkalmazás fut. A teszteket egy **másik, új terminálban** futtasd (Windows Terminal, PowerShell, vagy Visual Studióban a *View → Terminal* egy új lapja).

```powershell
Invoke-RestMethod http://localhost:5200/users
```

Az első hívásra egy üres tömböt (`[]`) kell kapnod. A listázást a böngészőben is megnyithatod. Kódmódosítás után az alkalmazást újra kell indítani: a futó terminálban **Ctrl + C**, majd újra `dotnet run`. Ha ezt nem szeretnéd minden változtatás után kézzel csinálni, használd a `dotnet watch` parancsot, amely a fájlok mentésekor magától újraindítja az alkalmazást.

### 0.7 Ellenőrző lista

- `wsl --version` kiír egy verziót (csak Windowson)
- `docker run hello-world` sikeres
- `dotnet --version` kiír egy verziót
- a projekt `Sdk="Microsoft.NET.Sdk.Web"` típusú, és a célplatform illik a Visual Studióhoz
- az API a mockkal fut (`Now listening on...`), és a `/users` válaszol

---

## 1. lépés: MySQL indítása Dockerrel

Hozz létre két fájlt a projekt mappájában (a `.csproj` mellett).

**docker-compose.yml**

```yaml
services:
  mysql:
    image: mysql:8.4
    container_name: mysql-dev
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: rootpw
      MYSQL_DATABASE: appdb
      MYSQL_USER: appuser
      MYSQL_PASSWORD: apppw
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql

volumes:
  mysql_data:
```

**init.sql**

```sql
CREATE TABLE IF NOT EXISTS users (
  id INT AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(50) NOT NULL UNIQUE,
  password VARCHAR(100) NOT NULL,
  email VARCHAR(100) NOT NULL UNIQUE
);
```

> Az `init.sql` **csak az első indításkor** fut le, üres adatkötetnél. Ha később módosítod, és újra le akarod futtatni: `docker compose down -v`, majd újra `docker compose up -d`. A `-v` törli az adatokat is!

Indítás és ellenőrzés (a projekt mappájában, ahol a `docker-compose.yml` van):

```bash
docker compose up -d
docker logs mysql-dev          # várd meg a "ready for connections" sort
docker exec -it mysql-dev mysql -uappuser -papppw appdb -e "SHOW TABLES;"
```

Ha a `users` tábla megjelenik, az adatbázis kész. A konténer futását a `docker ps` paranccsal is ellenőrizheted (a `mysql-dev` legyen a listában).

## 2. lépés: NuGet csomagok telepítése

A parancsokat **a projekt mappájában** futtasd (ahol a `.csproj` van). A BCrypt csomagot a 0.6 lépésben már telepítetted.

```bash
dotnet add package MySqlConnector
dotnet add package Dapper
```

A MySqlConnector a MySQL-driver, a Dapper egyszerű SQL-lekérdezésekhez kell. Ha Visual Studióban szeretnéd: jobb klikk a projekten → *Manage NuGet Packages* → Browse fül.

## 3. lépés: Modellek és interfész

Ezt a 0.6 lépésben már létrehoztad a `Models.cs` fájlban, ezért itt nincs teendő. Ha eddig is interfészen keresztül használtad a mockot, ugyanez igaz nálad is. Az `IUserRepository` interfész teszi lehetővé, hogy a mockot egyetlen sor cseréjével lecseréld egy másik implementációra.

## 4. lépés: MySQL-implementáció megírása

Hozz létre egy **új fájlt** `MySqlUserRepository.cs` néven, ugyanott, ahol a `MockUserRepository.cs` van, és másold bele az alábbi kódot a két `using` sorral együtt. Ne legyen benne `namespace` sor. **A `MockUserRepository.cs`-t ne töröld és ne írd át**: mindkét osztály mellette maradhat, a regisztráció dönti el, melyiket használja az alkalmazás.

```csharp
using Dapper;
using MySqlConnector;

public class MySqlUserRepository(IConfiguration config) : IUserRepository
{
    private readonly string _cs = config.GetConnectionString("Default")!;

    private const string Select =
        "SELECT id AS Id, username AS Username, " +
        "password AS PasswordHash, email AS Email FROM users";

    public async Task<IEnumerable<User>> GetAll()
    {
        await using var c = new MySqlConnection(_cs);
        return await c.QueryAsync<User>(Select);
    }

    public async Task<User?> GetById(int id)
    {
        await using var c = new MySqlConnection(_cs);
        return await c.QuerySingleOrDefaultAsync<User>(
            Select + " WHERE id = @id", new { id });
    }

    public async Task<int> Create(string username, string passwordHash, string email)
    {
        await using var c = new MySqlConnection(_cs);
        return await c.ExecuteScalarAsync<int>(
            "INSERT INTO users (username, password, email) " +
            "VALUES (@username, @passwordHash, @email); SELECT LAST_INSERT_ID();",
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
        return await c.ExecuteAsync(
            "DELETE FROM users WHERE id = @id", new { id }) > 0;
    }
}
```

A `SELECT`-ben az aliasok (`AS Id`, `AS PasswordHash`) azért kellenek, hogy a Dapper az oszlopneveket a record tulajdonságaihoz tudja rendelni.

## 5. lépés: Connection string beállítása

Az `appsettings.json` fájl legfelső szintjére vedd fel a `ConnectionStrings` blokkot, a meglévő elemek mellé, **vesszővel elválasztva**. A teljes fájl így néz ki:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=appdb;User=appuser;Password=apppw;"
  }
}
```

## 6. lépés: Mock lecserélése a Program.cs-ben

A `Program.cs`-ben **csak a regisztráció sora változik**, a végpontok maradnak:

```csharp
var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddSingleton<IUserRepository, MockUserRepository>();  // régi
builder.Services.AddScoped<IUserRepository, MySqlUserRepository>();       // új

var app = builder.Build();

// ... a végpontok változatlanok (MapGet, MapPost, MapPut, MapDelete) ...

app.Run();
```

Két fontos szempont a végpontokról:
- A jelszót **soha nem tároljuk sima szövegként**, ezért hash-eljük BCrypttel.
- A GET végpontok **nem adják vissza a jelszót**, csak az id-t, a felhasználónevet és az e-mailt.

Ezután állítsd le a futó alkalmazást (**Ctrl + C**), és indítsd újra (`dotnet run`). A MySQL konténernek közben futnia kell.

## 7. lépés: Kipróbálás

Az alkalmazás futása közben, egy **másik terminálban**. A példákban az 5200-as port szerepel, nálad a `Now listening on` sorban látható portot használd.

**Windows (PowerShell):** a régebbi PowerShellben a `curl` valójában az `Invoke-WebRequest` álneve, ezért a curl-os parancsok hibát dobhatnak. Használd az `Invoke-RestMethod` parancsot (vagy a `curl.exe`-t):

```powershell
# Létrehozás
Invoke-RestMethod -Method Post -Uri http://localhost:5200/users `
  -ContentType "application/json" `
  -Body '{"username":"teszt","password":"titok123","email":"teszt@example.com"}'

# Listázás
Invoke-RestMethod http://localhost:5200/users
```

**macOS / Linux (curl):**

```bash
# Létrehozás
curl -X POST http://localhost:5200/users -H "Content-Type: application/json" \
  -d '{"username":"teszt","password":"titok123","email":"teszt@example.com"}'

# Listázás
curl http://localhost:5200/users
```

A listázást (GET) a böngészőben is megnyithatod: `http://localhost:5200/users`. Grafikus alternatíva: Postman, vagy a VS Code *REST Client* bővítménye.

Ellenőrizheted közvetlenül az adatbázisban is. A jelszó oszlopban egy `$2a$...` kezdetű hash-nek kell lennie:

```bash
docker exec -it mysql-dev mysql -uappuser -papppw appdb -e "SELECT * FROM users;"
```

Ha újraindítod az alkalmazást, a felhasználók megmaradnak, mert az adatok a MySQL-ben vannak, nem a memóriában. A mock ezzel szemben minden újraindításkor elveszítette őket.

---

## Bónusz: Entity Framework Core

Ez a Dapper helyett használható, nem mellette.

**Csomagok és eszköz:**

```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet tool install --global dotnet-ef
```

**Entitás és DbContext** (töröld a `Models.cs`-ből a `User` recordot, mert ütközne az új `User` osztállyal):

```csharp
using Microsoft.EntityFrameworkCore;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Email { get; set; } = "";
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Username).IsUnique();
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
}
```

**Repository EF-fel:**

```csharp
public class EfUserRepository(AppDbContext db) : IUserRepository
{
    public async Task<IEnumerable<User>> GetAll() =>
        await db.Users.AsNoTracking().ToListAsync();

    public async Task<User?> GetById(int id) =>
        await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public async Task<int> Create(string username, string passwordHash, string email)
    {
        var u = new User { Username = username, Password = passwordHash, Email = email };
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return u.Id;
    }

    public async Task<bool> Update(int id, string username, string email)
    {
        var u = await db.Users.FindAsync(id);
        if (u is null) return false;
        u.Username = username;
        u.Email = email;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var u = await db.Users.FindAsync(id);
        if (u is null) return false;
        db.Users.Remove(u);
        await db.SaveChangesAsync();
        return true;
    }
}
```

**Regisztráció a Program.cs-ben** (a `MySqlUserRepository` regisztrációja helyett):

```csharp
var cs = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(cs, ServerVersion.AutoDetect(cs)));
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
```

**Migráció.** Ha EF-fel mész, az `init.sql`-t elhagyhatod: töröld a fájlt **és** a hozzá tartozó sort a `docker-compose.yml` volumes részéből (különben a Docker az `init.sql` helyén egy üres mappát hoz létre). Ha a tábla már létrejött az `init.sql`-ből, először resetelj: `docker compose down -v`, majd `docker compose up -d`. Ezután:

```bash
dotnet ef migrations add Init
dotnet ef database update
```

---

## Hibaelhárítás

### Visual Studio: nem találom az "ASP.NET Core Empty" sablont

- A *Create a new project* ablakban a szűrők legyenek: **All languages** (vagy C#) / **All platforms** / **All project types**, és a keresőbe csak ennyit írj: `ASP.NET Core Empty`.
- Ha nincs, hiányzik a webfejlesztési komponens: Visual Studio Installer → *Modify* → **ASP.NET and web development** → *Install*.
- Megkerülés: hozd létre a projektet parancssorból (`dotnet new web -n UserApi`), majd nyisd meg a `.csproj` fájlt a *File → Open → Project/Solution* paranccsal.
- Az **Empty Project (.NET Framework)**, a **Blazor Server App Empty** és az **Aspire Empty App** sablon **nem megfelelő** ehhez a feladathoz.

### Visual Studio: az Add → Class nem kínál osztályt

Az *Add → New Item...* ablakban próbáld a **Show All Templates** linket, és nézd meg, hogy a projektet a *File → Open → Project/Solution* paranccsal nyitottad-e meg (a *Folder* nézet kevesebb sablont kínál). Ha egyik sem segít, hozd létre a `.cs` fájlokat kézzel az Intézőben (lásd a 0.6 lépést). A projekt típusa ettől még rendben van, ha a `.csproj` első sora `Sdk="Microsoft.NET.Sdk.Web"`.

### "The current Visual Studio version does not support targeting .NET 10.0"

A Visual Studio 2022 nem tud .NET 10-re fordítani. Három megoldás:
1. **Átírás .NET 9-re (legegyszerűbb):** a `.csproj` fájlban cseréld a `<TargetFramework>net10.0</TargetFramework>` sort `net9.0`-ra (régebbi Visual Studiónál `net8.0`-ra). Ha hiányzik a runtime, telepítsd a megfelelő SDK-t (`winget install Microsoft.DotNet.SDK.9`).
2. **Visual Studio frissítése** a 18.0-s (2026) vagy újabb verzióra.
3. **Futtatás a terminálból:** a `dotnet run` a Visual Studio verziójától függetlenül működik.

### Build error: CS0103 "The name 'BCrypt' does not exist in the current context"

A build nem látja a BCrypt csomagot. Ellenőrizd, hogy a `dotnet add package BCrypt.Net-Next` parancsot a **`.csproj` mappájában** futtattad-e (nem a solution mappájában). A `.csproj` fájlban ilyen sornak kell lennie: `<PackageReference Include="BCrypt.Net-Next" Version="..." />`. Ha nincs, futtasd újra a parancsot a helyes mappában, majd `dotnet build`.

### Build error: CS0246 "type or namespace could not be found"

Valószínűleg beszúrt egy `namespace ...;` sort a Visual Studio valamelyik fájl tetejére. Töröld ki a `Models.cs`, a `MockUserRepository.cs` és a `MySqlUserRepository.cs` fájlok elejéről. Ellenőrizd azt is, hogy a `using Dapper;` és a `using MySqlConnector;` sor benne van-e a `MySqlUserRepository.cs`-ben.

### Build error: CS0101 / CS0111 "already contains a definition"

Valamelyik osztály vagy sor kétszer szerepel. Ilyen lehet, ha a Visual Studio által létrehozott üres osztály bent maradt a fájlban, vagy a `Program.cs` végén dupla `app.Run();` van. Ha nem látod a pontos hibát, futtasd a `dotnet build` parancsot, vagy nézd meg az **Error List** ablakot (*View → Error List*).

### Hol teszteljem a curl / Invoke-RestMethod parancsot?

Egy **másik, új terminálban**, miközben az alkalmazás fut a másikban. PowerShellben a `curl` helyett használd az `Invoke-RestMethod` vagy a `curl.exe` parancsot. A GET a böngészőben is megnyitható. Ha *"Unable to connect"* hibát kapsz, az alkalmazás nem fut, vagy más a port. Ha 404-et kapsz, a `Program.cs` még a sablon eredeti tartalmát tartalmazza.

### A `dotnet run` már fut, kell még az F5?

Nem. Ha a `dotnet run` kiírja a `Now listening on` sort, az alkalmazás fut, és az F5 csak ütközne vele. Kódmódosítás után: **Ctrl + C**, majd újra `dotnet run` (vagy használd a `dotnet watch` parancsot).

### Docker: "500 Internal Server Error ... dockerDesktopLinuxEngine/_ping"

A Docker Desktop felülete fut, de a mögötte lévő Linux-motor nem indult el rendesen. Menj végig ezeken sorban, és minden lépés után próbáld újra a `docker run hello-world` parancsot:

1. Nézd meg, hogy a Docker Desktop bal alsó sarkában zöld-e az állapotjelző. Ha sárga, várj 1-2 percet.
2. Lépj ki a Docker Desktopból (tálca ikon → *Quit Docker Desktop*), várj 10 másodpercet, majd indítsd újra.
3. Frissítsd és állítsd le a WSL-t rendszergazdai PowerShellben, majd indítsd újra a Docker Desktopot: `wsl --update` és `wsl --shutdown`
4. Ellenőrizd a virtualizációt (Feladatkezelő → Teljesítmény → CPU) és a WSL állapotát (`wsl --status`, `wsl -l -v`). Ha a WSL hiányzik, lásd a 0.1 lépést.
5. Ellenőrizd, hogy a **Virtual Machine Platform** és a **Windows Subsystem for Linux** szolgáltatás be van kapcsolva.
6. Docker Desktop → Settings → General → legyen bejelölve a **Use the WSL 2 based engine**.
7. Ellenőrizd a contextet: `docker context ls`. A `desktop-linux` mellett csillagnak kell lennie, ha nem: `docker context use desktop-linux`
8. Végső megoldás: Docker Desktop → bogár ikon (Troubleshoot) → *Restart*, majd szükség esetén *Reset to factory defaults*. Ez törli a meglévő konténereket és image-eket.

### A 3306-os port foglalt

Ha már fut egy másik MySQL a gépeden, a konténer nem tud elindulni a 3306-os porton. Állítsd le a másik MySQL-t, vagy módosítsd a `docker-compose.yml`-ben a portot (például `"3307:3306"`), és a connection stringben is írd át a `Port` értékét.

### Az alkalmazás nem éri el az adatbázist indításkor

A MySQL konténernek az első indításkor több másodpercre van szüksége. Ellenőrizd a `docker logs mysql-dev` paranccsal, hogy megjelent-e a "ready for connections" sor, és csak utána indítsd az API-t. Ellenőrizd a `docker ps` paranccsal azt is, hogy a konténer fut-e.

### Nem szeretném a Docker Desktopot használni

A Docker Hub csak image-tár (innen töltődik le a `mysql` image), konténert nem futtat, ezért valamilyen Docker-motor mindenképp kell. Alternatívák:
- **Docker Engine WSL 2-ben:** Ubuntu a WSL-ben (`wsl --install`), abba a Docker Engine a hivatalos leírás szerint (https://docs.docker.com/engine/install/ubuntu/). A `docker compose` ugyanúgy működik, és a 3306-os port a Windowsból is elérhető `localhost`-on.
- **Podman Desktop vagy Rancher Desktop:** a Docker Desktop ingyenes alternatívái.
- **MySQL közvetlen telepítése Docker nélkül:** a MySQL Community Serverrel vagy a MySQL Installerrel. Az 1. lépés kimarad, a felhasználót és az adatbázist kézzel kell létrehoznod az `init.sql` alapján.
- **Felhős MySQL-szolgáltatás:** a connection stringben a szolgáltató adatait kell megadni.
