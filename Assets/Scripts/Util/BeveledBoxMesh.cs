using UnityEngine;

/// <summary>
/// Builds a simple chamfered box mesh. Used for tiles, island platforms and rocks
/// so edges catch the light instead of looking like flat programmer cubes.
/// </summary>
public static class BeveledBoxMesh
{
    public static Mesh Create(float width, float height, float depth, float bevel)
    {
        bevel = Mathf.Clamp(bevel, 0.001f, Mathf.Min(width, depth) * 0.35f);
        bevel = Mathf.Min(bevel, height * 0.35f);

        float hx = width * 0.5f;
        float hy = height * 0.5f;
        float hz = depth * 0.5f;
        float bx = hx - bevel;
        float bz = hz - bevel;
        float chamferY = hy - bevel;

        Vector3[] verts = new Vector3[12];
        // Bottom ring (full footprint)
        verts[0] = new Vector3(-hx, -hy, -hz);
        verts[1] = new Vector3(hx, -hy, -hz);
        verts[2] = new Vector3(hx, -hy, hz);
        verts[3] = new Vector3(-hx, -hy, hz);
        // Chamfer ring (full footprint, just below the top)
        verts[4] = new Vector3(-hx, chamferY, -hz);
        verts[5] = new Vector3(hx, chamferY, -hz);
        verts[6] = new Vector3(hx, chamferY, hz);
        verts[7] = new Vector3(-hx, chamferY, hz);
        // Top ring (inset)
        verts[8] = new Vector3(-bx, hy, -bz);
        verts[9] = new Vector3(bx, hy, -bz);
        verts[10] = new Vector3(bx, hy, bz);
        verts[11] = new Vector3(-bx, hy, bz);

        int[] tris = new int[4 * 12 + 12];
        int t = 0;
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            // Vertical side.
            tris[t++] = i;
            tris[t++] = i + 4;
            tris[t++] = j + 4;
            tris[t++] = i;
            tris[t++] = j + 4;
            tris[t++] = j;
            // Chamfer strip.
            tris[t++] = i + 4;
            tris[t++] = i + 8;
            tris[t++] = j + 8;
            tris[t++] = i + 4;
            tris[t++] = j + 8;
            tris[t++] = j + 4;
        }

        // Top face (facing up).
        tris[t++] = 8;
        tris[t++] = 10;
        tris[t++] = 9;
        tris[t++] = 8;
        tris[t++] = 11;
        tris[t++] = 10;

        // Bottom face (facing down).
        tris[t++] = 0;
        tris[t++] = 1;
        tris[t++] = 2;
        tris[t++] = 0;
        tris[t++] = 2;
        tris[t++] = 3;

        var mesh = new Mesh();
        mesh.name = "BeveledBox";
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
