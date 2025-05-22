using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Canvas objCanvas;
    [SerializeField] GameObject[] firePreventObjects;

    ObjectUICtrl objectUICtrl;

    void Start()
    {
        objectUICtrl = objCanvas.GetComponent<ObjectUICtrl>();
        for (int i=0; i<firePreventObjects.Length; i++)
        {
            var interactable = firePreventObjects[i].GetComponent<XRSimpleInteractable>();
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
