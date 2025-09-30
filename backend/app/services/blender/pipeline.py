import subprocess
import sys
import os

# =========================
# PARAMETERS
# =========================
CURRENT_DIR = os.path.dirname(__file__)
EXTRACT_POSE_FROM_IMAGE_PY_PATH = os.path.join(CURRENT_DIR, "extract_pose_from_image.py")
IMPORT_FBX_PY_PATH = os.path.join(CURRENT_DIR, "import_fbx.py")
SHOW_MEDIAPIPE_POINTS_PY_PATH = os.path.join(CURRENT_DIR, "show_mediapipe_points.py")
LINK_JOINTS_PY_PATH = os.path.join(CURRENT_DIR, "link_joints.py")
POSE_RETARGETING_EXPORT_FBX_PY_PATH = os.path.join(CURRENT_DIR, "pose_retargeting_export_fbx.py")
BLENDER_PATH = "/home/sylimi2r2/blender-4.4.3-linux-x64/blender"           # 서버에 설치된 Blender 실행 파일 경로

def run_pipeline(id: int):
    IMAGE_PATH = os.path.join("/home/sylimi2r2/meARy/Backend/images", f"{id}.jpg")
    JSON_PATH = os.path.join("/home/sylimi2r2/meARy/Backend/poses", f"{id}.json")
    EXPORT_PATH = os.path.join("/home/sylimi2r2/meARy/Backend/prefabs", f"{id}.glb")
    # =========================
    # STEP 1. Mediapipe로 포즈 추출
    # =========================
    print("🔹 Step 1: Extracting pose from image...")
    subprocess.run([
        sys.executable, EXTRACT_POSE_FROM_IMAGE_PY_PATH,
        "--image_path", f"/home/sylimi2r2/meARy/Backend/images/{id}.jpg",
        "--output_path", f"/home/sylimi2r2/meARy/Backend/poses/{id}.json",
        ], check=True)

    if not os.path.exists(f"/home/sylimi2r2/meARy/Backend/poses/{id}.json"):
        raise FileNotFoundError("❌ {id}.json 생성 실패")

    # =========================
    # STEP 2. Blender에서 리타게팅 및 FBX Export
    # =========================
    print("🔹 Step 2: Running Blender retargeting...")
    blender_cmd = [
        BLENDER_PATH, "--background",
        "--python", IMPORT_FBX_PY_PATH,
        # "--python", SHOW_MEDIAPIPE_POINTS_PY_PATH,
        # "--python", LINK_JOINTS_PY_PATH,
        "--python", POSE_RETARGETING_EXPORT_FBX_PY_PATH,
        "--",
        "--json_path", f"/home/sylimi2r2/meARy/Backend/poses/{id}.json",
        "--export_path", f"/home/sylimi2r2/meARy/Backend/prefabs/{id}.glb",
    ]
    subprocess.run(blender_cmd, check=True)

    #if not os.path.exists(OUTPUT_FBX):
    #    raise FileNotFoundError("❌ retargeted_pose.fbx 생성 실패")

    print(f"✅ 파이프라인 완료")

if __name__=="__main__":
    run_pipeline(1)