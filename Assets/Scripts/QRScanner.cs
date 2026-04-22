////using UnityEngine;
////using ZXing;
////using System.Collections;

////public class QRScanner : MonoBehaviour
////{
////    private WebCamTexture camTexture;
////    private IBarcodeReader reader;

////    public ARModelLoader loader;

////    private bool hasScanned = false;

////    IEnumerator Start()
////    {
////        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

////        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
////        {
////            Debug.LogError("Camera permission denied. Cannot start QR scanner.");
////            yield break;
////        }

////        camTexture = new WebCamTexture();
////        camTexture.Play();

////        reader = new BarcodeReader();
////        StartCoroutine(Scan());
////    }

////    IEnumerator Scan()
////    {
////        while (!hasScanned)
////        {
////            if (camTexture.width > 100)
////            {
////                Color32[] pixels = camTexture.GetPixels32();

////                if (pixels != null && pixels.Length > 0)
////                {
////                    try
////                    {
////                        var result = reader.Decode(pixels, camTexture.width, camTexture.height);

////                        if (result != null && !string.IsNullOrEmpty(result.Text))
////                        {
////                            Debug.Log("QR Code Scanned: " + result.Text);
////                            hasScanned = true;
////                            camTexture.Stop();
////                            loader.LoadModel(result.Text.Trim());
////                        }
////                    }
////                    catch (System.Exception e)
////                    {
////                        Debug.LogWarning("ZXing decode error: " + e.Message);
////                    }
////                }
////            }

////            yield return new WaitForSeconds(0.5f);
////        }
////    }

////    void OnDestroy()
////    {
////        if (camTexture != null && camTexture.isPlaying)
////            camTexture.Stop();
////    }
////}
//using UnityEngine;
//using UnityEngine.XR.ARFoundation;
//using ZXing;
//public class QRScanner : MonoBehaviour
//{
//    private ARCameraManager arCameraManager;
//    public ARModelLoader loader;
//    private IBarcodeReader reader = new BarcodeReader();
//    private bool hasScanned = false;
//    private int frameSkip = 0;
//    void Awake() { arCameraManager = GetComponent<ARCameraManager>(); }
//    void OnEnable() { if (arCameraManager != null) arCameraManager.frameReceived += OnFrame; }
//    void OnDisable() { if (arCameraManager != null) arCameraManager.frameReceived -= OnFrame; }
//    public void ResetScanner()
//    {
//        hasScanned = false;
//        frameSkip = 0;
//        Debug.Log("Scanner reset - ready to scan again");
//    }
//    private void OnFrame(ARCameraFrameEventArgs eventArgs)
//    {
//        if (hasScanned) return;
//        // Only scan every 20 frames to keep the phone from overheating/lagging
//        frameSkip++;
//        if (frameSkip % 20 != 0) return;
//        if (!arCameraManager.TryAcquireLatestCpuImage(out var image)) return;
//        using (image)
//        {
//            var conversion = new UnityEngine.XR.ARSubsystems.XRCpuImage.ConversionParams
//            {
//                inputRect = new RectInt(0, 0, image.width, image.height),
//                outputDimensions = new Vector2Int(image.width / 4, image.height / 4), // Small image = Fast scan
//                outputFormat = TextureFormat.RGBA32
//            };
//            int size = image.GetConvertedDataSize(conversion);
//            using (var buffer = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.Temp))
//            {
//                image.Convert(conversion, buffer);
//                var result = reader.Decode(buffer.ToArray(), conversion.outputDimensions.x, conversion.outputDimensions.y, RGBLuminanceSource.BitmapFormat.RGBA32);
//                if (result != null)
//                {
//                    hasScanned = true;
//                    Handheld.Vibrate(); // Vibration lets you know it worked!
//                    loader.LoadModel(result.Text.Trim());
//                }
//            }
//        }
//    }
//}
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ZXing;

public class QRScanner : MonoBehaviour
{
    private ARCameraManager arCameraManager;
    public ARModelLoader loader;
    private IBarcodeReader reader = new BarcodeReader();
    private bool hasScanned = false;
    private int frameSkip = 0;

    void Awake()
    {
        arCameraManager = GetComponent<ARCameraManager>();
        Debug.Log("QRScanner Awake - arCameraManager: " +
            (arCameraManager == null ? "NULL" : "found"));
    }

    void OnEnable()
    {
        if (arCameraManager != null)
            arCameraManager.frameReceived += OnFrame;
        Debug.Log("QRScanner enabled and listening for frames");
    }

    void OnDisable()
    {
        if (arCameraManager != null)
            arCameraManager.frameReceived -= OnFrame;
    }

    public void ResetScanner()
    {
        hasScanned = false;
        frameSkip = 0;
        Debug.Log("Scanner reset");
    }

    private void OnFrame(ARCameraFrameEventArgs eventArgs)
    {
        if (hasScanned) return;

        frameSkip++;
        if (frameSkip % 60 != 0) return;

        // Debug: confirm scanning is happening
        Debug.Log("Attempting QR scan frame: " + frameSkip);

        if (arCameraManager == null)
        {
            Debug.LogError("arCameraManager is null!");
            return;
        }

        if (!arCameraManager.TryAcquireLatestCpuImage(out var image))
        {
            Debug.LogWarning("Could not acquire camera image");
            return;
        }

        using (image)
        {
            try
            {
                var conversion = new UnityEngine.XR.ARSubsystems.XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(image.width / 4, image.height / 4),
                    outputFormat = TextureFormat.RGBA32
                };

                int size = image.GetConvertedDataSize(conversion);

                using (var buffer = new Unity.Collections.NativeArray<byte>(
                    size, Unity.Collections.Allocator.Temp))
                {
                    image.Convert(conversion, buffer);

                    var result = reader.Decode(
                        buffer.ToArray(),
                        conversion.outputDimensions.x,
                        conversion.outputDimensions.y,
                        RGBLuminanceSource.BitmapFormat.RGBA32
                    );

                    //  Debug: show decode result every attempt
                    Debug.Log("Decode result: " +
                        (result == null ? "NULL - no QR found" : result.Text));

                    if (result != null)
                    {
                        hasScanned = true;
                        Debug.Log("QR SUCCESS: " + result.Text);
                        Handheld.Vibrate();
                        loader.LoadModel(result.Text.Trim());
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("QR scan error: " + e.Message);
            }
        }
    }
}