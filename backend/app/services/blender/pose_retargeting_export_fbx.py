import bpy
import json
from mathutils import Vector, Matrix
import math
import os
import sys
import argparse

# =========================
# PARAMETERS
# =========================
ARMATURE_NAME = "Armature"           # Mixamo Armature 오브젝트 이름
JSON_PATH = "/home/sylimi2r2/meARy/Backend/poses"
SCALE = 2.0                         # MediaPipe -> Blender 스케일 (MP Empty 생성 시와 동일)

ALIGN_STARTS_ONLY = False             # True: 시작점만 일치 / False: 방향+길이도 맞춤
ALLOW_LENGTH_SCALE = True            # False면 길이는 그대로 두고 방향만 (ALIGN_STARTS_ONLY=False일 때 적용)
BAKE = True                          # 베이크해서 제약/헬퍼 정리
BAKE_FRAME = 1

# =========================
# UTILITIES
# =========================
def compute_auto_scale(arm_obj):
    def mp_center(a, b):
        obj_a, obj_b = bpy.data.objects.get(f"MP_{a}"), bpy.data.objects.get(f"MP_{b}")
        if not obj_a or not obj_b: raise ValueError(f"MP Empty({a} or {b}) not found.")
        return 0.5 * (obj_a.matrix_world.translation + obj_b.matrix_world.translation)
    try:
        hips, head, foot = mp_center(23, 24), bpy.data.objects.get("MP_0").matrix_world.translation, mp_center(27, 28)
        mp_height = (head - hips).length + (hips - foot).length
        
        # 'Apply Transform'을 먼저 하므로, 이제 matrix_world를 걱정할 필요 없이 계산할 수 있습니다.
        bpy.context.view_layer.update() # 업데이트 보장
        arm_hips = arm_obj.matrix_world @ arm_obj.data.bones["mixamorig:Hips"].head_local
        arm_head = arm_obj.matrix_world @ arm_obj.data.bones["mixamorig:Head"].head_local
        arm_foot = 0.5 * (arm_obj.matrix_world @ arm_obj.data.bones["mixamorig:LeftFoot"].tail_local + arm_obj.matrix_world @ arm_obj.data.bones["mixamorig:RightFoot"].tail_local)
        
        arm_height = (arm_head - arm_hips).length + (arm_hips - arm_foot).length
        return mp_height / arm_height if arm_height > 1e-6 else 1.0
    except Exception as e:
        print(f"⚠️ Scale calculation warning: {e}. Using default scale 1.0.")
        return 1.0

def load_or_create_mp_empties():
    if any(o.name.startswith("MP_") for o in bpy.data.objects): return
    with open(JSON_PATH) as f: data = json.load(f)
    for idx, lm in data.items():
        pos = Vector((lm['x'], lm['z'], -lm['y'])) * SCALE
        empty = bpy.data.objects.new(f"MP_{idx}", None)
        empty.location, empty.empty_display_size, empty.empty_display_type = pos, 0.03, 'SPHERE'
        bpy.context.collection.objects.link(empty)

def get_mp_loc(idx):
    obj = bpy.data.objects.get(f"MP_{idx}")
    if not obj: raise ValueError(f"MP_{idx} not found.")
    return obj.matrix_world.translation.copy()

def center_of(a, b): return 0.5 * (get_mp_loc(a) + get_mp_loc(b))
def lerp(p0, p1, t): return p0 * (1.0 - t) + p1 * t

def make_helper(name, loc):
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, None)
        obj.empty_display_size, obj.empty_display_type = 0.035, 'ARROWS'
        bpy.context.collection.objects.link(obj)
    obj.location = loc
    return obj

def clear_old_helpers_and_constraints(arm_obj):
    objs_to_remove = [o for o in bpy.data.objects if o.name.startswith("MP_HEAD_") or o.name.startswith("MP_TAIL_")]
    for o in objs_to_remove: bpy.data.objects.remove(o, do_unlink=True)
    for pb in arm_obj.pose.bones:
        constraints_to_remove = [c for c in pb.constraints if c.name.startswith("MP_COPYLOC") or c.name.startswith("MP_DTRACK")]
        for c in constraints_to_remove: pb.constraints.remove(c)

