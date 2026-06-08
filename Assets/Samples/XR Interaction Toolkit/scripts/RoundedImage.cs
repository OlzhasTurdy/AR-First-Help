using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RoundedImage : MonoBehaviour
{
    [Range(0f, 0.5f)]
    public float radius = 0.15f;

    private Image image;
    private Material mat;

    void Awake()
    {
        image = GetComponent<Image>();
        // Создаём копию материала чтобы не менять общий
        mat = new Material(Shader.Find("UI/RoundedCorners"));
        image.material = mat;
    }

    void OnValidate()
    {
        if (mat != null)
            mat.SetFloat("_Radius", radius);
    }

    void Update()
    {
        if (mat != null)
            mat.SetFloat("_Radius", radius);
    }
}