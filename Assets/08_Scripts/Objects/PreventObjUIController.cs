using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreventObjUIController : MonoBehaviour
{
    [SerializeField] private GameObject _infoMessageUI;
    [SerializeField] private GameObject _outLineEffect;

    [Range(0.01f, 0.1f), Header("UI위치 offset 오브젝트 상단")]
    [SerializeField] private float _offset = 0.1f;
    public Vector3 GetTopPosition()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // 윗면 중심 계산
            Vector3 rendTopPos = rend.bounds.center + Vector3.up * (rend.bounds.extents.y + _offset);
            return rendTopPos;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            // Collider 기준 윗면 중심
            Vector3 colTopPos = col.bounds.center + Vector3.up * (col.bounds.extents.y + _offset);
            return colTopPos;
        }

        // fallback: 그냥 자기 위치 위쪽
        return transform.position + Vector3.up * _offset;
    }
    public void Spawn()
    {
        if (_infoMessageUI != null)
        {
            Vector3 spawnPosition = GetTopPosition();
            Instantiate(_infoMessageUI, spawnPosition, Quaternion.identity);
        }
    }
    void Update()
    {
        
    }
}
