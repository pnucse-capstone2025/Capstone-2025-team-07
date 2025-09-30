using UnityEngine;

using UnityEngine.UI;
using Google.XR.ARCoreExtensions;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
using meARy;
using DG.Tweening; // DOTween 무료 버전의 핵심 네임스페이스
using TMPro;
using System;
using System.Collections.Generic;
namespace meARy
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private ARCaptureController captureController;
        [SerializeField] private ServerInteractionManager serverInteractionManager;

        [SerializeField] private GameObject mainScreenPanel;
        [SerializeField] private GameObject previewScreenPanel;
        [SerializeField] private CanvasGroup fadeInScreenPanel;
        [SerializeField] private RawImage previewImage;
        [SerializeField] private CanvasGroup popupPanel;
        [SerializeField] private TextMeshProUGUI popupText;
        [SerializeField] private PostingManager postingManager;
        private Sequence activeSequence;

        private Texture2D texture = default;
        private GeospatialPose geospatialPose = default;

        public GameObject placedPrefab; // 배치할 프리팹
        [SerializeField] private AREarthManager earthManager;

        IEnumerator Start()
        {
            Debug.Log("로딩 화면 시작. AR 세션이 Tracking 상태가 되기를 기다립니다...");

            yield return new WaitUntil(() => ARSession.state == ARSessionState.SessionTracking);

            Debug.Log("AR 세션 Tracking 시작! 로딩 화면을 서서히 사라지게 합니다.");
            mainScreenPanel.SetActive(true);
            yield return fadeInScreenPanel.DOFade(0f, 0.3f).WaitForCompletion();
            fadeInScreenPanel.gameObject.SetActive(false);
        }

        public void OnCaputureButtonClick()
        {
            try
            {
                captureController.CaptureRawCameraImage();
            }
            catch (CannotCaptureScreenException ex)
            {
                viewPopupPanel(ex);
                Debug.Log("uimanager 에서 exception catch!!");
            }
            catch (CannotConvertRequestToImageException ex)
            {
                viewPopupPanel(ex);
                Debug.Log("uimanager 에서 exception catch!!");

            }
            catch (NoPoseDetectedException ex)
            {
                viewPopupPanel(ex);
                Debug.Log("uimanager 에서 exception catch!!");

            }
            catch (NoRaycastResultException ex)
            {
                viewPopupPanel(ex);
                Debug.Log("uimanager 에서 exception catch!!");

            }
            catch (CannotConvertPoseToGeospatialPoseExpcetion ex)
            {
                viewPopupPanel(ex);
                Debug.Log("uimanager 에서 exception catch!!");
            }
            catch (Exception ex)
            {
                // 다른 monobehavior에서 나온 것을 
                viewPopupPanel(ex);

            }

        }

        public void OnReturnButtonClick()
        {
            ShowMainScreen();

        }
        public void OnCheckButtonClick()
        {
            var tempTexture = texture;
            var tempPose = geospatialPose;
            ShowMainScreen(); //texture, geospatialPose를 초기화 하기 때문에 request를 위해 이전에 temp로 backup

            if (tempTexture == default || tempPose.Equals(new GeospatialPose())) // 오류로 인해 texture 가 없으면 main 화면으로 바로 돌아감.
            {
                Debug.LogWarning("Check 버튼을 눌렀지만 프로세싱 할 texture or geospatial pose 가 없음 오류!!!");

            }
            else
            {
                serverInteractionManager.UploadPost(tempPose, tempTexture);
            }

        }
        public void OnRefreshButtonClick()
        {
            var earthTrackingState = earthManager.EarthTrackingState;
            if (earthTrackingState == TrackingState.Tracking)
            {
                GeospatialPose myPose = earthManager.CameraGeospatialPose;
                Debug.Log($"현재 위치 - 위도: {myPose.Latitude}, 경도: {myPose.Longitude}, 고도: {myPose.Altitude}");
                serverInteractionManager.DownloadPost(myPose, (success, postInfos, postGlbPaths)=>
                {
                    if (success)
                    {
                        viewPopupPanel("!!!download surround post is start!!!");
                        StartCoroutine(postingManager.ViewPostings(postInfos, postGlbPaths));
                    }
                    else
                    {
                        Debug.Log("Failed to get surround posts... ");
                        viewPopupPanel("Failed to get surround posts... ");
                    }    
                });
                
            }
            else
            {
                Debug.Log("현재 위치를 추적할 수 없습니다. 상태: " + earthTrackingState);
                viewPopupPanel("Failed Earth Tracking");
            }
        }
        public void ShowMainScreen()
        {
            Debug.Log("Main 화면으로 전환");
            if (texture != default)
            {
                Destroy(texture);
            }
            texture = default;
            geospatialPose = default;
            mainScreenPanel.SetActive(true);
            previewScreenPanel.SetActive(false);
            if (previewImage.texture != null)
            {
                Destroy(previewImage.texture);
            }


        }
        public void ShowPreviewScreen(Texture2D texture, GeospatialPose geospatialPose)
        {
            this.texture = texture;
            this.geospatialPose = geospatialPose;
            Debug.Log("Preview 화면으로 전환");
            mainScreenPanel.SetActive(false);
            previewScreenPanel.SetActive(true);
            previewImage.texture = texture;
        }
        public void viewPopupPanel(Exception ex)
        {
            Debug.Log("viewPopupPanel 이 켜짐. caputer 과정에서의 문제 발생");
            popupText.text = ex.Message;
            popupPanel.gameObject.SetActive(true);
            activeSequence = DOTween.Sequence()
                // 1. Fade-in: 0.5초 동안 투명도를 1로 만들어 부드럽게 나타나게 합니다.
                .Append(popupPanel.DOFade(1.0f, 0.5f))
                // 2. Wait: duration(기본 2초)만큼 기다립니다.
                .AppendInterval(2.0f)
                // 3. Fade-out: 0.5초 동안 투명도를 0으로 만들어 부드럽게 사라지게 합니다.
                .Append(popupPanel.DOFade(0.0f, 0.5f))
                // 4. (선택사항) 모든 애니메이션이 끝나면 게임 오브젝트를 비활성화합니다.
                .OnComplete(() =>
                {
                    popupPanel.gameObject.SetActive(false);
                });
        }
        public void viewPopupPanel(string message)
        {
            Debug.Log("viewPopupPanel 이 켜짐. ");
            popupText.text = message;
            popupPanel.gameObject.SetActive(true);
            activeSequence = DOTween.Sequence()
                // 1. Fade-in: 0.5초 동안 투명도를 1로 만들어 부드럽게 나타나게 합니다.
                .Append(popupPanel.DOFade(1.0f, 0.5f))
                // 2. Wait: duration(기본 2초)만큼 기다립니다.
                .AppendInterval(3.0f)
                // 3. Fade-out: 0.5초 동안 투명도를 0으로 만들어 부드럽게 사라지게 합니다.
                .Append(popupPanel.DOFade(0.0f, 0.5f))
                // 4. (선택사항) 모든 애니메이션이 끝나면 게임 오브젝트를 비활성화합니다.
                .OnComplete(() => {
                    popupPanel.gameObject.SetActive(false);
                });
        }

    }   
}