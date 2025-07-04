using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 엔딩씬 타임라인 관련 모든 내용이 다 들어있는 스크립트
public class Ending : MonoBehaviour
{
    [SerializeField] private DialoguePlayer _player;
    [SerializeField] private ParticleSystem _playerParticles;
    [SerializeField] private ParticleSystem _particl01;
    [SerializeField] private ParticleSystem _particl02;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip __siren;

    [SerializeField] private FixedViewCanvasController _fVCCon;
    void Start()
    {
        if (_player == null)
        {
            _player = FindObjectOfType<DialoguePlayer>();
        }
    }
    // 다 태우리 관련 UI 및 텍스트 나레이션
    private void DataewooriDialogue(string dialogueId)
    {
        _player.PlayWithText(dialogueId, UIType.Dataewoori);
    }
    public void Dialogue01() => DataewooriDialogue("EXIT_005");
    public void Dialogue02() => DataewooriDialogue("EXIT_006");
    public void Dialogue03() => DataewooriDialogue("EXIT_009");
    public void Dialogue04() => DataewooriDialogue("EXIT_011");
    public void Dialogue05() => DataewooriDialogue("EXIT_012");

    // 소백이 관련 UI 및 텍스트 나레이션
    private void SobaekDialogue(string dialogueId)
    {
        _player.PlayWithText(dialogueId, UIType.Sobaek);
    }
    public void Dialogue06() => SobaekDialogue("EXIT_007");
    public void Dialogue07() => SobaekDialogue("EXIT_008");
    public void Dialogue08() => SobaekDialogue("EXIT_010");
    public void Dialogue09() => SobaekDialogue("EXIT_013");
    public void Dialogue10() => SobaekDialogue("EXIT_014");

    // 파티클 시작
    public void StartParticle()
    {
        _particl01.Play();
        _particl02.Play();
    }
    // 파티클 종료
    public void SoptParticle()
    {
        _particl01.Stop();
        _particl02.Stop();
    }
    // 플레이어 파티클 시작
    public void StartPlayerParticle()
    {
        _playerParticles.Play();
    }
    // 플레이어 파티클 종료
    public void SotpPlayerParticle()
    {
        _playerParticles.Stop();
    }
    // 플레이어 파티클 속도 증가
    public void PowerUP()
    {
        var main = _playerParticles.main;
        main.startSpeed = 50;
    }
    // 점수판 띄우기
    public void showScoreBoard()
    {
        _fVCCon.TurnOnScoreBoard();
    }
    // 소방차 소리 사운드 시작
    public void OnSiren()
    {
        _audioSource.clip = __siren;
        _audioSource.Play();
    }
    // 소방차 소리 사운드 종료
    public void OffSiren()
    {
        _audioSource.Stop();
    }
}
