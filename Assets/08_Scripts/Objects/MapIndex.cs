using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapIndex : MonoBehaviour
{
    // 맵(영역)의 인덱스 번호
    [SerializeField] private int _mapIndex;

    // 이 맵에 포함된 화재 오브젝트 리스트
    [SerializeField] private List<FireObjScript> _fireObjects = new List<FireObjScript>();

    // 이 맵에 포함된 화재 예방 오브젝트 리스트
    [SerializeField] private List<FirePreventable> _firePreventables = new List<FirePreventable>();

    /// <summary>
    /// 맵 인덱스 번호를 반환
    /// </summary>
    public int MapIndexValue => _mapIndex;

    /// <summary>
    /// 이 맵에 포함된 화재 오브젝트 리스트를 반환
    /// </summary>
    public List<FireObjScript> FireObjects => _fireObjects;

    /// <summary>
    /// 이 맵에 포함된 화재 예방 오브젝트 리스트를 반환
    /// </summary>
    public List<FirePreventable> FirePreventables => _firePreventables;

    private void Start()
    {
        // 자식 오브젝트에서 화재/예방 오브젝트를 수집
        CollectChildren();
    }

    /// <summary>
    /// 현재 게임오브젝트의 자식들 중에서
    /// FireObjScript와 FirePreventable 컴포넌트를 찾아 각각 리스트에 추가
    /// </summary>
    public void CollectChildren()
    {
        _fireObjects.Clear();
        _firePreventables.Clear();

        // 현재 오브젝트의 모든 자식 Transform을 순회
        foreach (Transform child in transform)
        {
            // 자식에서 FireObjScript 컴포넌트 탐색 및 추가
            var fireObj = child.GetComponent<FireObjScript>();
            if (fireObj != null)
            {
                _fireObjects.Add(fireObj);
            }

            // 자식에서 FirePreventable 컴포넌트 탐색 및 추가
            var preventable = child.GetComponent<FirePreventable>();
            if (preventable != null)
            {
                _firePreventables.Add(preventable);
            }
        }
    }
}
