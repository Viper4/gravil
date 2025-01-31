using MyUnityAddons.Calculations;
using UnityEngine;
using System.Collections;

public class PlayerSpawn : MonoBehaviour
{
    private Collider _collider;

    [SerializeField] private Vector3 defaultGravityDirection;

    private IEnumerator AddResetListener()
    {
        yield return new WaitWhile(() => PlayerControl.Instance == null);
        PlayerControl.Instance.OnRespawn += ResetPlayer;
    }

    private void OnEnable()
    {
        StartCoroutine(AddResetListener());
    }

    private void OnDisable()
    {
        PlayerControl.Instance.OnRespawn -= ResetPlayer;
    }

    private void Start()
    {
        _collider = GetComponent<Collider>();
        StartCoroutine(ResetPlayerWait());
    }

    private void Update()
    {
        if (PlayerControl.Instance.inputActions.Player.Reset.triggered)
        {
            PlayerControl.Instance.OnRespawn?.Invoke();
            ResetPlayer();
        }
    }
    
    private IEnumerator ResetPlayerWait()
    {
        yield return new WaitWhile(() => PlayerControl.Instance == null);
        ResetPlayer();
    }

    private void ResetPlayer()
    {
        PlayerControl.Instance.isGrounded = false;
        if(defaultGravityDirection != Vector3.zero)
        {
            PlayerControl.Instance.networkGravity.SetDirection(defaultGravityDirection);
        }
        PlayerControl.Instance.transform.position = CustomRandom.GetPointInCollider(_collider);
        PlayerControl.Instance.ResetPlayer();
    }
}
