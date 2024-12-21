using UnityEngine;

public class RotateWithCamera : MonoBehaviour
{
    [SerializeField] private Transform parent;

    void Update()
    {
        if(Camera.main != null)
        {
            if (parent != null)
            {
                transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, parent.up);
            }
            else
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}
