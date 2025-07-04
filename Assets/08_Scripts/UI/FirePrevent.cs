using System;
using System.Collections;
using UnityEngine;

public partial class FirePreventable : MonoBehaviour
{
    public FirePreventableObject fireObject { get; set; }
    bool isHaveChild;
    Renderer childRend;
    bool isAlreadyHandled = false;

    public event Action OnAlreadyPrevented;
    public event Action OnHaveToPrevented;

    public bool isAlreadyOn { get; set; } = false;

    void SetMaterial()
    {
        _renderer = GetComponent<Renderer>();
        // 예방 가능한 오브젝트에 새로운 Material(아웃라인, 빛나는 거) 생성
        arrMat = new Material[2];
        arrMat[0] = Resources.Load<Material>("Materials/OutlineMat");
        arrMat[1] = Resources.Load<Material>("Materials/OriginMat");

        // 기존의 메테리얼 저장
        originMats = new Material[_renderer.materials.Length];
        originMats = _renderer.materials;

        // 자식 오브젝트가 같은 레이어면 처리
        if (transform.childCount > 0 && transform.GetChild(0).gameObject.layer == gameObject.layer)
        {
            isHaveChild = true;
            childRend = transform.GetChild(0).GetComponent<Renderer>();
            // 기존의 자식 오브젝트 Material 저장
            originChildMats = new Material[childRend.materials.Length];
            originChildMats = childRend.materials;
        }
    }

    public void SetMaterials(Renderer rend, bool isActive)
    {
        // Material 속성 값 변경
        foreach (var mat in rend.materials)
        {
            // 해당 Material 아웃라인, 빛나는거 키고 끄는 것
            if (mat.HasProperty("_isNearPlayer"))
            {
                //Debug.Log(mat.GetFloat("_isNearPlayer"));
                mat.SetFloat("_isNearPlayer", isActive ? 1f : 0f);
            }
        }
    }

    public void SetActiveOnMaterials(bool isActive)
    {
        SetMaterials(_renderer, isActive);
        if (isHaveChild == true)
        {
            SetMaterials(childRend, isActive);
        }
    }

    public bool GetHighlightProperty()
    {
        bool isHaveProperty = false;
        // Renderer에 "_RimPower" 속성 존재 여부 확인
        foreach (var mat in _renderer.materials)
        {
            if (mat.HasProperty("_RimPower"))
            {
                isHaveProperty = true;
                break;
            }
            else
            {
                isHaveProperty = false;
            }
        }
        return isHaveProperty;
    }

    public bool GetNearPlayer()
    {
        // _isNearPlayer 프로퍼티 값 반환
        foreach(var mat in _renderer.materials)
        {
            if(mat.HasProperty("_isNearPlayer"))
            {
                float property = mat.GetFloat("_isNearPlayer");
                if(property >= 1)
                {
                    return true;
                }
            }
        }
        return false;
    }

    void SetHighlightStronger(Renderer rend, float interValue)
    {
        // 하이라이트 세기 조절
        if (rend.materials.Length < 2)
        {
            Debug.Log(rend.gameObject.name);
            return;
        }
        Material highlightMat = rend.materials[1];
        if (highlightMat.HasProperty("_RimPower"))
        {
            // 근접 시 밝아지고 멀어지면 어두어짐
            float rimPower = Mathf.Lerp(2, -0.8f, interValue);
            highlightMat.SetFloat("_RimPower", rimPower);
        }
    }

    public void SetHighlight(float interValue)
    {
        SetHighlightStronger(_renderer, interValue);
        if (isHaveChild == true)
        {
            SetHighlightStronger(childRend, interValue);
        }
    }

    bool isActiveOnMaterials()
    {
        float activeNum;
        bool isActive = false;
        // 현재 _isNearPlayer가 켜져 있는지 확인
        foreach (var mat in _renderer.materials)
        {
            if (mat.HasProperty("_isNearPlayer"))
            {
                activeNum = mat.GetFloat("_isNearPlayer");
                // 체크표시가 켜져있으면
                if (activeNum == 1)
                {
                    isActive = true;
                    break;
                }
            }
        }
        return isActive;
    }

    public void ChangeMaterial(GameObject obj)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        Texture baseTexture;

