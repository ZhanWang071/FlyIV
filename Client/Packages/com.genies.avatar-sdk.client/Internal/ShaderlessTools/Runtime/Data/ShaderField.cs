using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    [Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum ShaderFieldType
#else
    public enum ShaderFieldType
#endif
    {
        Int,
        StringList,
        Boolean,
        String,
        Guid
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ShaderField
#else
    public class ShaderField
#endif
    {
        public string name;
        public ShaderFieldType type;
        [SerializeReference] public object Data;

        public ShaderField()
        {
        }

        public ShaderField(string name, object data, ShaderFieldType type)
        {
            this.name = name;
            this.Data = data;
            this.type = type;
        }
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class StringField
#else
    public class StringField
#endif
    {
        public string value;

        public StringField()
        {
        }

        public StringField(string value)
        {
            this.value = value;
        }
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class StringListField
#else
    public class StringListField
#endif
    {
        public List<string> value;

        public StringListField()
        {
        }

        public StringListField(List<string> value)
        {
            this.value = value;
        }
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class IntField
#else
    public class IntField
#endif
    {
        public int value;

        public IntField()
        {
        }

        public IntField(int value)
        {
            this.value = value;
        }
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class BoolField
#else
    public class BoolField
#endif
    {
        public bool value;

        public BoolField()
        {
        }

        public BoolField(bool value)
        {
            this.value = value;
        }
    }

    [Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class ShaderFieldExtensions
#else
    public static class ShaderFieldExtensions
#endif
    {
        public static string GetAsString(this ShaderField field)
        {
            var data = field.Data as StringField;
            return data?.value;
        }

        public static List<string> GetAsStringList(this ShaderField field)
        {
            var data = field.Data as StringListField;

            return data?.value;
        }

        public static int GetAsInt(this ShaderField field)
        {
            var data = field.Data as IntField;
            return data?.value ?? 0;
        }

        public static bool GetAsBool(this ShaderField field)
        {
            var data = field.Data as BoolField;
            return data?.value ?? false;
        }
    }
}
