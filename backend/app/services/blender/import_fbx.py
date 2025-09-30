import bpy
import os
import argparse
import sys

argv = sys.argv
parser = argparse.ArgumentParser()
args, unknown = parser.parse_known_args(argv)


FBX_PATH = bpy.path.abspath(os.path.join(os.path.dirname(__file__), "jy_norelative_uniry.fbx"))

# 초기화
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

# FBX Import
bpy.ops.import_scene.fbx(filepath=FBX_PATH)
print(f"✅ FBX Import 완료: {FBX_PATH}")
