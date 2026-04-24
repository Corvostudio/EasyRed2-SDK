bl_info = {
    "name": "ER2 Blender RIG Exporter",
    "author": "Corvostudio",
    "version": (1, 0),
    "blender": (2, 80, 0),
    "location": "File > Export",
    "description": "ER2 RIG export setting for Uniform/Body skinning made in Blender",
    "category": "Import-Export",
}


import bpy
from bpy_extras.io_utils import ExportHelper


#------export function------
def ExportMesh(skeleton_name, isTPS):
    #------make sure object selection is correct -------
    #0) Must be in object mode
    if bpy.context.mode != 'OBJECT':
        raise ValueError('YOU ARE NOT IN OBJECT MODE!')
    #1) make sure i selected at least one mesh
    mesh_obj = next((obj for obj in bpy.context.selected_objects if obj.type == 'MESH'), None)
    if mesh_obj is None:
        raise ValueError('NO MESHES TO EXPORT SELECTED!')
    #2) get armature modifier from the mesh and put it in the selected objects (if not already)
    armature_modifier = next((mod for mod in mesh_obj.modifiers if mod.type == 'ARMATURE'), None)
    if armature_modifier is None or armature_modifier.object is None:
        raise ValueError('MESH IS NOT SKINNED TO AN ARMATURE (Check uniform mesh modifier section)!')
    #3) make sure armature is in selected set
    if armature_modifier.object not in bpy.context.selected_objects:
        armature_modifier.object.select_set(True)

    # Call the operator with 'INVOKE_DEFAULT' to show the file selector
    bpy.ops.export_scene.fbx_custom('INVOKE_DEFAULT', skeleton_name=skeleton_name, isTPS=isTPS, originalSelectedMeshName = mesh_obj.name)



#---------export --------------
# Define a custom operator that inherits from ExportHelper
class ExportFBX(bpy.types.Operator, ExportHelper):
    # Set the bl_idname and bl_label attributes
    bl_idname = "export_scene.fbx_custom"
    bl_label = "Export FBX"

    # Set the filename_ext and filter_glob attributes
    filename_ext = ".fbx"
    filter_glob: bpy.props.StringProperty(
        default="*.fbx",
        options={'HIDDEN'},
        maxlen=255,
     )
    skeleton_name: bpy.props.StringProperty(name="Mesh to export name", options={'HIDDEN'})
    isTPS: bpy.props.BoolProperty(name="Is TPS?", options={'HIDDEN'})
    originalSelectedMeshName: bpy.props.StringProperty(name="Original selected mesh name", options={'HIDDEN'})

    # Define the execute method
    def execute(self, context):
        # Get the filepath from the file selector
        filepath = self.filepath

        # Get all armatures and selected meshes
        bpy.context.scene.render.fps = 1
        if (self.isTPS):#le uniformi TPS usano un dummy skeleton ruotato correttamente
            selected_objects = FixBones(self.skeleton_name);
        else:
            selected_objects = bpy.context.selected_objects
        armatures = [obj for obj in bpy.data.objects if obj.type == 'ARMATURE']
        selected_meshes = [obj for obj in selected_objects if obj.type == 'MESH']

        # Call the export operator with the desired settings
        for obj in armatures + selected_meshes:
            bpy.ops.export_scene.fbx(
                filepath=filepath,
                use_selection=True,
                object_types={'ARMATURE', 'MESH'},
                use_armature_deform_only=True,
                add_leaf_bones=False
            )
        
        #delete dummy created for TPS export
        if (self.isTPS):
            DeleteDummy(selected_objects)
        
        #back in object mode
        bpy.context.view_layer.objects.active = bpy.data.objects[self.originalSelectedMeshName]# bpy.data.objects['Armature']
        bpy.ops.object.mode_set(mode='OBJECT')
        return {'FINISHED'}


