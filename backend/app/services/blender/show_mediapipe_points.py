import bpy
import json
from mathutils import Vector
import argparse
import sys

def main():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:]
    parser = argparse.ArgumentParser()
    parser.add_argument("--json_path", required=True)
    args, unknown = parser.parse_known_args(argv)
    # === 1. JSON 파일 로드 ===
    json_path = bpy.path.abspath(args.json_path)
    with open(json_path) as f:
        landmarks = json.load(f)

    # === 2. 기존 포인트 제거 (Optional) ===
    for obj in bpy.data.objects:
        if obj.name.startswith("MP_"):
            bpy.data.objects.remove(obj, do_unlink=True)

    # === 3. 관절 좌표를 Empty로 생성 ===
    for idx, lm in landmarks.items():
        x, y, z = lm['x'], lm['y'], lm['z']
        
        # 좌표계 변환 (MediaPipe → Blender)
        pos = Vector((x, z, -y)) * 2.0

        empty = bpy.data.objects.new(f"MP_{idx}", None)
        empty.location = pos
        empty.empty_display_size = 0.03
        empty.empty_display_type = 'SPHERE'

        bpy.context.collection.objects.link(empty)

    print("✅ 관절 시각화 완료")

if __name__=="__main__":
    main()