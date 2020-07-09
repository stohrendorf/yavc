using System.Collections.Generic;
using System.Linq;

namespace VMFIO
{
    public class Entity
    {
        private readonly List<Entity> _children;
        private readonly List<KeyValue> _keyValues;
        public readonly string Typename;

        public Entity(string typename, List<KeyValue> keyValues, List<Entity> children)
        {
            Typename = typename;
            _keyValues = keyValues;
            _children = children;
        }

        public string Classname => GetValue("classname");

        public void Accept(EntityVisitor visitor)
        {
            foreach (var child in _children) visitor.Visit(child);
        }

        public string GetValue(string key)
        {
            var kvs = _keyValues.SingleOrDefault(_ => _.Key == key);
            return kvs.Value;
        }

        public override string ToString()
        {
            return $"{Typename}[{Classname}]";
        }
    }
}
