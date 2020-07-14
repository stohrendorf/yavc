using System.Collections.Generic;
using System.Linq;

namespace geometry.components
{
    public class VertexCollection : List<Vertex>
    {
        public readonly VertexCoAccessor Co;
        public readonly VertexUVAccessor UV0;
        public readonly VertexUVAccessor[] UVs;

        public VertexCollection()
        {
            Co = new VertexCoAccessor(this);
            UVs = Enumerable.Range(0, Vertex.UVChannels).Select(i => new VertexUVAccessor(this, i)).ToArray();
            UV0 = UVs[0];
            UV0.Active = true;
        }
    }
}
