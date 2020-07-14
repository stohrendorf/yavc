using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace geometry.components
{
    public class VertexUVAccessor : IEnumerable<Vector2>
    {
        private readonly int _channel;
        private readonly IList<Vertex> _vertices;

        public bool Active = false;

        public VertexUVAccessor(IList<Vertex> vertices, int channel)
        {
            if (channel < 0 || channel >= Vertex.UVChannels)
                throw new ArgumentException();

            _vertices = vertices;
            _channel = channel;
        }

        public Vector2 this[int i]
        {
            get => _vertices[i].UVs[_channel];
            set => _vertices[i].UVs[_channel] = value;
        }

        public IEnumerator<Vector2> GetEnumerator()
        {
            return _vertices.Select(_ => _.UVs[_channel]).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
