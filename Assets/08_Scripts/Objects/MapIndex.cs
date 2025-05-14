using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapIndex : MonoBehaviour
{
    [SerializeField] private int _mapIndex;
    [SerializeField] private List<FireObjScript> _fireObjects = new List<FireObjScript>();
    [SerializeField] private List<FirePreventable> _firePreventables = new List<FirePreventable>();
    public int MapIndexValue => _mapIndex;

    public List<FireObjScript> FireObjects => _fireObjects;
    public List<FirePreventable> FirePreventables => _firePreventables;

    private void Start()
    {
        CollectChildren();
    }
    public void CollectChildren()
    {
        _fireObjects.Clear();
        _firePreventables.Clear();

        // 현재 게임오브젝트의 자식에서 컴포넌트 탐색
        foreach (Transform child in transform)
        {
            var fireObj = child.GetComponent<FireObjScript>();
            {
                if (fireObj != null)
                {
                    _fireObjects.Add(fireObj);
                }
            }
            var preventable = child.GetComponent<FirePreventable>();
            {
                if (preventable != null)
                {
                    _firePreventables.Add(preventable);
                }
            }
        }
    }
}
