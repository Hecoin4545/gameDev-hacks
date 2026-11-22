using UnityEngine;

public class GridManager : MonoBehaviour
{
    public Material safeMat;
    public Material unsafeMat;
    public Material tileEdgeMat;
    public Material tileRockMat;

    public void Build(LevelData data)
    {
        foreach (Transform c in transform) Destroy(c.gameObject);

        float size = data.tileSize;
        float gap = data.tileGap;
        float stride = size + gap;
        int cols = data.columns;
        int rows = data.rows;

        float originX = -(cols - 1) * stride / 2f;
        float originZ = -(rows - 1) * stride / 2f;

        float slabHeight = 0.22f;
        float slabTop = slabHeight * 0.5f;

        Material topMaterial = safeMat != null ? safeMat : unsafeMat;
        Material baseMaterial = tileEdgeMat != null ? tileEdgeMat : topMaterial;
        Material rockMaterial = tileRockMat != null ? tileRockMat : baseMaterial;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = new Vector3(originX + c * stride, 0f, originZ + r * stride);
                GameObject go = new GameObject(string.Format("Tile_R{0}_C{1}", r + 1, c + 1));
                go.transform.SetParent(transform);
                go.transform.position = pos;

                // Chamfered black top slab (walkable).
                var slabMesh = BeveledBoxMesh.Create(size, slabHeight, size, 0.075f);
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = slabMesh;
                var mr = go.AddComponent<MeshRenderer>();
                if (topMaterial != null) mr.sharedMaterial = topMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;

                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(size, slabHeight, size);
                box.center = Vector3.zero;

                // Chunky stone base, like a floating rock platform.
                float baseHeight = 1.05f;
                AddMeshChild(go.transform, "StoneBase",
                    BeveledBoxMesh.Create(size * 0.86f, baseHeight, size * 0.86f, 0.12f),
                    baseMaterial,
                    new Vector3(0f, -slabTop - baseHeight * 0.5f, 0f));

                float rockHeight = 0.95f;
                AddMeshChild(go.transform, "RockTip",
                    BeveledBoxMesh.Create(size * 0.52f, rockHeight, size * 0.52f, 0.1f),
                    rockMaterial,
                    new Vector3(0f, -slabTop - baseHeight - rockHeight * 0.5f, 0f));

                var tile = go.AddComponent<Tile>();
                tile.row = r + 1;
                tile.column = c + 1;

                bool safe = data.safeTiles.Contains(new Vector2Int(r + 1, c + 1));
                tile.isSafe = safe;

                tile.stepIndex = -1;
                for (int i = 0; i < data.sequence.Count; i++)
                {
                    if (data.sequence[i].x == r + 1 && data.sequence[i].y == c + 1)
                    {
                        tile.stepIndex = i;
                        break;
                    }
                }

                tile.rend = mr;
                tile.ApplyColor(false);
            }
        }
    }

    private static void AddMeshChild(Transform parent, string name, Mesh mesh, Material material, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        if (material != null) mr.sharedMaterial = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
    }
}
