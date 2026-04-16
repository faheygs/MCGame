using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class MinimapArrowGenerator : MonoBehaviour
{
    [SerializeField] private Color arrowColor = new Color(0.2f, 0.6f, 1f);

    private void Start()
    {
        Texture2D texture = GenerateDirectionalMarker(128, 128);
        GetComponent<Renderer>().material.mainTexture = texture;
    }

    private Texture2D GenerateDirectionalMarker(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;

        // Clear
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, transparent);

        // Draw filled triangle using barycentric coordinates
        // Three points: tip at top center, bottom-left, bottom-right
        Vector2 p1 = new Vector2(width * 0.5f, height * 0.95f);  // tip
        Vector2 p2 = new Vector2(width * 0.05f, height * 0.2f);  // bottom left
        Vector2 p3 = new Vector2(width * 0.95f, height * 0.2f);  // bottom right

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);

                if (PointInTriangle(p, p1, p2, p3))
                    texture.SetPixel(x, y, arrowColor);
            }
        }

        // Draw notch at the base to make direction obvious
        // Cut a triangle out of the base center
        Vector2 n1 = new Vector2(width * 0.5f, height * 0.45f); // notch tip
        Vector2 n2 = new Vector2(width * 0.3f, height * 0.2f);  // notch left
        Vector2 n3 = new Vector2(width * 0.7f, height * 0.2f);  // notch right

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);

                if (PointInTriangle(p, n1, n2, n3))
                    texture.SetPixel(x, y, transparent);
            }
        }

        // Add white outline for visibility
        AddOutline(texture, arrowColor, white, 2);

        texture.Apply();
        return texture;
    }

    private void AddOutline(Texture2D texture, Color fillColor, Color outlineColor, int thickness)
    {
        int width = texture.width;
        int height = texture.height;
        Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                copy.SetPixel(x, y, texture.GetPixel(x, y));

        copy.Apply();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (copy.GetPixel(x, y).a < 0.1f)
                {
                    // Check if any neighbor is filled
                    for (int dy = -thickness; dy <= thickness; dy++)
                    {
                        for (int dx = -thickness; dx <= thickness; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (copy.GetPixel(nx, ny).a > 0.1f)
                                {
                                    texture.SetPixel(x, y, outlineColor);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}