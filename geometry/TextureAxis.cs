namespace geometry
{
    public class TextureAxis
    {
        private readonly Vector _axis;

        /// <summary>
        ///     World units per texel.
        /// </summary>
        private readonly double _scale;

        public readonly double Shift;

        public TextureAxis(Vector axis, double shift, double scale)
        {
            _axis = axis;
            Shift = shift;
            _scale = scale;
        }

        public Vector ScaledAxis => _axis / _scale;
    }
}
