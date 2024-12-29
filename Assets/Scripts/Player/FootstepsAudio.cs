using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepsAudio : NetworkBehaviour
{
    [System.Serializable]
    private struct GroundClips
    {
        public string groundTag;
        public AudioClip[] clips;
    }

    private AudioSource audioSource;

    private bool isWalking = false;
    private bool isRunning = false;
    [SerializeField] private float walkStepInterval = 0.35f;
    [SerializeField] private float runStepInterval = 0.2f;
    private float timer = 0;

    [SerializeField] private GroundClips[] walkClips;
    [SerializeField] private GroundClips[] runClips;
    [SerializeField] private GroundClips[] jumpClips;
    [SerializeField] private GroundClips[] landClips;

    private Dictionary<string, AudioClip[]> footstepClips = new Dictionary<string, AudioClip[]>();
    private string groundTag = "Air";

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        foreach(GroundClips groundClips in walkClips)
        {
            footstepClips.Add("Walk" + groundClips.groundTag, groundClips.clips);
        }
        foreach (GroundClips groundClips in runClips)
        {
            footstepClips.Add("Run" + groundClips.groundTag, groundClips.clips);
        }
        foreach (GroundClips groundClips in jumpClips)
        {
            footstepClips.Add("Jump" + groundClips.groundTag, groundClips.clips);
        }
        foreach (GroundClips groundClips in landClips)
        {
            footstepClips.Add("Land" + groundClips.groundTag, groundClips.clips);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (isWalking && groundTag != "Air")
        {
            if (isRunning)
            {
                if (timer >= runStepInterval)
                {
                    if (!footstepClips.TryGetValue("Run" + groundTag, out AudioClip[] runClips))
                    {
                        runClips = footstepClips.First((kvp) => kvp.Key.Contains("Run")).Value;
                    }
                    AudioClip audioClip = runClips[Random.Range(0, runClips.Length)];
                    audioSource.PlayOneShot(audioClip);
                    timer = 0;
                }
            }
            else
            {
                if (timer >= walkStepInterval)
                {
                    if (!footstepClips.TryGetValue("Walk" + groundTag, out AudioClip[] walkClips))
                    {
                        walkClips = footstepClips.First((kvp) => kvp.Key.Contains("Walk")).Value;
                    }
                    AudioClip audioClip = walkClips[Random.Range(0, walkClips.Length)];
                    audioSource.PlayOneShot(audioClip);
                    timer = 0;
                }
            }
        }
    }

    public void StartWalking()
    {
        isWalking = true;
        if (IsOwner)
        {
            if (IsServer)
            {
                SetWalkingClientRpc(true);
            }
            else
            {
                SetWalkingServerRpc(true);
            }
        }
    }

    public void StopWalking()
    {
        isWalking = false;
        if (IsOwner)
        {
            if (IsServer)
            {
                SetWalkingClientRpc(false);
            }
            else
            {
                SetWalkingServerRpc(false);
            }
        }
    }

    public void StartRunning()
    {
        isRunning = true;
    }

    public void StopRunning()
    {
        isRunning = false;
    }

    [ClientRpc(RequireOwnership = false)]
    private void SetWalkingClientRpc(bool isWalking)
    {
        if (IsOwner)
            return;
        
        this.isWalking = isWalking;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetWalkingServerRpc(bool isWalking)
    {
        this.isWalking = isWalking;
        SetWalkingClientRpc(isWalking);
    }

    public void PlayJump()
    {
        if (groundTag == "Air")
            return;

        if (!footstepClips.TryGetValue("Jump" + groundTag, out AudioClip[] jumpClips))
        {
            jumpClips = footstepClips.First((kvp) => kvp.Key.Contains("Jump")).Value;
        }
        audioSource.PlayOneShot(jumpClips[Random.Range(0, jumpClips.Length)]);
        if (IsOwner)
        {
            if (IsServer)
            {
                PlayClipClientRpc("Jump" + groundTag);
            }
            else
            {
                PlayClipServerRpc("Jump" + groundTag);
            }
        }
    }

    public void PlayLand()
    {
        if (groundTag == "Air")
            return;

        if (!footstepClips.TryGetValue("Land" + groundTag, out AudioClip[] landClips))
        {
            landClips = footstepClips.First((kvp) => kvp.Key.Contains("Land")).Value;
        }
        audioSource.PlayOneShot(landClips[Random.Range(0, landClips.Length)]);
        if (IsOwner)
        {
            if (IsServer)
            {
                PlayClipClientRpc("Land" + groundTag);
            }
            else
            {
                PlayClipServerRpc("Land" + groundTag);
            }
        }
    }

    public void ChangeGround(string groundTag)
    {
        this.groundTag = groundTag;
        if (IsOwner)
        {
            if (IsServer)
            {
                SetGroundTagClientRpc(groundTag);
            }
            else
            {
                SetGroundTagServerRpc(groundTag);
            }
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void PlayClipClientRpc(string key)
    {
        if (IsOwner)
            return;

        if (footstepClips.TryGetValue(key, out AudioClip[] audioClips))
        {
            audioSource.PlayOneShot(audioClips[Random.Range(0, audioClips.Length)]);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayClipServerRpc(string key)
    {
        if (footstepClips.TryGetValue(key, out AudioClip[] audioClips))
        {
            audioSource.PlayOneShot(audioClips[Random.Range(0, audioClips.Length)]);
        }
        PlayClipClientRpc(key);
    }

    [ClientRpc(RequireOwnership = false)]
    private void SetGroundTagClientRpc(string groundTag)
    {
        if (IsOwner)
            return;

        this.groundTag = groundTag;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetGroundTagServerRpc(string groundTag)
    {
        this.groundTag = groundTag;
        SetGroundTagClientRpc(groundTag);
    }
}
