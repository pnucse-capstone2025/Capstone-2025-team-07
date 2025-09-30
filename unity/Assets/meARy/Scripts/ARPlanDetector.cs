using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using DG.Tweening;
using Google.XR.ARCoreExtensions;
using System;

// 이 스크립트는 화면 중앙을 기준으로 AR 평면(PlaneWithinPolygon)이 감지되었는지
// 지속적으로 확인하고, 감지되었을 때 안내 팝업을 꺼주는 역할을 합니다.
namespace meARy
{
    public class ARPlaneDetector : MonoBehaviour
    {
        // Inspector 창에서 AR Raycast Manager를 연결해줘야 합니다.
        [SerializeField] private ARRaycastManager raycastManager;

        // Inspector 창에서 위에서 만든 팝업 Panel 오브젝트를 연결해줘야 합니다.
        [SerializeField] private CanvasGroup planeDetectionPopup_Panel;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private UIManager uiManager;
        public float groundY = 1.0f;
        private bool planeDetected = false;

        // Raycast 결과를 담을 리스트 (매번 새로 생성하는 것을 방지하기 위해 멤버 변수로 선언)
        private List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private Vector2 screenCenter;
        private void Start()
        {
            screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            StartCoroutine(DetectingPlane());
            
        }

        IEnumerator DetectingPlane()
        {
            while (!planeDetected)
            {
                if (planeManager.trackables.count > 0)
                {
                    
                    // foreach (var plane in planeManager.trackables)
                    // {
                        // groundY = Math.Min(groundY, plane.center.y);
                        // Debug.Log($"평면 감지됨, groundY - {groundY}");

                    // }
                    foreach (var plane in planeManager.trackables)
                    {
                        // 수평 바닥만 대상으로 (아래 향하는 평면은 제외)
                        // ✅ 평면의 월드 높이
                        var y = plane.transform.position.y;
                        Debug.Log($"plane.transform.position.y : {plane.transform.position.y}");
                        uiManager.viewPopupPanel($"plane.transform.position.y : {plane.transform.position.y}");
                        planeDetected = true;
                        if (y < groundY)
                            groundY = y;
                        
                    }
                }     
                yield return null; 
            }
            uiManager.viewPopupPanel($"plane.transform.position.y : {groundY}");
           
            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                // 화면 중앙을 기준으로 '경계가 있는 평면(PlaneWithinPolygon)'을 대상으로 Raycast를 실행합니다.
                if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
                {
                    if (planeDetectionPopup_Panel.gameObject.activeSelf)
                    {
                        planeDetectionPopup_Panel.DOFade(0.0f, 0.5f);
                        planeDetectionPopup_Panel.gameObject.SetActive(false);
                    }
                }

                else if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneEstimated))
                {
                    if (planeDetectionPopup_Panel.gameObject.activeSelf)
                    {
                        planeDetectionPopup_Panel.DOFade(0.0f, 0.5f);
                        planeDetectionPopup_Panel.gameObject.SetActive(false);
                    }
                }
                else if (raycastManager.Raycast(screenCenter, hits, TrackableType.Depth))
                {
                    if (planeDetectionPopup_Panel.gameObject.activeSelf)
                    {
                        planeDetectionPopup_Panel.DOFade(0.0f, 0.5f);
                        planeDetectionPopup_Panel.gameObject.SetActive(false);
                    }
                }
                else if (raycastManager.Raycast(screenCenter, hits, TrackableType.FeaturePoint))
                {
                    if (planeDetectionPopup_Panel.gameObject.activeSelf)
                    {
                        planeDetectionPopup_Panel.DOFade(0.0f, 0.5f);
                        planeDetectionPopup_Panel.gameObject.SetActive(false);
                    }
                }
                if (raycastManager.Raycast(screenCenter, hits, TrackableType.AllTypes))
                {
                    // Raycast가 성공했다는 것은 사용자가 유효한 평면을 바라보고 있다는 의미입니다.
                    // 따라서 안내 팝업을 비활성화합니다.
                    if (planeDetectionPopup_Panel.gameObject.activeSelf)
                    {
                        planeDetectionPopup_Panel.DOFade(0.0f, 0.5f);
                        planeDetectionPopup_Panel.gameObject.SetActive(false);
                    }
                }
                // Raycast가 실패하면 (아직 평면을 못 찾았거나, 다른 곳을 보고 있으면) 팝업은 계속 켜진 상태로 유지됩니다.
                else
                {
                    if (!planeDetectionPopup_Panel.gameObject.activeSelf)
                    {
                        planeDetectionPopup_Panel.DOFade(1.0f, 0.5f);
                        planeDetectionPopup_Panel.gameObject.SetActive(true);
                    }
                }
            }
        }
    }    
}
