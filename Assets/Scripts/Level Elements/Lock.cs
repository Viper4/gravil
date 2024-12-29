using UnityEngine;
using UnityEngine.Events;

public class Lock : MonoBehaviour
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
            OnUnlock?.Invoke();
        }
    }

    public void Remove()
    {
        currentAmount--;
        if (currentAmount < unlockAmount)
        {
            OnLock?.Invoke();
        }
    }
}