# def build_targets():
#     # 기존과 동일한 부분 (어깨까지)
#     hips_center = center_of(23, 24)
#     shoulder_center = center_of(11, 12)
#     ear_center = center_of(7, 8)
#     inner_eye_center = center_of(3, 4)
#     nose = get_mp_loc(0)
#     spine1_end = lerp(hips_center, shoulder_center, 0.75)
#     spine2_end = lerp(hips_center, shoulder_center, 0.85)
    
#     # [수정] left_shoulder_end, right_shoulder_end를 직접 계산하지 않고,
#     # MediaPipe의 어깨 위치를 바로 사용하거나 약간의 보간을 통해 계산합니다.
#     # 이는 팔의 시작점을 더 명확하게 하기 위함입니다.
#     left_shoulder_end = get_mp_loc(11)
#     right_shoulder_end = get_mp_loc(12)

#     head_helper = lerp(ear_center,inner_eye_center,0.1)
#     head_end = lerp(hips_center, head_helper, 1.2)
#     neck_base = shoulder_center
#     neck_top = lerp(shoulder_center, nose, 0.75)
#     spine_pts = [lerp(hips_center, spine2_end, t/0.85) for t in [0.20, 0.45]]
#     shoulder_start_point = lerp(spine1_end, neck_base, 1)
#     left_shoulder_dir = (get_mp_loc(11) - shoulder_center).normalized()
#     right_shoulder_dir = (get_mp_loc(12) - shoulder_center).normalized()
#     shoulder_offset = 0.15
#     left_shoulder_start = shoulder_start_point + left_shoulder_dir * shoulder_offset
#     right_shoulder_start = shoulder_start_point + right_shoulder_dir * shoulder_offset

#     # ================================================================
#     # ▼▼▼ 팔, 손 오프셋을 위한 추가/수정된 부분 ▼▼▼
#     # ================================================================

#     # --- 1. 팔/손 오프셋 값 정의 (이 값들을 조절해 보세요) ---
#     arm_offset = 0.15      # 팔꿈치를 밖으로 밀어낼 거리
#     forearm_offset = 0.15  # 손목을 밖으로 밀어낼 거리

#     # --- 2. 주요 관절 위치 미리 계산 ---
#     left_elbow_orig = get_mp_loc(13)
#     right_elbow_orig = get_mp_loc(14)
#     left_wrist_orig = get_mp_loc(15)
#     right_wrist_orig = get_mp_loc(16)
#     left_hand_end = get_mp_loc(19)  # 손끝 위치
#     right_hand_end = get_mp_loc(20) # 손끝 위치

#     # --- 3. 왼쪽 팔의 새로운 목표점 계산 ---
#     # 어깨 -> 팔꿈치 방향으로 팔꿈치 밀어내기
#     left_arm_dir = (left_elbow_orig - left_shoulder_end).normalized()
#     left_elbow_new = left_elbow_orig + left_arm_dir * arm_offset
    
#     # 팔꿈치 -> 손목 방향으로 손목 밀어내기
#     left_forearm_dir = (left_wrist_orig - left_elbow_new).normalized() # 새로 계산된 팔꿈치 기준
#     left_wrist_new = left_wrist_orig + left_forearm_dir * forearm_offset

#     # --- 4. 오른쪽 팔의 새로운 목표점 계산 ---
#     # 어깨 -> 팔꿈치 방향으로 팔꿈치 밀어내기
#     right_arm_dir = (right_elbow_orig - right_shoulder_end).normalized()
#     right_elbow_new = right_elbow_orig + right_arm_dir * arm_offset
    
#     # 팔꿈치 -> 손목 방향으로 손목 밀어내기
#     right_forearm_dir = (right_wrist_orig - right_elbow_new).normalized() # 새로 계산된 팔꿈치 기준
#     right_wrist_new = right_wrist_orig + right_forearm_dir * forearm_offset

#     # ================================================================
#     # ▲▲▲ 여기까지 추가/수정된 부분 ▲▲▲
#     # ================================================================

#     return {
#         "mixamorig:Hips": (hips_center, spine_pts[0]), 
#         "mixamorig:Spine": (spine_pts[0], spine_pts[1]),
#         "mixamorig:Spine1": (spine_pts[1], spine1_end), 
#         "mixamorig:Spine2": (spine1_end, neck_base),
#         "mixamorig:Neck": (neck_base, ear_center), 
#         "mixamorig:Head": (ear_center, head_end),
#         "mixamorig:LeftShoulder": (left_shoulder_start, left_shoulder_end), 
#         "mixamorig:RightShoulder": (right_shoulder_start, right_shoulder_end),
            
