using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform reticle;

    [SerializeField] private float dstFromTarget = 4;
    [SerializeField] private Vector2 targetDstMinMax = new Vector2(0, 30);

    [SerializeField] private Vector2 pitchMinMax3rdPerson = new Vector2(-40, 80);
    [SerializeField] private Vector2 pitchMinMax1stPerson = new Vector2(-20, 20);

    [SerializeField] private bool cameraSmoothing = true;
    [SerializeField] private float rotationSmoothing = 0.05f;
    private Vector3 rotationSmoothVelocity;
    private Vector3 currentRotation;

    [SerializeField] private LayerMask ignoreLayers;

    private float yaw;
    private float pitch;
    private float lastYaw;

    [SerializeField] private float sensitivity = 5f;
    [SerializeField] private float zoomRate;

    private Vector3 lastParentUp;

    private bool cursorLockToggle = false;

    private void Start()
    {
        // If target is not set, automatically set it to the parent
        if (target == null)
        {
            target = transform.parent;
        }
    }

    private void Update()
    {
        if (!PlayerControl.Instance.IsPaused)
        {
            if (GameManager.Instance.inputActions.Player.LockCursor.triggered)
            {
                cursorLockToggle = !cursorLockToggle;
            }

            // Translating inputs from mouse into smoothed rotation of camera
            Vector2 look = GameManager.Instance.inputActions.Player.Look.ReadValue<Vector2>();
            yaw += look.x * sensitivity;
            pitch -= look.y * sensitivity;

            // Offsetting pitch of parent. Dont need to do this for yaw since we directly rotate the parent for yaw
            float pitchDelta = Vector3.SignedAngle(transform.parent.up, lastParentUp, transform.right);
            pitch += pitchDelta;
            currentRotation.x += pitchDelta; // Ignore smoothing when offsetting pitch

            // Zoom with scroll
            float scroll = GameManager.Instance.inputActions.Player.Scroll.ReadValue<float>();
            if (scroll > 0)
            {
                dstFromTarget = Mathf.Clamp(dstFromTarget - zoomRate, targetDstMinMax.x, targetDstMinMax.y);
                if (dstFromTarget == 0)
                {
                    foreach (MeshRenderer faceRenderer in PlayerControl.Instance.faceRenderers)
                    {
                        faceRenderer.enabled = false;
                    }
                }
            }
            else if (scroll < 0)
            {
                dstFromTarget = Mathf.Clamp(dstFromTarget + zoomRate, targetDstMinMax.x, targetDstMinMax.y);
                if (dstFromTarget > 0)
                {
                    foreach (MeshRenderer faceRenderer in PlayerControl.Instance.faceRenderers)
                    {
                        faceRenderer.enabled = true;
                    }
                }
            }

            reticle.gameObject.SetActive(true);
            Cursor.visible = false;

            if (dstFromTarget == 0)
            {
                Cursor.lockState = CursorLockMode.Locked;
                reticle.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
                pitch = Mathf.Clamp(pitch, pitchMinMax1stPerson.x, pitchMinMax1stPerson.y);
                currentRotation.x = Mathf.Clamp(currentRotation.x, pitchMinMax1stPerson.x, pitchMinMax1stPerson.y);
            }
            else
            {
                if (cursorLockToggle)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Confined;
                }
                reticle.position = GameManager.Instance.inputActions.UI.Point.ReadValue<Vector2>();
                pitch = Mathf.Clamp(pitch, pitchMinMax3rdPerson.x, pitchMinMax3rdPerson.y);
                currentRotation.x = Mathf.Clamp(currentRotation.x, pitchMinMax3rdPerson.x, pitchMinMax3rdPerson.y);
            }

            // Apply pitch and yaw
            if (cameraSmoothing)
            {
                currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw, 0f), ref rotationSmoothVelocity, rotationSmoothing);
            }
            else
            {
                currentRotation = new Vector3(pitch, yaw, 0f);
            }
            transform.localEulerAngles = new Vector3(currentRotation.x, 0f, 0f);
            transform.parent.Rotate(transform.parent.up, currentRotation.y - lastYaw, Space.World);
            lastYaw = currentRotation.y;
        }

        // Prevent clipping of camera
        if (Physics.Raycast(target.position, -transform.forward, out RaycastHit clippingHit, dstFromTarget, ~ignoreLayers))
        {
            transform.position = clippingHit.point + transform.forward * 0.1f;
        }
        else
        {
            transform.position = target.position - transform.forward * dstFromTarget;
        }

        lastParentUp = transform.parent.up;
    }
}