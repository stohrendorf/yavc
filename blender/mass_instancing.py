# ##### BEGIN GPL LICENSE BLOCK #####
#
#  This program is free software; you can redistribute it and/or
#  modify it under the terms of the GNU General Public License
#  as published by the Free Software Foundation; either version 2
#  of the License, or (at your option) any later version.
#
#  This program is distributed in the hope that it will be useful,
#  but WITHOUT ANY WARRANTY; without even the implied warranty of
#  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#  GNU General Public License for more details.
#
#  You should have received a copy of the GNU General Public License
#  along with this program; if not, write to the Free Software Foundation,
#  Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
#
# ##### END GPL LICENSE BLOCK #####

# <pep8 compliant>
import bpy
from bpy.types import Panel, Menu, Operator


class OBJECT_PT_mass_instancing(Panel):
    bl_space_type = 'PROPERTIES'
    bl_region_type = 'WINDOW'
    bl_context = "object"
    bl_label = "Mass Instancing"
    bl_options = {'DEFAULT_CLOSED'}

    def draw(self, context):
        layout = self.layout
        ob = context.object

        if ob.instance_type == 'COLLECTION':
            row = layout.row()
            row.prop(ob, "instance_collection", text="Collection")
            row = layout.row()
            row.operator("mass_instancing.operator", text="Use Instance")
        else:
            row = layout.row(align=True)
            row.label("Only available for Collection instancers")


class MATLIB_OT_mass_instancing(Operator):
    """Apply instancing to all selected objects"""
    bl_label = "New"
    bl_idname = "mass_instancing.operator"

    @classmethod
    def poll(cls, context):
        return context.active_object is not None

    def invoke(self, context, event):
        ob = context.object
        if ob.instance_type != 'COLLECTION':
            self.report({'ERROR'}, "Source is not a collection instancer")
            return {'CANCELLED'}

        collection = ob.instance_collection
        if not collection:
            self.report({'ERROR'}, "Source has no collection to transfer")
            return {'CANCELLED'}

        n = 0
        for target in context.selected_objects:
            if target.type != 'EMPTY':
                continue
            if target.instance_type != 'COLLECTION':
                continue
            target.instance_collection = collection
            n += 1

        self.report({'INFO'}, f"Transferred collection to {n} instance(s)")
        return {'FINISHED'}


def register():
    bpy.utils.register_class(MATLIB_OT_mass_instancing)


def unregister():
    bpy.utils.unregister_class(MATLIB_OT_mass_instancing)


if __name__ == "__main__":  # only for live edit.
    from bpy.utils import register_class

    register()
    register_class(OBJECT_PT_mass_instancing)
