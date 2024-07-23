namespace VMFIO;

internal sealed class KeyValue
{
    internal readonly string Key;
    internal readonly string Value;

    internal KeyValue(string key, string value)
    {
        Key = key.ToLower();
        Value = value;
    }

    public override string ToString()
    {
        return $"{Key} = {Value}";
    }
}
