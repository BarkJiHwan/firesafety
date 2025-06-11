using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Canvas objCanvas;
    FirePreventableObject[] firePreventObjects;

    ObjectUICtrl objectUICtrl;

    void Start()
    {
        // FirePreventable이 달려있는 게임 오브젝트 찾기
        FirePreventable[] firePrevent = FindObjectsOfType<FirePreventable>();
        firePreventObjects = new FirePreventableObject[firePrevent.Length];
        bool isHaveChild = false;

        for(int i=0; i< firePrevent.Length; i++)
        {
            // 자식이 있는지 확인
            if (firePrevent[i].gameObject.transform.childCount > 0)
            {
                GameObject child = firePrevent[i].gameObject.transform.GetChild(0).gameObject;
                // 자식이 활성화되어 있으면
                if (child.activeSelf == true)
                {
                    // 자식이 있다는 표시를 Bool형으로
                    isHaveChild = true;
                }
            }
            // 자식 있는 게임 오브젝트 찾기
            firePreventObjects[i] = new FirePreventableObject(firePrevent[i].gameObject, firePrevent[i].MyType, isHaveChild);
            if(isHaveChild == true)
            {
                isHaveChild = false;
            }
        }

        // 오브젝트 위에 뜨는 UI 받아오기
        objectUICtrl = objCanvas.GetComponent<ObjectUICtrl>();
        for (int i=0; i<firePreventObjects.Length; i++)
        {
            var interactable = firePreventObjects[i].preventObject.GetComponent<XRSimpleInteractable>();
            interactable.hoverEntered.AddListener((args) =>
            {
                if(objectUICtrl != null)
                {
                    objectUICtrl.SelectedObject(args);
                }
            });
            interactable.hoverExited.AddListener((args) =>
            {
                if (objectUICtrl != null)
                {
                    objectUICtrl.DisSelectedObject();
                }
            });
        }
    }

    void Update()
    {
    }
}
