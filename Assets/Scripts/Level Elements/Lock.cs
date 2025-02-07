using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class Lock : NetworkBehaviour
{
    [SerializeField] private UnityEvent OnUnlock;
    [SerializeField] private UnityEvent OnLock;

    [SerializeField] private int unlockAmount = 1;
    private int currentAmount = 0;

    public void Add()
    {
        currentAmount++;
        if (currentAmount >= unlockAmount)
        {
            currentAmount = unlockAmount;
            OnUnlock?.Invoke();
        }
    }

    public void Remove()
    {
        currentAmount--;
        if(currentAmount < 0)
        {
            currentAmount = 0;
        }
        if (currentAmount < unlockAmount)
        {
            OnLock?.Invoke();
        }
    }
}
