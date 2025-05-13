using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 화재 오브젝트를 관리하는 싱글톤 매니저 클래스
/// </summary>
public class FireObjectManager : MonoBehaviour
{
    public static FireObjectManager Instance;

    [SerializeField] private GameObject _fireObjectPrefab; // 화재 오브젝트 프리팹
    [SerializeField] private Transform[] _spawnPoints; // 화재 오브젝트 스폰 포인트 배열

    private List<GameObject> _fireObjects = new List<GameObject>(); // 화재 오브젝트 리스트

    [SerializeField] private TaeuriPoolManager _taeuriManager; // 태우리 풀링 매니저 참조

    /// <summary>
    /// 싱글톤 패턴 구현
    /// </summary>
    private void Awake()
    {
        // 싱글톤 패턴 적용
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 시작 시 화재 오브젝트 생성
    /// </summary>
    private void Start()
    {
        CreateFireObjects();

        // 태우리 매니저 참조가 있으면 화재 오브젝트에 태우리 설정
        if (_taeuriManager != null)
        {
            _taeuriManager.SetupTaeurisToFireObjects(_fireObjects);
        }
    }

    /// <summary>
    /// 화재 오브젝트 생성 함수
    /// </summary>
    private void CreateFireObjects()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("스폰 포인트가 설정되지 않았습니다.");
            return;
        }

        // 각 스폰 포인트마다 화재 오브젝트 생성
        foreach (Transform spawnPoint in _spawnPoints)
        {
            if (spawnPoint != null)
            {
                GameObject fireObject = Instantiate(_fireObjectPrefab, spawnPoint.position, Quaternion.identity);
                _fireObjects.Add(fireObject);
            }
        }

        Debug.Log($"총 {_fireObjects.Count}개의 화재 오브젝트가 생성되었습니다.");
    }

    /// <summary>
    /// 모든 화재 오브젝트의 태우리 활성화
    /// </summary>
    public void ActivateAllTaeuris()
    {
        if (_taeuriManager != null)
        {
            _taeuriManager.ActivateAllTaeuris(_fireObjects);
        }
    }

    /// <summary>
    /// 화재 오브젝트 리스트 가져오기
    /// </summary>
    public List<GameObject> GetFireObjects()
    {
        return _fireObjects;
    }

    /// <summary>
    /// 특정 인덱스의 화재 오브젝트 가져오기
    /// </summary>
    public GameObject GetFireObject(int index)
    {
        if (index >= 0 && index < _fireObjects.Count)
        {
            return _fireObjects[index];
        }
        return null;
    }

    /// <summary>
    /// 특정 화재 오브젝트의 태우리 활성화
    /// </summary>
    public void ActivateTaeurisOnFireObject(int index)
    {
        GameObject fireObject = GetFireObject(index);
        if (fireObject != null && _taeuriManager != null)
        {
            _taeuriManager.ActivateTaeurisOnFireObject(fireObject);
        }
    }

    /// <summary>
    /// 특정 화재 오브젝트의 태우리 비활성화
    /// </summary>
    public void DeactivateTaeurisOnFireObject(int index)
    {
        GameObject fireObject = GetFireObject(index);
        if (fireObject != null && _taeuriManager != null)
        {
            _taeuriManager.DeactivateTaeurisOnFireObject(fireObject);
        }
    }
}
