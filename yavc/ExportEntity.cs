using System;
using System.Collections.Generic;
using System.Linq;
using yavc.visitors;

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace yavc
{
    internal class ExportEntity
    {
        public ExportEntity(VMFProp entity)
        {
            var color = entity.Color.Split(' ');
            if (color.Length < 3)
                throw new Exception();

            color = color.Take(3).ToArray();

            Color = color.Take(3).Select(double.Parse).ToList();
            Location = new List<double> {entity.Origin.X, entity.Origin.Y, entity.Origin.Z};
            Rotation = new List<double> {entity.Rotation.Z, entity.Rotation.X, entity.Rotation.Y};
            Skin = entity.Skin;
            Model = entity.Model;
        }

        public string Model;
        public IList<double> Location;
        public IList<double> Rotation;
        public IList<double> Color;
        public int Skin;
    }

    internal class ExportInstance
    {
        public ExportInstance(VMFInstance instance)
        {
            File = instance.File;
            Location = new List<double> {instance.Origin.X, instance.Origin.Y, instance.Origin.Z};
            Rotation = new List<double> {instance.Angles.Z, instance.Angles.X, instance.Angles.Y};
        }

        public string File;
        public IList<double> Location;
        public IList<double> Rotation;
    }

    internal class ExportEnvCubemap
    {
        public ExportEnvCubemap(VMFEnvCubemap cubemap)
        {
            Location = new List<double> {cubemap.Origin.X, cubemap.Origin.Y, cubemap.Origin.Z};
            Sides = new List<int>(cubemap.Sides);
        }

        public IList<double> Location;
        public IList<int> Sides;
    }

    internal class ExportData
    {
        public readonly IList<ExportEntity> Entities = new List<ExportEntity>();
        public readonly IList<ExportInstance> Instances = new List<ExportInstance>();
        public readonly IList<ExportEnvCubemap> EnvCubemaps = new List<ExportEnvCubemap>();
    }
}
