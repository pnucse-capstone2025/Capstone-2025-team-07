using System;
using System.Collections;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace meARy
{
    public class ServerInteractionManager : MonoBehaviour
    {
        string URL = "서버 url을 넣어주세요";
        [SerializeField] UIManager uiManager;
        List<PostInfo> postInfos = new List<PostInfo>();
        List<string> postGlbPaths = new List<string>();
        public void DownloadPost(GeospatialPose myPose, Action<bool, List<PostInfo>, List<string>> onComplete)
        {
            postInfos.Clear();
            postGlbPaths.Clear();
            GeoposeData dataForDownload = new GeoposeData();
            try
            {
                dataForDownload.latitude = myPose.Latitude;
                dataForDownload.longitude = myPose.Longitude;
                dataForDownload.altitude = myPose.Altitude;

            }
            catch (Exception e)
            {
            }
            string dataForDownloadJson = JsonUtility.ToJson(dataForDownload);
            StartCoroutine(GetRequest(dataForDownloadJson, onComplete));
        }
        
        private IEnumerator GetRequest(string jsonBody, Action<bool, List<PostInfo>, List<string>> onComplete)
        {
            string myPositionUrl =  URL + "/posts/five";
            byte[] bodyRaw = new UTF8Encoding().GetBytes(jsonBody);
            using (UnityWebRequest webRequest = new UnityWebRequest(myPositionUrl, "GET"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string getPostGLBUrl = URL + "/download/prefab/";
                    Debug.Log("주변 포스트 요청 전송 성공! 응답: " + webRequest.downloadHandler.text);
                    string jsonResponse = webRequest.downloadHandler.text;
                    PostList postList = JsonUtility.FromJson<PostList>(jsonResponse);
                    postInfos = postList.posts;
                    foreach (PostInfo post in postList.posts)
                    {
                        Debug.Log($"ID: {post.id}, 위도: {post.geopose.latitude}, 경도: {post.geopose.longitude}, 고도 : {post.geopose.altitude}");
                        using (UnityWebRequest getGLBRequest = new UnityWebRequest(getPostGLBUrl + post.id, "GET"))
                        {
                            getGLBRequest.downloadHandler = new DownloadHandlerBuffer();
                            yield return getGLBRequest.SendWebRequest();
                            if (getGLBRequest.result == UnityWebRequest.Result.Success)
                            {
                                byte[] glbData = getGLBRequest.downloadHandler.data;
                                string savePath = System.IO.Path.Combine(Application.persistentDataPath, $"downloaded_model{post.id}.glb");
                                try
                                {
                                    File.WriteAllBytes(savePath, glbData);
                                    Debug.Log($"GLB 파일 저장 성공! 경로: {savePath}");
                                    postGlbPaths.Add(savePath);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError("파일 저장 실패: " + ex.Message);

                                }
                            }
                        }
                    }
                    if (postGlbPaths.Count == postInfos.Count && postInfos.Count != 0)
                    {
                        onComplete?.Invoke(true, postInfos, postGlbPaths);
                    }
                    else
                    {
                        onComplete?.Invoke(false, null, null);

                    }
                }
                else
                {
                    Debug.LogError("주변 포스트 조회 실패: " + webRequest.error);
                    onComplete?.Invoke(false, null, null);
                    yield break;
                }
            }
        }
        
        public void UploadPost(GeospatialPose pose, Texture2D photo)
        {
            UploadData uploadData = CreateJsonDataFromPose(pose);
            byte[] photoData = photo.EncodeToJPG(75);
            string base64PhotoData = Convert.ToBase64String(photoData);
            uploadData.image_base64 = base64PhotoData;
            string uploadDataJson = JsonUtility.ToJson(uploadData);
            StartCoroutine(PosetRequest(uploadDataJson));
        }
        private IEnumerator PosetRequest(string jsonBody)
        {
            string url = URL + "/posts";
            byte[] bodyRaw = new UTF8Encoding().GetBytes(jsonBody);
            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                // 응답 결과 확인
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.Success:
                        Debug.Log("전송 성공! 응답: " + webRequest.downloadHandler.text);
                        break;
                    default:
                        Debug.LogError("전송 실패: " + webRequest.error);
                        uiManager.viewPopupPanel(new Exception("failed to send your post..."));
                        break;
                }
            }
        }
        private UploadData CreateJsonDataFromPose(GeospatialPose pose)
        {
            UploadData data = new UploadData();
            data.latitude = pose.Latitude;
            data.longitude = pose.Longitude;
            data.altitude = pose.Altitude;
            data.x = pose.EunRotation.x;
            data.y = pose.EunRotation.y;
            data.z = pose.EunRotation.z;
            data.w = pose.EunRotation.w;

            return data;
        }
        
    }
}