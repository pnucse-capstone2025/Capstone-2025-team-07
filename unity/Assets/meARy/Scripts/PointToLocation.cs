using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace meARy
{
    public class PointToLocation
    {
        
        private ARRaycastManager raycastManager;
        private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

        public PointToLocation(ARRaycastManager raycastManager)
        {
            this.raycastManager = raycastManager;
            Debug.Log("PointToLocation 생성자 호출!");   
        }

        public bool ConvertPointToLocalPose(Vector2 screenPoint, out Pose pose)
        {
            // ConvertPointToLocalPose를 호출하기 직전에 이 로그를 추가
            Debug.Log($"현재 AR 세션 상태: {ARSession.state}");
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                pose = default;
                return false;
            }
            if (raycastManager == null)
            {
                Debug.Log("raycastManager is null");
            }
            else if (raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon))
            {
                pose = hits[0].pose;
                return true;
            }

            // else if (raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneEstimated))
            // {
            //     pose = hits[0].pose;
            //     return true;
            // }
            // else if (raycastManager.Raycast(screenPoint, hits, TrackableType.Depth))
            // {
            //     pose = hits[0].pose;
            //     return true;
            // }
            // else if (raycastManager.Raycast(screenPoint, hits, TrackableType.FeaturePoint))
            // {
            //     pose = hits[0].pose;
            //     return true;
            // }
           
            pose = default;
            return false;
        }

        public Vector2 CalculateFootPosition(float leftHeelX, float leftHeelY, float rightHeelX, float rightHeelY,
                                             float leftFootX, float leftFootY, float rightFootX, float rightFootY, int width, int height)
        {
            // TODO: w - imageWidth, h - imageHeight
           
            
            int centerX = Mathf.RoundToInt((leftHeelX + rightHeelX + leftFootX + rightFootX) / 4.0f * width);
            int centerY = Mathf.RoundToInt(((leftHeelY + rightHeelY + leftFootY + rightFootY) / 4.0f) * height);
            Debug.Log($"발 중심 좌표: [{centerX}, {centerY}]");
            return new Vector2(centerX, centerY);
        }
    }    
}