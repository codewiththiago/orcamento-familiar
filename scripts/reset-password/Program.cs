using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Npgsql;

var connection = GetArg("--connection");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine("Uso: dotnet run -- --connection \"<CONNECTION_STRING>\" [--email <email> --password <nova-senha>] [--family] [--list]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  --connection  Connection string ou URL postgres (ex.: postgres://user:pass@host:5432/db)");
    Console.Error.WriteLine("  --email       E-mail do usuário a redefinir");
    Console.Error.WriteLine("  --password    Nova senha");
    Console.Error.WriteLine("  --family      Mostra o codigo/PIN da familia (cria um novo se vazio)");
    Console.Error.WriteLine("  --list        Lista usuários cadastrados");
    return 1;
}

if (HasFlag("--list"))
{
    await using var c = await OpenConnection(connection);
    await using var cmd = new NpgsqlCommand("SELECT \"Email\" FROM \"AspNetUsers\" ORDER BY \"Email\"", c);
    await using var reader = await cmd.ExecuteReaderAsync();
    Console.WriteLine("Usuários cadastrados:");
    while (await reader.ReadAsync())
        Console.WriteLine($"  - {reader.GetString(0)}");
    return 0;
}

if (HasFlag("--family"))
{
    await using var c = await OpenConnection(connection);
    var (code, pin) = await GetFamilyAsync(c);
    if (code is null)
    {
        (code, pin) = (GenerateCode(), Random.Shared.Next(1000, 10000).ToString());
        await using var ins = new NpgsqlCommand(
            "INSERT INTO \"FamilyAccess\" (\"InviteCode\", \"Pin\", \"CreatedAt\") VALUES (@c, @p, @t)",
            c);
        ins.Parameters.AddWithValue("c", code);
        ins.Parameters.AddWithValue("p", pin);
        ins.Parameters.AddWithValue("t", DateTime.UtcNow);
        await ins.ExecuteNonQueryAsync();
        Console.WriteLine("FamilyAccess estava vazio — novo acesso criado:");
    }
    else
    {
        Console.WriteLine("Código da família atual:");
    }
    Console.WriteLine($"  Código: {code}");
    Console.WriteLine($"  PIN   : {pin}");
    Console.WriteLine("Use no app em 'Esqueci minha senha' (e-mail + código + PIN).");
    return 0;
}

var email = GetArg("--email");
var password = GetArg("--password");
if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
{
    Console.Error.WriteLine("Informe --email e --password para redefinir a senha.");
    return 1;
}

await using var conn = await OpenConnection(connection);
await using var find = new NpgsqlCommand(
    "SELECT \"Id\" FROM \"AspNetUsers\" WHERE LOWER(\"Email\") = LOWER(@email)",
    conn);
find.Parameters.AddWithValue("email", email);
var userId = (string?)await find.ExecuteScalarAsync();
if (userId is null)
{
    Console.Error.WriteLine($"Usuário '{email}' não encontrado.");
    return 1;
}

var hash = new PasswordHasher<User>().HashPassword(null!, password);

await using var upd = new NpgsqlCommand(
    "UPDATE \"AspNetUsers\" SET \"PasswordHash\" = @hash WHERE \"Id\" = @id",
    conn);
upd.Parameters.AddWithValue("hash", hash);
upd.Parameters.AddWithValue("id", userId);
var rows = await upd.ExecuteNonQueryAsync();

Console.WriteLine(rows > 0
    ? $"Senha de '{email}' redefinida com sucesso. Faça login no app com a nova senha."
    : "Nenhuma linha atualizada.");

return 0;

static string? GetArg(string name)
{
    var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == name)
            return args[i + 1];
    return null;
}

static bool HasFlag(string name) =>
    Environment.GetCommandLineArgs().Skip(1).Contains(name);

static async Task<NpgsqlConnection> OpenConnection(string connection)
{
    var conn = new NpgsqlConnection(connection);
    await conn.OpenAsync();
    return conn;
}

static async Task<(string? Code, string? Pin)> GetFamilyAsync(NpgsqlConnection conn)
{
    await using var cmd = new NpgsqlCommand(
        "SELECT \"InviteCode\", \"Pin\" FROM \"FamilyAccess\" ORDER BY \"CreatedAt\" LIMIT 1",
        conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return (null, null);
    return (reader.GetString(0), reader.GetString(1));
}

static string GenerateCode()
{
    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
}

sealed class User
{
}

static partial class Program { }