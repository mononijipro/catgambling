using System.Collections.Generic;
using UnityEngine;

public class InfiniteRoadLooper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private List<Transform> roadTiles = new List<Transform>();
    [SerializeField] private bool autoPopulateFromChildren = true;
    [SerializeField] private Transform roadTilePrefab;
    [SerializeField] private List<Transform> alternativePrefabs = new List<Transform>();
    [SerializeField] private float alternativePrefabChance = 0.2f;

    [Header("Loop Settings")]
    [SerializeField] private float recycleOffset = 2f;
    [SerializeField] private bool snapTilesOnStart = true;
    [SerializeField] private bool alignRoadToPlayerLane = true;
    [SerializeField] private float laneXOffset = 0f;
    [SerializeField] private float roadY = -3.72f;
    [SerializeField] private bool autoDetectTileLength = true;
    [SerializeField] private float tileLength = 10f;
    [SerializeField] private bool autoGenerateIfTooFewTiles = true;
    [SerializeField] private int generatedTileCount = 8;

    private float lockedRoadX;
    private float lockedRoadY;
    private bool hasLockedRoadAnchor;

    private void Reset()
    {
        if (player == null)
        {
            CatRunnerController runner = FindObjectOfType<CatRunnerController>();
            if (runner != null)
            {
                player = runner.transform;
            }
        }
    }

    private void Awake()
    {
        if (player == null)
        {
            CatRunnerController runner = FindObjectOfType<CatRunnerController>();
            if (runner != null)
            {
                player = runner.transform;
            }
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (autoPopulateFromChildren && roadTiles.Count == 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                roadTiles.Add(transform.GetChild(i));
            }
        }

        // Prefab assets (from Project view) are not scene tiles; capture one as template and remove them from runtime list.
        for (int i = roadTiles.Count - 1; i >= 0; i--)
        {
            Transform tile = roadTiles[i];
            if (tile == null)
            {
                continue;
            }

            if (!tile.gameObject.scene.IsValid())
            {
                if (roadTilePrefab == null)
                {
                    roadTilePrefab = tile;
                }

                roadTiles.RemoveAt(i);
            }
        }

        roadTiles.RemoveAll(tile => tile == null);

        // Keep the list sorted so index 0 is always the furthest-back tile.
        roadTiles.Sort((a, b) => a.position.z.CompareTo(b.position.z));

        if (autoDetectTileLength)
        {
            TryDetectTileLength();
        }

        if (autoGenerateIfTooFewTiles)
        {
            EnsureGeneratedStrip();
        }

        LockRoadAnchor();

        if (snapTilesOnStart)
        {
            SnapTilesIntoStrip();
        }

        if (roadTiles.Count == 0)
        {
            Debug.LogWarning("InfiniteRoadLooper: no runtime road tiles found. Assign scene tiles, add child tiles, or assign Road Tile Prefab and enable auto-generate.", this);
        }
    }

    private void Update()
    {
        if (player == null || roadTiles.Count == 0 || tileLength <= 0.001f)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // Recycle as many tiles as needed this frame when the player is moving fast.
        while (true)
        {
            Transform firstTile = roadTiles[0];
            if (!CanRecycleTile(firstTile))
            {
                break;
            }

            Transform lastTile = roadTiles[roadTiles.Count - 1];
            float targetX = hasLockedRoadAnchor ? lockedRoadX : firstTile.position.x;
            float targetY = hasLockedRoadAnchor ? lockedRoadY : firstTile.position.y;

            // Detect the actual length of the last tile (in case it's a different prefab)
            float lastTileLength = tileLength;
            if (lastTile.TryGetComponent<Renderer>(out Renderer rend))
            {
                lastTileLength = rend.bounds.size.z;
            }

            firstTile.position = new Vector3(
                targetX,
                targetY,
                lastTile.position.z + lastTileLength);

            roadTiles.RemoveAt(0);
            roadTiles.Add(firstTile);
        }
    }

    private bool CanRecycleTile(Transform tile)
    {
        if (tile == null)
        {
            return false;
        }

        // Keep the tile until its forward edge is behind the camera by recycleOffset units.
        if (targetCamera != null)
        {
            float cameraZ = targetCamera.transform.position.z;
            float tileFrontEdgeZ = tile.position.z + tileLength * 0.5f;
            return tileFrontEdgeZ < cameraZ - Mathf.Abs(recycleOffset);
        }

        // Fallback when camera reference is missing.
        return player.position.z > tile.position.z + Mathf.Abs(recycleOffset);
    }

    private void TryDetectTileLength()
    {
        if (roadTiles.Count == 0 || roadTiles[0] == null)
        {
            if (roadTilePrefab == null)
            {
                return;
            }

            Renderer prefabRenderer = roadTilePrefab.GetComponentInChildren<Renderer>();
            if (prefabRenderer != null)
            {
                tileLength = prefabRenderer.bounds.size.z;
            }

            return;
        }

        if (roadTiles.Count >= 2)
        {
            float spacing = Mathf.Abs(roadTiles[1].position.z - roadTiles[0].position.z);
            if (spacing > 0.001f)
            {
                tileLength = spacing;
                return;
            }
        }

        Renderer tileRenderer = roadTiles[0].GetComponentInChildren<Renderer>();
        if (tileRenderer != null)
        {
            tileLength = tileRenderer.bounds.size.z;
        }
    }

    private void EnsureGeneratedStrip()
    {
        int targetCount = Mathf.Max(2, generatedTileCount);
        if (roadTiles.Count >= targetCount || roadTilePrefab == null)
        {
            return;
        }

        float startZ = player != null ? player.position.z - recycleOffset : transform.position.z;
        float anchorX = transform.position.x;
        float anchorY = roadY;

        // Existing first tile anchors the strip; otherwise start from player zone.
        if (roadTiles.Count > 0)
        {
            startZ = roadTiles[0].position.z;
            anchorX = roadTiles[0].position.x;
            anchorY = roadTiles[0].position.y;
        }
        else if (roadTilePrefab != null && roadTilePrefab.gameObject.scene.IsValid())
        {
            anchorX = roadTilePrefab.position.x;
            anchorY = roadTilePrefab.position.y;
        }

        if (!hasLockedRoadAnchor)
        {
            if (alignRoadToPlayerLane && player != null)
            {
                anchorX = player.position.x + laneXOffset;
                anchorY = roadY;
            }
        }
        else
        {
            anchorX = lockedRoadX;
            anchorY = lockedRoadY;
        }

        while (roadTiles.Count < targetCount)
        {
            int index = roadTiles.Count;
            Vector3 spawnPos = new Vector3(anchorX, anchorY, startZ + tileLength * index);
            
            // Select either the main prefab or a random alternative
            Transform prefabToUse = roadTilePrefab;
            if (alternativePrefabs.Count > 0 && Random.value < alternativePrefabChance)
            {
                prefabToUse = alternativePrefabs[Random.Range(0, alternativePrefabs.Count)];
            }
            
            Transform tile = Instantiate(prefabToUse, spawnPos, Quaternion.identity, transform);
            roadTiles.Add(tile);
        }
    }

    private void SnapTilesIntoStrip()
    {
        if (roadTiles.Count <= 1 || tileLength <= 0.001f)
        {
            return;
        }

        float startZ = roadTiles[0].position.z;
        float anchorX = hasLockedRoadAnchor ? lockedRoadX : roadTiles[0].position.x;
        float anchorY = hasLockedRoadAnchor ? lockedRoadY : roadTiles[0].position.y;

        roadTiles[0].position = new Vector3(anchorX, anchorY, startZ);
        for (int i = 1; i < roadTiles.Count; i++)
        {
            Transform tile = roadTiles[i];
            tile.position = new Vector3(anchorX, anchorY, startZ + tileLength * i);
        }
    }

    private void LockRoadAnchor()
    {
        if (hasLockedRoadAnchor)
        {
            return;
        }

        if (roadTiles.Count > 0 && roadTiles[0] != null)
        {
            lockedRoadX = roadTiles[0].position.x;
            lockedRoadY = roadTiles[0].position.y;
            hasLockedRoadAnchor = true;
            return;
        }

        if (alignRoadToPlayerLane && player != null)
        {
            lockedRoadX = player.position.x + laneXOffset;
            lockedRoadY = roadY;
            hasLockedRoadAnchor = true;
            return;
        }

        lockedRoadX = transform.position.x;
        lockedRoadY = roadY;
        hasLockedRoadAnchor = true;
    }
}