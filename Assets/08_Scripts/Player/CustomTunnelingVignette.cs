using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CustomTunnelingVignette : MonoBehaviour, ITunnelingVignetteProvider
{
    [SerializeField] private TunnelingVignetteController _tunnelingVignetteController;

    public void FadeOut()
    {
        _tunnelingVignetteController.BeginTunnelingVignette(this);
    }

    public void FadeIn()
    {
        _tunnelingVignetteController.EndTunnelingVignette(this);
    }


    public VignetteParameters vignetteParameters { get; }
}
