using System.Linq;

namespace Spard.Service.Helpers
{
    /// <summary>
    /// Provides helper method for working with culture string.
    /// </summary>
    public static class CultureHelper
    {
        public const string DefaultCulture = "en-US";

        /// <summary>
        /// Gets first language from Accept-Language header.
        /// </summary>
        /// <param name="acceptHeaderValue">Accept-Language header value.</param>
        /// <returns>First language from Accept-Language header value or default value.</returns>
        public static string GetCultureFromAcceptLanguageHeader(string acceptHeaderValue)
        {
            var firstLanguage = acceptHeaderValue.Split(',').FirstOrDefault();
            return string.IsNullOrEmpty(firstLanguage) ? DefaultCulture : firstLanguage;
        }
    }
}
