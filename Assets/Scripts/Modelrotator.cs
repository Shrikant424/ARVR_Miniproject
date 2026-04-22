using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
/// <summary>
/// Attach this to any persistent GameObject (e.g. ARModelLoader's GameObject).
/// It reads the currentModel reference from ARModelLoader every frame, so it
/// automatically works whenever a new model is loaded.
///
/// Gestures supported
/// ------------------
///   One-finger drag   rotate the model around its Y-axis (and optionally X)
///   Two-finger pinch  uniform scale (zoom in / out)
/// </summary>
public class ModelRotator : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Degrees rotated per pixel of finger drag")]
    public float rotationSpeed = 0.3f;
    public bool allowVerticalRotation = false;   // tilt up/down as well

    [Header("Pinch-to-Scale")]
    public bool allowPinchScale = true;
    public float scaleSpeed = 0.005f;
    public float minScale = 0.05f;
    public float maxScale = 5f;

    [Header("Source")]
    [Tooltip("Drag your ARModelLoader component here so we always have the latest model")]
    public ARModelLoader modelLoader;

    void Start()
    {
        modelLoader = FindObjectOfType<ARModelLoader>();
        Debug.Log("ModelLoader found: " + modelLoader);

    }
    //  private state 
    private float prevPinchDistance;
    private bool isPinching;
    private Vector2 prevTouchPos;
    private bool wasSingleTouch;

    //  helpers 
    private GameObject Target => modelLoader != null ? modelLoader.currentModel : null;




void OnEnable()
{
    EnhancedTouchSupport.Enable();
}

void OnDisable()
{
    EnhancedTouchSupport.Disable();
}

void Update()
{
    if (Target == null) return;

    var touches = Touch.activeTouches;
        Debug.Log($"Touch count: {Input.touchCount}");

        if (touches.Count == 2 && allowPinchScale)
    {
        wasSingleTouch = false;
        float currentDist = Vector2.Distance(
            touches[0].screenPosition,
            touches[1].screenPosition);

        if (!isPinching)
        {
            prevPinchDistance = currentDist;
            isPinching = true;
        }
        else
        {
            float delta = currentDist - prevPinchDistance;
            float newScale = Target.transform.localScale.x + delta * scaleSpeed;
            newScale = Mathf.Clamp(newScale, minScale, maxScale);
            Target.transform.localScale = Vector3.one * newScale;
            prevPinchDistance = currentDist;
        }
        return;
    }

    isPinching = false;

    if (touches.Count == 1)
    {
         if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(touches[0].touchId))
                return;
         var touch = touches[0];

        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            prevTouchPos = touch.screenPosition;
            wasSingleTouch = true;
            return;
        }

        if (wasSingleTouch && touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            Vector2 delta = touch.screenPosition - prevTouchPos;
            Target.transform.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
            if (allowVerticalRotation)
                Target.transform.Rotate(Vector3.right, delta.y * rotationSpeed, Space.World);
        }

        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
            touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            wasSingleTouch = false;

        prevTouchPos = touch.screenPosition;
    }
    else
    {
        wasSingleTouch = false;
    }
}
    ////  Update 
    //void Update()
    //{
    //    if (Target == null) return;
    //    Debug.Log("Target: " + Target);
    //    int touchCount = Input.touchCount;

    //    //  Two-finger pinch to scale 
    //    if (allowPinchScale && touchCount == 2)
    //    {
    //        wasSingleTouch = false;   // reset single-touch tracking

    //        Touch t0 = Input.GetTouch(0);
    //        Touch t1 = Input.GetTouch(1);

    //        float currentDist = Vector2.Distance(t0.position, t1.position);

    //        if (!isPinching)
    //        {
    //            prevPinchDistance = currentDist;
    //            isPinching = true;
    //        }
    //        else
    //        {
    //            float delta = currentDist - prevPinchDistance;
    //            float newScale = Target.transform.localScale.x + delta * scaleSpeed;
    //            newScale = Mathf.Clamp(newScale, minScale, maxScale);
    //            Target.transform.localScale = Vector3.one * newScale;
    //            prevPinchDistance = currentDist;
    //        }
    //        return;   // don't also rotate while pinching
    //    }

    //    isPinching = false;   // reset pinch state when fingers lift
    //    Debug.Log("Touch Count: " + Input.touchCount);
    //    //  One-finger drag to rotate 
    //    if (touchCount == 1)
    //    {
    //        if (EventSystem.current != null &&
    //EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
    //            return;
    //        Touch touch = Input.GetTouch(0);

    //        if (!wasSingleTouch || touch.phase == TouchPhase.Began)
    //        {
    //            prevSingleTouch = touch;
    //            wasSingleTouch = true;
    //            return;
    //        }

    //        if (touch.phase == TouchPhase.Moved)
    //        {
    //            Debug.Log("Touch moved detected");

    //            Vector2 delta = touch.position - prevSingleTouch.position;
    //            Debug.Log("Delta: " + delta);

    //            float rotY = -delta.x * rotationSpeed;
    //            Debug.Log("RotY: " + rotY);

    //            Target.transform.Rotate(Vector3.up, rotY, Space.World);
    //            if (allowVerticalRotation)
    //            {
    //                float rotX = delta.y * rotationSpeed;
    //                Target.transform.Rotate(Vector3.up, rotX, Space.Self);
    //            }
    //        }

    //        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
    //            wasSingleTouch = false;

    //        prevSingleTouch = touch;
    //    }
    //    else
    //    {
    //        wasSingleTouch = false;
    //    }
    //}
}