using System;
using System.Linq;
using System.Text;

public class PasswordGenerator
{
    private static readonly Random Random = new Random();
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string SpecialCharacters = "!@#$%^&*()_+[]{}|;:,.<>?";

    public static string GeneratePassword(int length = 12)
    {
        var allCharacters = Uppercase + Lowercase + Digits + SpecialCharacters;
        var password = new StringBuilder();

        // Ensure the password contains at least one character from each category
        password.Append(Uppercase[Random.Next(Uppercase.Length)]);
        password.Append(Lowercase[Random.Next(Lowercase.Length)]);
        password.Append(Digits[Random.Next(Digits.Length)]);
        password.Append(SpecialCharacters[Random.Next(SpecialCharacters.Length)]);

        // Fill the rest of the password length with random characters
        for (int i = 4; i < length; i++)
        {
            password.Append(allCharacters[Random.Next(allCharacters.Length)]);
        }

        // Shuffle the characters to ensure randomness
        return new string(password.ToString().OrderBy(c => Random.Next()).ToArray());
    }
}
