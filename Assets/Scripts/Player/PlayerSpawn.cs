using MyUnityAddons.Calculations;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    private Collider _collider;

    [SerializeField] private int gravityIndex = -1;

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
        if(gravityIndex != -1)
        {
            PlayerControl.Instance.SetGravityIndex(gravityIndex);
        }
        PlayerControl.Instance.transform.position = CustomRandom.GetPointInCollider(_collider);
    }
}
