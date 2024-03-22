using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using geometry;
using geometry.components;
using geometry.entities;
using geometry.materials;
using geometry.utils;
using utility;
using VMFIO;

namespace yavc.visitors;

internal sealed class SolidVisitor : EntityVisitor
{
    private readonly string _vtfBasePath;
    public readonly Vmf Vmf = new();
    private Displacement? _displacement;
    private List<Side>? _sides;

    public SolidVisitor(string vtfBasePath)
    {
        _vtfBasePath = vtfBasePath;
    }

    private void ReadSolid(Entity entity, bool skipTools)
    {
        _sides = new List<Side>();
        entity.Accept(this, skipTools);
        var solid = new Solid(int.Parse(entity["id"]), _sides);
        _sides = null;
        Vmf.Solids.Add(solid);
    }

    private void ReadSide(Entity entity, bool skipTools)
    {
        var plane = entity["plane"].ParseToPlane();
        var uAxis = entity["uaxis"].ParseToTextureAxis();
        var vAxis = entity["vaxis"].ParseToTextureAxis();

        _displacement = null;
        entity.Accept(this, skipTools);

        var material = entity["material"];
        var vmt = skipTools && material.ToLower().StartsWith("tools/")
            ? null
            : VMT.TryGetCached(_vtfBasePath, material + ".vmt");

        var side = new Side(entity["id"].ParseToInt(), plane, vmt, uAxis, vAxis, _displacement);

        _displacement = null;

        Debug.Assert(_sides is not null);
        _sides!.Add(side);
    }

    private void ReadDisplacementInfo(Entity entity, bool skipTools)
    {
        _displacement = new Displacement { Power = int.Parse(entity["power"]) };
        var cols = entity["startposition"].Replace("[", "").Replace("]", "").Split(" ");
        if (cols.Length != 3) throw new Exception();

        _displacement.StartPosition = new Vector(
            StringUtil.ParseDouble(cols[0]),
            StringUtil.ParseDouble(cols[1]),
            StringUtil.ParseDouble(cols[2])
        );
        _displacement.Elevation = StringUtil.ParseDouble(entity["elevation"]);
        var n = (1 << _displacement.Power) + 1;
        _displacement.Normals.AddRange(Enumerable.Range(0, n).Select(static _ => new List<Vector>()));
        _displacement.OffsetNormals.AddRange(Enumerable.Range(0, n).Select(static _ => new List<Vector>()));
        _displacement.Offsets.AddRange(Enumerable.Range(0, n).Select(static _ => new List<Vector>()));
        _displacement.Distances.AddRange(Enumerable.Range(0, n).Select(static _ => new List<double>()));
        _displacement.Alphas.AddRange(Enumerable.Range(0, n).Select(static _ => new List<double>()));
        entity.Accept(this, skipTools);
    }

    private void ReadVectorRow(IList<List<Vector>> dest, Entity entity)
    {
        var n = _displacement!.Dimension + 1;
        for (var i = 0; i < n; ++i)
        {
            var row = entity["row" + i].Split(" ");
            if (row.Length == 0)
            {
                dest[i].AddRange(Enumerable.Repeat(Vector.Zero, n));
                continue;
            }

            if (row.Length != n * 3) throw new Exception($"{n * 3} != {row.Length}");

            for (var j = 0; j < n; ++j)
                dest[i].Add(new Vector(
                    StringUtil.ParseDouble(row[j * 3 + 0]),
                    StringUtil.ParseDouble(row[j * 3 + 1]),
                    StringUtil.ParseDouble(row[j * 3 + 2])
                ));
        }
    }

    private void ReadScalarRow(IList<List<double>> dest, Entity entity)
    {
        var n = (1 << _displacement!.Power) + 1;
        for (var i = 0; i < n; ++i)
        {
            var row = entity["row" + i].Split(" ");
            if (row.Length == 0)
            {
                dest[i].AddRange(Enumerable.Repeat(0.0, n));
                continue;
            }

            if (row.Length != n) throw new Exception($"{n} != {row.Length}");

            for (var j = 0; j < n; ++j) dest[i].AddRange(row.Select(StringUtil.ParseDouble));
        }
    }

    public override void Visit(Entity entity, bool skipTools)
    {
        switch (entity.Typename)
        {
            case "solid":
                ReadSolid(entity, skipTools);
                break;
            case "side":
                ReadSide(entity, skipTools);
                break;
            case "dispinfo":
                ReadDisplacementInfo(entity, skipTools);
                break;
            case "normals":
                Debug.Assert(_displacement is not null);
                ReadVectorRow(_displacement.Normals, entity);
                break;
            case "distances":
                Debug.Assert(_displacement is not null);
                ReadScalarRow(_displacement.Distances, entity);
                break;
            case "alphas":
                Debug.Assert(_displacement is not null);
                ReadScalarRow(_displacement.Alphas, entity);
                break;
            case "offsets":
                Debug.Assert(_displacement is not null);
                ReadVectorRow(_displacement.Offsets, entity);
                break;
            case "offset_normals":
                Debug.Assert(_displacement is not null);
                ReadVectorRow(_displacement.OffsetNormals, entity);
                break;
            default:
                entity.Accept(this, skipTools);
                break;
        }
    }
}
