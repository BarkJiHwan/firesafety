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

        // 기존의 메테리얼
        originMats = new Material[_renderer.materials.Length];
        originMats = _renderer.materials;

        if (transform.childCount > 0 && transform.GetChild(0).gameObject.layer == gameObject.layer)
        {
            isHaveChild = true;
            childRend = transform.GetChild(0).GetComponent<Renderer>();
            originChildMats = new Material[childRend.materials.Length];
            originChildMats = childRend.materials;
        }
    }

    public void SetMaterials(Renderer rend, bool isActive)
    {
        foreach (var mat in rend.materials)
        {
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
        Material highlightMat = rend.materials[1];
        if (highlightMat.HasProperty("_RimPower"))
        {
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

        Material[] arrMaterials;
        if (obj == gameObject)
        {
            arrMaterials = new Material[originMats.Length];
            arrMaterials = originMats;
        }
        else
        {
            arrMaterials = new Material[originChildMats.Length];
            arrMaterials = originChildMats;
        }

        // BaseMap이 있는 Material이면 Texture 받아오기
        foreach (Material originMat in arrMaterials)
        {
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
        if (_myType == PreventType.PowerStrip || _myType == PreventType.OldWire)
        {
            if(_myType== PreventType.PowerStrip)
            {
                _renderer.materials = originMats;
            }
            if (isHaveChild == true)
            {
                childRend.materials = originChildMats;
            }
        }
    }

    // UI 액션
    void OnSetUIAction(GamePhase phase)
    {
        if (phase == GamePhase.Prevention)
        {
            OnAlreadyPrevented += OnSetPreventMaterialsOff;
        }

        // 예방 페이즈가 아닐때 Material이 켜져 있으면 끄기
        else if(phase == GamePhase.Fire)
        {
            if (isActiveOnMaterials())
            {
                SetActiveOnMaterials(false);
                // 예외인 애들 추가
                MakeExceptObjectOff();
            }
            GameManager.Instance.OnPhaseChanged -= OnSetUIAction;
        }
    }

    public void TriggerPreventObejct(bool isOn)
    {
        if(isAlreadyHandled == true)
        {
            return;
        }
        isAlreadyHandled = false;
        if(isOn == true)
        {
            OnHaveToPrevented?.Invoke();
        }
        else
        {
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
        MakeExceptPreventObject(_myType);
        SetActiveOnMaterials(true);
        OnHaveToPrevented -= OnSetPreventMaterialsOn;
    }
}
