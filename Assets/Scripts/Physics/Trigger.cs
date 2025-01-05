using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Trigger : MonoBehaviour
{
    [SerializeField] LayerMask ignoreLayers;
    [SerializeField] private string[] triggerTags;
    public HashSet<string> tagHashSet = new HashSet<string>();
    [SerializeField] private UnityEvent<Collider> onTriggerEnter;
    [SerializeField] private UnityEvent<Collider> onTriggerExit;
    [SerializeField] private UnityEvent<Collision> onCollisionEnter;
    [SerializeField] private UnityEvent<Collision> onCollisionExit;

    [SerializeField] private UnityEvent<Collider> onAnyTriggerEnter;
    [SerializeField] private UnityEvent<Collider> onAnyTriggerExit;
    [SerializeField] private UnityEvent<Collision> onAnyCollisionEnter;
    [SerializeField] private UnityEvent<Collision> onAnyCollisionExit;
    public int collidersInTrigger = 0;

    private void Start()
    {
        InitializeHashSet();
    }

    public void InitializeHashSet()
    {
        foreach (string tag in triggerTags)
        {
            if (!tagHashSet.Contains(tag))
                tagHashSet.Add(tag);
        }
    }

    public virtual void TriggerEnter(Collider collider)
    {
        collidersInTrigger++;
        onTriggerEnter?.Invoke(collider);
    }

    public virtual void TriggerExit(Collider collider)
    {
        collidersInTrigger--;
        if(collidersInTrigger <= 0)
            collidersInTrigger = 0;
        onTriggerExit?.Invoke(collider);
    }

    public virtual void CollisionEnter(Collision collision)
    {
        collidersInTrigger++;
        onCollisionEnter?.Invoke(collision);
    }

    public virtual void CollisionExit(Collision collision)
    {
        collidersInTrigger--;
        if (collidersInTrigger <= 0)
            collidersInTrigger = 0;
        onCollisionExit?.Invoke(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((ignoreLayers & (1 << other.gameObject.layer)) == 0)
        {
            if (tagHashSet.Count == 0 || tagHashSet.Contains(other.tag))
                TriggerEnter(other);
            onAnyTriggerEnter?.Invoke(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((ignoreLayers & (1 << other.gameObject.layer)) == 0)
        {
            if (tagHashSet.Count == 0 || tagHashSet.Contains(other.tag))
                TriggerExit(other);
            onAnyTriggerExit?.Invoke(other);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if ((ignoreLayers & (1 << collision.gameObject.layer)) == 0)
        {
            if (tagHashSet.Count == 0 || tagHashSet.Contains(collision.transform.tag))
                CollisionEnter(collision);
            onAnyCollisionEnter?.Invoke(collision);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if ((ignoreLayers & (1 << collision.gameObject.layer)) == 0)
        {
            if (tagHashSet.Count == 0 || tagHashSet.Contains(collision.transform.tag))
                CollisionExit(collision);
            onAnyCollisionExit?.Invoke(collision);
        }
    }

    public void DestroyTarget(Object toDestroy)
    {
        Destroy(toDestroy);
    }

    public void LoadScene(string sceneName)
    {
        SceneLoader.Instance.LoadScene(sceneName);
    }

    public void LoadNextLevel()
    {
        LobbyManager.Instance.LoadNextLevel();
    }
}
