using UnityEngine;

// MinimapMarker creates a colored circle on the minimap
// at the position of a world object.

public class MinimapMarker : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Material markerMaterial;
    [SerializeField] private float markerSize = 2f;

    private GameObject _marker;

    private void Start()
    {
        CreateMarker();
    }

    private void CreateMarker()
    {
        _marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _marker.name = $"MinimapMarker_{gameObject.name}";

        Destroy(_marker.GetComponent<Collider>());

        _marker.layer = LayerMask.NameToLayer("Minimap");

        // Create a new material instance with circle texture
        if (markerMaterial != null)
        {
            Material mat = new Material(markerMaterial);
            mat.mainTexture = GenerateCircleTexture(64);
            _marker.GetComponent<Renderer>().material = mat;
        }

        _marker.transform.localScale = new Vector3(markerSize, markerSize, 1f);
        _marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private Texture2D GenerateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;

        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, dist <= radius ? white : transparent);
            }
        }

        texture.Apply();
        return texture;
    }

    private void LateUpdate()
    {
        if (_marker == null) return;

        _marker.transform.position = new Vector3(
            transform.position.x,
            0.25f,
            transform.position.z
        );
    }

    private void OnDestroy()
    {
        if (_marker != null)
            Destroy(_marker);
    }

    public void SetVisible(bool visible)
    {
        if (_marker != null)
            _marker.SetActive(visible);
    }
}