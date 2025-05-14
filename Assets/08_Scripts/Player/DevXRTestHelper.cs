using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

public class DevXRTestHelper : MonoBehaviour
{
    public XROrigin xrOrigin;
    public GameObject deviceSimulator;

    public void Start()
    {
        if (!XRSettings.enabled)
        {
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;

            if (deviceSimulator != null)
            {
                deviceSimulator.SetActive(true);
            }

            Debug.Log("XR 환경 설정 안됨, 가상 환경 설정");
        }
    }
}
