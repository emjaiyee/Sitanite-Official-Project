using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum ProceduralRoomShape
{
    Rectangle,
    Chambers,
    Organic,
    Cavern
}

[Flags]
public enum ProceduralTileSides
{
    None = 0,
    NorthEast = 1,
    NorthWest = 2,
    SouthWest = 4,
    SouthEast = 8
}

[Serializable]
public class ProceduralWeightedTile
{
    public TileBase tile;
    [Min(1)] public int weight = 1;
}

[Serializable]
public class ProceduralEdgeTile
{
    public ProceduralTileSides sides;
    public TileBase tile;
}

[CreateAssetMenu(menuName = "Dungeon/Procedural Room Profile")]
public class ProceduralRoomProfile : ScriptableObject
{
    [Header("Layout (Cell Coordinates)")]
    public bool isSecretRoom;
    public int seed = 12345;
    public ProceduralRoomShape shape = ProceduralRoomShape.Chambers;
    [Range(16, 128)] public int width = 36;
    [Range(16, 128)] public int height = 28;
    [Range(3, 24)] public int chamberCount = 8;
    [Range(2, 8)] public int corridorHalfWidth = 3;

    [Header("Room Plane")]
    [Tooltip("Gameplay elevation for navigation, markers, visibility, and lights. Independent of tile cell Z.")]
    [Min(0)] public int elevationLevel;
    [Tooltip("Actual starting tile cell Z. Walls stack from this Z; markers and collision follow the native Grid displacement.")]
    [Min(0)] public int baseZ;

    [Header("Cavern")]
    [Range(0.4f, 0.7f)] public float cavernFloorFill = 0.58f;
    [Range(1, 6)] public int cavernSmoothingPasses = 4;

    [Header("Cube Surface")]
    [Tooltip("Upward offset in grid cell heights from the cube tile anchor to its flat top. One height is 0.16 world units, not the Grid Z size of 2.")]
    [Range(0f, 4f)] public float surfaceHeightInTiles = 1f;

    [Header("Wall Height (Tile Z Layers)")]
    [Range(1, 8)] public int wallHeightInTiles = 1;

    [Header("Wall Occlusion")]
    public bool fadeObscuringWalls = true;
    [Min(0f)] public float wallFadePadding = 0.24f;
    [Range(0f, 1f)] public float wallFadedAlpha = 0.15f;
    [Min(0.01f)] public float wallFadeDuration = 0.2f;

    [Header("Tiles")]
    public List<ProceduralWeightedTile> groundTiles = new List<ProceduralWeightedTile>();
    public List<ProceduralWeightedTile> wallTiles = new List<ProceduralWeightedTile>();
    [Tooltip("Exact missing-neighbor mask on a ground cell. Unmatched cells use Ground Tiles.")]
    public List<ProceduralEdgeTile> groundEdges = new List<ProceduralEdgeTile>();
    [Tooltip("Exact adjacent-ground mask on a wall cell. Unmatched cells use Wall Tiles.")]
    public List<ProceduralEdgeTile> wallEdges = new List<ProceduralEdgeTile>();
    public TileBase navigationTile;
    public TileBase collisionTile;

    [Header("Rendering")]
    public Material tilemapMaterial;
    public string groundSortingLayer = "Default";
    public int groundSortingOrder;
    [HideInInspector] public string wallSortingLayer = "Default";
    [HideInInspector] public int wallSortingOrder = 20;

    [Header("Gateways")]
    [Tooltip("Optional visual prefab. The generator supplies the gateway trigger and progression components.")]
    public GameObject gatewayVisualPrefab;
    public GatewayDirection forwardDirection = GatewayDirection.NorthEast;
    public GatewayDirection backwardDirection = GatewayDirection.SouthWest;
    public GatewayDirection secretRoomBackwardDirection = GatewayDirection.SouthEast;
    [Tooltip("Width along the edge and depth across it, in grid cells.")]
    public Vector2 forwardColliderSize = new Vector2(2f, 1f);
    public Vector2 backwardColliderSize = new Vector2(2f, 1f);
    [Tooltip("Optional Light2D prefab used instead of a gateway visual on south-facing exits.")]
    public GameObject southGatewayLightPrefab;
    public Color southGatewayLightColor = new Color(0.4f, 0.9f, 1f, 1f);
    [Min(0f)] public float southGatewayLightIntensity = 2f;
    [Min(0.01f)] public float southGatewayLightRadius = 0.6f;

    [Header("Floor Trapdoor (Regular Rooms Only)")]
    public bool includeFloorGateway = true;
    public TileBase trapdoorNorthEast;
    public TileBase trapdoorNorthWest;
    public TileBase trapdoorSouthEast;
    public TileBase trapdoorSouthWest;

    [Header("Secret Entrance (Regular Rooms Only)")]
    public bool includeSecretGateway;
    public GatewayDirection secretDirection = GatewayDirection.NorthWest;
    public Vector2 secretColliderSize = new Vector2(2f, 1f);
    [Range(6, 48)] public int secretPathLength = 16;
    [Range(2, 12)] public int secretPathHalfWidth = 5;

    public bool UsesFloorGateway => includeFloorGateway && !isSecretRoom;
    public bool UsesSecretEntrance => includeSecretGateway && !isSecretRoom;
    public GatewayDirection EntranceDirection => isSecretRoom ? secretRoomBackwardDirection : backwardDirection;

    [Header("Population")]
    [Range(0, 24)] public int enemySpawnPoints = 6;
    [Tooltip("Optional configured marker prefab containing EnemySpawnPoint.")]
    public GameObject enemySpawnPointPrefab;
    public List<GameObject> decorationPrefabs = new List<GameObject>();
    [Range(0, 64)] public int decorationCount = 8;
    [Tooltip("Empty space around each decoration footprint, in cells.")]
    [Range(0, 4)] public int decorationClearance = 1;

    [Header("Scattered Lights")]
    public List<GameObject> lightPrefabs = new List<GameObject>();
    [Range(0, 64)] public int lightCount = 8;
    [Min(1)] public int lightSpacing = 3;
}