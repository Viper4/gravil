using UnityEngine;

public class GravityOffset : MonoBehaviour
{
    [SerializeField] private Transform origin;
    [SerializeField] private float offset;

    private void Update()
    {
        transform.position = origin.position - PlayerControl.Instance.gravity.direction * offset;
    }
}
