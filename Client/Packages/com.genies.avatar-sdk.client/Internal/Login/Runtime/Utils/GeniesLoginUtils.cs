using System.Net.Mail;

namespace Genies.Login
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class GeniesLoginUtils
#else
    public static class GeniesLoginUtils
#endif
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.EndsWith("."))
            {
                return false;
            }

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsLikelyPhone(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim();

            bool hasDigit = false;
            foreach (var ch in value)
            {
                if (char.IsDigit(ch))
                {
                    hasDigit = true;
                }
                else if (ch != ' ' && ch != '+' && ch != '-' && ch != '(' && ch != ')')
                {
                    return false;
                }
            }

            return hasDigit;
        }
    }
}
