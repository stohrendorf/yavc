using System.Collections.Generic;
using System.Linq;

namespace VMFIO
{
    public class Entity
    {
        private readonly List<KeyValue> _keyValues;

        public readonly List<Entity> Children;
        public readonly string Typename;

        public Entity(string typename, List<KeyValue> keyValues, List<Entity> children)
        {
            Typename = typename;
            _keyValues = keyValues;
            Children = children;
        }

        public string? Classname => GetOptionalValue("classname");

        public void Accept(EntityVisitor visitor)
        {
            foreach (var child in Children) visitor.Visit(child);
        }

        public string? GetOptionalValue(string key)
        {
            var kvs = _keyValues.SingleOrDefault(_ => _.Key == key);
            return kvs?.Value;
        }

        public string GetValue(string key)
        {
            var value = GetOptionalValue(key);
            if (value == null)
                throw new KeyNotFoundException();
            return value;
        }

        public override string ToString()
        {
            return $"{Typename}[{Classname}]";
        }
    }
}
