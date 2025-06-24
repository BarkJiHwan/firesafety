using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

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

    private IEnumerator FadeOutAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        FadeOut();
    }

    private IEnumerator FadeInAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        FadeIn();
    }

    public VignetteParameters vignetteParameters { get; }
}
