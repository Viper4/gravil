using UnityEngine;

public class CollisionAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip[] sounds;
    [SerializeField] private float minVolume = 0.5f;
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 1f;

    private void OnCollisionEnter(Collision collision)
    {
        float collisionSpeed = collision.relativeVelocity.magnitude;
        if (collisionSpeed >= minSpeed)
        {
            float volume = Mathf.Lerp(minVolume, maxVolume, collisionSpeed / maxSpeed);
            audioSource.transform.position = collision.GetContact(0).point;
            audioSource.PlayOneShot(sounds[Random.Range(0, sounds.Length)], volume);
        }
    }
}