#         # [수정] 계산된 새로운 목표점을 사용하도록 변경
#         "mixamorig:LeftArm": (left_shoulder_end, left_elbow_new), 
#         "mixamorig:LeftForeArm": (left_elbow_new, left_wrist_new), 
#         "mixamorig:LeftHand": (left_wrist_new, left_hand_end),
#         "mixamorig:RightArm": (right_shoulder_end, right_elbow_new), 
#         "mixamorig:RightForeArm": (right_elbow_new, right_wrist_new), 
#         "mixamorig:RightHand": (right_wrist_new, right_hand_end),

#         # 다리는 기존과 동일
#         "mixamorig:LeftUpLeg": (get_mp_loc(23), get_mp_loc(25)), 
#         "mixamorig:LeftLeg": (get_mp_loc(25), get_mp_loc(27)), 
#         "mixamorig:LeftFoot": (get_mp_loc(27), get_mp_loc(31)),
#         "mixamorig:RightUpLeg": (get_mp_loc(24), get_mp_loc(26)), 
#         "mixamorig:RightLeg": (get_mp_loc(26), get_mp_loc(28)), 
#         "mixamorig:RightFoot": (get_mp_loc(28), get_mp_loc(32)),
#     }

def build_targets():
    # This function defines the target points for each bone based on MediaPipe landmarks.
    # It remains unchanged as it's not related to the export issue.
    hips_center = center_of(23, 24)
    shoulder_center = center_of(11, 12)
    ear_center = center_of(7, 8)
    inner_eye_center = center_of(3, 4)
    nose = get_mp_loc(0)
    spine1_end = lerp(hips_center, shoulder_center, 0.75)
    spine2_end = lerp(hips_center, shoulder_center, 0.85)
    left_shoulder_end = lerp(get_mp_loc(23),get_mp_loc(11),0.9)
    right_shoulder_end = lerp(get_mp_loc(24),get_mp_loc(12),0.9)
    head_helper = lerp(ear_center, inner_eye_center,0.1)
    head_end = lerp(hips_center, head_helper, 1.02)
    #neck_base, neck_top = spine2_end, lerp(shoulder_center, nose, 0.25)
    neck_base = shoulder_center
    neck_top = lerp(shoulder_center, nose, 0.75)
    spine_pts = [lerp(hips_center, spine2_end, t/0.85) for t in [0.20, 0.45]]
    shoulder_start_point = lerp(spine1_end, neck_base, 1)
    # 몸 중심에서 각 어깨로 향하는 방향 벡터
    left_shoulder_dir = (get_mp_loc(11) - shoulder_center).normalized()
    right_shoulder_dir = (get_mp_loc(12) - shoulder_center).normalized()
    
    # 밖으로 밀어낼 거리 (이 값을 조절해 보세요)
    shoulder_offset = 0.06
    
    left_plus = left_shoulder_dir * 0.0093
    right_plus = right_shoulder_dir * 0.0093

    # 최종 어깨 시작점 계산
    left_shoulder_start = shoulder_start_point + left_shoulder_dir * shoulder_offset
    right_shoulder_start = shoulder_start_point + right_shoulder_dir * shoulder_offset
    left_shoulder_end += left_plus
    right_shoulder_end += right_plus

    head_start = lerp(neck_base, ear_center, 0.95)

    return {
        "mixamorig:Hips": (hips_center, spine_pts[0]), 
        "mixamorig:Spine": (spine_pts[0], spine_pts[1]),
        "mixamorig:Spine1": (spine_pts[1], spine1_end), 
        "mixamorig:Spine2": (spine1_end, neck_base),
        "mixamorig:Neck": (neck_base, ear_center), 
        "mixamorig:Head": (head_start, head_end),
        "mixamorig:HeadTop_End": (head_end, head_end),
        # [수정] 좌우 어깨뼈의 시작점을 각각 계산된 위치로 변경
        "mixamorig:LeftShoulder": (left_shoulder_start, left_shoulder_end), 
        "mixamorig:RightShoulder": (right_shoulder_start, right_shoulder_end),
            
        "mixamorig:LeftArm": (left_shoulder_end, get_mp_loc(13) + left_plus), 
        "mixamorig:LeftForeArm": (get_mp_loc(13) + left_plus, get_mp_loc(15) + left_plus), 
        "mixamorig:LeftHand": (get_mp_loc(15) + left_plus, get_mp_loc(19) + left_plus),
        "mixamorig:RightArm": (right_shoulder_end, get_mp_loc(14) + right_plus), 
        "mixamorig:RightForeArm": (get_mp_loc(14) + right_plus, get_mp_loc(16) + right_plus), 
        "mixamorig:RightHand": (get_mp_loc(16) + right_plus, get_mp_loc(20) + right_plus),
        "mixamorig:LeftUpLeg": (get_mp_loc(23), get_mp_loc(25)), 
        "mixamorig:LeftLeg": (get_mp_loc(25), get_mp_loc(27)), 
        "mixamorig:LeftFoot": (get_mp_loc(27), get_mp_loc(31)),
        "mixamorig:RightUpLeg": (get_mp_loc(24), get_mp_loc(26)), 
        "mixamorig:RightLeg": (get_mp_loc(26), get_mp_loc(28)), 
        "mixamorig:RightFoot": (get_mp_loc(28), get_mp_loc(32)),
    }

