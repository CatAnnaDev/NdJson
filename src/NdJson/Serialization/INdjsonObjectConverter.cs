namespace NdJson.Serialization
{
    public interface INdjsonObjectConverter
    {
        void WriteMembers(ref JsonWriter writer, object value, NdjsonOptions options);
    }
}
