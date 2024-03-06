using geometry.components;

namespace geometry.entities;

public sealed class AmbientGeneric
{
    public readonly string Message;
    public readonly Vector Origin;
    public readonly double Pitch;
    public readonly double Radius;

    public AmbientGeneric(string message, double pitch, double radius, Vector origin)
    {
        Message = message;
        Pitch = pitch;
        Radius = radius;
        Origin = origin;
    }
}