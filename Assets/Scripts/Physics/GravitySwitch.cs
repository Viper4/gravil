using UnityEngine;

[RequireComponent(typeof(NetworkGravity))]
public class GravitySwitch : MonoBehaviour
{
    private NetworkGravity networkGravity;

    [SerializeField] private Vector3[] directions;
    [SerializeField] private int directionIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        networkGravity = GetComponent<NetworkGravity>();
        IncrementDirectionIndex(0);
    }

    public void IncrementDirectionIndex(int amount)
    {
        directionIndex += amount;
        if (directionIndex < 0)
        {
            directionIndex = directions.Length - 1;
        }
        else if (directionIndex >= directions.Length)
        {
            directionIndex = 0;
        }
        networkGravity.SetDirection(directions[directionIndex]);
    }
}
