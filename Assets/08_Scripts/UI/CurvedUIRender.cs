using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurvedUIRender : MonoBehaviour
{
    [SerializeField] LayerMask UILayer;

    public RenderTexture outputTexture;
    [SerializeField] Camera UICamera;
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject meshSurface;
    [SerializeField] Material curvedUIMaterial;

    Vector2 renderSize;
    void Awake()
    {
        renderSize = new Vector2(1024,512);
        SetRenderTextureUI();
    }

    void SetRenderTextureUI()
    {
        outputTexture = new RenderTexture((int)renderSize.x, (int)renderSize.y, 16);
        //outputTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
        outputTexture.useMipMap = false;
        outputTexture.autoGenerateMips = false;
        outputTexture.antiAliasing = 4;
        outputTexture.name = "CurvedUI_RenderTexture";
        outputTexture.useMipMap = false;
        outputTexture.filterMode = FilterMode.Bilinear;
        outputTexture.Create();

        //GameObject camObj = new GameObject("UI Render Camera");
        //// 카메라 Y축 위치 수정
        //camObj.transform.position = new Vector3(0, 0.9f, -10);
        //UICamera = camObj.AddComponent<Camera>();
        //UICamera.clearFlags = CameraClearFlags.SolidColor;
        //UICamera.backgroundColor = Color.gray;
        //UICamera.backgroundColor = new Color(0, 0, 0, 0);
        //UICamera.cullingMask = UILayer;
        //UICamera.orthographic = true;
        //UICamera.orthographicSize = 0.87f;
        UICamera.targetTexture = outputTexture;

        //canvas.worldCamera = UICamera;
        meshSurface.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        if(meshSurface != null)
        {
            var mr = meshSurface.GetComponent<MeshRenderer>();
            //var mat = new Material(curvedUIMaterial);
            mr.material = curvedUIMaterial;
            var mat = mr.material;
            //var mat = new Material(Shader.Find("Unlit/Transparent"));
            //mat.mainTexture = outputTexture;
            mat.SetTexture("_MainTex", outputTexture);
            //mat.SetTexture("_BaseMap", outputTexture);
            mr.material = mat;
        }
    }

}
