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
            if (timer > inactiveTime + emittingTime) // Deactivate and reset
            {
                Deactivate();
                ResetTimer();
            }
            else if (timer > inactiveTime) // Laser is active
            {
                Activate();
            }
            else // Laser is inactive
            {
                Deactivate();
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
            Activate();
        }
        else
        {
            Deactivate();
        }
    }

    private void Activate()
    {
        if (emitting)
            return;

        emitting = true;
        for (int i = 0; i < lasers.Length; i++)
        {
            lasers[i].Activate();
        }
    }

    private void ResetTimer()
    {
        timer = 0f;
        if (IsServer)
        {
            SetTimerClientRpc(timer); // Synchronize timer
        }
    }

    private void Deactivate()
    {
        if (!emitting)
            return;

        emitting = false;
        for (int i = 0; i < lasers.Length; i++)
        {
            lasers[i].Deactivate();
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void SetTimerClientRpc(float timer)
    {
        this.timer = timer;
    }
}
