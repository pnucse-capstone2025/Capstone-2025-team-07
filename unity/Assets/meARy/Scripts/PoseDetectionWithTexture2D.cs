using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Collections;
using UnityEngine.XR.ARFoundation;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Unity.Sample;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Unity;

/** 
    texture를 받아오면 발 좌표를 계산해서 넘기는 class
**/
namespace meARy
{
    public class PoseDetectionWithTexture2D
    {
        
        public readonly PoseLandmarkDetectionConfig config = new PoseLandmarkDetectionConfig();
        public Bootstrap bootstrap { get; private set; }
        private PoseLandmarkerOptions options;
        private PoseLandmarker taskApi;
        private PointToLocation pointToLocation;
        
        
        public bool isInitialized { get; private set; } = false; // 초기화 완료 플래그 추가

        public PoseDetectionWithTexture2D(Bootstrap bootstrap, ARRaycastManager raycastManager)
        {
            Debug.Log($"Delegate = {config.Delegate}");
            Debug.Log($"Image Read Mode = {config.ImageReadMode}");
            Debug.Log($"Model = {config.ModelName}");
            Debug.Log($"Running Mode = {config.RunningMode}");
            Debug.Log($"NumPoses = {config.NumPoses}");
            Debug.Log($"MinPoseDetectionConfidence = {config.MinPoseDetectionConfidence}");
            Debug.Log($"MinPosePresenceConfidence = {config.MinPosePresenceConfidence}");
            Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");
            Debug.Log($"OutputSegmentationMasks = {config.OutputSegmentationMasks}");
            if (bootstrap != null)
            {
                Debug.Log("bootstrap 게임 오브젝트 잘 받아옴");
            }
            else
            {
                Debug.Log("bootstrap 게임 오브젝트 못 받아옴!!");
            }
            this.bootstrap = bootstrap;
            pointToLocation = new PointToLocation(raycastManager);
            Debug.Log("PoseDetectionWithTexture2D 생성자 호출 및 생성 완료!");

   
        }

        // 비동기 초기화를 위한 별도의 코루틴 메소드
        public IEnumerator InitializeAsync()
        {
            // bootstrap이 끝날 때까지 기다림
            yield return new WaitUntil(() => bootstrap.isFinished);

            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            // yield return new WaitForSeconds(0.1f); // 0.1초 정도의 매우 짧은 딜레이 추가 -> ar camera가 켜지고 나서 나머지 설정 하도록
            Debug.Log("Bootstrap 초기화 완료. Task API 생성을 시작합니다.");

            // 기다림이 끝난 후에야 이 코드들이 실행됨
            options = config.GetPoseLandmarkerOptions(null);
            taskApi = PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);

            isInitialized = true; // 모든 준비가 끝났음을 알림
            Debug.Log("PoseDetectionWithTexture2D Init 및 Task API 생성 완료. 모든 준비가 끝났습니다.");
        }
        public bool poseFootDetect(Texture2D tex, out Pose footLocalPose)
        {
            Vector2[] points = new Vector2[4];
            if (tex == null)
            {
                Debug.LogError("Failed to get image bytes.");
                footLocalPose = default;
                return false;
            }
            int w = tex.width, h = tex.height;

            // 3) Texture2D -> MediaPipe Image (CPU)
            var tf = new Mediapipe.Unity.Experimental.TextureFrame(w, h, TextureFormat.RGBA32);
            tf.ReadTextureOnCPU(tex, flipHorizontally: false, flipVertically: false);
            using var image = tf.BuildCPUImage();
            tf.Release();

            // Always rotation=0 (샘플 정책)
            // var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);
            var imageProcessingOptions = new ImageProcessingOptions(rotationDegrees: 0);
            // 결과 버퍼 준비
            var result = PoseLandmarkerResult.Alloc(options.numPoses, options.outputSegmentationMasks);
            // 4) IMAGE 모드 동기 탐지 실행
            //    (config.RunningMode가 무엇이든, 이 한 장에 대해 TryDetect는 동작함)
            bool ok = taskApi.TryDetect(image, imageProcessingOptions, ref result);
            if (!ok || result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            {
                Debug.LogWarning("[StaticImage] 사람의 pose 가 감지되지 않았습니다.");
                throw new NoPoseDetectedException("There is not a person to detect pose");
            }
            else
            {
                // 첫 사람만 출력(여러 명이면 루프 확장)
                // 석윤이 result.poseLandmarks[0].landmarks가 있고 worldlandmarks가 있거든
                // 그냥 landmark는 이미지 위에서 상대적 위치라고 하고 worldlandmarks는 카메라 기준 미터단위
                // 아래는 그냥 landmark라서 x, y, z로 표현되는데 z는 landmark[0](코끝)을 기준으로 깊이를 매긴데
                // (x, y) 계산하는 방법은 (0,0)은 이미지 왼쪽 위, (1,1)은 이미지 오른쪽아래로 우리가 보통 계산하는 좌표계처럼 x는 가로, y는 세로
                // 29 - LEFT_HEEL, 30 - RIGHT_HEEL, 31 - LEFT_FOOT_INDEX, 32 - RIGHT_FOOT_INDEX
                var lms = result.poseLandmarks[0].landmarks;
                for (int i = 0; i < lms.Count; i++)
                {
                    float x = lms[i].x;
                    float y = lms[i].y;
                    float z = lms[i].z;
                    float vis = lms[i].visibility ?? 0f; // nullable float 가드
                }
                points[0] = new Vector2(lms[29].x, lms[29].y);
                points[1] = new Vector2(lms[30].x, lms[30].y);
                points[2] = new Vector2(lms[31].x, lms[31].y);
                points[3] = new Vector2(lms[32].x, lms[32].y);
                Debug.Log("발 좌표 4개\n"
                + $" [{points[0][0]} , {points[0][1]}]"
                + $" [{points[1][0]} , {points[1][1]}]"
                + $" [{points[2][0]} , {points[2][1]}]"
                + $" [{points[3][0]} , {points[3][1]}]");

                // 모델 세울 좌표 ar local pose로 구하기. raycastManager는 화면의 pixel단위로 계산하고, 왼쪽 아래가 (0,0) 이므로 상하반전 필요
                var footPosition = pointToLocation.CalculateFootPosition(lms[29].x, lms[29].y, lms[30].x, lms[30].y
                , lms[31].x, lms[31].y, lms[32].x, lms[32].y, UnityEngine.Screen.width, UnityEngine.Screen.height);

                bool convert_success = pointToLocation.ConvertPointToLocalPose(footPosition, out footLocalPose);
                if (convert_success)
                {
                    Vector3 v = footLocalPose.position;
                    Debug.Log($"[AR Local Pose] x={v.x} y={v.y} z={v.z}");

                    // Object.Instantiate(prefab, footLocalPose.position, footLocalPose.rotation);
                    // Debug.Log("발 위치에 prefab 생성됐다..");
                    return true;
                }
                else
                {
                    Debug.Log("[AR Local Pose] Failed converting to AR World Pose");
                    return false;
                }
            }
        }
    }
}