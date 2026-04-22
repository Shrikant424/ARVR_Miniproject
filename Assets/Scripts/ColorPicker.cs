//using UnityEngine;
//using UnityEngine.UI;

//public class ColorPicker : MonoBehaviour
//{
//    public Slider sliderR;
//    public Slider sliderG;
//    public Slider sliderB;

//    public Image colorPreview;

//    private GameObject target;

//    void Start()
//    {
//        sliderR.onValueChanged.AddListener(_ => ApplyColor());
//        sliderG.onValueChanged.AddListener(_ => ApplyColor());
//        sliderB.onValueChanged.AddListener(_ => ApplyColor());

//        sliderR.value = 1f;
//        sliderG.value = 1f;
//        sliderB.value = 1f;
//    }

//    //public void SetTarget(GameObject newTarget)
//    //{
//    //    target = newTarget;
//    //    ApplyColor();
//    //}

//    public void SetTarget(GameObject newTarget)
//    {
//        target = newTarget;

//        sliderR.onValueChanged.RemoveAllListeners();
//        sliderG.onValueChanged.RemoveAllListeners();
//        sliderB.onValueChanged.RemoveAllListeners();

//        sliderR.value = 1f;
//        sliderG.value = 1f;
//        sliderB.value = 1f;

//        sliderR.onValueChanged.AddListener(_ => ApplyColor());
//        sliderG.onValueChanged.AddListener(_ => ApplyColor());
//        sliderB.onValueChanged.AddListener(_ => ApplyColor());

//    }

//    void ApplyColor()
//    {
//        if (target == null) return;
//        Debug.Log("Target: " + target.name);

//        Color c = new Color(sliderR.value, sliderG.value, sliderB.value, 1f);

//        if (colorPreview != null)
//            colorPreview.color = c;

//        foreach (Renderer rend in target.GetComponentsInChildren<Renderer>())
//        {
//            foreach (Material mat in rend.materials)
//            {
//                for (int i = 0; i < mat.shader.GetPropertyCount(); i++)
//                {
//                    Debug.Log("Property: " + mat.shader.GetPropertyName(i));
//                }

//                Debug.Log("Shader name: " + mat.shader.name); // ADD THIS

//                if (mat.HasProperty("baseColorFactor"))
//                    mat.SetColor("baseColorFactor", c);
//                else if (mat.HasProperty("_BaseColor"))
//                    mat.SetColor("_BaseColor", c);
//                else if (mat.HasProperty("_Color"))
//                    mat.SetColor("_Color", c);
//            }
//        }
//    }

//    public void ResetToWhite()
//    {
//        sliderR.value = 1f;
//        sliderG.value = 1f;
//        sliderB.value = 1f;
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ColorPicker : MonoBehaviour
{
    public Slider sliderR;
    public Slider sliderG;
    public Slider sliderB;
    public Image colorPreview;

    private GameObject target;

    private struct MaterialState
    {
        public Color originalColor;
        public Texture originalTexture;
        public string colorProperty;
        public string textureProperty;
    }

    private List<(Material mat, MaterialState state)> savedMaterials = new();

    void Start()
    {
        sliderR.onValueChanged.AddListener(_ => ApplyColor());
        sliderG.onValueChanged.AddListener(_ => ApplyColor());
        sliderB.onValueChanged.AddListener(_ => ApplyColor());
        sliderR.value = 1f;
        sliderG.value = 1f;
        sliderB.value = 1f;
    }

    public void SetTarget(GameObject newTarget)
    {
        target = newTarget;

        sliderR.onValueChanged.RemoveAllListeners();
        sliderG.onValueChanged.RemoveAllListeners();
        sliderB.onValueChanged.RemoveAllListeners();

        sliderR.value = 1f;
        sliderG.value = 1f;
        sliderB.value = 1f;

        sliderR.onValueChanged.AddListener(_ => ApplyColor());
        sliderG.onValueChanged.AddListener(_ => ApplyColor());
        sliderB.onValueChanged.AddListener(_ => ApplyColor());

        SaveOriginalMaterials();
    }

    void SaveOriginalMaterials()
    {
        savedMaterials.Clear();

        if (target == null) return;

        foreach (Renderer rend in target.GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in rend.materials)
            {
                MaterialState state = new MaterialState();

                if (mat.HasProperty("_BaseColor"))
                {
                    state.colorProperty = "_BaseColor";
                    state.textureProperty = "_BaseMap";
                    state.originalColor = mat.GetColor("_BaseColor");
                    state.originalTexture = mat.HasProperty("_BaseMap")
                        ? mat.GetTexture("_BaseMap") : null;
                }
                else if (mat.HasProperty("_Color"))
                {
                    state.colorProperty = "_Color";
                    state.textureProperty = "_MainTex";
                    state.originalColor = mat.GetColor("_Color");
                    state.originalTexture = mat.HasProperty("_MainTex")
                        ? mat.GetTexture("_MainTex") : null;
                }
                else if (mat.HasProperty("baseColorFactor"))
                {
                    state.colorProperty = "baseColorFactor";
                    state.textureProperty = "baseColorTexture";
                    state.originalColor = mat.GetColor("baseColorFactor");
                    state.originalTexture = mat.HasProperty("baseColorTexture")
                        ? mat.GetTexture("baseColorTexture") : null;
                }
                else
                {
                    state.colorProperty = null;
                    state.originalColor = Color.white;
                    state.originalTexture = null;
                }

                savedMaterials.Add((mat, state));

                Debug.Log($"Saved material: {mat.name} | " +
                          $"Color prop: {state.colorProperty} | " +
                          $"Original color: {state.originalColor} | " +
                          $"Has texture: {state.originalTexture != null}");
            }
        }
    }

    void ApplyColor()
    {
        if (target == null) return;

        Color c = new Color(sliderR.value, sliderG.value, sliderB.value, 1f);

        if (colorPreview != null)
            colorPreview.color = c;

        foreach (var (mat, state) in savedMaterials)
        {
            if (mat == null || state.colorProperty == null) continue;

            Color tinted = state.originalColor * c;
            tinted.a = state.originalColor.a; 
            mat.SetColor(state.colorProperty, tinted);
        }
    }

    public void ResetToWhite()
    {
        sliderR.value = 1f;
        sliderG.value = 1f;
        sliderB.value = 1f;

        if (colorPreview != null)
            colorPreview.color = Color.white;
    }
}