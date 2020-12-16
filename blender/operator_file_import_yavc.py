import bpy

from bpy_extras.io_utils import ImportHelper
from bpy.props import StringProperty
from bpy.types import Operator

from pathlib import Path
import json


class ImportYAVCEntities(Operator, ImportHelper):
    """This appears in the tooltip of the operator and in the generated docs"""
    bl_idname = "import_yavc.entities"
    bl_label = "Import YAVC Entities as Empties"

    # ImportHelper mixin class uses this
    filename_ext = ".yavc"

    filter_glob: StringProperty(
        default="*.yavc",
        options={'HIDDEN'},
        maxlen=255,  # Max internal buffer length, longer would be clamped.
    )

    def execute(self, context):
        data = json.loads(Path(self.filepath).read_text())

        scene = bpy.context.scene

        for entity in data["Entities"]:
            obj_name: str = entity["Model"]
            if obj_name.startswith("models/"):
                obj_name = obj_name.lower()[len("models/"):]
            obj_name = f"{obj_name.replace(' ', '_')}:{entity['Skin']}"

            o = bpy.data.objects.new(obj_name, None)
            o.color = [x / 255.0 for x in entity["Color"]] + [1.0]
            o.location = list(entity["Location"])
            o.rotation_euler = list(entity["Rotation"])
            o.instance_type = "COLLECTION"

            for src, dst in (
                    ("Model", "model"),
                    ("Skin", "skin"),
            ):
                o[f"yavc:{dst}"] = entity[src]

            scene.collection.objects.link(o)

        for instance in data["Instances"]:
            obj_name: str = instance["File"]
            if obj_name.lower().startswith("instances/"):
                obj_name = obj_name[len("instances/"):]
            obj_name = obj_name.replace(' ', '_')

            o = bpy.data.objects.new(obj_name, None)
            o.location = list(instance["Location"])
            o.rotation_euler = list(instance["Rotation"])
            o.instance_type = "COLLECTION"

            for src, dst in (
                    ("File", "filename"),
            ):
                o[f"yavc:{dst}"] = instance[src]

            scene.collection.objects.link(o)

        for i, cubemap in enumerate(data["EnvCubemaps"]):
            lp_name = f"env_cubemap:{i}"
            lp = bpy.data.lightprobes.new(lp_name, "CUBE")
            # TODO just some random values here
            lp.clip_end = 20000
            lp.influence_distance = 200
            lp.influence_type = "BOX"
            o = bpy.data.objects.new(lp_name, lp)
            o.location = list(cubemap["Location"])

            for src, dst in (
                    ("Sides", "sides"),
            ):
                o[f"yavc:{dst}"] = cubemap[src]

            scene.collection.objects.link(o)

        return {'FINISHED'}


def menu_func_import(self, context):
    self.layout.operator(ImportYAVCEntities.bl_idname, text="YAVC Entities")


def register():
    bpy.utils.register_class(ImportYAVCEntities)
    bpy.types.TOPBAR_MT_file_import.append(menu_func_import)


def unregister():
    bpy.utils.unregister_class(ImportYAVCEntities)
    bpy.types.TOPBAR_MT_file_import.remove(menu_func_import)


if __name__ == "__main__":
    register()

    # test call
    bpy.ops.import_yavc.entities('INVOKE_DEFAULT')
