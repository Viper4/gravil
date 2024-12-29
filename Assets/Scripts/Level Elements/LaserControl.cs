using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class LaserControl : NetworkBehaviour
{
    [SerializeField] private Laser[] lasers;
    private float timer;

    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startDelay = 0f;
    private bool started = false;

    [SerializeField] private float inactiveTime = 1f;
    [SerializeField] private float scaleTime = 0.1f;
    [SerializeField] private float emittingTime = 3f;
    [SerializeField] private bool looping = true;

    private bool emitting = false;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(startDelay);
        if (!looping && playOnStart)
            SetEmitting(true);
        started = true;
        timer = 0;
    }

    private void Update()
    {
        if (started && looping)
        {
            if (timer > inactiveTime + emittingTime + scaleTime) // Reset loop
            {
                timer = scaleTime;
                if (IsServer)
                {
                    SetTimerClientRpc(scaleTime); // Synchronize timer
                }
            }
            else if (timer > inactiveTime + emittingTime) // Deactivating laser
            {
                for(int i = 0; i < lasers.Length; i++)
                {
                    lasers[i].Deactivate((timer - inactiveTime - emittingTime) / scaleTime);
                }
            }
            else if (timer > inactiveTime) // Activating laser
            {
                emitting = true;
                for (int i = 0; i < lasers.Length; i++)
                {
                    lasers[i].Activate((timer - inactiveTime) / scaleTime);
                }
            }
            else // Laser is inactive
            {
                StopEmit();
            }
            timer += Time.deltaTime;
        }
    }

    public void ToggleEmitting()
    {
        SetEmitting(!emitting);
    }

    public void SetEmitting(bool value)
    {
        if (value)
        {
            if (!emitting)
            {
                StartCoroutine(StartEmit());
                emitting = true;
            }
        }
        else
        {
            if (emitting)
            {
                StopEmit();
                emitting = false;
            }
        }
    }

    private void StopEmit()
    {
        if (emitting)
        {
            StopAllCoroutines();
            emitting = false;
            for (int i = 0; i < lasers.Length; i++)
            {
                lasers[i].Deactivate(1f);
            }
        }
    }

    private IEnumerator StartEmit()
    {
        float emitTimer = 0;
        while (emitTimer < scaleTime)
        {
            for(int i = 0; i < lasers.Length; i++)
            {
                lasers[i].Activate(emitTimer / scaleTime);
            }
            yield return null;
            emitTimer += Time.deltaTime;
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void SetTimerClientRpc(float timer)
    {
        this.timer = timer;
    }
}
