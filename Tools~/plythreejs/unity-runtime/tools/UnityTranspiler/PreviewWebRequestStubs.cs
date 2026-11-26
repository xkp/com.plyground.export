namespace UnityEngine.Networking
{
    public class DownloadHandler : System.IDisposable
    {
        public byte[] data { get; }
        public string text { get; }
        public string error { get; }
        public bool isDone { get; }
        public void Dispose() { }
    }
    public class DownloadHandlerBuffer : DownloadHandler
    {
        public static byte[] GetContent(UnityWebRequest request) => null!;
    }
    public class DownloadHandlerTexture : DownloadHandler
    {
        public DownloadHandlerTexture(bool readable = false) { }
        public UnityEngine.Texture2D texture { get; }
        public static UnityEngine.Texture2D GetContent(UnityWebRequest request) => null!;
    }
    public class DownloadHandlerAudioClip : DownloadHandler
    {
        public DownloadHandlerAudioClip(string url, UnityEngine.AudioType audioType) { }
        public UnityEngine.AudioClip audioClip { get; }
        public static UnityEngine.AudioClip GetContent(UnityWebRequest request) => null!;
    }
    public class DownloadHandlerAssetBundle : DownloadHandler
    {
        public DownloadHandlerAssetBundle(string url) { }
        public UnityEngine.AssetBundle assetBundle { get; }
        public static UnityEngine.AssetBundle GetContent(UnityWebRequest request) => null!;
    }
    public class UploadHandler : System.IDisposable
    {
        public string contentType { get; set; }
        public byte[] data { get; }
        public float progress { get; }
        public void Dispose() { }
    }
    public class UploadHandlerRaw : UploadHandler
    {
        public UploadHandlerRaw(byte[] data) { }
    }
    public class UnityWebRequestAsyncOperation : UnityEngine.AsyncOperation
    {
        public UnityWebRequest webRequest { get; }
    }
    public class UnityWebRequest : System.IDisposable
    {
        public enum Result { InProgress = 0, Success = 1, ConnectionError = 2,
            ProtocolError = 3, DataProcessingError = 4 }
        public const string kHttpVerbGET = "GET", kHttpVerbPOST = "POST",
            kHttpVerbPUT = "PUT", kHttpVerbDELETE = "DELETE", kHttpVerbHEAD = "HEAD";
        public UnityWebRequest() { }
        public UnityWebRequest(string url) { }
        public UnityWebRequest(string url, string method) { }
        public UnityWebRequest(string url, string method, DownloadHandler downloadHandler,
            UploadHandler uploadHandler) { }
        public static UnityWebRequest Get(string url) => null!;
        public static UnityWebRequest Post(string url, string body) => null!;
        public static UnityWebRequest Post(string url, string body, string contentType) => null!;
        public static UnityWebRequest Post(string url,
            System.Collections.Generic.Dictionary<string, string> fields) => null!;
        public static UnityWebRequest Put(string url, byte[] body) => null!;
        public static UnityWebRequest Put(string url, string body) => null!;
        public static UnityWebRequest Delete(string url) => null!;
        public static UnityWebRequest Head(string url) => null!;
        public static UnityWebRequest GetTexture(string url) => null!;
        public static UnityWebRequest GetTexture(string url, bool readable) => null!;
        public static UnityWebRequest GetAudioClip(string url, UnityEngine.AudioType audioType) => null!;
        public static UnityWebRequest GetAssetBundle(string url) => null!;
        public static string EscapeURL(string value) => null!;
        public static string UnEscapeURL(string value) => null!;
        public string url { get; set; }
        public string method { get; set; }
        public DownloadHandler downloadHandler { get; set; }
        public UploadHandler uploadHandler { get; set; }
        public bool disposeDownloadHandlerOnDispose { get; set; }
        public bool disposeUploadHandlerOnDispose { get; set; }
        public int timeout { get; set; }
        public int redirectLimit { get; set; }
        public Result result { get; }
        public long responseCode { get; }
        public string error { get; }
        public bool isDone { get; }
        public bool isModifiable { get; }
        public bool isNetworkError { get; }
        public bool isHttpError { get; }
        public bool isError { get; }
        public float downloadProgress { get; }
        public float uploadProgress { get; }
        public ulong downloadedBytes { get; }
        public ulong uploadedBytes { get; }
        public void SetRequestHeader(string name, string value) { }
        public string GetRequestHeader(string name) => null!;
        public string GetResponseHeader(string name) => null!;
        public System.Collections.Generic.Dictionary<string, string> GetResponseHeaders() => null!;
        public UnityWebRequestAsyncOperation SendWebRequest() => null!;
        public UnityWebRequestAsyncOperation Send() => null!;
        public void Abort() { }
        public void Dispose() { }
    }
    public static class UnityWebRequestTexture
    {
        public static UnityWebRequest GetTexture(string url) => null!;
        public static UnityWebRequest GetTexture(string url, bool readable) => null!;
    }
    public static class UnityWebRequestMultimedia
    {
        public static UnityWebRequest GetAudioClip(string url, UnityEngine.AudioType audioType) => null!;
    }
    public static class UnityWebRequestAssetBundle
    {
        public static UnityWebRequest GetAssetBundle(string url) => null!;
    }
}