def apply_constraints(arm_obj, targets):
    for bone_name, (start, end) in targets.items():
        pb = arm_obj.pose.bones.get(bone_name)
        if not pb: continue
        head_empty = make_helper(f"MP_HEAD_{bone_name}", start)
        con = pb.constraints.new("COPY_LOCATION"); con.name = "MP_COPYLOC_HEAD"; con.target = head_empty
        if not ALIGN_STARTS_ONLY:
            tail_empty = make_helper(f"MP_TAIL_{bone_name}", end)
            dtrack = pb.constraints.new("DAMPED_TRACK"); dtrack.name = "MP_DTRACK_TAIL"; dtrack.target = tail_empty; dtrack.track_axis = 'TRACK_Y'
            if ALLOW_LENGTH_SCALE and pb.bone.length > 1e-6: 
                pb.scale.y = (end - start).length / pb.bone.length
def align_root_to_feet(arm_obj):
    try:
        # MediaPipe Empty 존재 확인
        foot_indices = [27, 28, 31, 32]  # 왼발, 오른발 발목과 발끝
        foot_points = []
        for idx in foot_indices:
            obj = bpy.data.objects.get(f"MP_{idx}")
            if not obj:
                raise ValueError(f"MP_{idx} Empty가 없습니다.")
            foot_points.append(obj.matrix_world.translation.copy())
        # 발 중심 계산
        foot_center = sum(foot_points, Vector()) / len(foot_points)  
        # 이동 벡터 = 원점 - 발 중심
        delta = Vector((0, 0, 0)) - foot_center    
        # Armature 위치 이동
        arm_obj.location += delta
        bpy.context.view_layer.update()      
        print(f"👣 발 중심 기준으로 Armature 이동 완료 (delta={delta})") 
    except Exception as e:
        print(f"⚠️ align_root_to_feet 실패: {e}")
        
        
def bake_and_cleanup(arm_obj):
    print("굽는 중...")
    bpy.context.scene.frame_set(BAKE_FRAME)
    bpy.ops.nla.bake(frame_start=BAKE_FRAME, frame_end=BAKE_FRAME, visual_keying=True, clear_constraints=True, bake_types={'POSE'})
    clear_old_helpers_and_constraints(arm_obj)
    print("✅ 베이킹 및 정리 완료.")

def scale_mp_empties(uniform_scale: float, pivot: str = "hips"):
    """
    모든 MP_* Empty를 'pivot'을 기준으로 균일 스케일.
    pivot: "hips" (MP_23/24 평균) 또는 "origin" (월드 원점)
    """
    # 피벗 계산
    if pivot == "hips":
        hips23 = bpy.data.objects.get("MP_23")
        hips24 = bpy.data.objects.get("MP_24")
        if not hips23 or not hips24:
            raise ValueError("MP_23 또는 MP_24가 없어 pivot='hips'를 쓸 수 없습니다.")
        pivot_pt = 0.5 * (hips23.matrix_world.translation + hips24.matrix_world.translation)
    else:
        pivot_pt = Vector((0.0, 0.0, 0.0))

    # 스케일 적용
    for obj in bpy.data.objects:
        if obj.name.startswith("MP_"):
            p = obj.matrix_world.translation
            obj.location = pivot_pt + (p - pivot_pt) * uniform_scale

    # 보기 편하게 표시 크기도 맞춤(선택)
    for obj in bpy.data.objects:
        if obj.name.startswith("MP_"):
            try:
                obj.empty_display_size *= uniform_scale
            except:
                pass



