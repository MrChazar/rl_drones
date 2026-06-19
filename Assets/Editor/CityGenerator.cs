using UnityEngine;
using UnityEditor;

// Tools → Generate City
// Builds a small 3x3-block city (4 intersections) from the POLYGON city pack.
// Adjust BLOCK and ROAD at the top if tiles don't line up.
public static class CityGenerator
{
    const float BLOCK = 40f;   // city block width (world units)
    const float ROAD  = 10f;   // road width (world units)

    const string PACK = "Assets/POLYGON city pack/Prefabs/";

    // 9 buildings, one per block — cycled round-robin
    static readonly string[] Buildings =
    {
        "Buildings/Building_A_prefab",
        "Buildings/Building_B_prefab",
        "Buildings/Building_D_prefab",
        "Buildings/Building_E_prefab",
        "Buildings/Building_H_prefab",
        "Buildings/Building_J_prefab",
        "Buildings/Building_K_prefab",
        "Buildings/Building_M_prefab",
        "Buildings/Building_S_prefab",
    };

    [MenuItem("Tools/Generate City")]
    public static void Generate()
    {
        var existing = GameObject.Find("City");
        if (existing != null)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Generate City",
                "A 'City' object already exists. Replace it?",
                "Replace", "Cancel");
            if (!confirm) return;
            Undo.DestroyObjectImmediate(existing);
        }

        var root = new GameObject("City");
        Undo.RegisterCreatedObjectUndo(root, "Generate City");

        // 5x5 logical grid:
        //   even column/row index  → city block
        //   odd  column/row index  → road
        //   odd+odd                → 4-way intersection

        int buildingIdx = 0;

        for (int gz = 0; gz < 5; gz++)
        {
            for (int gx = 0; gx < 5; gx++)
            {
                float cx = CenterOf(gx);
                float cz = CenterOf(gz);
                Vector3 pos = new Vector3(cx, 0f, cz);

                bool isRoadX = (gx % 2 == 1);
                bool isRoadZ = (gz % 2 == 1);

                if (isRoadX && isRoadZ)
                {
                    PlacePrefab("Floor/Street 3 Prefab", pos, 0f, root);

                    float offset = ROAD * 0.55f;
                    PlacePrefab("Lamps/Lamp_1_prefab", pos + new Vector3( offset, 0f,  offset), 225f, root);
                    PlacePrefab("Lamps/Lamp_1_prefab", pos + new Vector3(-offset, 0f,  offset), 135f, root);
                    PlacePrefab("Lamps/Lamp_1_prefab", pos + new Vector3( offset, 0f, -offset),  45f, root);
                    PlacePrefab("Lamps/Lamp_1_prefab", pos + new Vector3(-offset, 0f, -offset), 315f, root);
                }
                else if (isRoadX)
                {
                    // Road running along Z axis — rotate 90°
                    PlacePrefab("Floor/Street 4 Prefab", pos, 90f, root);
                }
                else if (isRoadZ)
                {
                    // Road running along X axis
                    PlacePrefab("Floor/Street 4 Prefab", pos, 0f, root);
                }
                else
                {
                    float yRot = (buildingIdx % 4) * 90f;
                    PlacePrefab(Buildings[buildingIdx % Buildings.Length], pos, yRot, root);
                    buildingIdx++;
                }
            }
        }

        Selection.activeGameObject = root;
        Debug.Log($"[CityGenerator] Done — {root.transform.childCount} objects placed. " +
                  $"Press F in Scene View to frame the city.");
    }

    // World-space center for grid index i (0..4).
    // Layout per axis: [BLOCK][ROAD][BLOCK][ROAD][BLOCK]
    static float CenterOf(int i)
    {
        float start = ((i + 1) / 2) * BLOCK + (i / 2) * ROAD;   // integer division
        float size  = (i % 2 == 0) ? BLOCK : ROAD;
        return start + size * 0.5f;
    }

    static void PlacePrefab(string path, Vector3 pos, float yRot, GameObject parent)
    {
        string fullPath = PACK + path + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[CityGenerator] Prefab not found: {fullPath}");
            return;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yRot, 0f));
    }
}
