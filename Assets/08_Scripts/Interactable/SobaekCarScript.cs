using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public enum CarSpped {
    Stop,
    GearOne,
    GearTwo,
    GearThree
}

public class SobaekCarScript : MonoBehaviour
{
    public GameObject seatPosition;
    public GameObject player;

    private XRSimpleInteractable _simpleInteractable;
    private void Awake()
    {
        _simpleInteractable = GetComponent<XRSimpleInteractable>();
        _simpleInteractable.selectEntered.AddListener(OnEnteredCar);
    }

    /* 한번 탑승하면 Interactable 꺼버리기 */
    private void OnEnteredCar(SelectEnterEventArgs Args)
    {
        Debug.Log("Entered Car");
        player.transform.position = seatPosition.transform.position;
        player.transform.rotation = seatPosition.transform.rotation;
        player.transform.parent = gameObject.transform;

        _simpleInteractable.selectEntered.RemoveListener(OnEnteredCar);
        _simpleInteractable.enabled = false;
    }
}
