using System.Collections;
using System.Collections.Generic;

namespace geometry.components
{
    public class VertexCollection : IEnumerable<Vertex>
    {
        private readonly List<Vertex> _vertices = new List<Vertex>();

        public readonly VertexAlphaAccessor Alpha;
        public readonly VertexCoAccessor Co;
        public readonly VertexUVAccessor UV;

        public VertexCollection(IEnumerable<Vertex> vertices) : this()
        {
            AddRange(vertices);
        }

        public VertexCollection()
        {
            Co = new VertexCoAccessor(_vertices);
            Alpha = new VertexAlphaAccessor(_vertices);
            UV = new VertexUVAccessor(_vertices);
        }

        public int Count => _vertices.Count;

        public Vertex this[int idx] => _vertices[idx];

        public IEnumerator<Vertex> GetEnumerator()
        {
            return _vertices.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddRange(IEnumerable<Vertex> vertices)
        {
            _vertices.AddRange(vertices);
        }

        public void Add(Vertex vertex)
        {
            _vertices.Add(vertex);
        }
    }
}
