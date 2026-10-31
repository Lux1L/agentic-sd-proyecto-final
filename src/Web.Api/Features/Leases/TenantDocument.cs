using System.Text.RegularExpressions;

namespace Web.Api.Features.Leases;

public static partial class TenantDocument
{
    private static readonly int[] RucWeights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
    private static readonly string[] RucPrefixes = ["10", "15", "17", "20"];

    public static bool IsValid(DocumentType type, string? number) => type switch
    {
        DocumentType.Dni => number is not null && DniRegex().IsMatch(number),
        DocumentType.Ce => number is not null && CeRegex().IsMatch(number),
        DocumentType.Ruc => IsValidRuc(number),
        _ => false
    };

    public static bool IsValidPeruvianMobile(string? phone) =>
        phone is not null && MobileRegex().IsMatch(phone);

    private static bool IsValidRuc(string? number)
    {
        if (number is null || !RucRegex().IsMatch(number))
        {
            return false;
        }

        if (!RucPrefixes.Contains(number[..2], StringComparer.Ordinal))
        {
            return false;
        }

        int sum = 0;
        for (int i = 0; i < RucWeights.Length; i++)
        {
            sum += (number[i] - '0') * RucWeights[i];
        }

        int check = 11 - sum % 11;
        check = check switch
        {
            10 => 0,
            11 => 1,
            _ => check
        };

        return check == number[10] - '0';
    }

    [GeneratedRegex(@"^[0-9]{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex DniRegex();

    [GeneratedRegex(@"^[A-Za-z0-9]{9,12}$", RegexOptions.CultureInvariant)]
    private static partial Regex CeRegex();

    [GeneratedRegex(@"^[0-9]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex RucRegex();

    [GeneratedRegex(@"^9[0-9]{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex MobileRegex();
}
