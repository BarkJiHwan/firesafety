using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/*
    ITunnelingVignettesProvider를 상속하여
    커스텀하게 멀미효과 / FadeIn, Out 효과를 내기 위한 클래스 입니다.
    기본 Locomotion에 따라 동작하는 Vignette 효과를 제거하고 넣었습니다.
 */
public class CustomTunnelingVignette : MonoBehaviour, ITunnelingVignetteProvider
{
    [SerializeField] private TunnelingVignetteController _tunnelingVignetteController;
    [SerializeField] private VignetteParameters _fadeOutParam;
    [SerializeField] private VignetteParameters _sightShrinkParam;

    /* 까매지는 효과 시작 */
    public void FadeOut()
    {
        _tunnelingVignetteController.defaultParameters = _fadeOutParam;
        _tunnelingVignetteController.BeginTunnelingVignette(this);
    }

    public void FadeIn()
    {
        _tunnelingVignetteController.defaultParameters = _fadeOutParam;
        _tunnelingVignetteController.EndTunnelingVignette(this);
    }

    public void SightShrink()
    {
        _tunnelingVignetteController.defaultParameters = _sightShrinkParam;
        _tunnelingVignetteController.BeginTunnelingVignette(this);
    }

    public void ClearSight()
    {
        _tunnelingVignetteController.EndTunnelingVignette(this);
    }

    public VignetteParameters vignetteParameters { get; }
}
