using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

/// <summary>
/// 태우리 오브젝트 관리를 담당하는 매니저 클래스
/// </summary>
public class TaeuriPoolManager : MonoBehaviour
{
    [SerializeField] private GameObject _taeuriPrefab; // 기본 태우리 프리팹
    [SerializeField] private GameObject _smallTaeuriPrefab; // 작은 태우리 프리팹   
    [SerializeField] private GameObject _deathEffectPrefab; // 태우리 죽을 때 효과 프리팹

    [SerializeField] private int _defaultPoolSize = 20; // 기본 풀 사이즈
    [SerializeField] private int _maxPoolSize = 100; // 최대 풀 사이즈

    // 태우리 풀
    private IObjectPool<GameObject> _taeuriPool;
    // 작은 태우리 풀
    private IObjectPool<GameObject> _smallTaeuriPool;
    // 죽는 이펙트 풀
    private IObjectPool<GameObject> _deathEffectPool;

    private void Awake()
    {
        // 풀 초기화
        InitializePools();
    }

    /// <summary>
    /// 풀 초기화
    /// </summary>
    private void InitializePools()
    {
        // 태우리 풀 생성
        _taeuriPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(_taeuriPrefab),
            actionOnGet: (obj) => OnTaeuriGet(obj),
            actionOnRelease: (obj) => OnTaeuriRelease(obj),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: true,
            defaultCapacity: _defaultPoolSize,
            maxSize: _maxPoolSize
        );

        // 작은 태우리 풀 생성
        _smallTaeuriPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(_smallTaeuriPrefab),
            actionOnGet: (obj) => OnTaeuriGet(obj, true),
            actionOnRelease: (obj) => OnTaeuriRelease(obj),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: true,
            defaultCapacity: _defaultPoolSize,
            maxSize: _maxPoolSize
        );

        // 죽는 이펙트 풀 생성
        _deathEffectPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(_deathEffectPrefab),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: true,
            defaultCapacity: _defaultPoolSize,
            maxSize: _maxPoolSize
        );
    }

    /// <summary>
    /// 태우리 풀에서 가져올 때 호출
    /// </summary>
    private void OnTaeuriGet(GameObject taeuriObj, bool isSmall = false)
    {
        taeuriObj.SetActive(true);

        // 풀링 매니저 참조 설정
        if (isSmall)
        {
            SmallTaeuri smallTaeuriComponent = taeuriObj.GetComponent<SmallTaeuri>();
            if (smallTaeuriComponent != null)
            {
                smallTaeuriComponent.SetPoolManager(this);
            }
        }
        else
        {
            Taeuri taeuriComponent = taeuriObj.GetComponent<Taeuri>();
            if (taeuriComponent != null)
            {
                taeuriComponent.SetPoolManager(this);
            }
        }

        
    }

    /// <summary>
    /// 태우리 풀로 반환할 때 호출
    /// </summary>
    private void OnTaeuriRelease(GameObject taeuriObj)
    {
        taeuriObj.SetActive(false);
    }

    /// <summary>
    /// 죽는 이펙트 생성
    /// </summary>
    public void CreateDeathEffect(Vector3 position)
    {
        GameObject deathEffect = _deathEffectPool.Get();

        if (deathEffect != null)
        {
            // 위치 설정
            deathEffect.transform.position = position;

            // 파티클 시스템 재생 시작
            ParticleSystem particleSystem = deathEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Play();

                // 파티클 재생 후 자동으로 풀로 반환
                StartCoroutine(ReleaseAfterPlay(deathEffect, particleSystem.main.duration));
            }
        }
    }

    /// <summary>
    /// 파티클 재생 후 풀로 반환
    /// </summary>
    private IEnumerator ReleaseAfterPlay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null)
        {
            _deathEffectPool.Release(obj);
        }
    }

    /// <summary>
    /// 화재 오브젝트에 태우리 스폰
    /// </summary>
    /// <param name="fireObjects">화재 오브젝트 리스트</param>
    public void SetupTaeurisToFireObjects(List<GameObject> fireObjects)
    {
        if (fireObjects == null || fireObjects.Count == 0)
        {
            Debug.LogWarning("화재 오브젝트 리스트가 비어있습니다.");
            return;
        }

        // 각 화재 오브젝트마다 태우리 생성
        foreach (GameObject fireObj in fireObjects)
        {
            if (fireObj != null)
            {
                // 기본 태우리 1개 생성하여 자식으로 추가
                GameObject taeuri = _taeuriPool.Get();
                taeuri.transform.position = fireObj.transform.position;
                taeuri.transform.SetParent(fireObj.transform);
                taeuri.SetActive(false); // 초기에는 비활성화 상태로 생성

                // 작은 태우리 4개 생성하여 자식으로 추가
                for (int i = 0; i < 4; i++)
                {
                    GameObject smallTaeuri = _smallTaeuriPool.Get();
                    smallTaeuri.transform.position = fireObj.transform.position;
                    smallTaeuri.transform.SetParent(fireObj.transform);
                    smallTaeuri.SetActive(false); // 초기에는 비활성화 상태로 생성
                }
            }
        }
    }

    /// <summary>
    /// 태우리 풀로 반환
    /// </summary>
    /// <param name="taeuriObj">반환할 태우리 오브젝트</param>
    /// <param name="isSmall">작은 태우리 여부</param>
    public void ReleaseTaeuri(GameObject taeuriObj, bool isSmall = false)
    {
        if (taeuriObj == null)
            return;

        // 태우리를 부모에서 분리
        taeuriObj.transform.SetParent(null);

        // 적절한 풀로 반환
        if (isSmall)
        {
            _smallTaeuriPool.Release(taeuriObj);
        }
        else
        {
            _taeuriPool.Release(taeuriObj);
        }
    }

    /// <summary>
    /// 모든 화재 오브젝트의 태우리를 활성화하는 함수
    /// </summary>
    /// <param name="fireObjects">화재 오브젝트 리스트</param>
    public void ActivateAllTaeuris(List<GameObject> fireObjects)
    {
        if (fireObjects == null || fireObjects.Count == 0)
        {
            Debug.LogWarning("화재 오브젝트 리스트가 비어있습니다.");
            return;
        }

        foreach (GameObject fireObj in fireObjects)
        {
            if (fireObj != null)
            {
                ActivateTaeurisOnFireObject(fireObj);
            }
        }
    }

    /// <summary>
    /// 특정 화재 오브젝트의 태우리를 활성화하는 함수
    /// </summary>
    /// <param name="fireObject">화재 오브젝트</param>
    public void ActivateTaeurisOnFireObject(GameObject fireObject)
    {
        if (fireObject == null)
        {
            return;
        }

        // 해당 화재 오브젝트의 모든 자식 태우리를 활성화
        foreach (Transform child in fireObject.transform)
        {
            if (child.GetComponent<Taeuri>() != null || child.GetComponent<SmallTaeuri>() != null)
            {
                child.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 특정 화재 오브젝트의 태우리를 비활성화하는 함수
    /// </summary>
    /// <param name="fireObject">화재 오브젝트</param>
    public void DeactivateTaeurisOnFireObject(GameObject fireObject)
    {
        if (fireObject == null)
        {
            return;
        }

        // 해당 화재 오브젝트의 모든 자식 태우리를 비활성화
        foreach (Transform child in fireObject.transform)
        {
            if (child.GetComponent<Taeuri>() != null || child.GetComponent<SmallTaeuri>() != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
