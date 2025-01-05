using MyUnityAddons.Calculations;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    private Collider _collider;

    [SerializeField] private Vector3 defaultGravityDirection;

    private void OnEnable()
    {
        PlayerControl.Instance.OnRespawn += ResetPlayer;
    }

    private void OnDisable()
    {
        PlayerControl.Instance.OnRespawn -= ResetPlayer;
    }

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        ResetPlayer();
    }

    private void Update()
    {
        if (PlayerControl.Instance.inputActions.Player.Reset.triggered)
        {
            ResetPlayer();
        }
    }

    private void ResetPlayer()
    {
        PlayerControl.Instance.isGrounded = false;
        if(defaultGravityDirection != Vector3.zero)
        {
            PlayerControl.Instance.gravity.SetDirection(defaultGravityDirection);
        }
        PlayerControl.Instance.transform.position = CustomRandom.GetPointInCollider(_collider);
        PlayerControl.Instance.ResetPlayer();
    }
}
