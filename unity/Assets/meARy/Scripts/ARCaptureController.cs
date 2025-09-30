using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
using Mediapipe.Unity.Sample;
using Google.XR.ARCoreExtensions;
using System;



namespace meARy
{
    public class ARCaptureController : MonoBehaviour
    {
        [SerializeField] private ARCameraManager arCameraManager;
        [SerializeField] private Bootstrap bootstrap;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AREarthManager earthManager;
        public bool isOperating = false; // ARCaptureController 가 이미지 생성, pose detect, raycast, geospatialPose 변환을 수행중임을 알리기 위함.
        private PoseDetectionWithTexture2D poseDetectionWithTexture2D;

        


        IEnumerator Start()
        {
            Debug.Log("ARCaputerController Start() 가 불러짐");
            yield return new WaitUntil(() => ARSession.state == ARSessionState.SessionTracking);
            Debug.Log("AR 카메라 정상 작동 중 (Session Tracking)");

            if (poseDetectionWithTexture2D == null)
            {
                poseDetectionWithTexture2D = new PoseDetectionWithTexture2D(bootstrap, raycastManager); 
            }
            // Start 코루틴 안에서 poseDetector의 비동기 초기화를 기다립니다.
            // poseDetectionWithTexture2D가 null이 아닐 때만 실행하도록 방어 코드를 추가하는 것이 좋습니다.
            if (poseDetectionWithTexture2D != null)
            {
                yield return StartCoroutine(poseDetectionWithTexture2D.InitializeAsync());
                Debug.Log("SomeManager: poseDetector가 완전히 초기화되었습니다.");
            }
            else
            {
                Debug.LogError("poseDetectionWithTexture2D 객체가 Awake에서 생성되지 않았습니다!");
            }
        }
        /// <summary>
        /// UI 버튼 등에서 이 함수를 호출하여 원본 카메라 이미지 캡처를 시작합니다.
        /// </summary>
        public void CaptureRawCameraImage()
        {
            isOperating = true;
            // 최신 CPU 이미지를 동기적으로 가져옵니다.
            // 성공하지 못하면 여기서 함수를 종료합니다.
            if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
            {
                Debug.LogWarning("카메라 이미지를 얻는 데 실패했습니다.");
                isOperating = false;
                throw new CannotCaptureScreenException("카메라 이미지를 얻는 데 실패했습니다.");
            }


            // using 구문을 사용하면 블록이 끝날 때 cpuImage.Dispose()가 자동으로 호출되어
            // 메모리 누수를 방지할 수 있습니다. 매우 중요합니다!
            using (cpuImage)
            {
                Debug.Log($"이미지 획득 성공! 포맷: {cpuImage.format}, 크기: {cpuImage.width}x{cpuImage.height}");
                Debug.Log($"이미지 획득 성공! 스크린 크기: {UnityEngine.Screen.width}x{UnityEngine.Screen.height}");

                // CPU 이미지를 Texture2D로 변환하는 작업을 시작합니다.
                // 이 작업은 비동기적으로 처리하는 것이 좋습니다.
                ProcessImageToTexture(cpuImage);
                isOperating = false;
            } 
        }


        // 실제 변환 및 저장 프로세스 (비동기 처리를 위해 코루틴으로 분리)
        private void ProcessImageToTexture(XRCpuImage cpuImage)
        {
            // 1. 현재 화면 비율에 맞는 ConversionParams를 계산합니다.
            var conversionParams = GetScreenMatchedConversionParams(cpuImage);
            // Texture2D로 변환 요청
            var request = cpuImage.ConvertAsync(conversionParams);

            // 변환이 완료될 때까지 기다립니다.
            while (!request.status.IsDone())
            {
                
            }
            // 변환에 성공했는지 확인
            if (request.status == XRCpuImage.AsyncConversionStatus.Ready)
            {

                // 변환된 텍스처 데이터를 가져옵니다.
                var rawTextureData = request.GetData<byte>();

                // Texture2D 객체를 생성하고 픽셀 데이터를 로드합니다.
                Texture2D texture = new Texture2D(
                    request.conversionParams.outputDimensions.x,
                    request.conversionParams.outputDimensions.y,
                    request.conversionParams.outputFormat,
                    false);

                texture.LoadRawTextureData(rawTextureData);
                texture.Apply();
                Texture2D rotatedTexture = RotateTexture90Clockwise(texture);
                Debug.Log($"스크린 사이즈에 맞게 변환된 이미지 : {rotatedTexture.width} x {rotatedTexture.height}");
                Pose footLocalPose;
                bool footLocalPoseSuccess = poseDetectionWithTexture2D.poseFootDetect(rotatedTexture, out footLocalPose);
                if (footLocalPoseSuccess)
                {
                    Vector3 v = footLocalPose.position;
                    Debug.Log($"사진에서 발의 localPose 성공적으로 가져옴. 결과 - x={v.x} y={v.y} z={v.z}");
                    if (earthManager.EarthTrackingState == TrackingState.Tracking)
                    {
                        GeospatialPose geospatialPose = earthManager.Convert(footLocalPose);
                        GeospatialPose defaultPose = default;
                        if (geospatialPose.Equals(defaultPose))
                            throw new CannotConvertPoseToGeospatialPoseExpcetion("Cannot Generate Geospatial Pose. Even if it in earth tracking.");

                        Debug.Log($"latitude:{geospatialPose.Latitude}, longitude: {geospatialPose.Longitude}, altitude: {geospatialPose.Altitude}");

                        
                        uiManager.ShowPreviewScreen(rotatedTexture, geospatialPose);
                    }
                    else
                    {
                        throw new CannotConvertPoseToGeospatialPoseExpcetion("Earth tracking is failed. Cannot return GeospatialPose");
                    }
                    
                }
                else
                {
                    Debug.Log("사진에서 발의 localPose 가져오기 실패함.");
                    throw new NoRaycastResultException("Fail to get the localPose from foot pose.");
                }

                // 비동기 요청 리소스를 해제합니다.
            }
            else
            {
                Debug.LogError($"이미지 변환 실패: {request.status}");
                throw new CannotConvertRequestToImageException($"Fail to convert CPU Image to AsyncConversion: {request.status}");
            }
            request.Dispose();
        }
        /**
        <summary>
            XRCpuImage를 현재 화면 비율에 맞게 자르기 위한 ConversionParams를 계산하여 반환합니다.
        </summary>
        **/
        private XRCpuImage.ConversionParams GetScreenMatchedConversionParams(XRCpuImage cpuImage)
        {
            int screenWidth = UnityEngine.Screen.height;
            int screenHeight = UnityEngine.Screen.width;
            double screenAspect = (double)screenWidth / screenHeight;
            double imageAspect = (double)cpuImage.width / cpuImage.height;
            var inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height);

