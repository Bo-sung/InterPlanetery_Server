using BaseServer.Database;

namespace BaseServer.SecurityTests;

internal static class Program
{
    private static readonly (string Name, Action Run)[] Tests =
    {
        ("hash round trip", TestHashRoundTrip),
        ("wrong password rejected", TestWrongPassword),
        ("unique salt", TestUniqueSalt),
        ("malformed hash rejected", TestMalformedHash),
        ("legacy password requests upgrade", TestLegacyUpgrade),
        ("unknown PBKDF2 format rejected", TestUnknownPbkdf2Format)
    };

    public static int Main()
    {
        int failed = 0;
        foreach ((string name, Action run) in Tests)
        {
            try
            {
                run();
                Console.WriteLine($"PASS: {name}");
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine($"FAIL: {name}\n{exception}");
            }
        }

        Console.WriteLine($"Completed {Tests.Length} tests: {Tests.Length - failed} passed, {failed} failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void TestHashRoundTrip()
    {
        const string password = "correct horse battery staple";
        string stored = PasswordHasher.HashPassword(password);

        AssertFalse(stored.Contains(password, StringComparison.Ordinal), "stored value must not contain password");
        AssertEqual(PasswordVerificationResult.Success, PasswordHasher.Verify(password, stored), "round trip");
    }

    private static void TestWrongPassword()
    {
        string stored = PasswordHasher.HashPassword("expected-password");
        AssertEqual(PasswordVerificationResult.Failed, PasswordHasher.Verify("different-password", stored), "wrong password");
    }

    private static void TestUniqueSalt()
    {
        string first = PasswordHasher.HashPassword("same-password");
        string second = PasswordHasher.HashPassword("same-password");
        AssertFalse(string.Equals(first, second, StringComparison.Ordinal), "hashes must use unique salts");
    }

    private static void TestMalformedHash()
    {
        AssertEqual(
            PasswordVerificationResult.Failed,
            PasswordHasher.Verify("password", "pbkdf2-sha256$invalid$not-base64$not-base64"),
            "malformed known format");
    }

    private static void TestLegacyUpgrade()
    {
        AssertEqual(
            PasswordVerificationResult.SuccessRehashNeeded,
            PasswordHasher.Verify("legacy-password", "legacy-password"),
            "legacy match");
        AssertEqual(
            PasswordVerificationResult.Failed,
            PasswordHasher.Verify("wrong-password", "legacy-password"),
            "legacy mismatch");
    }

    private static void TestUnknownPbkdf2Format()
    {
        AssertEqual(
            PasswordVerificationResult.Failed,
            PasswordHasher.Verify("value", "pbkdf2-sha512$value"),
            "unknown PBKDF2 format must not fall back to plaintext");
    }

    private static void AssertFalse(bool value, string message)
    {
        if (value)
            throw new InvalidOperationException($"Expected false: {message}");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
        where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message}: expected {expected}, actual {actual}");
    }
}
