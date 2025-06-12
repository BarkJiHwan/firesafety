using System.Collections;
using UnityEngine;

public class TaewooriTutorial : MonoBehaviour, IDamageable
{
    [Header("체력 설정")]    
    public float currentHealth = 100f;

    [Header("리스폰 설정")]
    [SerializeField] private float respawnCooltime = 3f; // 리스폰 쿨타임   
    // 1 -> 메인 / 2 -> 메인
    [Header("체력별 색상 설정")]
    [SerializeField] private float maxGreenBoost = 1.0f; // 최대 G값 증가량 (체력 0%일 때)   
    [SerializeField] private ParticleSystem[] targetParticleSystems; // 색상 변경할 파티클 시스템들 
    // 원본 그라디언트 저장용 (초기값 보존)
    private Gradient[] originalGradients;

    private bool isDead = false;

    void Start()
    {
        SaveOriginalGradients();

        isDead = false;
    }

    // 원본 그라디언트들을 저장
    private void SaveOriginalGradients()
    {
        if (targetParticleSystems == null || targetParticleSystems.Length == 0)
            return;

        originalGradients = new Gradient[targetParticleSystems.Length];

        for (int i = 0; i < targetParticleSystems.Length; i++)
        {
            if (targetParticleSystems[i] != null)
            {
                var colorOverLifetime = targetParticleSystems[i].colorOverLifetime;
                if (colorOverLifetime.enabled)
                {
                    // 원본 그라디언트 복사하여 저장
                    originalGradients[i] = new Gradient();
                    var original = colorOverLifetime.color.gradient;
                    originalGradients[i].SetKeys(original.colorKeys, original.alphaKeys);
                }
            }
        }
    }

    // IDamageable 구현
    public virtual void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        // 데미지 받은 후 색상 업데이트
        UpdateHealthColor();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 체력에 따른 색상 업데이트
    private void UpdateHealthColor()
    {
        // 지정된 파티클 시스템들만 변경
        if (targetParticleSystems != null && targetParticleSystems.Length > 0)
        {
            float healthPercentage = currentHealth > 0 ? currentHealth / 100 : 0f;
            UpdateParticleColorValues(healthPercentage);
        }
    }

    private void UpdateParticleColorValues(float healthPercentage)
    {
        if (targetParticleSystems == null || targetParticleSystems.Length == 0 || originalGradients == null)
            return;

        // 데미지 비율 계산 (체력이 낮을수록 값이 커짐: 0% = 1.0, 100% = 0.0)
        float damageRatio = 1f - healthPercentage;

        // G값 증가량 (1배)
        float greenBoost = damageRatio * maxGreenBoost;

        // B값 증가량 (G값의 30%)
        float blueBoost = greenBoost * 0.3f;

        for (int psIndex = 0; psIndex < targetParticleSystems.Length; psIndex++)
        {
            ParticleSystem ps = targetParticleSystems[psIndex];

            if (ps == null || originalGradients[psIndex] == null)
                continue;

            var colorOverLifetime = ps.colorOverLifetime;

            // 원본 그라디언트에서 새로운 그라디언트 생성
            Gradient originalGradient = originalGradients[psIndex];
            Gradient newGradient = new Gradient();

            // 원본 Color Key들을 기반으로 새로운 컬러 키 생성
            GradientColorKey[] originalColorKeys = originalGradient.colorKeys;
            GradientColorKey[] newColorKeys = new GradientColorKey[originalColorKeys.Length];

            for (int i = 0; i < originalColorKeys.Length; i++)
            {
                Color originalColor = originalColorKeys[i].color;
                float keyTime = originalColorKeys[i].time;
                Color newColor;

                // 키별 다른 처리 (예시: 첫 번째 키만 변경)
                if (i == 0) // 첫 번째 키만 색상 변화 적용
                {
                    newColor = new Color(
                        originalColor.r, // R값은 원본 유지
                        Mathf.Clamp01(originalColor.g + greenBoost), // G값 증가 (1배)
                        Mathf.Clamp01(originalColor.b + blueBoost),  // B값 증가 (G값의 절반)
                        originalColor.a  // A값은 원본 유지
                    );
                }
                else // 나머지 키들은 원본 그대로
                {
                    newColor = originalColor;
                }

                newColorKeys[i] = new GradientColorKey(newColor, keyTime);
            }

            // 기존 Alpha Key들 복사 (알파키 사용하지 않음)
            GradientAlphaKey[] originalAlphaKeys = originalGradient.alphaKeys;

            // 새 Gradient 설정
            newGradient.SetKeys(newColorKeys, originalAlphaKeys);
            colorOverLifetime.color = newGradient;
        }
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        //사망시 소화기 비활성화
        var playerSuppressor = FindObjectOfType<TutorialSuppressor>();
        var playerRPCSuppressor = FindObjectOfType<FireSuppressantManager>();
        playerSuppressor.DetachSuppressor();
        playerSuppressor.enabled = false;
        playerRPCSuppressor.enabled = true;
    }
}
