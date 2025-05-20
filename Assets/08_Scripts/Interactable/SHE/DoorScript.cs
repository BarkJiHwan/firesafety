using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : MonoBehaviour
{

    [SerializeField] private GameObject _door;
    [SerializeField] private float _closeAngle;
    [SerializeField] private float _openAngle;
    [SerializeField] private float _speed;

    private void Start()
    {
        _openAngle = transform.eulerAngles.y;
    }


    public void DoorInteract()
    {

    }

}
