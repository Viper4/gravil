using UnityEngine;

public class TextureScroll : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    private Material material;
    private float scrollSpeedX = 0.1f;
    private float scrollSpeedY = 0.1f;

    private void Start()
    {
        material = _renderer.material;
    }

    private void Update()
    {
        material.mainTextureOffset += new Vector2(Time.deltaTime * scrollSpeedX, Time.deltaTime * scrollSpeedY);
    }

    public void SetScrollSpeedX(float speed)
    {
        scrollSpeedX = speed;
    }

    public void SetScrollSpeedY(float speed)
    {
        scrollSpeedY = speed;
    }
}
