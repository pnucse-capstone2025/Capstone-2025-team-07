import bpy
import  argparse
import sys

argv = sys.argv
parser = argparse.ArgumentParser()
args, unknown = parser.parse_known_args(argv)

# MediaPipe 연결 순서 (예: 어깨 → 팔꿈치 → 손목 등)
connections = [
    (11, 13), (13, 15),        # Left arm
    (12, 14), (14, 16),        # Right arm
    (11, 12),                  # Shoulders
    (23, 25), (25, 27),        # Left leg
    (24, 26), (26, 28),        # Right leg
    (23, 24),                  # Hips
    (11, 23), (12, 24),        # Torso sides
    (0, 11), (0, 12),          # Neck base
    (0, 1), (1, 2), (2, 3), (3, 7),  # Head center line
]

# 점들을 기반으로 선형 메시 생성
verts = []
edges = []
idx_map = {}

for i in range(33):
    name = f"MP_{i}"
    if name in bpy.data.objects:
        loc = bpy.data.objects[name].location
        idx_map[i] = len(verts)
        verts.append(loc)

for a, b in connections:
    if a in idx_map and b in idx_map:
        edges.append((idx_map[a], idx_map[b]))

# Mesh 생성
mesh = bpy.data.meshes.new("MP_Skeleton")
mesh.from_pydata(verts, edges, [])
obj = bpy.data.objects.new("MP_Skeleton", mesh)
bpy.context.collection.objects.link(obj)

print("✅ 연결선 생성 완료")
