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
    [SerializeField] private InputActionProperty[] stateInputActions;

    [SerializeField] private bool wrapAround = false;
    [SerializeField] private InputActionProperty incrementAction;
    [SerializeField] private InputActionProperty decrementAction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OnChangeState?.Invoke(state);
        TryInvokeStateEvent(state);
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
            else if (stateInputActions != null)
            {
                for (int i = 0; i < stateInputActions.Length; i++)
                {
                    if (stateInputActions[i].action != null && stateInputActions[i].action.triggered)
                    {
                        SetState(i);
                    }
                }
            }
        }
        else
        {
            popup.Hide();
        }
    }

    private void TryInvokeStateEvent(int index)
    {
        if (index >= 0 && index < StateEvents.Length)
        {
            StateEvents[index]?.Invoke();
        }
    }

    private void UpdateLocalState(int newState)
    {
        state = newState;
        OnChangeState?.Invoke(state);
        TryInvokeStateEvent(state);
    }

    public void SetState(int newState)
    {
        if (state == newState)
            return;

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

        UpdateLocalState(newState);
        SendStateRpc(newState);
    }

    [Rpc(SendTo.NotMe)]
    private void SendStateRpc(int newState)
    {
        UpdateLocalState(newState);
    }

    private void UpdateLocalStateSilent(int newState)
    {
        state = newState;
    }

    public void SetStateSilent(int newState)
    {
        newState = Mathf.Clamp(newState, 0, totalStates - 1);
        SendStateSilentRpc(newState);
        UpdateLocalStateSilent(newState);
    }

    [Rpc(SendTo.NotMe)]
    private void SendStateSilentRpc(int newState)
    {
        UpdateLocalStateSilent(newState);
    }
}
