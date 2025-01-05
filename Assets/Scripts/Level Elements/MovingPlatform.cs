using UnityEngine;

[RequireComponent(typeof(SmoothTransform))]
public class MovingPlatform : MonoBehaviour
{
    private SmoothTransform smoothTransform;
    [SerializeField] private Transform[] navPoints;

    private int currentIndex = 0;
    private int targetIndex = 0;

    private void Awake()
    {
        smoothTransform = GetComponent<SmoothTransform>();
        smoothTransform.OnPositionReached.AddListener((ctx) => currentIndex = targetIndex);
    }

    public void MoveNext(int amount)
    {
        targetIndex = currentIndex + amount;
        targetIndex = Mathf.Clamp(targetIndex, 0, navPoints.Length - 1);

        smoothTransform.StartMove(navPoints[targetIndex].position);
        smoothTransform.StartRotate(navPoints[targetIndex].rotation);
    }

    public void Stop()
    {
        smoothTransform.StopMove();
        smoothTransform.StopRotate();
    }

    public void SetPoint(int index)
    {
        transform.SetPositionAndRotation(navPoints[index].position, navPoints[index].rotation);
        currentIndex = index;
        targetIndex = index;
    }
}
