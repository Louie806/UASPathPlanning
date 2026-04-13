using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    [Header("User Interface")]
    [SerializeField] private TMP_Text screenText;

    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject pointPrefab;

    [Header("Inputs")]
    [SerializeField] private GameObject startPoint;
    [SerializeField] private GameObject endPoint;
    [SerializeField] private GameObject buildings;
    [SerializeField] private GameObject ground;

    [Header("Path Simplification")]
    [SerializeField] private bool simplifyPath = true;
    [SerializeField] private float densityKeepThreshold = 0.15f;
    [SerializeField] private float angleKeepThresholdDeg = 20f;

    [Header("Runtime Path Visualization")]
    [SerializeField] private Material pathLineMaterial;
    [SerializeField] private float pathLineWidth = 0.3f;
    [SerializeField] private float pathHeightOffset = 0.05f;


    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask buildingLayer;

    [Header("Placement Offsets")]
    [SerializeField] private float groundYOffset = 1f;
    [SerializeField] private float buildingSurfaceOffset = 1f;

    private readonly List<Vector3> rawWorldPath = new();
    private readonly List<Vector3> worldPath = new();
    private readonly List<Node> rawNodePath = new();
    private readonly List<Node> simplifiedNodePath = new();

    private List<GameObject> buildingList = new();

    private LineRenderer pathLine;

    private AStar _aStar = new();
    private Grid _grid = new();

    private bool placingPoints = false;
    private bool startPlaced = false;
    private bool endPlaced = false;

    private Vector3 startPosition;
    private Vector3 endPosition;

    private void Start() {
        screenText.text = "";
        startPoint = null;
        endPoint = null;
    }

    private void Awake() {
        Transform[] allBuildings = buildings.GetComponentsInChildren<Transform>();
        foreach (Transform building in allBuildings) {
            if (building == buildings.transform) continue;
            GameObject buildingGO = building.gameObject;
            buildingList.Add(buildingGO);
        }
    }

    private void Update() {
        if (!placingPoints) return;

        if (Input.GetMouseButtonDown(0)) {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f)) {
                Vector3 spawnPos = AdjustPointPosition(hit);
                GameObject point = Instantiate(pointPrefab, spawnPos, Quaternion.identity);

                Debug.Log("Placed object at: " + spawnPos);

                if (!startPlaced) {
                    startPoint = point;
                    startPosition = spawnPos;
                    startPlaced = true;
                }
                else if (!endPlaced) {
                    endPoint = point;
                    endPosition = spawnPos;
                    endPlaced = true;
                }
            }
        }
    }

    private Vector3 AdjustPointPosition(RaycastHit hit) {
        int hitLayerMask = 1 << hit.collider.gameObject.layer;

        if ((groundLayer.value & hitLayerMask) != 0) {
            return hit.point + Vector3.up * groundYOffset;
        }

        if ((buildingLayer.value & hitLayerMask) != 0) {
            return GetPointOutsideBuilding(hit.collider, hit.point);
        }

        return hit.point;
    }

    private Vector3 GetPointOutsideBuilding(Collider buildingCollider, Vector3 hitPoint) {
        Bounds bounds = buildingCollider.bounds;
        Vector3 center = bounds.center;

        float distToRight = Mathf.Abs(bounds.max.x - hitPoint.x);
        float distToLeft = Mathf.Abs(hitPoint.x - bounds.min.x);
        float distToTop = Mathf.Abs(bounds.max.y - hitPoint.y);
        float distToBottom = Mathf.Abs(hitPoint.y - bounds.min.y);
        float distToFront = Mathf.Abs(bounds.max.z - hitPoint.z);
        float distToBack = Mathf.Abs(hitPoint.z - bounds.min.z);

        float minDist = distToRight;
        Vector3 snappedPoint = new Vector3(bounds.max.x + buildingSurfaceOffset, hitPoint.y, hitPoint.z);

        if (distToLeft < minDist) {
            minDist = distToLeft;
            snappedPoint = new Vector3(bounds.min.x - buildingSurfaceOffset, hitPoint.y, hitPoint.z);
        }

        if (distToTop < minDist) {
            minDist = distToTop;
            snappedPoint = new Vector3(hitPoint.x, bounds.max.y + buildingSurfaceOffset, hitPoint.z);
        }

        if (distToBottom < minDist) {
            minDist = distToBottom;
            snappedPoint = new Vector3(hitPoint.x, bounds.min.y - buildingSurfaceOffset, hitPoint.z);
        }

        if (distToFront < minDist) {
            minDist = distToFront;
            snappedPoint = new Vector3(hitPoint.x, hitPoint.y, bounds.max.z + buildingSurfaceOffset);
        }

        if (distToBack < minDist) {
            snappedPoint = new Vector3(hitPoint.x, hitPoint.y, bounds.min.z - buildingSurfaceOffset);
        }

        return snappedPoint;
    }

    public void CreateStartEndPoints() {
        Destroy(startPoint);
        Destroy(endPoint);
        StartCoroutine(AwaitInputPoints());
    }

    private IEnumerator AwaitInputPoints() {
        placingPoints = true;

        screenText.text = "Place Start Point";
        startPlaced = false;
        endPlaced = false;

        yield return new WaitUntil(() => startPlaced);

        screenText.text = "Place End Point";

        yield return new WaitUntil(() => endPlaced);

        placingPoints = false;

        screenText.text = "Ready to begin A*.";
    }

    public void FindPath() {
        ClearPathLine();

        if (startPoint == null || endPoint == null) {
            screenText.text = " Assign start and end points";
            return;
        }

        _grid.BuildGrid(startPoint.transform.position, endPoint.transform.position, buildingList, ground);

        Node startNode = _grid.WorldToNode(startPoint.transform.position);
        Node endNode = _grid.WorldToNode(endPoint.transform.position);

        if (startNode == null || endNode == null) {
            screenText.text = "Start or end point is outside the generated grid.";
            return;
        }

        if (startNode.blocked) {
            screenText.text = "Start point is inside an obstacle.";
            return;
        }

        if (endNode.blocked) {
            screenText.text = "End point is inside an obstacle.";
            return;
        }

        screenText.text = "Finding path.";
        bool found = _aStar.RunAStar(startNode, endNode, _grid);

        rawNodePath.Clear();
        simplifiedNodePath.Clear();
        rawWorldPath.Clear();
        worldPath.Clear();

        if (!found) {
            Debug.LogWarning("No path found.");
            return;
        }

        RetraceRawPath(startNode, endNode);

        if (simplifyPath) {
            SimplifyNodePath();
        }
        else {
            simplifiedNodePath.AddRange(rawNodePath);
        }

        for (int i = 0; i < rawNodePath.Count; i++) {
            rawWorldPath.Add(rawNodePath[i].worldPosition);
        }

        for (int i = 0; i < simplifiedNodePath.Count; i++) {
            worldPath.Add(simplifiedNodePath[i].worldPosition);
        }

        CreatePathLine();
        
        screenText.text = "Path found.";
        Debug.Log($"Path found. Raw nodes: {rawWorldPath.Count}, Final nodes: {worldPath.Count}");
    }

    public void Quit() {
        Application.Quit();
    }

    private void RetraceRawPath(Node startNode, Node endNode) {
        Node current = endNode;

        while (current != null && current != startNode) {
            rawNodePath.Add(current);
            current = current.parent;
        }

        rawNodePath.Add(startNode);
        rawNodePath.Reverse();
    }

    private void SimplifyNodePath() {
        if (rawNodePath.Count <= 2) {
            simplifiedNodePath.AddRange(rawNodePath);
            return;
        }

        simplifiedNodePath.Add(rawNodePath[0]);

        for (int i = 1; i < rawNodePath.Count - 1; i++) {
            Node prev = rawNodePath[i - 1];
            Node curr = rawNodePath[i];
            Node next = rawNodePath[i + 1];

            bool keep = false;

            if (curr.obstacleDensity >= densityKeepThreshold)
                keep = true;

            bool densityEntering = prev.obstacleDensity <= 0f && curr.obstacleDensity > 0f;
            bool densityLeaving = prev.obstacleDensity > 0f && curr.obstacleDensity <= 0f;
            bool densityJump = Mathf.Abs(curr.obstacleDensity - prev.obstacleDensity) > 0.1f;

            if (densityEntering || densityLeaving || densityJump)
                keep = true;

            Vector3 dirA = (curr.worldPosition - prev.worldPosition).normalized;
            Vector3 dirB = (next.worldPosition - curr.worldPosition).normalized;
            float angle = Vector3.Angle(dirA, dirB);

            if (angle >= angleKeepThresholdDeg)
                keep = true;

            if (keep)
                simplifiedNodePath.Add(curr);
        }

        simplifiedNodePath.Add(rawNodePath[rawNodePath.Count - 1]);
    }

    

    private void ClearPathLine() {
        if (pathLine != null) {
            Destroy(pathLine.gameObject);
            pathLine = null;
        }
    }

    private void CreatePathLine() {
        if (worldPath.Count < 2)
            return;

        GameObject lineObj = new GameObject("PathLine");
        lineObj.transform.SetParent(transform);

        pathLine = lineObj.AddComponent<LineRenderer>();
        pathLine.positionCount = worldPath.Count;
        pathLine.useWorldSpace = true;
        pathLine.widthMultiplier = pathLineWidth;
        pathLine.numCapVertices = 4;
        pathLine.numCornerVertices = 4;

        Material lineMat;

        if (pathLineMaterial != null) {
            lineMat = new Material(pathLineMaterial);
        }
        else {
            Shader shader = Shader.Find("Sprites/Default");
            lineMat = new Material(shader);
            lineMat.color = Color.cyan;
        }

        lineMat.renderQueue = 5000;

        if (lineMat.HasProperty("_ZTest"))
            lineMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

        if (lineMat.HasProperty("_ZWrite"))
            lineMat.SetInt("_ZWrite", 0);

        pathLine.material = lineMat;
        pathLine.startColor = Color.cyan;
        pathLine.endColor = Color.cyan;

        for (int i = 0; i < worldPath.Count; i++) {
            pathLine.SetPosition(i, worldPath[i] + Vector3.up * pathHeightOffset);
        }
    }


}
