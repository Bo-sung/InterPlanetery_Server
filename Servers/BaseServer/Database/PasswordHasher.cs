using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BaseServer.Database;

internal enum PasswordVerificationResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}

internal static class PasswordHasher
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int Iterations = 600_000;
    private const int MaximumAcceptedIterations = 2_000_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MaximumPasswordLength = 1_024;

    internal static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        if (password.Length > MaximumPasswordLength)
            throw new ArgumentOutOfRangeException(nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        try
        {
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            try
            {
                return string.Join(
                    '$',
                    Algorithm,
                    Iterations.ToString(CultureInfo.InvariantCulture),
                    Convert.ToBase64String(salt),
                    Convert.ToBase64String(hash));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(hash);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(salt);
        }
    }

    internal static PasswordVerificationResult Verify(string password, string storedValue)
    {
        if (string.IsNullOrEmpty(password) || password.Length > MaximumPasswordLength || string.IsNullOrEmpty(storedValue))
            return PasswordVerificationResult.Failed;

        if (storedValue.StartsWith(Algorithm + '$', StringComparison.Ordinal))
            return VerifyPbkdf2(password, storedValue);

        if (storedValue.StartsWith("pbkdf2-", StringComparison.OrdinalIgnoreCase))
            return PasswordVerificationResult.Failed;

        return FixedTimeLegacyEquals(password, storedValue)
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Failed;
    }

    private static PasswordVerificationResult VerifyPbkdf2(string password, string storedValue)
    {
        string[] parts = storedValue.Split('$');
        if (parts.Length != 4 ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int iterations) ||
            iterations <= 0 ||
            iterations > MaximumAcceptedIterations)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        if (salt.Length != SaltSize || expectedHash.Length != HashSize)
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(expectedHash);
            return PasswordVerificationResult.Failed;
        }

        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            try
            {
                if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
                    return PasswordVerificationResult.Failed;

                return iterations < Iterations
                    ? PasswordVerificationResult.SuccessRehashNeeded
                    : PasswordVerificationResult.Success;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(actualHash);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(expectedHash);
        }
    }

    private static bool FixedTimeLegacyEquals(string password, string storedValue)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] storedBytes = Encoding.UTF8.GetBytes(storedValue);

        try
        {
            byte[] passwordDigest = SHA256.HashData(passwordBytes);
            byte[] storedDigest = SHA256.HashData(storedBytes);

            try
            {
                return CryptographicOperations.FixedTimeEquals(passwordDigest, storedDigest);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordDigest);
                CryptographicOperations.ZeroMemory(storedDigest);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(storedBytes);
        }
    }
}
