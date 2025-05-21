using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Outline : MonoBehaviour
{
    [SerializeField] Material outLineMat;
    [SerializeField] float outlineScale;
    [SerializeField] Color outlineColor;
    Renderer outlineRenderer;

    void Start()
    {
        outlineRenderer = CreateOutline(outLineMat, outlineScale, outlineColor);
        outlineRenderer.enabled = true;
    }

    Renderer CreateOutline(Material material, float scale, Color color)
    {
        GameObject outlineObject = Instantiate(gameObject, transform.position, transform.rotation, transform);
        Renderer rend = outlineObject.GetComponent<Renderer>();
        rend.material = material;
        rend.material.SetColor("OutlineColor", color);
        rend.material.SetFloat("Scale", scale);
        rend.shadowCastingMode = ShadowCastingMode.Off;

        outlineObject.GetComponent<Outline>().enabled = false;

        rend.enabled = false;
        return rend;
    }
}
