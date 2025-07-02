using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public void Dialogue01() => DataewooriDialogue("EXIT_005");
    public void Dialogue02() => DataewooriDialogue("EXIT_006");
    public void Dialogue03() => DataewooriDialogue("EXIT_009");
    public void Dialogue04() => DataewooriDialogue("EXIT_011");
    public void Dialogue05() => DataewooriDialogue("EXIT_012");

    private void DataewooriDialogue(string dialogueId)
    {
        _player.PlayWithText(dialogueId, UIType.Dataewoori);
    }
    public void Dialogue06() => SobaekDialogue("EXIT_007");
    public void Dialogue07() => SobaekDialogue("EXIT_008");
    public void Dialogue08() => SobaekDialogue("EXIT_010");
    public void Dialogue09() => SobaekDialogue("EXIT_013");
    public void Dialogue10() => SobaekDialogue("EXIT_014");

    private void SobaekDialogue(string dialogueId)
    {
        _player.PlayWithText(dialogueId, UIType.Sobaek);
    }

    public void StartParticle()
    {
        _particl01.Play();
        _particl02.Play();
    }
    public void SoptParticle()
    {
        _particl01.Stop();
        _particl02.Stop();
    }

    public void StartPlayerParticle()
    {
        _playerParticles.Play();
    }
    public void SotpPlayerParticle()
    {
        _playerParticles.Stop();
    }
    public void PowerUP()
    {
        var main = _playerParticles.main;
        main.startSpeed = 50;
    }

    public void showScoreBoard()
    {
        _fVCCon.TurnOnScoreBoard();
    }

    public void OnSiren()
    {
        _audioSource.clip = __siren;
        _audioSource.Play();
    }
    public void OffSiren()
    {
        _audioSource.Stop();
    }
}
