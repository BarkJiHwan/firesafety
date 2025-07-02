using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Phase2_BombScript : MonoBehaviour
{
    [SerializeField] private bool _isBig;
    [SerializeField] private int _damage;
    [SerializeField] private DaTaewoori _boss;
    private void OnTriggerEnter(Collider other)
    {
        if (_boss == null)
        {
            _boss = FindObjectOfType<DaTaewoori>();
        }
        if (other.CompareTag("Head"))
        {
            _boss.TakeDamage(_damage * 2);
        }
        else if (other.CompareTag("Body"))
        {
            _boss.TakeDamage(_damage);
        }
        else
        {
            Phase2_BombPoolManager.Instance.ReturnBomb(gameObject, _isBig);
        }
    }
}
