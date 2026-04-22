//using UnityEngine;
//using UnityEngine.Networking;
//using System.Collections;
//using GLTFast;

//public class ARModelLoader : MonoBehaviour
//{
//    [Header("Model Size")]
//    [Tooltip("How tall/wide the model should appear in the real world (meters)")]
//    public float targetSizeInMeters = 0.3f; // 30 cm — tweak this in Inspector
//    public Transform spawnPoint;
//    public ColorPicker colorPicker;
//    public GameObject loadingScreen;
//    public GameObject scanAgainButton;

//    [HideInInspector]
//    public GameObject currentModel;

//    void Start()
//    {
//        if (scanAgainButton != null)
//            scanAgainButton.SetActive(false);
//    }

//    public void LoadModel(string modelName)
//    {
//        StartCoroutine(LoadCoroutine(modelName));
//    }
//    void AutoScaleModel(GameObject model, float targetSize)
//    {
//        // Measure the bounding box of all renderers combined
//        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
//        if (renderers.Length == 0)
//        {
//            Debug.LogWarning("No renderers found on model — using default scale.");
//            model.transform.localScale = Vector3.one * targetSize;
//            return;
//        }

//        Bounds bounds = renderers[0].bounds;
//        foreach (Renderer r in renderers)
//            bounds.Encapsulate(r.bounds);

//        // Use the largest dimension so the model fits in a cube of targetSize
//        float largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

//        if (largestDimension <= 0f)
//        {
//            Debug.LogWarning("Model has zero size bounds.");
//            return;
//        }

//        float scaleFactor = targetSize / largestDimension;
//        model.transform.localScale = Vector3.one * scaleFactor;

//        Debug.Log($"Model bounds: {bounds.size} | Scale applied: {scaleFactor}");
//    }

//    IEnumerator LoadCoroutine(string modelName)
//    {
//        if (loadingScreen != null)
//            loadingScreen.SetActive(true);

//        string path = System.IO.Path.Combine(
//            Application.streamingAssetsPath,
//            modelName + ".glb"
//        );

//        Debug.Log("Loading model from path: " + path);

//        UnityWebRequest request = UnityWebRequest.Get(path);
//        yield return request.SendWebRequest();

//        if (request.result != UnityWebRequest.Result.Success)
//        {
//            Debug.LogError("Failed to load GLB: " + request.error);
//            Debug.LogError("Path was: " + path);

//            if (loadingScreen != null)
//                loadingScreen.SetActive(false);

//            yield break;
//        }

//        byte[] glbData = request.downloadHandler.data;

//        var gltf = new GltfImport();
//        var loadTask = gltf.LoadGltfBinary(glbData);
//        yield return new WaitUntil(() => loadTask.IsCompleted);

//        if (!loadTask.Result)
//        {
//            Debug.LogError("GLTFast failed to parse GLB file: " + modelName);

//            if (loadingScreen != null)
//                loadingScreen.SetActive(false);

//            yield break;
//        }

//        if (currentModel != null)
//            Destroy(currentModel);

//        currentModel = new GameObject(modelName);

//        Camera arCamera = Camera.main;
//        Vector3 spawnPosition = arCamera.transform.position +
//                                arCamera.transform.forward * 2f;

//        //currentModel.transform.position = spawnPosition;
//        //currentModel.transform.rotation = Quaternion.identity;
//        //currentModel.transform.localScale = new Vector3(1f, 1f, 1f);

//        //var instantiateTask = gltf.InstantiateMainSceneAsync(
//        //    currentModel.transform
//        //);
//        //yield return new WaitUntil(() => instantiateTask.IsCompleted);

//        currentModel.transform.position = spawnPosition;
//        currentModel.transform.rotation = Quaternion.identity;
//        currentModel.transform.localScale = Vector3.one; // reset first

//        var instantiateTask = gltf.InstantiateMainSceneAsync(currentModel.transform);
//        yield return new WaitUntil(() => instantiateTask.IsCompleted);

