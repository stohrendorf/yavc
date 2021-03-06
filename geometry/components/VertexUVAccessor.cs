using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace geometry.components
{
    public class VertexUVAccessor : IEnumerable<Vector2>
    {
        private readonly IList<Vertex> _vertices;

        public VertexUVAccessor(IList<Vertex> vertices)
        {
            _vertices = vertices;
        }

        public Vector2 this[int i]
        {
            get => _vertices[i].UV;
            set => _vertices[i].UV = value;
        }

        public IEnumerator<Vector2> GetEnumerator()
        {
            return _vertices.Select(_ => _.UV).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
