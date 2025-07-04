using UnityEngine;

public class ScaledHeightSyncronizer : MonoBehaviour
{
    // 사람의 키 최대치, 2m라고 가정
    private float maxHeight = 2f;

    // 원하는 맵의 최대 눈높이
    public float maxEyeHeight = 0.7f;

    // 디버그용 내 환산 눈높이
    [SerializeField] private float scaledHeight;

    public float CalculateScaledHeight()
    {
        float myCamHeight = transform.position.y;
        float scaledMyCamHeight = myCamHeight / maxHeight;

        if (scaledMyCamHeight >= 1f)
        {
            scaledMyCamHeight = 1f;
        }

        if (scaledMyCamHeight <= 0f)
        {
            scaledMyCamHeight = 0f;
        }

        scaledHeight = scaledMyCamHeight * maxEyeHeight;

        return scaledHeight;
    }
}
