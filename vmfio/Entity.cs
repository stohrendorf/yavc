using System.Collections.Generic;
using System.Linq;

namespace VMFIO;

public sealed class Entity
{
  private readonly List<KeyValue> _keyValues;

  public readonly List<Entity> Children;
  public readonly string Typename;

  internal Entity(string typename, List<KeyValue> keyValues, List<Entity> children)
  {
    Typename = typename;
    _keyValues = keyValues;
    Children = children;
  }

  public string? Classname => GetOptionalValue("classname");

  public string this[string key] => GetValue(key);

  public void Accept(EntityVisitor visitor)
  {
    foreach (var child in Children)
    {
      visitor.Visit(child);
    }
  }

  public string? GetOptionalValue(string key)
  {
    key = key.ToLower();
    var kvs = _keyValues.SingleOrDefault(entry => entry.Key == key);
    return kvs?.Value;
  }

  public string GetValue(string key)
  {
    var value = GetOptionalValue(key);
    if (value == null)
    {
      throw new KeyNotFoundException($"Key {key} not found");
    }

    return value;
  }

  public override string ToString()
  {
    return $"{Typename}[{Classname}]";
  }
}
