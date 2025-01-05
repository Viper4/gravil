using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Switch : NetworkBehaviour
{
    [SerializeField] private int state = 0;
    [SerializeField] private int totalStates = 2;
    [SerializeField] private Popup popup;

    [SerializeField] private UnityEvent<int> OnChangeState;
    [SerializeField] private UnityEvent[] StateEvents;

    [SerializeField] private bool wrapAround = false;
    [SerializeField] private InputActionProperty incrementAction;
    [SerializeField] private InputActionProperty decrementAction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OnChangeState?.Invoke(state);
        StateEvents[state]?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        if ((PlayerControl.Instance.HoveredObject == transform || (PlayerControl.Instance.HoveredObject != null && PlayerControl.Instance.HoveredObject.IsChildOf(transform))) && !PlayerControl.Instance.isDead)
        {
            popup.Show();

            if (incrementAction.action != null && incrementAction.action.triggered)
            {
                SetState(state + 1);
            }
            else if (decrementAction.action != null && decrementAction.action.triggered)
            {
                SetState(state - 1);
            }
        }
        else
        {
            popup.Hide();
        }
    }

    private void UpdateLocalState(int newState)
    {
        Debug.Log("Update local state to " + newState);
        state = newState;
        OnChangeState?.Invoke(state);
        StateEvents[state]?.Invoke();
    }

    public void SetState(int newState)
    {
        if (wrapAround)
        {
            if(newState > totalStates - 1)
            {
                newState = 0;
            }
            else if(newState < 0)
            {
                newState = totalStates - 1;
            }
        }
        else
        {
            newState = Mathf.Clamp(newState, 0, totalStates - 1);
        }

        if (IsServer)
        {
            SetStateClientRpc(newState);
            UpdateLocalState(newState);
        }
        else
        {
            SetStateServerRpc(newState);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void SetStateClientRpc(int newState)
    {
        if (!IsServer)
            UpdateLocalState(newState);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetStateServerRpc(int newState)
    {
        SetState(newState);
    }

    private void UpdateLocalStateSilent(int newState)
    {
        state = newState;
    }

    public void SetStateSilent(int newState)
    {
        newState = Mathf.Clamp(newState, 0, totalStates - 1);
        if (IsServer)
        {
            SetStateSilentClientRpc(newState);
            UpdateLocalStateSilent(newState);
        }
        else
        {
            SetStateSilentServerRpc(newState);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void SetStateSilentClientRpc(int newState)
    {
        if (!IsServer)
            SetStateSilent(newState);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetStateSilentServerRpc(int newState)
    {
        SetStateSilent(newState);
    }
}
