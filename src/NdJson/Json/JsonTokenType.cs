namespace NdJson
{
    public enum JsonTokenType : byte
    {
        None = 0,
        StartObject = 1,
        EndObject = 2,
        StartArray = 3,
        EndArray = 4,
        PropertyName = 5,
        String = 6,
        Number = 7,
        True = 8,
        False = 9,
        Null = 10
    }
}