def main():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:]
    parser = argparse.ArgumentParser()
    parser.add_argument("--json_path", required=True)
    parser.add_argument("--export_path", required=True)
    args = parser.parse_args(argv)
    global JSON_PATH
    JSON_PATH = bpy.path.abspath(args.json_path)
    EXPORT_PATH = bpy.path.abspath(args.export_path)
    # =========================
    # RUN
    # =========================
    load_or_create_mp_empties()
    bpy.context.view_layer.update()
    arm_obj = next((obj for obj in bpy.data.objects if obj.type == 'ARMATURE'), None)
    if arm_obj is None: raise ValueError("Armature not found!")

    # 🔽🔽🔽 [핵심 수정 부분] 트랜스폼 적용 (Apply Transforms) 🔽🔽🔽
    print("🔧 Armature와 관련 Mesh의 트랜스폼을 적용합니다...")
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')

    # Armature와 연결된 Mesh들을 함께 선택
    arm_obj.select_set(True)
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            for mod in obj.modifiers:
                if mod.type == 'ARMATURE' and mod.object == arm_obj:
                    obj.select_set(True)
                    break
    # 선택된 모든 오브젝트(Armature + Meshes)의 트랜스폼 적용
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    print("✅ 트랜스폼 적용 완료.")
    # 🔼🔼🔼 여기까지 수정 🔼🔼🔼

    try:
        scale = compute_auto_scale(arm_obj)
        print(f"🔹 자동 계산된 스케일: {scale:.3f}")
        # 스케일을 직접 적용하는 대신, 매트릭스에 통합하여 나중에 한번에 적용
        # initial_scale_matrix = Matrix.Diagonal((scale, scale, scale, 1))
        # arm_obj.matrix_world = arm_obj.matrix_world @ initial_scale_matrix
        scale_mp_empties(1.0 / scale, pivot="hips")
        bpy.context.view_layer.update()
    except Exception as e:
        print(f"❌ 스케일 적용 중 오류 발생: {e}")

    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')

    
    print("🦴 모든 뼈의 'Connected' 속성을 해제합니다...")
    # 에디트 모드로 전환
    bpy.ops.object.mode_set(mode='EDIT')
    # 모든 에디트 본을 순회하며 'Connected' 체크 해제
    for eb in arm_obj.data.edit_bones:
        eb.use_connect = False
    # 다시 포즈 모드로 복귀
    bpy.ops.object.mode_set(mode='POSE')
    
    clear_old_helpers_and_constraints(arm_obj)
    targets = build_targets()
    apply_constraints(arm_obj, targets)

    bpy.context.view_layer.update()
    if BAKE:
        bake_and_cleanup(arm_obj)

    bpy.context.view_layer.update()
    # align_root_to_feet(arm_obj)

    print("✅ MP 시작점 정렬 완료" + ("" if ALIGN_STARTS_ONLY else " (방향/길이도 맞춤)"))

    # =========================
    # glTF/GLB 내보내기 (수정된 부분)
    # =========================

    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')

    if arm_obj:
        arm_obj.select_set(True)
        bpy.context.view_layer.objects.active = arm_obj
        for obj in bpy.data.objects:
            if obj.type == 'MESH':
                for mod in obj.modifiers:
                    if mod.type == 'ARMATURE' and mod.object == arm_obj:
                        obj.select_set(True)
                        break


    # 2. (추가된 부분) 유니티 호환을 위해 X축 -90도 회전
    print("🔄 Z-up -> Y-up 변환을 위해 회전 중...")
    bpy.ops.transform.rotate(value=math.pi/2, orient_axis='X')

    # 3. (추가된 부분) 회전 값 적용 (Apply Rotation)
    print("🔩 회전 값을 기본값으로 적용 중...")
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

    arm_obj.scale *= 1.7

    bpy.context.view_layer.update()

    bpy.ops.export_scene.gltf(
        filepath=EXPORT_PATH,
        export_format='GLB',
        use_selection=True,
        export_apply=True,
        export_animations=False,
        export_skins=True,
        
        export_rest_position_armature=False,
        export_yup=True 
    )



    print(f"✅ GLB 내보내기 완료: {EXPORT_PATH}")

if __name__=="__main__":
    main()