        // obj가 자신이면 originMats 사용
        Material[] arrMaterials;
        if (obj == gameObject)
        {
            arrMaterials = new Material[originMats.Length];
            arrMaterials = originMats;
        }
        // 자식이면 originChildMats 사용
        else
        {
            arrMaterials = new Material[originChildMats.Length];
            arrMaterials = originChildMats;
        }

        // BaseMap이 있는 Material이면 Texture 받아오기
        foreach (Material originMat in arrMaterials)
        {
            // _preventTexture에 원래 텍스처 복사
            baseTexture = originMat.GetTexture("_BaseMap");
            if (baseTexture != null)
            {
                arrMat[1].SetTexture("_PreventTexture", baseTexture);
            }
        }
        // _myType이 OldWire와 PowerStrip이 아니면 실행
        if (_myType != PreventType.PowerStrip)
        {
            rend.materials = arrMat;
            SetActiveOnMaterials(false);
        }
    }

    public void MakeExceptPreventObject(PreventType type)
    {
        // 밑에 것은 플레이어가 시야 안으로 들어오면 실행
        switch (type)
        {
            // OldWire면 기존 Material Texture 받고 지우고 추가
            case PreventType.OldWire:
                MakeOldWire();
                break;
            // Powerstrip은 기존 메테리얼 중 63(두번째 거) 하나만 냅두고 추가
            case PreventType.PowerStrip:
                MakePowerStrip();
                break;
        }
    }

    void MakeOldWire()
    {
        //_renderer.materials = arrMat;
        // 자식이 지우고 추가
        if (isHaveChild == true)
        {
            Material[] mats = new Material[arrMat.Length];
            mats = arrMat;
            mats[1].SetTexture("_PreventTexture", null);
            childRend.materials = mats;
        }
    }

    void MakePowerStrip()
    {
        // PowerStrip 타입은 3개의 Material을 사용
        Material[] powerstrip = new Material[3] { arrMat[0], arrMat[1], originMats[1] };
        powerstrip[1].SetTexture("_PreventTexture", null);
        _renderer.materials = powerstrip;

        // 자식도 추가 필요
        if (isHaveChild == true)
        {
            childRend.materials = arrMat;
        }
    }

    public void MakeExceptObjectOff()
    {
        // PowerStrip, OldWire 타입 복구 처리
        if (_myType == PreventType.PowerStrip || _myType == PreventType.OldWire)
        {
            if(_myType== PreventType.PowerStrip)
            {
                _renderer.materials = originMats;
            }
            // 자식이 있으면 자식도 복구 처리
            if (isHaveChild == true)
            {
                childRend.materials = originChildMats;
            }
        }
    }

    // UI 액션
    void OnSetUIAction(GamePhase phase)
    {
        // 예방 페이즈이면 이벤트 구독
        if (phase == GamePhase.Prevention)
        {
            OnAlreadyPrevented += OnSetPreventMaterialsOff;
        }

        // 예방 페이즈가 아닐때 Material이 켜져 있으면 끄기
        else if(phase == GamePhase.FireWaiting)
        {
            if (isActiveOnMaterials())
            {
                SetActiveOnMaterials(false);
                // 예외인 애들 추가
                MakeExceptObjectOff();
            }
            // 불필요한 이벤트 해제
            GameManager.Instance.OnPhaseChanged -= OnSetUIAction;
        }
    }

    public void TriggerPreventObejct(bool isOn)
    {
        // 한번만 처리되도록 플래그 사용
        if(isAlreadyHandled == true)
        {
            return;
        }
        isAlreadyHandled = false;
        // 켜져 있으면
        if(isOn == true)
        {
            // 예방해야 하는 오브젝트 이벤트 발생
            OnHaveToPrevented?.Invoke();
        }
        // 꺼져 있으면
        else
        {
            // 이미 예방한 오브젝트 이벤트 발생
            OnAlreadyPrevented?.Invoke();
        }
    }

    public void OnSetPreventMaterialsOff()
    {
        // 예방 완료하면 Material 끄기
        SetActiveOnMaterials(false);
        // 예외인 애들 추가
        MakeExceptObjectOff();
        OnAlreadyPrevented -= OnSetPreventMaterialsOff;
    }

    public void OnSetPreventMaterialsOn()
    {
        // 예방 필요하면 빛나는거 표시
        MakeExceptPreventObject(_myType);
        SetActiveOnMaterials(true);
        OnHaveToPrevented -= OnSetPreventMaterialsOn;
    }
}
