using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestParticleSys : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particle;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _particle.Play();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            _particle.Stop();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            _particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
