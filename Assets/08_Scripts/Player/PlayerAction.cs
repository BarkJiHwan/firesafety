using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    private static readonly int _moving = Animator.StringToHash("IsMoving");

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

            //
            if (_observedMove != _playerBehavior.IsMoving)
            {
                _observedMove = _playerBehavior.IsMoving;
                DecideAnimationMethod(_moving, _observedMove);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    // 포톤 뷰 존재 여부에 따라 애니메이션 재생 수행하는 방법 분기
    public void DecideAnimationMethod(int animatorParam, bool flag)
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
