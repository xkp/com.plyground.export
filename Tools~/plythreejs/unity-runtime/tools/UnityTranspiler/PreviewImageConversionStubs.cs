namespace UnityEngine
{
    public static class ImageConversion
    {
        public static byte[] EncodeToPNG(this Texture2D texture) => [];
        public static byte[] EncodeToJPG(this Texture2D texture, int quality = 75) => [];
        public static byte[] EncodeToEXR(this Texture2D texture, params object[] args) => [];
        public static byte[] EncodeToTGA(this Texture2D texture) => [];
        public static bool LoadImage(this Texture2D texture, byte[] data, bool markNonReadable = false) => false;
        public static byte[] EncodeArrayToPNG(params object[] args) => [];
        public static byte[] EncodeArrayToJPG(params object[] args) => [];
        public static byte[] EncodeArrayToEXR(params object[] args) => [];
        public static byte[] EncodeArrayToTGA(params object[] args) => [];
        public static byte[] EncodeArrayToR2D(params object[] args) => [];
        public static byte[] EncodeNativeArrayToPNG(params object[] args) => [];
        public static byte[] EncodeNativeArrayToJPG(params object[] args) => [];
        public static byte[] EncodeNativeArrayToEXR(params object[] args) => [];
        public static byte[] EncodeNativeArrayToTGA(params object[] args) => [];
        public static byte[] EncodeNativeArrayToR2D(params object[] args) => [];
    }
}
