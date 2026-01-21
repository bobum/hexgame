using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages terrain feature instantiation and placement.
/// Ported exactly from Catlike Coding Hex Map Tutorials 9-10.
/// </summary>
public partial class HexFeatureManager : Node3D
{
    // Feature collections by type and density level (index 0 = level 1, etc.)
    private HexFeatureCollection[] _urbanCollections = null!;
    private HexFeatureCollection[] _farmCollections = null!;
    private HexFeatureCollection[] _plantCollections = null!;

    // Container for instantiated features (destroyed and recreated on refresh)
    private Node3D? _container;

    // Tutorial 10: Wall mesh
    private HexMesh? _walls;

    // Tutorial 11: Wall tower prefab
    private PackedScene? _wallTowerPrefab;

    // Tutorial 11: Bridge prefab
    private PackedScene? _bridgePrefab;

    // Tutorial 11: Special feature prefabs (castle, ziggurat, megaflora)
    private PackedScene[] _specialPrefabs = null!;

    // Feature selection thresholds per level
    // Level 1 threshold = 0.4, Level 2 = 0.6, Level 3 = 0.8
    private static readonly float[] FeatureThresholds = { 0.0f, 0.4f, 0.6f, 0.8f };

    /// <summary>
    /// Initializes feature collections programmatically.
    /// Called after manager is added to scene tree.
    /// </summary>
    public void Initialize()
    {
        // Initialize with 3 density levels
        _urbanCollections = new HexFeatureCollection[3];
        _farmCollections = new HexFeatureCollection[3];
        _plantCollections = new HexFeatureCollection[3];

        // Load prefabs programmatically
        LoadFeaturePrefabs();

        // Tutorial 10: Create wall mesh
        _walls = new HexMesh();
        _walls.Name = "WallsMesh";
        _walls.UseColors = false;
        _walls.UseUVCoordinates = false;
        AddChild(_walls);
        _walls.EnsureInitialized();

        // Set wall material - RED for visibility during debugging
        var wallMaterial = new StandardMaterial3D();
        wallMaterial.AlbedoColor = new Color(1.0f, 0.0f, 0.0f); // Bright RED
        wallMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled; // Show both sides
        _walls.MaterialOverride = wallMaterial;

        // Tutorial 11: Load wall tower prefab
        _wallTowerPrefab = GD.Load<PackedScene>("res://prefabs/features/wall_tower.tscn");

        // Tutorial 11: Load bridge prefab
        _bridgePrefab = GD.Load<PackedScene>("res://prefabs/features/bridge.tscn");

        // Tutorial 11: Load special feature prefabs
        _specialPrefabs = new PackedScene[3];
        _specialPrefabs[0] = GD.Load<PackedScene>("res://prefabs/features/special/castle.tscn");
        _specialPrefabs[1] = GD.Load<PackedScene>("res://prefabs/features/special/ziggurat.tscn");
        _specialPrefabs[2] = GD.Load<PackedScene>("res://prefabs/features/special/megaflora.tscn");
    }

    /// <summary>
    /// Clears all existing features and creates a fresh container.
    /// Called at the start of chunk triangulation.
    /// </summary>
    public void Clear()
    {
        if (_container != null)
        {
            _container.QueueFree();
        }
        _container = new Node3D();
        _container.Name = "Features Container";
        AddChild(_container);

        // Tutorial 10: Clear wall mesh
        _walls?.Clear();
    }

    /// <summary>
    /// Finalizes feature placement.
    /// Called at the end of chunk triangulation.
    /// </summary>
    public void Apply()
    {
        // Tutorial 10: Apply wall mesh
        _walls?.Apply();
    }

