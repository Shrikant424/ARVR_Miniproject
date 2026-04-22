using UnityEngine;
using System.Collections;

public class ColorExtractor : MonoBehaviour
{
    public static IEnumerator CaptureAndApply(GameObject model)
    {
        yield return new WaitForEndOfFrame();

        Texture2D tex = null;

        try
        {
            tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();

            Color avg = GetAverageColor(tex);
            Debug.Log("Extracted color: " + avg);

            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
            {
                foreach (Material m in r.materials)
                {
                    if (m.HasProperty("_BaseColor"))
                        m.SetColor("_BaseColor", avg);
                    else if (m.HasProperty("_Color"))
                        m.SetColor("_Color", avg);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("ColorExtractor error: " + e.Message);
        }
        finally
        {
            if (tex != null)
                Destroy(tex);
        }
    }

    static Color GetAverageColor(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();
        float r = 0f, g = 0f, b = 0f;
        int count = pixels.Length;

        for (int i = 0; i < count; i++)
        {
            r += pixels[i].r;
            g += pixels[i].g;
            b += pixels[i].b;
        }

        return new Color(r / count, g / count, b / count, 1f);
    }
}