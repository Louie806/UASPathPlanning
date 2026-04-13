using UnityEngine;

public class Node {
    public int x;
    public int y;
    public int z;

    public Vector3 worldPosition;
    public bool blocked;
    public float obstacleDensity;
    public float weight;

    public float gCost = Mathf.Infinity;
    public float hCost = 0f;
    public float FCost => gCost + hCost;

    public Node parent;

    public Node(int x, int y, int z, Vector3 worldPosition) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.worldPosition = worldPosition;
    }
}