            if (imageAspect > screenAspect) //이미지가 가로 비율이 더 크면 -> 세로를 맞추고 가로를 잘라야함
            {
                int newWidth = (int)Math.Round(cpuImage.height * screenAspect, MidpointRounding.AwayFromZero);
                int xOffset =(int)Math.Round((double)(cpuImage.width - newWidth) / 2, MidpointRounding.AwayFromZero);

                inputRect = new RectInt(xOffset, 0, newWidth, cpuImage.height);
            }
            else // 이미지가 세로 비율이 더 크면 -> 가로를 맞추고 세로를 잘라내야함
            {
                int newHeight = (int)Math.Round((cpuImage.width / screenAspect), MidpointRounding.AwayFromZero);
                int yOffset = (int)Math.Round((double)(cpuImage.height - newHeight) / 2, MidpointRounding.AwayFromZero);
                inputRect = new RectInt(0, yOffset, cpuImage.width, newHeight);
            }

            return new XRCpuImage.ConversionParams
            {
                inputRect = inputRect,
                outputDimensions = new Vector2Int(inputRect.width, inputRect.height),
                outputFormat = TextureFormat.RGB24,
                transformation = XRCpuImage.Transformation.None
            };
        }


        /**
        <summary>
            UIManager에서 CheckButtonClick시에 texture를 로컬에 저장 및 path 받아옴.
        </summary>
        <param name = "texture">CPU Image로부터 얻은 texture결과</param>
        <return>texture 파일 저장 path </return>
        **/
        public bool SaveImage(Texture2D texture, out string filePath)
        {
            // Texture를 PNG 파일로 인코딩
            byte[] fileBytes = texture.EncodeToPNG();
            Destroy(texture); // 메모리 해제

            string fileName = "AR_RawCapture_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
            string savePath = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllBytes(savePath, fileBytes);
            // 파일로 저장
            Debug.Log($"원본 카메라 이미지 저장 성공: {savePath}");
            filePath = savePath;
            return true;
        }

        /**
            <summary>
            Texture2D를 90도 시계 방향으로 회전시킵니다.  
            MirrorY로 뒤집힌 입력을 가정하고 이를 보정합니다.
            (주의: CPU 연산이라 느릴 수 있습니다.)
            </summary>
            <param name="originalTexture">회전시킬 원본 텍스처</param>
            <returns>새롭게 회전된 텍스처</returns>
        **/
        private Texture2D RotateTexture90Clockwise(Texture2D originalTexture)
        {
            // 1. 원본 텍스처의 픽셀과 크기를 가져옵니다.
            Color32[] originalPixels = originalTexture.GetPixels32();
            int width = originalTexture.width;
            int height = originalTexture.height;

            // 2. 90도 회전하면 가로/세로 크기가 바뀌므로, 뒤바뀐 크기로 새 텍스처를 준비합니다.
            Texture2D rotatedTexture = new Texture2D(height, width);
            Color32[] rotatedPixels = new Color32[width * height];

            // 3. 픽셀을 하나씩 옮기는 회전 로직을 실행합니다.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 원본 픽셀의 인덱스 (1차원 배열 기준)
                    int originalIndex = y * width + x;

                    // 90도 시계 방향 회전을 위한 새 좌표 계산
                    // (x, y) -> (height - 1 - y, x)
                    // 새 텍스처의 가로는 원본의 height 이므로, y 좌표에 height를 곱해야 함

                    int newX = (width - 1) - x;
                    int rotatedIndex = newX * height + (height - 1 - y);  // 반시계방향으로 회전
                                                                          // int rotatedIndex = ((width - 1) - newX) * height + y;    // 시계방향으로 회전
                                                                          // 계산된 위치에 픽셀을 복사
                    rotatedPixels[rotatedIndex] = originalPixels[originalIndex];
                }
            }

            // 4. 회전된 픽셀 데이터를 새 텍스처에 설정하고 GPU에 업로드합니다.
            rotatedTexture.SetPixels32(rotatedPixels);
            rotatedTexture.Apply();

            return rotatedTexture;
        }
    }
    
}
