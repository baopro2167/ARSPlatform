using System.Text.RegularExpressions;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public static class OrcidIdUtility
    {
        private const string OrcidHttpsPrefix = "https://orcid.org/";

        private static readonly Regex CanonicalOrcidRegex =
            new Regex(
                @"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$",
                RegexOptions.Compiled);

        public static bool TryNormalize(
            string? input,
            out string normalizedOrcidId)
        {
            normalizedOrcidId = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var value = input.Trim();

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                if (!string.Equals(
                        uri.Scheme,
                        Uri.UriSchemeHttps,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.Equals(
                        uri.Host,
                        "orcid.org",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(uri.Query) ||
                    !string.IsNullOrEmpty(uri.Fragment))
                {
                    return false;
                }

                value = uri.AbsolutePath.Trim('/');
            }

            value = value.ToUpperInvariant();

            if (!CanonicalOrcidRegex.IsMatch(value))
                return false;

            normalizedOrcidId = value;
            return true;
        }

        public static bool HasValidChecksum(
            string normalizedOrcidId)
        {
            if (string.IsNullOrWhiteSpace(normalizedOrcidId))
                return false;

            var value = normalizedOrcidId
                .Replace("-", string.Empty)
                .ToUpperInvariant();

            if (value.Length != 16)
                return false;

            for (var index = 0; index < 15; index++)
            {
                if (!char.IsDigit(value[index]))
                    return false;
            }

            if (!char.IsDigit(value[15]) &&
                value[15] != 'X')
            {
                return false;
            }

            var total = 0;

            for (var index = 0; index < 15; index++)
            {
                var digit = value[index] - '0';
                total = (total + digit) * 2;
            }

            var remainder = total % 11;
            var result = (12 - remainder) % 11;

            var expectedCheckDigit =
                result == 10
                    ? 'X'
                    : (char)('0' + result);

            return value[15] == expectedCheckDigit;
        }

        public static bool TryNormalizeAndValidate(
            string? input,
            out string normalizedOrcidId)
        {
            normalizedOrcidId = string.Empty;

            if (!TryNormalize(
                    input,
                    out var normalized))
            {
                return false;
            }

            if (!HasValidChecksum(normalized))
                return false;

            normalizedOrcidId = normalized;
            return true;
        }

        public static string ToHttpsUrl(
            string normalizedOrcidId)
        {
            return OrcidHttpsPrefix + normalizedOrcidId;
        }
    }
}