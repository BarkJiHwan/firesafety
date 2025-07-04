using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

/*
 *  XR 시뮬레이터 사용시 쉽게 테스트 위한 코드입니다.
 *  XR Plug-in Management - PC의 Initialize XR on Startup을 체크 해제하고 피씨 테스트를 가정했습니다
 */
public class DevXRTestHelper : MonoBehaviour
{
    public XROrigin xrOrigin;

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
                Vector3 minHeightVector = new Vector3(0, 1f, 0);
                xrOrigin.CameraFloorOffsetObject.transform.position += minHeightVector;
                controllersOffset.transform.position += (minHeightVector / 2);
            }
        }
    }
}
