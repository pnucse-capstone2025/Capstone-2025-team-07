using UnityEngine;
using System.Collections.Generic;
namespace meARy
{
    [System.Serializable]
    public class UploadData
    {
        public double latitude;
        public double longitude;
        public double altitude;
        public float x;
        public float y;
        public float z;
        public float w;
        public string image_base64;
    }
    [System.Serializable]
    public class GeoposeData
    {
        public double latitude;
        public double longitude;
        public double altitude;
    }
    [System.Serializable]
    public class QuaternionData
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }
    [System.Serializable]
    public class PostInfo
    {
        public GeoposeData geopose;
        public int id;
        public QuaternionData quaternion;
    }
    [System.Serializable]
    public class PostList
    {
        // JSON 배열을 이 리스트에 매핑합니다.
        // 변수 이름은 중요하지 않습니다.
        public List<PostInfo> posts;
    }
    
}
