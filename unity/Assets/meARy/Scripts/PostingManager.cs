using System.Collections.Generic;
using System.Diagnostics;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Siccity.GLTFUtility;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using System;
using System.Threading.Tasks;


namespace meARy
{
    public class PostingManager : MonoBehaviour
    {
        [SerializeField] AREarthManager earthManager;
        [SerializeField] ARAnchorManager anchorManager;
        [SerializeField] UIManager uiManager;
        [SerializeField] ARPlaneDetector planeDetector;
        List<GameObject> postingPrefabs = new List<GameObject>();
        public List<GameObject> postingModels = new List<GameObject>(); // Instantiate 된 gameobject

        public IEnumerator ViewPostings(List<PostInfo> postInfos, List<string> postGlbPaths)
        {
            postingPrefabs.Clear();
            foreach (GameObject postingModel in postingModels)
            {
                Destroy(postingModel);
            }
            postingModels.Clear();
            bool loadSuccess = false;
            yield return StartCoroutine(LoadGLB(postGlbPaths, (success) =>
            {
                loadSuccess = success;
            }));

            if (loadSuccess && postingPrefabs.Count == postGlbPaths.Count && postingPrefabs.Count == postInfos.Count)
            {
                yield return StartCoroutine(SetModelOnAnchor(postInfos, postGlbPaths));
                uiManager.viewPopupPanel("!!!download surround post is done!!!");
            }
            
        }
        private IEnumerator SetModelOnAnchor(List<PostInfo> postInfos, List<string> postGlbPaths)
        {
            for (int i = 0; i < postInfos.Count; i++)
            {
                GameObject prefab = postingPrefabs[i];
                PostInfo postInfo = postInfos[i];
                Quaternion rotation = new Quaternion(
                    postInfo.quaternion.x,
                    postInfo.quaternion.y,
                    postInfo.quaternion.z,
                    postInfo.quaternion.w
                );
                rotation = Quaternion.Normalize(rotation);
                var promise = anchorManager.ResolveAnchorOnTerrainAsync(
                    postInfo.geopose.latitude,
                    postInfo.geopose.longitude,
                    0.0f,
                    rotation);
                while (promise.State == PromiseState.Pending)
                    yield return null;

                var result = promise.Result;
                if (result != null && result.TerrainAnchorState == TerrainAnchorState.Success && result.Anchor != null)
                {
                    UnityEngine.Debug.Log($"Use Terrain Anchor");
                    var anchor = result.Anchor; // ARGeospatialAnchor
                    // 앵커를 부모로 붙이면 월드 포즈는 앵커를 따라감 (y=바닥)
                    var postingModel = Instantiate(prefab, anchor.transform);
                    postingModel.transform.localPosition = Vector3.zero;
                    postingModel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    postingModel.SetActive(true);
                    postingModels.Add(postingModel);
                    var position = anchor.transform.position;
                    UnityEngine.Debug.Log($"anchor 박은 좌표 : {position.x}, {position.y}, {position.z}");
                    
                }

                else
                {
                    
                    var anchor = anchorManager.AddAnchor(postInfo.geopose.latitude, postInfo.geopose.longitude, postInfo.geopose.altitude, rotation);
                    if (anchor != null)
                    {
                        UnityEngine.Debug.Log($"Use GroundY Anchor");
                        yield return null;
                        int guard = 60; // 최대 1초(60fps 가정)
                        while (anchor.trackingState != TrackingState.Tracking && guard-- > 0)
                            yield return null;
                        
                        Vector3 temp = anchor.transform.position;
                        UnityEngine.Debug.Log($"anchor 박기 전에 anchor.transform.position 정보 111 : {temp.x}, {temp.y}, {temp.z}");
                        temp.y = planeDetector.groundY;
                        UnityEngine.Debug.Log($"anchor 박기 전에 anchor.transform.position 정보 222 : {temp.x}, {temp.y}, {temp.z}");
                        Pose poseOnFloor = new Pose(temp, rotation);
                        GeospatialPose poseOnFloorGeo = earthManager.Convert(poseOnFloor);
                        Destroy(anchor);
                        ARGeospatialAnchor geoAnchor = anchorManager.AddAnchor(
                            poseOnFloorGeo.Latitude,
                            poseOnFloorGeo.Longitude,
                            poseOnFloorGeo.Altitude,
                            poseOnFloorGeo.EunRotation
                        );
                        if (geoAnchor != null)
                        {
                            yield return null;
                            while (geoAnchor.trackingState != TrackingState.Tracking) yield return null;
                            UnityEngine.Debug.Log($"anchor 박기 전의 local pose : {poseOnFloor.position.x}, {poseOnFloor.position.y}, {poseOnFloor.position.z}");
                            // var postingModel = Instantiate(prefab, geoAnchor.transform);
                            // postingModel.transform.localPosition = Vector3.zero;
                            // postingModel.transform.localRotation *= Quaternion.Euler(90f, 0f, 0f);
                            // postingModel.SetActive(true);
                            // postingModels.Add(postingModel);
                            
                            // 2) 프리팹을 먼저 “월드 기준”으로 세움
                            var postingModel = Instantiate(prefab);

                            // 원하는 월드 회전 (hitPose 때처럼 그냥 90도 세워두기)
                            Quaternion desiredWorldRot = Quaternion.Euler(90f, 180f, 0f);

                            // 앵커의 월드 위치로 옮기고, 월드 회전을 바로 세팅
                            postingModel.transform.SetPositionAndRotation(
                                geoAnchor.transform.position,
                                desiredWorldRot
                            );

                            // 3) 이제 앵커의 자식으로 붙이되, **worldPositionStays=true** 로 유지
                            postingModel.transform.SetParent(geoAnchor.transform, /*worldPositionStays:*/ true);
                            var position = geoAnchor.transform.position;
                            // (선택) 로컬 위치/스케일 미세 조정
                            postingModel.transform.localPosition = Vector3.zero;
                            postingModel.SetActive(true);
                            postingModels.Add(postingModel);
                            UnityEngine.Debug.Log($"anchor 박은 좌표 : {position.x}, {position.y}, {position.z}");
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"Ancor == null!!!, earthManager.EarthState : {earthManager.EarthState},   earthManager.EarthTrackingState : {earthManager.EarthTrackingState}");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"Ancor == null!!!, earthManager.EarthState : {earthManager.EarthState},   earthManager.EarthTrackingState : {earthManager.EarthTrackingState}");
                    }
                }

            }
            if (postGlbPaths.Count == postingModels.Count)
            {
                uiManager.viewPopupPanel("successfully get surround posts!!!");
            }
            else
            {
                UnityEngine.Debug.Log($"Failed to get surround posts... , postGlbPaths.Count : {postGlbPaths.Count}, postingModels.Count : {postingModels.Count}");
                uiManager.viewPopupPanel($"Failed to get surround posts... , postGlbPaths.Count : {postGlbPaths.Count}, postingModels.Count : {postingModels.Count}");
            }
                
        }
        /**
            휴대폰 로컬에 저장한 파일 읽어와서 GameObject 프리펩으로 생성
        **/
        private IEnumerator LoadGLB(List<string> postGlbPaths, Action<bool> onComplete)
        {

            foreach (string path in postGlbPaths)
            {
                if (!File.Exists(path))
                {
                    UnityEngine.Debug.LogError("프리펩 파일을 찾을 수 없음..");
                    onComplete?.Invoke(false);
                    yield break;
                }

                byte[] glbData = null;
                var t = Task.Run(() => { glbData = File.ReadAllBytes(path); }); //백그라운드에서 파일 읽음
                while (!t.IsCompleted) yield return null;
                try
                {
                    GameObject prefab = Importer.LoadFromBytes(glbData);
                    if (prefab == null)
                    {
                        UnityEngine.Debug.LogError("GLB parse failed");
                        onComplete?.Invoke(false);
                        yield break;
                    }

                    prefab.SetActive(false);
                    postingPrefabs.Add(prefab);
                    UnityEngine.Debug.Log("Loaded GLB from bytes: " + path);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("Load GLB failed: " + ex.Message);
                    onComplete?.Invoke(false);
                    yield break;
                }
            }
            onComplete?.Invoke(true);
        }
        
    }
}