import cv2
import mediapipe as mp
import json
import os
import argparse
import sys

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--image_path", required=True)
    parser.add_argument("--output_path", required=True)
    args = parser.parse_args()
    image_path = args.image_path
    output_path = args.output_path

    mp_pose = mp.solutions.pose
    pose = mp_pose.Pose(static_image_mode=True, model_complexity=1)

    image = cv2.imread(image_path)
    image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    results = pose.process(image_rgb)

    if results.pose_world_landmarks:
        data = {}
        for i, lm in enumerate(results.pose_world_landmarks.landmark):
            data[i] = {"x": lm.x, "y": lm.y, "z": lm.z}
        with open(output_path, "w") as f:
            json.dump(data, f, indent=2)
        print("✅ pose_landmarks.json 저장 완료")
    else:
        print("❌ 사람 인식 실패")

if __name__ == "__main__":
    main()