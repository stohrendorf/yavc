import bpy

from bpy_extras.io_utils import ImportHelper
from bpy.props import StringProperty
from bpy.types import Operator

from pathlib import Path


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
        lines = Path(self.filepath).read_text().splitlines()

        scene = bpy.context.scene

        for line in lines:
            name, r, g, b, loc_x, loc_y, loc_z, rot_x, rot_y, rot_z = line.split()

            o = bpy.data.objects.new(name, None)
            o.color = [float(r) / 255.0, float(g) / 255.0, float(b) / 255.0, 1.0]
            o.location = [float(loc_x), float(loc_y), float(loc_z)]
            o.rotation_euler = [float(rot_x), float(rot_y), float(rot_z)]
            o.instance_type = "COLLECTION"
            scene.collection.objects.link(o)

        return {'FINISHED'}


def menu_func_import(self, context):
    self.layout.operator(ImportYAVCEntities.bl_idname, text="Import YAVC Entities")


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