#------generates a copy of the skeleton with well oriented bones------
def FixBones(skeleton_name):   
    #------duplicate selected stuff-------  
    # Duplicate the selected objects
    bpy.ops.object.duplicate()
    #get the duplicated objects
    selected_objects = bpy.context.selected_objects
    #get the duplicated armature in particular
    armature1 = next((obj for obj in selected_objects if obj.type == 'ARMATURE'), None)
    if armature1 is None:
        DeleteDummy(selected_objects)
        raise ValueError('ARMATURE WAS NOT DUPLICATE CORRETLY, PLEASE START AGAIN THE PROCESS! (01)')


    #------Remove unusued bones and move bones to right position / rotation-------
    # Set the active object to your first armature
    bpy.context.view_layer.objects.active = armature1

    # Switch to edit mode
    bpy.ops.object.mode_set(mode='EDIT')

    # Get the second armature
    armature2 = bpy.data.objects[skeleton_name]
    if armature2 is None:
        DeleteDummy(selected_objects)
        raise ValueError('ARMATURE WAS NOT DUPLICATE CORRETLY, PLEASE START AGAIN THE PROCESS! (02)')

    # Store a reference to the current active object
    active_object = bpy.context.view_layer.objects.active

    # Set the second armature as the active object and switch to edit mode
    bpy.context.view_layer.objects.active = armature2
    bpy.ops.object.mode_set(mode='EDIT')

    # Iterate over each bone in the first armature
    for bone1 in armature1.data.edit_bones:
        # Check if this bone exists in the second armature
        if bone1.name in armature2.data.edit_bones:
            # Get the corresponding bone in the second armature
            bone2 = armature2.data.edit_bones[bone1.name]
            
            # Match the position and rotation of the bone in the first armature to that of the second armature
            bone1.head = bone2.head
            bone1.tail = bone2.tail
            bone1.roll = bone2.roll
        else:
            # Remove the bone from the first armature if it does not exist in the second one
            bpy.ops.armature.select_all(action='DESELECT')
            bone1.select = True
            bpy.ops.armature.delete()

    # Switch back to object mode for both armatures
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.view_layer.objects.active = active_object
    bpy.ops.object.mode_set(mode='OBJECT')
    #bpy.data.objects.remove(armature2, do_unlink=True)
    
    return selected_objects
    
def DeleteDummy(selected_objects):
    #-----end - delete duplicated objects------
    for obj in selected_objects:
        obj.select_set(True)
    bpy.ops.object.delete()



class CustomExportOperator_Vest(bpy.types.Operator):
    bl_idname = "export.er2_vest"
    bl_label = "ER2 Vest Export TPS"
    def execute(self, context):
        ExportMesh("VestArmature",True);
        return {'FINISHED'}

class CustomExportOperator_Uniform(bpy.types.Operator):
    bl_idname = "export.er2_uniform"
    bl_label = "ER2 Uniform Export TPS"
    def execute(self, context):
        ExportMesh("UniformArmature",True);
        return {'FINISHED'}

class CustomExportOperator_Head(bpy.types.Operator):
    bl_idname = "export.er2_head"
    bl_label = "ER2 Head Export TPS"
    def execute(self, context):
        ExportMesh("HeadArmature",True);
        return {'FINISHED'}

class CustomExportOperator_Hands(bpy.types.Operator):
    bl_idname = "export.er2_hands"
    bl_label = "ER2 Hands Export TPS"
    def execute(self, context):
        ExportMesh("HandsArmature",True);
        return {'FINISHED'}

class CustomExportOperator_TPS_Anim(bpy.types.Operator):
    bl_idname = "export.er2_tps_anim"
    bl_label = "ER2 TPS Animation Export TPS"
    def execute(self, context):
        # Call your Python script here
        print("Button clicked!")
        return {'FINISHED'}

class CustomExportOperator_Uniform_FPS(bpy.types.Operator):
    bl_idname = "export.er2_uniform_fps"
    bl_label = "ER2 Uniform Export FPS"
    def execute(self, context):
        ExportMesh("Uniform FPS",False);
        return {'FINISHED'}

def menu_func_export(self, context):
    self.layout.operator(CustomExportOperator_Hands.bl_idname, text="ER2 - Export TPS Hands")
    self.layout.operator(CustomExportOperator_Head.bl_idname, text="ER2 - Export TPS Head")
    self.layout.operator(CustomExportOperator_Uniform.bl_idname, text="ER2 - Export TPS Uniform")
    self.layout.operator(CustomExportOperator_Vest.bl_idname, text="ER2 - Export TPS Vest")
    self.layout.operator(CustomExportOperator_Uniform_FPS.bl_idname, text="ER2 - Export FPS Uniform")
    #self.layout.operator(CustomExportOperator_TPS_Anim.bl_idname, text="ER2 - Export TPS Anim")

def register():
    bpy.utils.register_class(ExportFBX)
    bpy.utils.register_class(CustomExportOperator_Hands)
    bpy.utils.register_class(CustomExportOperator_Head)
    bpy.utils.register_class(CustomExportOperator_Uniform)
    bpy.utils.register_class(CustomExportOperator_Vest)
    bpy.utils.register_class(CustomExportOperator_Uniform_FPS)
    #bpy.utils.register_class(CustomExportOperator_TPS_Anim)
    bpy.types.TOPBAR_MT_file_export.append(menu_func_export)

def unregister():
    bpy.utils.unregister_class(ExportFBX)
    bpy.utils.unregister_class(CustomExportOperator_Hands)
    bpy.utils.unregister_class(CustomExportOperator_Head)
    bpy.utils.unregister_class(CustomExportOperator_Uniform)
    bpy.utils.unregister_class(CustomExportOperator_Vest)
    bpy.utils.unregister_class(CustomExportOperator_Uniform_FPS)
    #bpy.utils.unregister_class(CustomExportOperator_TPS_Anim)
    bpy.types.TOPBAR_MT_file_export.remove(menu_func_export)

if __name__ == "__main__":
    register()

