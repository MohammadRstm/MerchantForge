using System.Security.Cryptography;

namespace MerchForge.api.Services.Common;

/// <summary>
/// Generates the initial password for an account someone else creates — a business
/// owner completing registration, or an owner adding a team member.
///
/// Shared rather than duplicated so both paths produce passwords with the same
/// guarantees. Excludes the characters that are easy to confuse when a password is
/// read aloud or copied off a screen (I/l/1, O/0), which is exactly how these are
/// handed over.
/// </summary>
public static class PasswordGenerator
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Numbers = "23456789";
    private const string Special = "!@#$%^&*";

    private const string All = Upper + Lower + Numbers + Special;

    public static string Generate(int length = 16)
    {
        var password = new char[length];

        // Guarantee at least one character from each category.
        password[0] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
        password[1] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
        password[2] = Numbers[RandomNumberGenerator.GetInt32(Numbers.Length)];
        password[3] = Special[RandomNumberGenerator.GetInt32(Special.Length)];

        for (var i = 4; i < length; i++)
        {
            password[i] = All[RandomNumberGenerator.GetInt32(All.Length)];
        }

        // Shuffle so the first four positions aren't predictable by category.
        for (var i = password.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
