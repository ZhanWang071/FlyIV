using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    /// <summary>
    /// This class is used to deserialize material data extracted with ShaderUtility
    /// UnityJsonSerializer only supports field names that match the Json Data
    /// NewtonsoftJson uses JsonProperty attribute to match the right properties
    /// </summary>
    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class MaterialData
#else
    public class MaterialData
#endif
    {
        [JsonProperty("serializedVersion")] public string serializedVersion;
        [JsonProperty("m_Name")] public string m_Name;
        [JsonProperty("m_Shader")] public ShaderData m_Shader;
        [JsonProperty("m_ValidKeywords")] public List<string> m_ValidKeywords;
        [JsonProperty("m_InvalidKeywords")] public List<string> m_InvalidKeywords;
        [JsonProperty("m_LightmapFlags")] public int m_LightmapFlags;

        [JsonProperty("m_EnableInstancingVariants")]
        public bool m_EnableInstancingVariants;

        [JsonProperty("m_DoubleSidedGI")] public bool m_DoubleSidedGI;
        [JsonProperty("m_CustomRenderQueue")] public int m_CustomRenderQueue;
        [JsonProperty("stringTagMap")] public Dictionary<string, object> stringTagMap;
        [JsonProperty("disabledShaderPasses")] public List<string> disabledShaderPasses;
        [JsonProperty("m_SavedProperties")] public SavedProperties m_SavedProperties;
        [JsonProperty("m_BuildTextureStacks")] public List<object> m_BuildTextureStacks;
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ShaderData
#else
    public class ShaderData
#endif
    {
        [JsonProperty("fileID")] public long fileID;
        [JsonProperty("guid")] public string guid;
        [JsonProperty("type")] public int type;
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SavedProperties
#else
    public class SavedProperties
#endif
    {
        [JsonProperty("serializedVersion")] public string serializedVersion;
        [JsonProperty("m_TexEnvs")] public List<TexEnv> m_TexEnvs;
        [JsonProperty("m_Ints")] public List<IntegerProperty> m_Ints;
        [JsonProperty("m_Floats")] public List<FloatProperty> m_Floats;
        [JsonProperty("m_Colors")] public List<ColorProperty> m_Colors;
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class TexEnv
#else
    public class TexEnv
#endif
    {
        /// <summary>
        /// Texture Property Name
        /// </summary>
        [JsonProperty("first")] public string first;

        /// <summary>
        /// Texture Data
        /// </summary>
        [JsonProperty("second")] public TextureProperty second;
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class TextureProperty
#else
    public class TextureProperty
#endif
    {
        public Texture texture;
        [JsonProperty("m_Texture")] public SerializedTex m_Texture;
        [JsonProperty("m_Scale")] public Vector2 m_Scale;
        [JsonProperty("m_Offset")] public Vector2 m_Offset;
    }

    /// <summary>
    /// Used to read UnityEngine.Object.Texture data
    /// </summary>
    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SerializedTex
#else
    public class SerializedTex
#endif
    {
        public long instanceID;
        public long fileID;
        public string guid;
        public int type;
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class IntegerProperty
#else
    public class IntegerProperty
#endif
    {
        [JsonProperty("first")] public string first;
        [JsonProperty("second")] public int second;
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FloatProperty
#else
    public class FloatProperty
#endif
    {
        [JsonProperty("first")] public string first;
        [JsonProperty("second")] public double second;
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ColorProperty
#else
    public class ColorProperty
#endif
    {
        [JsonProperty("first")] public string first;
        [JsonProperty("second")] public Color second;
    }

    /// <summary>
    /// Wraps Material data to match the json output from ShaderUtil.
    /// </summary>
    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class MaterialDataJson
#else
    public class MaterialDataJson
#endif
    {
        [JsonProperty("Material")] public MaterialData Material;
    }
}
