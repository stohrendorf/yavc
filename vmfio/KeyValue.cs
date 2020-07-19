namespace VMFIO
{
    public sealed class KeyValue
    {
        public readonly string Key;
        public readonly string Value;

        public KeyValue(string key, string value)
        {
            Key = key.ToLower();
            Value = value;
        }

        public override string ToString()
        {
            return $"{Key} = {Value}";
        }
    }
}
