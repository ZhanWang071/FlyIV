using System;
using System.Collections.Generic;

namespace Genies.Analytics {
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AnalyticProperties {
#else
    public class AnalyticProperties {
#endif
        private Dictionary<string, object> Properties = new Dictionary<string, object>();

        public AnalyticProperties() { }
        public AnalyticProperties(AnalyticProperties properties) {
            Properties = new Dictionary<string, object>(properties.GetDictionary());
        }

        public void AddProperty(string propertyName, string propertyValue) {
            Properties[propertyName] = propertyValue;
        }

        public void AddProperty(string propertyName, object propertyValue) {
            Properties[propertyName] = propertyValue;
        }

        public Dictionary<string, object> GetDictionary() => Properties;
    }
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAnalyticsService {
#else
    public interface IAnalyticsService {
#endif
        void LogEvent(string eventName);
        void LogEvent(string eventName, AnalyticProperties properties);
        void SetUserProperty(string propertyName, string value);
        void SetIdentity(IdentityData identityData);
        void IncrementProfileProperty(string key, int incrementValue);
    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class IdentityData
#else
    public class IdentityData
#endif
    {
        public string CognitoId { get; }
        public string Username { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string PhoneNumber { get; }
        public string Email { get; }
        public string Birthday { get; }
        public string School { get; }
        public string EventId { get; }
        public string Iat { get; }

        public IdentityData(string cognitoId, string phoneNumber, string email = null, string birthday = null, string school = null, string username = null, string firstName = null, string lastName = null, string eventId = null, string iat = null)
        {
            CognitoId = cognitoId;
            PhoneNumber = phoneNumber;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Birthday = birthday;
            School = school;
            EventId = eventId;
            Iat = iat;
        }
    }
}
