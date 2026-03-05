namespace Genies.CloudSave
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ICloudSaveJsonSerializer<T>
#else
    public interface ICloudSaveJsonSerializer<T>
#endif
    {
        string ToJson(T data);
        T FromJson(string json);
        bool IsValidJson(string json);
    }
}
