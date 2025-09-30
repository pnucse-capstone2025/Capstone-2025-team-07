using System.Collections.Generic;
using System.Diagnostics;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Siccity.GLTFUtility;
namespace meARy
    {
    public class Example : MonoBehaviour
    {
        //public GameObject placedPrefab; // 배치할 프리팹 
        private GameObject placedPrefab;
        [SerializeField] private ARRaycastManager raycastManager;
        private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
        [SerializeField] private AREarthManager earthManager;
        void Start()
        {
            StartCoroutine(LoadGLB());
        }
        System.Collections.IEnumerator LoadGLB()
        {
            string uri = System.IO.Path.Combine(Application.streamingAssetsPath, "retargeted_pose_real.glb");
            UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(uri);
            yield return www.SendWebRequest();
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                byte[] glbData = www.downloadHandler.data;
                placedPrefab = Importer.LoadFromBytes(glbData);
                placedPrefab.SetActive(false);
                UnityEngine.Debug.Log("Successed to load glb: " + www.error);
            }
            else
            {
                UnityEngine.Debug.Log("Failed to load glb: " + www.error);
            }
        }
        
        void Update()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                var touchPosition = touch.position;
                UnityEngine.Debug.Log($"터치 감지!!! {touchPosition.ToString()}");
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
                    } //터치한 위치로 레이캐스트 
                    if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
                    {   //가장 가까운 평면 위치에 오브젝트 생성 
                        Pose hitPose = hits[0].pose;
                        // GeospatialPose geoPose = earthManager.Convert(hitPose);
                        // UnityEngine.Debug.Log($"latitude:{geoPose.Latitude}, longitude: {geoPose.Longitude}, altitude: {geoPose.Altitude}");
                        // Pose convert = earthManager.Convert(geoPose);
                        // GameObject obj = Instantiate(placedPrefab, convert.position, convert.rotation);
                        GameObject obj = Instantiate(placedPrefab, hitPose.position, hitPose.rotation);
                        obj.transform.localRotation = Quaternion.Euler(+90f, 0f, 0f);
                        UnityEngine.Debug.Log($"hitPose position : {hitPose.position} , rotation : {hitPose.rotation}");
                        obj.SetActive(true);
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"터치 감지는 됐지만 touch.phase = {touch.phase}");
                }
            }
        }
    }    
}
