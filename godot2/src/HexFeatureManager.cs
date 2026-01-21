using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages terrain feature instantiation and placement.
/// Ported exactly from Catlike Coding Hex Map Tutorial 9.
/// </summary>
public partial class HexFeatureManager : Node3D
{
    // Feature collections by type and density level (index 0 = level 1, etc.)
    private HexFeatureCollection[] _urbanCollections = null!;
    private HexFeatureCollection[] _farmCollections = null!;
    private HexFeatureCollection[] _plantCollections = null!;

    // Container for instantiated features (destroyed and recreated on refresh)
    private Node3D? _container;

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
    }

    /// <summary>
    /// Finalizes feature placement. Currently a stub for future optimization.
    /// Called at the end of chunk triangulation.
    /// </summary>
    public void Apply()
    {
        // Reserved for future batching/optimization
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
}
