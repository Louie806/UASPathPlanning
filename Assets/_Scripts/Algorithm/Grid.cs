using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour {
    [Header("Grid Settings")]
    [SerializeField] private float voxelSize = 2f;
    [SerializeField] private Vector3 gridPadding = new(1f, 1f, 1f);
    [SerializeField] private float obstacleBuffer = 0.5f;
    [SerializeField] private float baseMoveCost = 1f;

    [Header("Ground Clearance")]
    [SerializeField] private float groundClearance = 4f;
    [SerializeField] private float groundPenaltyWeight = 25f;

    public Node[,,] grid;
    public Vector3 gridOrigin;
    public int sizeX;
    public int sizeY;
    public int sizeZ;

    public float VoxelSize => voxelSize;

    public void BuildGrid(Vector3 startPoint, Vector3 endPoint, List<GameObject> buildings, GameObject ground) {
        Bounds totalBounds = new(startPoint, Vector3.zero);
        totalBounds.Encapsulate(endPoint);

        List<Bounds> buildingBounds = new();

        foreach (GameObject building in buildings) {
            if (building == null) continue;

            Collider col = building.GetComponent<Collider>();
            if (col != null) {
                Bounds b = col.bounds;
                b.Expand(obstacleBuffer * 2f);
                buildingBounds.Add(b);
                totalBounds.Encapsulate(b);
            }
            else {
                Renderer rend = building.GetComponent<Renderer>();
                if (rend != null) {
                    Bounds b = rend.bounds;
                    b.Expand(obstacleBuffer * 2f);
                    buildingBounds.Add(b);
                    totalBounds.Encapsulate(b);
                }
            }
        }

        Bounds groundBounds = default;
        bool hasGround = false;

        if (ground != null) {
            Collider groundCol = ground.GetComponent<Collider>();
            if (groundCol != null) {
                groundBounds = groundCol.bounds;
                hasGround = true;
                totalBounds.Encapsulate(groundBounds);
            }
            else {
                Renderer groundRend = ground.GetComponent<Renderer>();
                if (groundRend != null) {
                    groundBounds = groundRend.bounds;
                    hasGround = true;
                    totalBounds.Encapsulate(groundBounds);
                }
            }
        }

        totalBounds.Expand(gridPadding * 2f);

        gridOrigin = totalBounds.min;
        sizeX = Mathf.CeilToInt(totalBounds.size.x / voxelSize);
        sizeY = Mathf.CeilToInt(totalBounds.size.y / voxelSize);
        sizeZ = Mathf.CeilToInt(totalBounds.size.z / voxelSize);

        grid = new Node[sizeX, sizeY, sizeZ];

        for (int x = 0; x < sizeX; x++) {
            for (int y = 0; y < sizeY; y++) {
                for (int z = 0; z < sizeZ; z++) {
                    Vector3 worldPos = GridToWorld(x, y, z);
                    Node node = new(x, y, z, worldPos);

                    bool belowGround = hasGround && worldPos.y < groundBounds.min.y;
                    bool insideBuilding = IsInsideBuilding(worldPos, buildingBounds);

                    node.blocked = belowGround || insideBuilding;
                    grid[x, y, z] = node;
                }
            }
        }

        ComputeObstacleDensity(groundBounds.min.y, hasGround);
    }

    private bool IsInsideBuilding(Vector3 point, List<Bounds> buildingBounds) {
        for (int i = 0; i < buildingBounds.Count; i++) {
            if (buildingBounds[i].Contains(point))
                return true;
        }

        return false;
    }

    private void ComputeObstacleDensity(float groundTopY, bool hasGround) {
        for (int x = 0; x < sizeX; x++) {
            for (int y = 0; y < sizeY; y++) {
                for (int z = 0; z < sizeZ; z++) {
                    Node node = grid[x, y, z];

                    if (node.blocked) {
                        node.obstacleDensity = 1f;
                        node.weight = Mathf.Infinity;
                        continue;
                    }

                    int blockedNeighbors = 0;
                    int totalNeighbors = 0;

                    for (int dx = -1; dx <= 1; dx++) {
                        for (int dy = -1; dy <= 1; dy++) {
                            for (int dz = -1; dz <= 1; dz++) {
                                if (dx == 0 && dy == 0 && dz == 0)
                                    continue;

                                int nx = x + dx;
                                int ny = y + dy;
                                int nz = z + dz;

                                if (nx < 0 || nx >= sizeX ||
                                    ny < 0 || ny >= sizeY ||
                                    nz < 0 || nz >= sizeZ) {
                                    continue;
                                }

                                totalNeighbors++;

                                if (grid[nx, ny, nz].blocked)
                                    blockedNeighbors++;
                            }
                        }
                    }

                    node.obstacleDensity = totalNeighbors > 0
                        ? (float)blockedNeighbors / totalNeighbors
                        : 0f;

                    node.weight = baseMoveCost + node.obstacleDensity;

                    if (hasGround) {
                        float heightAboveGround = node.worldPosition.y - groundTopY;

                        if (heightAboveGround >= 0f && heightAboveGround < groundClearance) {
                            node.weight += groundPenaltyWeight;
                        }
                    }
                }
            }
        }
    }

    public void ResetGridSearchData() {
        for (int x = 0; x < sizeX; x++) {
            for (int y = 0; y < sizeY; y++) {
                for (int z = 0; z < sizeZ; z++) {
                    Node node = grid[x, y, z];
                    node.gCost = Mathf.Infinity;
                    node.hCost = 0f;
                    node.parent = null;
                }
            }
        }
    }

    public Node WorldToNode(Vector3 worldPos) {
        int x = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / voxelSize);
        int y = Mathf.FloorToInt((worldPos.y - gridOrigin.y) / voxelSize);
        int z = Mathf.FloorToInt((worldPos.z - gridOrigin.z) / voxelSize);

        if (x < 0 || x >= sizeX || y < 0 || y >= sizeY || z < 0 || z >= sizeZ)
            return null;

        return grid[x, y, z];
    }

    public Vector3 GridToWorld(int x, int y, int z) {
        return gridOrigin + new Vector3(
            (x + 0.5f) * voxelSize,
            (y + 0.5f) * voxelSize,
            (z + 0.5f) * voxelSize
        );
    }
}