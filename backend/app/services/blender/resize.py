import bpy
import os

# =========================
# PARAMETERS
FBX_INPUT = "/home/sylimi2r2/meARy/Backend/app/services/blender/jy_norelative_uniry.fbx"
FBX_OUTPUT = "/home/sylimi2r2/meARy/Backend/app/services/blender/output.fbx"
TARGET_ARMATURE_SCALE = 1.0
ORIGINAL_ARMATURE_SCALE = 0.00283
ARMATURE_NAME = "Armature"
NODE_NAME = "tripo_node_a39e22c6"
# =========================

# 1. FBX Import
bpy.ops.import_scene.fbx(filepath=FBX_INPUT)

# 2. Armature & Node 가져오기
armature = bpy.data.objects.get(ARMATURE_NAME)
node = bpy.data.objects.get(NODE_NAME)

# 3. Armature 1.0으로 변경 + Apply
armature.scale = (TARGET_ARMATURE_SCALE,) * 3
bpy.context.view_layer.objects.active = armature
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# 4. Node/자식 Mesh 상대 스케일 적용
scale_factor = ORIGINAL_ARMATURE_SCALE / TARGET_ARMATURE_SCALE
node.scale = tuple(s * scale_factor for s in node.scale)
bpy.context.view_layer.objects.active = node
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

for child in node.children_recursive:
    child.scale = tuple(s * scale_factor for s in child.scale)
    bpy.context.view_layer.objects.active = child
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# 5. FBX Export
bpy.ops.export_scene.fbx(
    filepath=FBX_OUTPUT,
    use_selection=False,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',
    object_types={'ARMATURE','MESH'},
    bake_space_transform=True
)

print(f"FBX export completed: {FBX_OUTPUT}")
