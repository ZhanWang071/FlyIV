using System;

namespace Genies.Login.Native.Data
{
    /// <summary>
    /// Local-only format validator for Genies client credentials.
    /// This does not call any network APIs; it just checks basic shape.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthLocalValidator
#else
    public static class GeniesAuthLocalValidator
#endif
    {
        private const int ExpectedClientSecretHexLength = 64;
        private const int ExpectedClientIdSuffixLength = 26;
        private const string ClientIdPrefix = "client_";

        public static bool LooksLikeClientSecret(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                return false;
            }

            if (secret.Length != ExpectedClientSecretHexLength)
            {
                return false;
            }

            for (int i = 0; i < secret.Length; i++)
            {
                char c = secret[i];
                bool isDigit = c >= '0' && c <= '9';
                bool isLowerHex = c >= 'a' && c <= 'f';
                bool isUpperHex = c >= 'A' && c <= 'F';

                if (!(isDigit || isLowerHex || isUpperHex))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool LooksLikeClientId(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return false;
            }

            if (!clientId.StartsWith(ClientIdPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            string suffix = clientId.Substring(ClientIdPrefix.Length);
            if (suffix.Length != ExpectedClientIdSuffixLength)
            {
                return false;
            }

            for (int i = 0; i < suffix.Length; i++)
            {
                char c = suffix[i];
                char u = char.ToUpperInvariant(c);

                bool isDigit = u >= '0' && u <= '9';
                bool isLetter = u >= 'A' && u <= 'Z';

                if (!(isDigit || isLetter))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool LooksLikeValidPair(string clientId, string clientSecret)
        {
            return LooksLikeClientId(clientId) && LooksLikeClientSecret(clientSecret);
        }
    }
}
