using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARSessionChecker : MonoBehaviour
{
    [SerializeField] private ARSession arSession;

    IEnumerator Start()
    {
        Debug.Log("Checking AR Support...");

        // Check if AR is supported on this device
        if (ARSession.state == ARSessionState.None ||
            ARSession.state == ARSessionState.CheckingAvailability)
        {
            yield return ARSession.CheckAvailability();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            Debug.LogError("AR is NOT supported on this device.");
            yield break;
        }

        Debug.Log("AR Supported! Starting session...");

        // Enable AR Session if it isnt already running
        if (arSession != null)
        {
            arSession.enabled = true;
        }

        // Wait until AR Session is fully ready
        float timeout = 10f;
        float elapsed = 0f;

        while (ARSession.state != ARSessionState.SessionTracking && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            Debug.Log("AR State: " + ARSession.state);
            yield return null;
        }

        if (ARSession.state == ARSessionState.SessionTracking)
        {
            Debug.Log("AR Session is READY and tracking!");
        }
        else
        {
            Debug.LogWarning("AR Session did not start in time. State: " + ARSession.state);
        }
    }
}
