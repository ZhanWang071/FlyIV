using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.IO;

namespace UMA.AssetBundles
{
	[System.Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
	internal partial class UMAEncryptedBundle : ScriptableObject
#else
	public partial class UMAEncryptedBundle : ScriptableObject
#endif
	{
		public string assetBundleName = "";
		public byte[] IV;
		public byte[] data;

#if UNITY_EDITOR
		/// <summary>
		/// encrypts an asset bundle into an this UMAEncryptedBundle's data
		/// </summary>
		/// <param name="bundleName">The bundleName as defined in a BuildPipeline buildmap (AssetBundleBuild[]}</param>
		/// <param name="originalPath">Path to the uncompressed bundle</param>
		public void GenerateData(string bundleName, string originalPath)
		{
			assetBundleName = bundleName;
			string path = originalPath;
			byte[] originalData = File.ReadAllBytes(path);
			data = EncryptionUtil.Encrypt(originalData, ref IV);
		}
#endif
	}
}
