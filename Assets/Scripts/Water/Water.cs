using UnityEngine;

public class Water : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    private Vector3[] vertices;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        vertices = meshRenderer.GetComponent<MeshFilter>().mesh.vertices;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float GetHeight(Vector3 position)
    {
        Material material = meshRenderer.material;
        float seed = material.GetFloat("_Seed");
        float angleSpeed = material.GetFloat("_VoronoiAngleSpeed");
        return transform.position.y;
    }
}
