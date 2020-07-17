using System.Collections.Generic;

namespace geometry.components
{
    public class VertexCollection : List<Vertex>
    {
        public readonly VertexAlphaAccessor Alpha;
        public readonly VertexCoAccessor Co;
        public readonly VertexUVAccessor UV;

        public VertexCollection()
        {
            Co = new VertexCoAccessor(this);
            Alpha = new VertexAlphaAccessor(this);
            UV = new VertexUVAccessor(this);
        }
    }
}
