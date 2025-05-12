using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BabyTaeuri : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Transform _target;           // 주위를 날 대상
    [SerializeField] private float _radius = 5f;          // 비행 반경
    [SerializeField] private float _speed = 1f;           // 비행 속도
    [SerializeField] private float _heightVariation = 1f; // 높이 변화량
    [SerializeField] private float _heightSpeed = 0.5f;   // 높이 변화 속도

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 5f;   // 회전 속도
    [SerializeField] private bool _faceMovementDirection = true; // 이동 방향을 바라보는지 여부

    private float _currentAngle = 0f;
    private Vector3 _targetPosition;
    private Vector3 _initialHeight;

    private void Start()
    {
        // 타겟이 설정되지 않았다면 경고 표시
        if (_target == null)
        {
            _target = new GameObject("TaeuriTarget").transform;
            _target.position = transform.position;
        }

        // 초기 높이 저장
        _initialHeight = new Vector3(0, transform.position.y - _target.position.y, 0);

        // 랜덤한 시작 위치에서 시작
        _currentAngle = Random.Range(0f, 360f);
    }

    private void Update()
    {
        // 각도 업데이트
        _currentAngle += _speed * Time.deltaTime;

        // 원형 경로를 따라 위치 계산
        float x = Mathf.Cos(_currentAngle * Mathf.Deg2Rad) * _radius;
        float z = Mathf.Sin(_currentAngle * Mathf.Deg2Rad) * _radius;

        // 높이는 사인 함수를 이용해 변화를 줌
        float heightOffset = Mathf.Sin(Time.time * _heightSpeed) * _heightVariation;

        // 최종 위치 계산
        _targetPosition = _target.position + new Vector3(x, _initialHeight.y + heightOffset, z);

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * 2f);

        // 이동 방향을 바라보게 설정
        if (_faceMovementDirection && Vector3.Distance(transform.position, _targetPosition) > 0.1f)
        {
            // 다음 위치 방향으로 회전
            Vector3 direction = _targetPosition - transform.position;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
            }
        }
    }

    // 에디터에서 경로를 시각화
    private void OnDrawGizmosSelected()
    {
        if (_target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_target.position, _radius);

            // 현재 목표 위치 표시
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_targetPosition, 0.2f);
            }
        }
    }
}
