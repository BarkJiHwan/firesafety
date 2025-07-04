using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurvedUIRender : MonoBehaviour
{
    // UI 전용 레이어
    [SerializeField] LayerMask UILayer;

    // UI 출력용 RenderTexture
    public RenderTexture outputTexture;
    // UI를 렌더링할 전용 카메라
    [SerializeField] Camera UICamera;
    // 렌더링할 UI가 담긴 캔버스
    [SerializeField] Canvas canvas;
    // UI를 입힐 곡면 메시
    [SerializeField] GameObject meshSurface;
    // 메시의 Material
    [SerializeField] Material curvedUIMaterial;

    Vector2 renderSize;
    void Awake()
    {
        // RenderTexture 해상도 설정
        renderSize = new Vector2(2560,1440);
        SetRenderTextureUI();
    }

    void SetRenderTextureUI()
    {
        // RenderTexture 생성 및 설정
        outputTexture = new RenderTexture((int)renderSize.x, (int)renderSize.y, 16);
        //outputTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
        outputTexture.useMipMap = false;
        outputTexture.autoGenerateMips = false;
        outputTexture.antiAliasing = 4;
        outputTexture.name = "CurvedUI_RenderTexture";
        outputTexture.useMipMap = false;
        // 픽셀 단위 필터링
        outputTexture.filterMode = FilterMode.Point;
        // 이방성 필터링 레벨
        outputTexture.anisoLevel = 1;
        // 생성
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

        // UI 카메라의 출력 타겟을 RenderTexture로 설정
        UICamera.targetTexture = outputTexture;

        //canvas.worldCamera = UICamera;
        // 곡면 메시 크기 조절
        meshSurface.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        if(meshSurface != null)
        {
            var mr = meshSurface.GetComponent<MeshRenderer>();
            //var mat = new Material(curvedUIMaterial);
            // 만든 Material 적용 (투명하게 하는 것)
            mr.material = curvedUIMaterial;
            var mat = mr.material;
            //var mat = new Material(Shader.Find("Unlit/Transparent"));
            //mat.mainTexture = outputTexture;

            // 출력된 UI Texture를 Material에 할당
            mat.SetTexture("_MainTex", outputTexture);
            //mat.SetTexture("_BaseMap", outputTexture);

            // Material 적용
            mr.material = mat;
        }
    }

}
