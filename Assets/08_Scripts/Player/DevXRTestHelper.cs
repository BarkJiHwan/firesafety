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
            if (deviceSimulator != null)
            {
                deviceSimulator.SetActive(true);
            }
        }
    }
}
