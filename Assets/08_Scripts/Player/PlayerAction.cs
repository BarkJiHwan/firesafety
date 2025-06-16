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
            if (!_playerBehavior)
            {
                Debug.Log("hihi");
                yield break;
            }

            if (_observedMove != _playerBehavior.IsMoving)
            {
                _observedMove = _playerBehavior.IsMoving;
                _photonView.RPC("AnimateWalk", RpcTarget.All);
            }

            Debug.Log("IsMoving : " + _playerBehavior.IsMoving);

            yield return new WaitForSeconds(0.1f);
        }
    }

    [PunRPC]
    public void AnimateWalk()
    {
        _animator.SetBool(_moving, _observedMove);
        Debug.Log("animateWalk");
    }
}
