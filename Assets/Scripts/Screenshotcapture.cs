using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ScreenshotCapture : MonoBehaviour
{
    [Header("UI References")]
    public Button captureButton;
    public GameObject flashOverlay;       // A full-screen white Image (CanvasGroup alpha)
    public Text savedNotification;        // Optional "Saved!" text

    [Header("Settings")]
    public float flashDuration = 0.15f;
    public float notificationDuration = 2f;

    void Start()
    {
        if (captureButton != null)
            captureButton.onClick.AddListener(TakeScreenshot);

        if (flashOverlay != null)
            flashOverlay.SetActive(false);

        if (savedNotification != null)
            savedNotification.gameObject.SetActive(false);
    }

    public void TakeScreenshot()
    {
        StartCoroutine(CaptureRoutine());
    }

    IEnumerator CaptureRoutine()
    {
        //// Brief camera shutter flash
        //if (flashOverlay != null)
        //{
        //    flashOverlay.SetActive(true);
        //    yield return new WaitForSeconds(flashDuration);
        //    flashOverlay.SetActive(false);
        //}

        // Wait for end of frame so the rendered frame is complete
        yield return new WaitForEndOfFrame();

        // Build a unique filename with timestamp
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = "ARCapture_" + timestamp + ".png";

#if UNITY_ANDROID
        string folder = "/storage/emulated/0/DCIM/ARCaptures/";
#elif UNITY_IOS
        string folder = Application.persistentDataPath + "/";
#else
        string folder = Application.persistentDataPath + "/Screenshots/";
#endif

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string fullPath = Path.Combine(folder, filename);

        // Capture the screen (includes AR feed + 3-D model)
        //ScreenCapture.CaptureScreenshot(fullPath);


        // Wait for end of frame so everything is rendered
        yield return new WaitForEndOfFrame();

        // ---------------- CAPTURE FIRST ----------------
        Texture2D screenImage = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenImage.Apply();

        byte[] imageBytes = screenImage.EncodeToPNG();
        File.WriteAllBytes(fullPath, imageBytes);

        Destroy(screenImage);
        Debug.Log("Screenshot saved to: " + fullPath);

        // ---------------- THEN FLASH ----------------
        if (flashOverlay != null)
        {
            flashOverlay.SetActive(true);
            yield return new WaitForSeconds(flashDuration);
            flashOverlay.SetActive(false);
        }
#if UNITY_ANDROID
        // Notify the Android media scanner so the image appears in the gallery
        using (AndroidJavaClass mediaScanner =
            new AndroidJavaClass("android.media.MediaScannerConnection"))
        {
            using (AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity =
                    unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                mediaScanner.CallStatic("scanFile",
                    activity,
                    new string[] { fullPath },
                    null,
                    null);
            }
        }
        Debug.Log("Android gallery scan triggered.");
#endif

        // Show "Saved!" notification
        if (savedNotification != null)
        {
            savedNotification.text = "Saved to Gallery!";
            savedNotification.gameObject.SetActive(true);
            yield return new WaitForSeconds(notificationDuration);
            savedNotification.gameObject.SetActive(false);
        }
    }
}