using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DaTaewoori : MonoBehaviour, IDamageable
{
    [SerializeField] private float _hp;

    public void TakeDamage(float damage) => _hp -= damage;
    //게임 매니저에게 죽으면 호출 가능한 함수 하나. 이벤트를 발생시켜야 해서 그럼

}
