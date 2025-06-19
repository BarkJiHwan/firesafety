using System.Collections;
using Photon.Pun;
using UnityEngine;

public class TaewooriTutorial : MonoBehaviour, IDamageable
{
    [Header("체력 설정")]    
    public float currentHealth = 100f;
    private bool isDead = false;

    void Start()
    {
        isDead = false;
    }

    // IDamageable 구현
    public virtual void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        //사망시 소화기 비활성화
        var players = FindObjectsOfType<FireSuppressantManager>();
        foreach (var player in players)
        {
            if (player.pView.IsMine)
            {
                var tuto = player.gameObject.GetComponent<TutorialSuppressor>();
                tuto.SetAmountZero();
            }
        }


        //var playerSuppressor = FindObjectOfType<TutorialSuppressor>();
        //var playerRPCSuppressor = FindObjectOfType<FireSuppressantManager>();
        //playerSuppressor.DetachSuppressor();
        //playerSuppressor.enabled = false;
        //playerRPCSuppressor.enabled = true;
    }
}
