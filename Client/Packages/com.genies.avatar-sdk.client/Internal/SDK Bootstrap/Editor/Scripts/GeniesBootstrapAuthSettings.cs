using System;
using UnityEngine;

namespace Genies.Sdk.Bootstrap.Editor
{
    [Serializable]
    internal class GeniesBootstrapAuthSettings
    {
        public string ClientId;
        public string ClientSecret;

        public static GeniesBootstrapAuthSettings LoadFromResources()
        {
            var ta = Resources.Load<TextAsset>("GeniesAuthSettings");
            if (ta == null)
            {
                return null;
            }

            byte[] bytes = ta.bytes;
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            // Reverse XOR
            byte key = 0x5A;
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= key;
            }

            string json = System.Text.Encoding.UTF8.GetString(bytes);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<GeniesBootstrapAuthSettings>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}