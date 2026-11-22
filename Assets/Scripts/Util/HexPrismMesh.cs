using UnityEngine;

/// <summary>
/// Generates a beveled hexagonal prism mesh.
/// Used for Level 3 cyber tiles, START pad, and GOAL pad to match the neon cyber reference.
/// </summary>
public static class HexPrismMesh
{
    public static Mesh Create(float radius, float height, float bevel = 0.05f)
    {
        bevel = Mathf.Clamp(bevel, 0.001f, radius * 0.35f);
        bevel = Mathf.Min(bevel, height * 0.35f);

        float rTop = radius - bevel;
        float hHalf = height * 0.5f;
        float yBottom = -hHalf;
        float yChamfer = hHalf - bevel;
        float yTop = hHalf;

        Mesh mesh = new Mesh();
        mesh.name = "HexPrism";

        Vector3[] verts = new Vector3[20];
        // 0..5: Bottom ring
        // 6..11: Chamfer ring
        // 12..17: Top ring
        // 18: Center bottom
        // 19: Center top
        verts[18] = new Vector3(0f, yBottom, 0f);
        verts[19] = new Vector3(0f, yTop, 0f);

        for (int i = 0; i < 6; i++)
        {
            float angle = (i * 60f + 30f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            verts[i] = new Vector3(cos * radius, yBottom, sin * radius);
            verts[i + 6] = new Vector3(cos * radius, yChamfer, sin * radius);
            verts[i + 12] = new Vector3(cos * rTop, yTop, sin * rTop);
        }

        // Triangles:
        // 6 side quads = 12 tris
        // 6 chamfer quads = 12 tris
        // 6 top tris = 6 tris
        // 6 bottom tris = 6 tris
        // Total = 36 tris = 108 indices
        int[] tris = new int[36 * 3];
        int t = 0;

        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;

            // Side quad
            tris[t++] = i;
            tris[t++] = i + 6;
            tris[t++] = next + 6;

            tris[t++] = i;
            tris[t++] = next + 6;
            tris[t++] = next;

            // Chamfer quad
            tris[t++] = i + 6;
            tris[t++] = i + 12;
            tris[t++] = next + 12;

            tris[t++] = i + 6;
            tris[t++] = next + 12;
            tris[t++] = next + 6;

            // Top fan
            tris[t++] = 19;
            tris[t++] = i + 12;
            tris[t++] = next + 12;

            // Bottom fan (facing down)
            tris[t++] = 18;
            tris[t++] = next;
            tris[t++] = i;
        }

        Vector2[] uvs = new Vector2[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            uvs[i] = new Vector2(verts[i].x / (radius * 2f) + 0.5f, verts[i].z / (radius * 2f) + 0.5f);
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
