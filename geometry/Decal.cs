namespace geometry
{
    public class Decal
    {
        public readonly int ID;
        public readonly VMT Material;
        public readonly Vector Origin;

        public Decal(int id, Vector origin, VMT material)
        {
            Origin = origin;
            Material = material;
            ID = id;
        }
    }
}
