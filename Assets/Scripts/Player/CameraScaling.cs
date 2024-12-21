using UnityEngine;

public class CameraScaling : MonoBehaviour
{
    [SerializeField] private float scaleFactor = 0.1f;

    private void FixedUpdate()
    {
        UpdateScale();
    }

    public void UpdateScale()
    {
        if (Camera.main != null)
        {
            float scale = Vector3.Distance(Camera.main.transform.position, transform.position) * scaleFactor;
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