    /// <summary>
    /// Adds a feature at the specified position if conditions allow.
    /// </summary>
    /// <param name="cell">The cell containing the feature</param>
    /// <param name="position">World position for the feature</param>
    public void AddFeature(HexCell cell, Vector3 position)
    {
        // Sample hash grid for this position
        HexHash hash = HexMetrics.SampleHashGrid(position);

        // Try to pick a prefab from each collection
        Node3D? prefab = PickPrefab(
            _urbanCollections, cell.UrbanLevel, hash.a, hash.d
        );
        float usedHash = hash.a;

        Node3D? otherPrefab = PickPrefab(
            _farmCollections, cell.FarmLevel, hash.b, hash.d
        );
        if (prefab != null)
        {
            if (otherPrefab != null && hash.b < hash.a)
            {
                prefab.QueueFree(); // Discard unused prefab
                prefab = otherPrefab;
                usedHash = hash.b;
            }
            else if (otherPrefab != null)
            {
                otherPrefab.QueueFree(); // Discard unused prefab
            }
        }
        else if (otherPrefab != null)
        {
            prefab = otherPrefab;
            usedHash = hash.b;
        }

        otherPrefab = PickPrefab(
            _plantCollections, cell.PlantLevel, hash.c, hash.d
        );
        if (prefab != null)
        {
            if (otherPrefab != null && hash.c < usedHash)
            {
                prefab.QueueFree(); // Discard unused prefab
                prefab = otherPrefab;
            }
            else if (otherPrefab != null)
            {
                otherPrefab.QueueFree(); // Discard unused prefab
            }
        }
        else if (otherPrefab != null)
        {
            prefab = otherPrefab;
        }

        if (prefab == null)
        {
            return;
        }

        // Position the feature with perturbation
        position = HexMetrics.Perturb(position);
        prefab.Position = position;

        // Random Y-axis rotation using hash.e
        prefab.RotationDegrees = new Vector3(0f, 360f * hash.e, 0f);

        // Add to container
        _container?.AddChild(prefab);
    }

    /// <summary>
    /// Picks a prefab from a collection based on level and hash values.
    /// </summary>
    /// <param name="collections">Array of collections by density level</param>
    /// <param name="level">Density level (0-3)</param>
    /// <param name="hash">Hash value for threshold comparison</param>
    /// <param name="choice">Hash value for variant selection</param>
    /// <returns>Instantiated prefab or null</returns>
    private Node3D? PickPrefab(
        HexFeatureCollection[] collections,
        int level,
        float hash,
        float choice)
    {
        if (level <= 0)
        {
            return null;
        }

        for (int i = level; i > 0; i--)
        {
            if (hash >= FeatureThresholds[i])
            {
                return collections[i - 1].Pick(choice);
            }
        }
        return null;
    }

    /// <summary>
    /// Loads feature prefabs programmatically.
    /// Replaces Unity's SerializeField inspector assignment.
    /// </summary>
    private void LoadFeaturePrefabs()
    {
        // Urban features (buildings)
        _urbanCollections[0] = new HexFeatureCollection
        {
            Prefabs = LoadPrefabArray("res://prefabs/features/urban/level1/")
        };
        _urbanCollections[1] = new HexFeatureCollection
        {
            Prefabs = LoadPrefabArray("res://prefabs/features/urban/level2/")
        };
        _urbanCollections[2] = new HexFeatureCollection
        {
            Prefabs = LoadPrefabArray("res://prefabs/features/urban/level3/")
        };

        // Farm features
        _farmCollections[0] = new HexFeatureCollection
        {
            Prefabs = LoadPrefabArray("res://prefabs/features/farm/level1/")
        };
        _farmCollections[1] = new HexFeatureCollection
        {
            Prefabs = LoadPrefabArray("res://prefabs/features/farm/level2/")
        };
        _farmCollections[2] = new HexFeatureCollection
        {
            Prefabs = LoadPrefabArray("res://prefabs/features/farm/level3/")
        };

        // Plant features (trees, bushes)
        _plantCollections[0] = new HexFeatureCollection
        {
            Prefabs = LoadPrefabArray("res://prefabs/features/plant/level1/")
        };
        _plantCollections[1] = new HexFeatureCollection
        {
            Prefabs = LoadPrefabArray("res://prefabs/features/plant/level2/")
        };
        _plantCollections[2] = new HexFeatureCollection
        {
            Prefabs = LoadPrefabArray("res://prefabs/features/plant/level3/")
        };
    }