//        AutoScaleModel(currentModel, targetSizeInMeters);

//        Debug.Log("Model loaded successfully: " + modelName);

//        if (loadingScreen != null)
//            loadingScreen.SetActive(false);
//        // StartCoroutine(ColorExtractor.CaptureAndApply(currentModel));

//        if (colorPicker != null)
//            colorPicker.SetTarget(currentModel);

//        if (scanAgainButton != null)
//            scanAgainButton.SetActive(true);
//    }
//}

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using GLTFast;

public class ARModelLoader : MonoBehaviour
{
    [Header("Model Size")]
    [Tooltip("How tall/wide the model should appear in the real world (meters)")]
    public float targetSizeInMeters = 0.3f;

    [Header("Spawn")]
    [Tooltip("How far in front of the camera the model spawns (meters)")]
    public float spawnDistance = 1.5f;

    public Transform spawnPoint;
    public ColorPicker colorPicker;
    public GameObject loadingScreen;
    public GameObject scanAgainButton;

    [HideInInspector]
    public GameObject currentModel;

    void Start()
    {
        if (scanAgainButton != null)
            scanAgainButton.SetActive(false);
    }

    public void LoadModel(string modelName)
    {
        StartCoroutine(LoadCoroutine(modelName));
    }

    public void DetachFromTracking()
    {
        if (currentModel == null) return;
        currentModel.transform.SetParent(null);
        DontDestroyOnLoad(currentModel);
        Debug.Log("Model detached from tracking — will stay visible.");
    }

    void AutoScaleModel(GameObject model, float targetSize)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found on model — using default scale.");
            model.transform.localScale = Vector3.one * targetSize;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        float largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        if (largestDimension <= 0f)
        {
            Debug.LogWarning("Model has zero size bounds.");
            return;
        }

        float scaleFactor = targetSize / largestDimension;
        model.transform.localScale = Vector3.one * scaleFactor;

        Debug.Log($"Model bounds: {bounds.size} | Scale applied: {scaleFactor}");
    }

    IEnumerator LoadCoroutine(string modelName)
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        string path = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            modelName + ".glb"
        );

        Debug.Log("Loading model from path: " + path);

        UnityWebRequest request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load GLB: " + request.error);
            Debug.LogError("Path was: " + path);

            if (loadingScreen != null)
                loadingScreen.SetActive(false);

            yield break;
        }

        byte[] glbData = request.downloadHandler.data;

        var gltf = new GltfImport();
        var loadTask = gltf.LoadGltfBinary(glbData);
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (!loadTask.Result)
        {
            Debug.LogError("GLTFast failed to parse GLB file: " + modelName);

            if (loadingScreen != null)
                loadingScreen.SetActive(false);

            yield break;
        }

        if (currentModel != null)
            Destroy(currentModel);

        currentModel = new GameObject(modelName);

        Camera arCamera = Camera.main;

        Vector3 spawnPosition = arCamera.transform.position +
                                arCamera.transform.forward * spawnDistance;

        spawnPosition.y = arCamera.transform.position.y - 0.2f;

        currentModel.transform.position = spawnPosition;
        currentModel.transform.localScale = Vector3.one;

        Vector3 lookDir = currentModel.transform.position - arCamera.transform.position;
        lookDir.y = 0; 
        if (lookDir != Vector3.zero)
            currentModel.transform.rotation = Quaternion.LookRotation(-lookDir);
        else
            currentModel.transform.rotation = Quaternion.identity;

        var instantiateTask = gltf.InstantiateMainSceneAsync(currentModel.transform);
        yield return new WaitUntil(() => instantiateTask.IsCompleted);

        AutoScaleModel(currentModel, targetSizeInMeters);

        DetachFromTracking();

        Debug.Log("Model loaded successfully: " + modelName);

        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        if (colorPicker != null)
            colorPicker.SetTarget(currentModel);

        if (scanAgainButton != null)
            scanAgainButton.SetActive(true);
    }
}