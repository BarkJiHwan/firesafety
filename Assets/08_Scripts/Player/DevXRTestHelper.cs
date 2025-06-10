using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class DevXRTestHelper : MonoBehaviour
{
    public XROrigin xrOrigin;
    public CharacterControllerDriver chaDriver;

    public GameObject deviceSimulator;
    public GameObject controllersOffset;

    public void Start()
    {
        // 에디터에서 플레이 (Pc, 디바이스 연결 안된상태) 할때 세팅 도와주는 클래스
        if (!XRSettings.isDeviceActive)
        {
            // 1. 디바이스 시뮬레이터 있을경우 켜주기
            if (deviceSimulator != null)
            {
                deviceSimulator.SetActive(true);
            }

            // 2. Floor 모드일때 최소 시야 볼수 있게 Y값 올려주기
            if (xrOrigin.RequestedTrackingOriginMode == XROrigin.TrackingOriginMode.Floor)
            {
                Vector3 minHeightVector = new Vector3(0, chaDriver.minHeight, 0);
                xrOrigin.CameraFloorOffsetObject.transform.position += minHeightVector;
                controllersOffset.transform.position += (minHeightVector / 2);
            }
        }
    }
}