    /// <summary>
    /// Loads all .tscn files from a directory as PackedScenes.
    /// </summary>
    private PackedScene[] LoadPrefabArray(string directoryPath)
    {
        var scenes = new List<PackedScene>();

        if (!DirAccess.DirExistsAbsolute(directoryPath))
        {
            // Directory doesn't exist yet, return empty array
            return scenes.ToArray();
        }

        var dir = DirAccess.Open(directoryPath);

        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (!dir.CurrentIsDir() && fileName.EndsWith(".tscn"))
                {
                    var scene = GD.Load<PackedScene>(directoryPath + fileName);
                    if (scene != null)
                    {
                        scenes.Add(scene);
                    }
                }
                fileName = dir.GetNext();
            }
            dir.ListDirEnd();
        }

        return scenes.ToArray();
    }

    // Tutorial 10: Wall methods

    /// <summary>
    /// Adds wall segments along an edge between two cells.
    /// Only places walls where one cell is walled and the other is not.
    /// </summary>
    public void AddWall(
        EdgeVertices near, HexCell nearCell,
        EdgeVertices far, HexCell farCell,
        bool hasRiver, bool hasRoad)
    {
        if (
            nearCell.Walled != farCell.Walled &&
            !nearCell.IsUnderwater && !farCell.IsUnderwater &&
            nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff
        )
        {
            AddWallSegment(near.V1, far.V1, near.V2, far.V2);
            if (hasRiver || hasRoad)
            {
                // Add caps at gap edges
                AddWallCap(near.V2, far.V2);
                AddWallCap(far.V4, near.V4);
            }
            else
            {
                AddWallSegment(near.V2, far.V2, near.V3, far.V3);
                AddWallSegment(near.V3, far.V3, near.V4, far.V4);
            }
            AddWallSegment(near.V4, far.V4, near.V5, far.V5);
        }
    }

    /// <summary>
    /// Adds wall corner segments where three cells meet.
    /// Handles all 8 configurations of walled state.
    /// </summary>
    public void AddWall(
        Vector3 c1, HexCell cell1,
        Vector3 c2, HexCell cell2,
        Vector3 c3, HexCell cell3)
    {
        if (cell1.Walled)
        {
            if (cell2.Walled)
            {
                if (!cell3.Walled)
                {
                    AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                }
            }
            else if (cell3.Walled)
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
            else
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
        }
        else if (cell2.Walled)
        {
            if (cell3.Walled)
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
            else
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
        }
        else if (cell3.Walled)
        {
            AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
        }
    }

    /// <summary>
    /// Creates an individual wall segment from four vertices (edge-based).
    /// </summary>
    private void AddWallSegment(
        Vector3 nearLeft, Vector3 farLeft,
        Vector3 nearRight, Vector3 farRight,
        bool addTower = false)
    {
        nearLeft = HexMetrics.Perturb(nearLeft);
        farLeft = HexMetrics.Perturb(farLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farRight = HexMetrics.Perturb(farRight);

        Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
        Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

        Vector3 leftThicknessOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
        Vector3 rightThicknessOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);

        float leftTop = left.Y + HexMetrics.WallHeight;
        float rightTop = right.Y + HexMetrics.WallHeight;

        Vector3 v1, v2, v3, v4;
        v1 = v3 = left - leftThicknessOffset;
        v2 = v4 = right - rightThicknessOffset;
        v3.Y = leftTop;
        v4.Y = rightTop;

        // Inner face - swap vertex order for outward-facing normal
        _walls?.AddQuadUnperturbed(v2, v1, v4, v3);

        Vector3 t1 = v3, t2 = v4;

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.Y = leftTop;
        v4.Y = rightTop;
        // Outer face - swap vertex order for outward-facing normal
        _walls?.AddQuadUnperturbed(v1, v2, v3, v4);

        // Top quad - swap for upward-facing normal
        _walls?.AddQuadUnperturbed(t2, t1, v4, v3);

        // Tutorial 11: Place tower at wall corner
        if (addTower && _wallTowerPrefab != null && _container != null)
        {
            Vector3 towerPosition = (left + right) * 0.5f;
            var tower = _wallTowerPrefab.Instantiate<Node3D>();
            tower.Position = towerPosition;

            // Align tower rotation to wall direction
            Vector3 direction = right - left;
            if (direction.LengthSquared() > 0.0001f)
            {
                float angle = Mathf.Atan2(direction.X, direction.Z);
                tower.RotationDegrees = new Vector3(0f, Mathf.RadToDeg(angle), 0f);
            }

            _container.AddChild(tower);
        }
    }

    /// <summary>
    /// Creates a corner wall segment where three cells meet (pivot-based).
    /// </summary>
    private void AddWallSegment(
        Vector3 pivot, HexCell pivotCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        if (pivotCell.IsUnderwater)
        {
            return;
        }

        bool hasLeftWall = !leftCell.IsUnderwater &&
            pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
        bool hasRightWall = !rightCell.IsUnderwater &&
            pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

        if (hasLeftWall)
        {
            if (hasRightWall)
            {
                bool hasTower = false;
                if (leftCell.Elevation == rightCell.Elevation)
                {
                    HexHash hash = HexMetrics.SampleHashGrid(
                        (pivot + left + right) * (1f / 3f)
                    );
                    hasTower = hash.e < HexMetrics.WallTowerThreshold;
                }
                AddWallSegment(pivot, left, pivot, right, hasTower);
            }
            else if (leftCell.Elevation < rightCell.Elevation)
            {
                AddWallWedge(pivot, left, right);
            }
            else
            {
                AddWallCap(pivot, left);
            }
        }
        else if (hasRightWall)
        {
            if (rightCell.Elevation < leftCell.Elevation)
            {
                AddWallWedge(right, pivot, left);
            }
            else
            {
                AddWallCap(right, pivot);
            }
        }
    }

    /// <summary>
    /// Creates a wall cap to seal endpoints at river/road openings.
    /// </summary>
    private void AddWallCap(Vector3 near, Vector3 far)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.Y = v4.Y = center.Y + HexMetrics.WallHeight;
        // Swap vertex order for correct outward-facing normal
        _walls?.AddQuadUnperturbed(v2, v1, v4, v3);
    }

    /// <summary>
    /// Creates a wall wedge to connect walls to cliff faces.
    /// </summary>
    private void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        point = HexMetrics.Perturb(point);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        Vector3 pointTop = point;
        point.Y = center.Y;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.Y = v4.Y = pointTop.Y = center.Y + HexMetrics.WallHeight;

        // Swap vertex orders for correct outward-facing normals
        _walls?.AddQuadUnperturbed(point, v1, pointTop, v3);
        _walls?.AddQuadUnperturbed(v2, point, v4, pointTop);
        _walls?.AddTriangleUnperturbed(pointTop, v4, v3);
    }

    // Tutorial 11: Bridge methods

    /// <summary>
    /// Adds a bridge across a river between road endpoints.
    /// Tutorial 11.
    /// </summary>
    /// <param name="roadCenter1">Road center on first bank</param>
    /// <param name="roadCenter2">Road center on second bank</param>
    public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
    {
        if (_bridgePrefab == null || _container == null)
        {
            return;
        }

        // Perturb positions to match terrain
        roadCenter1 = HexMetrics.Perturb(roadCenter1);
        roadCenter2 = HexMetrics.Perturb(roadCenter2);

        var bridge = _bridgePrefab.Instantiate<Node3D>();

        // Position at midpoint between road centers
        bridge.Position = (roadCenter1 + roadCenter2) * 0.5f;

        // Align rotation to bridge direction
        Vector3 direction = roadCenter2 - roadCenter1;
        if (direction.LengthSquared() > 0.0001f)
        {
            float angle = Mathf.Atan2(direction.X, direction.Z);
            bridge.RotationDegrees = new Vector3(0f, Mathf.RadToDeg(angle), 0f);
        }

        // Scale Z axis based on distance / design length
        float length = direction.Length();
        float scale = length / HexMetrics.BridgeDesignLength;
        bridge.Scale = new Vector3(1f, 1f, scale);

        _container.AddChild(bridge);
    }

    // Tutorial 11: Special feature methods

    /// <summary>
    /// Adds a special feature at the cell center.
    /// Tutorial 11.
    /// </summary>
    /// <param name="cell">Cell containing the special feature</param>
    /// <param name="position">World position for placement</param>
    public void AddSpecialFeature(HexCell cell, Vector3 position)
    {
        if (_container == null || _specialPrefabs == null)
        {
            return;
        }

        int index = cell.SpecialIndex - 1;  // Convert 1-3 to 0-2 array index
        if (index < 0 || index >= _specialPrefabs.Length || _specialPrefabs[index] == null)
        {
            return;
        }

        HexHash hash = HexMetrics.SampleHashGrid(position);
        var feature = _specialPrefabs[index].Instantiate<Node3D>();

        // Position with perturbation
        feature.Position = HexMetrics.Perturb(position);

        // Random Y rotation using hash.e (same as regular features)
        feature.RotationDegrees = new Vector3(0f, 360f * hash.e, 0f);

        _container.AddChild(feature);
    }
}
