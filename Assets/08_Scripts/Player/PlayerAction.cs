using System.Collections;
using Photon.Pun;
using UnityEngine;

/*
 * 플레이어의 액션을 담당하는 클래스입니다.
 * 현재는 움직임을 트래킹하여 애니메이션을 재생하는지 여부만 판단합니다.
 */
public class PlayerAction : MonoBehaviour
{
    private readonly int _moving = Animator.StringToHash("IsMoving");

    private Animator _animator;
    private PlayerBehavior _playerBehavior;
    private PhotonView _photonView;
    private bool _observedMove;

    public void Awake()
    {
        _playerBehavior = GetComponent<PlayerBehavior>();
        _animator = GetComponent<Animator>();
        _photonView = GetComponent<PhotonView>();
    }

    public void Start()
    {
        StartCoroutine(MovingObserver());
    }

    private IEnumerator MovingObserver()
    {
        while (this.enabled)
        {
            // 연동하고 있는 Behavior 없어졌을 경우 무한루프 종료
            if (!_playerBehavior)
            {
                yield break;
            }

            // 플레이어가 움직인다면 이동 애니메이션 재생
            if (_observedMove != _playerBehavior.IsMoving)
            {
                _observedMove = _playerBehavior.IsMoving;
                DecideAnimationMethod(_moving, _observedMove);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    // 포톤 뷰 존재 여부에 따라 애니메이션 재생 수행하는 방법 분기
    private void DecideAnimationMethod(int animatorParam, bool flag)
    {
        if (_photonView == null)
        {
            Animate(animatorParam, flag);
        }
        else
        {
            object[] parameters = { animatorParam, flag };
            _photonView.RPC("Animate", RpcTarget.All, parameters);
        }
    }

    [PunRPC]
    public void Animate(int animatorParam, bool flag)
    {
        _animator.SetBool(animatorParam, flag);
    }
}
