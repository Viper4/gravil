using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Gravity))]
public class BuoyancyObject : MonoBehaviour
{
    private Rigidbody attachedRigidbody;
    private Gravity gravity;

    [SerializeField] private Transform[] floaters;

    [SerializeField] private float waterDrag = 3f;
    [SerializeField] private float waterAngularDrag = 1f;

    [SerializeField] private float airDrag = 0f;
    [SerializeField] private float airAngularDrag = 0.05f;

    [SerializeField] private float floatPower = 15f;

    public float waterHeight;

    private bool underwater = false;
    private int floatersUnderwater = 0;

    private void Start()
    {
        attachedRigidbody = GetComponent<Rigidbody>();
        gravity = GetComponent<Gravity>();
    }

    private void FixedUpdate()
    {
        floatersUnderwater = 0;
        for(int i = 0; i < floaters.Length; i++)
        {
            float difference = transform.position.y - waterHeight;
            if (difference < 0)
            {
                attachedRigidbody.AddForceAtPosition(floatPower * Mathf.Abs(difference) * -gravity.GetDirection(), floaters[i].position, ForceMode.Force);

                floatersUnderwater++;

                if (!underwater)
                {
                    attachedRigidbody.linearDamping = waterDrag;
                    attachedRigidbody.angularDamping = waterAngularDrag;
                    underwater = true;
                }
            }
        }

        if (underwater && floatersUnderwater == 0)
        {
            attachedRigidbody.linearDamping = airDrag;
            attachedRigidbody.angularDamping = airAngularDrag;
            underwater = false;
        }
    }
}
