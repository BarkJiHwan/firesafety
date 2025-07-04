using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Outline : MonoBehaviour
{
    // 외곽선에 사용할 Material, 스케일, 색상을 인스펙터에서 설정할 수 있게 함
    [SerializeField] Material outLineMat;
    [SerializeField] float outlineScale;
    [SerializeField] Color outlineColor;
    Renderer outlineRenderer;

    void Start()
    {
        // 시작 시 외곽선 오브젝트를 생성하고 Renderer를 가져옴
        outlineRenderer = CreateOutline(outLineMat, outlineScale, outlineColor);

        // 외곽선 Renderer를 활성화
        outlineRenderer.enabled = true;
    }

    Renderer CreateOutline(Material material, float scale, Color color)
    {
        // 현재 오브젝트를 복제해서 외곽선용 오브젝트 생성
        GameObject outlineObject = Instantiate(gameObject, transform.position, transform.rotation, transform);
        Renderer rend = outlineObject.GetComponent<Renderer>();
        // 외곽선 전용 Material로 교체
        rend.material = material;
        // Material의 속성 설정 : 색상과 스케일
        rend.material.SetColor("OutlineColor", color);
        rend.material.SetFloat("Scale", scale);

        // 외곽선은 그림자를 캐스팅하지 않도록 설정
        rend.shadowCastingMode = ShadowCastingMode.Off;

        // 복제된 오브젝트의 Outline 스크립트는 비활성화 (무한 복제 방지 목적)
        outlineObject.GetComponent<Outline>().enabled = false;

        // 처음엔 외곽선 Renderer 비활성화
        rend.enabled = false;
        return rend;
    }
}
