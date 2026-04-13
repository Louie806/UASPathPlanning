using System.Collections.Generic;
using UnityEngine;

public class AStar : MonoBehaviour
{
    

    [Header("DA* Heuristic Weights")]
    [SerializeField] private float altitudeHeuristicWeight = 1.2f;
    [SerializeField] private float obstacleDensityHeuristicWeight = 3f;

    [Header("Movement Cost")]
    [SerializeField] private float baseMoveCost = 1f;
    [SerializeField] private float verticalMovePenalty = 2f;

    private Node _startNode;
    private Node _endNode;

    private Grid _grid;

    public bool RunAStar(Node startNode, Node endNode, Grid grid) {
        _startNode = startNode;
        _endNode = endNode;
        _grid = grid;

        _grid.ResetGridSearchData();

        PriorityQueue<Node> openSet = new();
        HashSet<Node> closedSet = new();

        _startNode.gCost = 0f;
        _startNode.hCost = Heuristic(_startNode, _endNode);
        _startNode.parent = null;

        openSet.Enqueue(_startNode, _startNode.FCost);

        while (openSet.Count > 0) {
            Node current = openSet.Dequeue();
            if (closedSet.Contains(current)) continue;

            closedSet.Add(current);
            if (current == endNode) return true;

            foreach (Node neighbor in GetNeighbors(current)) {
                if (neighbor.blocked || closedSet.Contains(neighbor))
                    continue;

                float stepCost = StepCost(current, neighbor);
                float tentativeG = current.gCost + stepCost;

                if (tentativeG < neighbor.gCost) {
                    neighbor.gCost = tentativeG;
                    neighbor.hCost = Heuristic(neighbor, endNode);
                    neighbor.parent = current;

                    openSet.Enqueue(neighbor, neighbor.FCost);
                }
            }
        }

        return false;
    }

    private IEnumerable<Node> GetNeighbors(Node node) {
        for (int dx = -1; dx <= 1; dx++) {
            for (int dy = -1; dy <= 1; dy++) {
                for (int dz = -1; dz <= 1; dz++) {
                    if (dx == 0 && dy == 0 && dz == 0) continue;

                    int nx = node.x + dx;
                    int ny = node.y + dy;
                    int nz = node.z + dz;

                    if (nx < 0 || nx >= _grid.sizeX || ny < 0 || ny >= _grid.sizeY || nz < 0 || nz >= _grid.sizeZ) continue;

                    yield return _grid.grid[nx, ny, nz]; 
                }
            }
        }
    }

    private float StepCost(Node from, Node to) {
        float moveDistance = Vector3.Distance(from.worldPosition, to.worldPosition);
        float cost = moveDistance + baseMoveCost;

        if (!Mathf.Approximately(from.worldPosition.y, to.worldPosition.y)) {
            cost += verticalMovePenalty;
        }

        return cost;
    }
    private float Heuristic(Node current, Node goal) {
        float dx = Mathf.Abs(goal.x - current.x);
        float dy = Mathf.Abs(goal.y - current.y);
        float dz = Mathf.Abs(goal.z - current.z);

        float chebyshev = Mathf.Max(dx, dy, dz); 
        float altitudePenalty = altitudeHeuristicWeight * dz;
        float densityPenalty = obstacleDensityHeuristicWeight * current.obstacleDensity;

        return chebyshev + altitudePenalty + densityPenalty;
    }
}
