using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] private float speed = 0.75f;
    private float layerHeight;

    public void Initialize(float height, float movementSpeed)
    {
        layerHeight = height;
        speed = movementSpeed;
    }

    private void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;
        if (layerHeight > 0f && transform.position.y <= -layerHeight)
        {
            transform.position += Vector3.up * layerHeight * 2f;
        }
    }
}
