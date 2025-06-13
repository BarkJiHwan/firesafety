using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CustomTunnelingVignette : MonoBehaviour, ITunnelingVignetteProvider
{
    [SerializeField] private TunnelingVignetteController _tunnelingVignetteController;

    // Start is called before the first frame update
    private void Start()
    {
        FadeOut();
        FadeIn();
    }

    private void FadeOut()
    {
        _tunnelingVignetteController.BeginTunnelingVignette(this);
    }

    private void FadeIn()
    {
        _tunnelingVignetteController.EndTunnelingVignette(this);
    }


    public VignetteParameters vignetteParameters { get; }
}
