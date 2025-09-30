using System.Collections.Generic;
using System.Diagnostics;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Debug = System.Diagnostics.Debug;

using TMPro;
using UnityEngine.UI;

public class Example : MonoBehaviour
{

    // public TMP_InputField latitudeInput;
    // public TMP_InputField longitudeInput;
    // public TMP_InputField altitudeInput;
    // public Button placeAnchorButton;
    [SerializeField] private ARAnchorManager anchorManager;




    public GameObject placedPrefab; // 배치할 프리팹
    [SerializeField] private ARRaycastManager raycastManager;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    [SerializeField] private AREarthManager earthManager;

    void Start()
    {
        // placeAnchorButton.onClick.AddListener(PlaceAnchorAtLocation);
    }

    // void PlaceAnchorAtLocation()
    // {
    //     // 입력값 확인
    //     if (!double.TryParse(latitudeInput.text, out double latitude) ||
    //         !double.TryParse(longitudeInput.text, out double longitude) ||
    //         !double.TryParse(altitudeInput.text, out double altitude))
    //     {
    //         UnityEngine.Debug.Log("위도/경도/고도를 올바르게 입력하세요.");
    //         return;
    //     }

    //     // Earth Tracking 상태 확인
    //     if (earthManager.EarthTrackingState != TrackingState.Tracking)
    //     {
    //         UnityEngine.Debug.Log("Earth가 아직 Tracking 상태가 아닙니다.");
    //         return;
    //     }

    //     // Geospatial Anchor 생성
    //     ARGeospatialAnchor anchor = anchorManager.AddAnchor(latitude, longitude, altitude, Quaternion.identity);
    //     //     earthManager.CreateAnchor(latitude, longitude, altitude, Quaternion.identity)
    //     // );

    //     if (anchor != null)
    //     {
    //         Instantiate(placedPrefab, anchor.transform);
    //         UnityEngine.Debug.Log($"✔ Anchor placed at ({latitude}, {longitude}, {altitude})");
    //     }
    //     else
    //     {
    //         UnityEngine.Debug.Log("❌ Anchor 생성 실패");
    //     }
    // }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            var touchPosition = touch.position;
            if (touch.phase == TouchPhase.Began)
            {
                UnityEngine.Debug.Log("Touched");
                var earthTrackingState = earthManager.EarthTrackingState;
                UnityEngine.Debug.Log($"{earthTrackingState}");
                if (earthTrackingState == TrackingState.Tracking)
                {
                    UnityEngine.Debug.Log("tracking");
                    var cameraGeoSpatialPose = earthManager.CameraGeospatialPose;
                    UnityEngine.Debug.Log($"latitude:{cameraGeoSpatialPose.Latitude}, longitude: {cameraGeoSpatialPose.Longitude}, altitude: {cameraGeoSpatialPose.Altitude}");
                }

                //터치한 위치로 레이캐스트
                if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    //가장 가까운 평면 위치에 오브젝트 생성
                    Pose hitPose = hits[0].pose;
                    GeospatialPose geoPose = earthManager.Convert(hitPose);
                    UnityEngine.Debug.Log($"latitude:{geoPose.Latitude}, longitude: {geoPose.Longitude}, altitude: {geoPose.Altitude}");
                    Pose convert = earthManager.Convert(geoPose);

                    Instantiate(placedPrefab, convert.position, convert.rotation);
                }
            }
        }
    }
}