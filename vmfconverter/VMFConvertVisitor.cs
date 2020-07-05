using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using geometry;
using utility;
using VMFIO;

namespace VMFConverter
{
    public class VMFConvertVisitor : EntityVisitor
    {
        private readonly string _vtfBasePath;
        public readonly VMF Vmf = new VMF();
        private Displacement? _displacement;

        private List<Face>? _faces;

        public VMFConvertVisitor(string vtfBasePath)
        {
            _vtfBasePath = vtfBasePath;
        }

        private void ReadSolid(Entity entity)
        {
            _faces = new List<Face>();
            entity.Accept(this);
            var solid = new Solid(entity.GetValue("id"), _faces);
            _faces = null;
            Vmf.Solids.Add(solid);
        }

        private void ReadSide(Entity entity)
        {
            var plane = ParserUtil.ParsePlaneString(entity.GetValue("plane"));
            var (uAxis, uShift) = ParserUtil.ParseTextureAxis(entity.GetValue("uaxis"));
            var (vAxis, vShift) = ParserUtil.ParseTextureAxis(entity.GetValue("vaxis"));

            _displacement = null;
            entity.Accept(this);

            var material = entity.GetValue("material");
            var (texWidth, texHeight) = VMT.GetSize(_vtfBasePath, Path.Join(_vtfBasePath, material) + ".vmt");

            var face = new Face(plane,
                material,
                uAxis, uShift,
                vAxis, vShift,
                _displacement,
                texWidth,
                texHeight
            );

            _displacement = null;

            Debug.Assert(_faces != null);
            _faces!.Add(face);
        }

        private void ReadDisplacementInfo(Entity entity)
        {
            _displacement = new Displacement {Power = int.Parse(entity.GetValue("power"))};
            var cols = entity.GetValue("startposition").Replace("[", "").Replace("]", "").Split(" ");
            if (cols.Length != 3)
                throw new Exception();
            _displacement.StartPosition = new Vector(
                ParserUtil.ParseDouble(cols[0]),
                ParserUtil.ParseDouble(cols[1]),
                ParserUtil.ParseDouble(cols[2])
            );
            _displacement.Elevation = ParserUtil.ParseDouble(entity.GetValue("elevation"));
            var n = (1 << _displacement.Power) + 1;
            _displacement.Normals.AddRange(Enumerable.Range(0, n).Select(_ => new List<Vector>()));
            _displacement.OffsetNormals.AddRange(Enumerable.Range(0, n).Select(_ => new List<Vector>()));
            _displacement.Offsets.AddRange(Enumerable.Range(0, n).Select(_ => new List<Vector>()));
            _displacement.Distances.AddRange(Enumerable.Range(0, n).Select(_ => new List<double>()));
            _displacement.Alphas.AddRange(Enumerable.Range(0, n).Select(_ => new List<double>()));
            entity.Accept(this);
        }

        private void ReadVectorRow(IList<List<Vector>> dest, Entity entity)
        {
            var n = (1 << _displacement!.Power) + 1;
            for (var i = 0; i < n; ++i)
            {
                var row = entity.GetValue("row" + i).Split(" ");
                if (row.Length == 0)
                {
                    dest[i].AddRange(Enumerable.Repeat(Vector.Zero, n));
                    continue;
                }

                if (row.Length != n * 3)
                    throw new Exception($"{n * 3} != {row.Length}");

                for (var j = 0; j < n; ++j)
                    dest[i].Add(new Vector(
                        ParserUtil.ParseDouble(row[j * 3 + 0]),
                        ParserUtil.ParseDouble(row[j * 3 + 1]),
                        ParserUtil.ParseDouble(row[j * 3 + 2])
                    ));
            }
        }

        private void ReadScalarRow(IList<List<double>> dest, Entity entity)
        {
            var n = (1 << _displacement!.Power) + 1;
            for (var i = 0; i < n; ++i)
            {
                var row = entity.GetValue("row" + i).Split(" ");
                if (row.Length == 0)
                {
                    dest[i].AddRange(Enumerable.Repeat(0.0, n));
                    continue;
                }

                if (row.Length != n)
                    throw new Exception($"{n} != {row.Length}");

                for (var j = 0; j < n; ++j)
                    dest[i].AddRange(row.Select(ParserUtil.ParseDouble));
            }
        }

        public override void Visit(Entity entity)
        {
            switch (entity.Typename)
            {
                case "solid":
                    ReadSolid(entity);
                    break;
                case "side":
                    ReadSide(entity);
                    break;
                case "dispinfo":
                    ReadDisplacementInfo(entity);
                    break;
                case "normals":
                    Debug.Assert(_displacement != null);
                    ReadVectorRow(_displacement!.Normals, entity);
                    break;
                case "distances":
                    Debug.Assert(_displacement != null);
                    ReadScalarRow(_displacement!.Distances, entity);
                    break;
                case "alphas":
                    Debug.Assert(_displacement != null);
                    ReadScalarRow(_displacement!.Alphas, entity);
                    break;
                case "offsets":
                    Debug.Assert(_displacement != null);
                    ReadVectorRow(_displacement!.Offsets, entity);
                    break;
                case "offset_normals":
                    Debug.Assert(_displacement != null);
                    ReadVectorRow(_displacement!.OffsetNormals, entity);
                    break;
                default:
                    entity.Accept(this);
                    break;
            }
        }
    }
}
