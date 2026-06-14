using System.Security.Cryptography;
using System.Text;
using AuctionNest.Application.Common.Interfaces.Services;
using Konscious.Security.Cryptography;

namespace AuctionNest.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 4;
    private const int MemorySize = 65536; // 64 MB
    private const int DegreeOfParallelism = 2;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Compute(password, salt);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hashedPassword)
    {
        var parts = hashedPassword.Split('.');
        if (parts.Length != 2) return false;

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Compute(password, salt);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Compute(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            Iterations = Iterations,
            MemorySize = MemorySize,
            DegreeOfParallelism = DegreeOfParallelism
        };
        return argon2.GetBytes(HashSize);
    }
}