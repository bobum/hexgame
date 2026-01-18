namespace HexGame.Rendering;

using HexGame.Core;

/// <summary>
/// Renders pathfinding visualization:
/// - Reachable cells (highlighted hexes)
/// - Path preview (line from unit to destination)
/// Direct port of path_renderer.gd
/// </summary>
public partial class PathRenderer : Node3D
{
    private HexGrid? _grid;

    // Reachable cells visualization
    private MultiMeshInstance3D? _reachableMeshes;
    private const int MaxReachableInstances = 500;

    // Path line visualization
    private MeshInstance3D? _pathLine;
    private StandardMaterial3D? _pathMaterial;

    // Destination marker
    private MeshInstance3D? _destinationMarker;

    public PathRenderer()
    {
        CreateReachableMesh();
        CreatePathMaterial();
        CreateDestinationMarker();
    }

    public void Setup(HexGrid grid)
    {
        _grid = grid;
    }

    private void CreateReachableMesh()
    {
        var mesh = BuildHexShapeMesh();

        var multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = MaxReachableInstances,
            VisibleInstanceCount = 0
        };

        _reachableMeshes = new MultiMeshInstance3D
        {
            Multimesh = multimesh
        };

        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.0f, 1.0f, 0.0f, 0.5f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            NoDepthTest = true,
            VertexColorUseAsAlbedo = true
        };
        _reachableMeshes.MaterialOverride = material;

        AddChild(_reachableMeshes);
    }

    private ArrayMesh BuildHexShapeMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        float radius = HexMetrics.OuterRadius * 0.9f;
        var corners = new Vector3[6];

        for (int i = 0; i < 6; i++)
        {
            float angle = (Mathf.Pi / 3.0f) * i - Mathf.Pi / 6.0f;
            corners[i] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        }

        // Build triangles from center
        var center = Vector3.Zero;
        st.SetNormal(Vector3.Up);
        for (int i = 0; i < 6; i++)
        {
            st.AddVertex(center);
            st.AddVertex(corners[i]);
            st.AddVertex(corners[(i + 1) % 6]);
        }

        return st.Commit();
    }

    private void CreatePathMaterial()
    {
        _pathMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.0f, 1.0f, 0.0f, 0.8f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };
    }

    private void CreateDestinationMarker()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        float innerRadius = 0.6f;
        float outerRadius = 0.8f;

        for (int i = 0; i < 6; i++)
        {
            float angle1 = (Mathf.Pi / 3.0f) * i - Mathf.Pi / 6.0f;
            float angle2 = (Mathf.Pi / 3.0f) * ((i + 1) % 6) - Mathf.Pi / 6.0f;

            var inner1 = new Vector3(Mathf.Cos(angle1) * innerRadius, 0, Mathf.Sin(angle1) * innerRadius);
            var outer1 = new Vector3(Mathf.Cos(angle1) * outerRadius, 0, Mathf.Sin(angle1) * outerRadius);
            var inner2 = new Vector3(Mathf.Cos(angle2) * innerRadius, 0, Mathf.Sin(angle2) * innerRadius);
            var outer2 = new Vector3(Mathf.Cos(angle2) * outerRadius, 0, Mathf.Sin(angle2) * outerRadius);

            st.SetNormal(Vector3.Up);
            st.AddVertex(inner1);
            st.AddVertex(outer1);
            st.AddVertex(outer2);

            st.AddVertex(inner1);
            st.AddVertex(outer2);
            st.AddVertex(inner2);
        }

        _destinationMarker = new MeshInstance3D
        {
            Mesh = st.Commit()
        };

        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.0f, 0.0f, 0.8f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        _destinationMarker.MaterialOverride = material;
        _destinationMarker.Visible = false;

        AddChild(_destinationMarker);
    }

    /// <summary>
    /// Show reachable cells for a unit.
    /// </summary>
    public void ShowReachableCells(Dictionary<HexCell, float> reachableCells)
    {
        if (_reachableMeshes?.Multimesh == null)
            return;

        var mm = _reachableMeshes.Multimesh;
        int index = 0;

        foreach (var kvp in reachableCells)
        {
            if (index >= MaxReachableInstances)
                break;

            var cell = kvp.Key;
            float cost = kvp.Value;
            var worldPos = new HexCoordinates(cell.Q, cell.R).ToWorldPosition(0);

            // For water cells, render on water surface; for land, render on terrain
            float yOffset;
            if (cell.Elevation < HexMetrics.SeaLevel)
            {
                yOffset = HexMetrics.SeaLevel * HexMetrics.ElevationStep + 0.1f;
            }
            else
            {
                yOffset = cell.Elevation * HexMetrics.ElevationStep + 0.15f;
            }

            var transform = Transform3D.Identity;
            transform.Origin = new Vector3(worldPos.X, yOffset, worldPos.Z);
            mm.SetInstanceTransform(index, transform);

            // Color based on movement cost (green = cheap, yellow = expensive)
            float t = Mathf.Min(cost / 4.0f, 1.0f);
            var color = Color.FromHsv(0.33f - t * 0.33f, 0.8f, 0.7f, 0.5f);
            mm.SetInstanceColor(index, color);

            index++;
        }

        mm.VisibleInstanceCount = index;
    }

    /// <summary>
    /// Hide reachable cells.
    /// </summary>
    public void HideReachableCells()
    {
        if (_reachableMeshes?.Multimesh != null)
        {
            _reachableMeshes.Multimesh.VisibleInstanceCount = 0;
        }
    }

    /// <summary>
    /// Show path preview from unit to destination.
    /// </summary>
    public void ShowPath(List<HexCell> path)
    {
        // Remove old path line
        if (_pathLine != null)
        {
            _pathLine.QueueFree();
            _pathLine = null;
        }

        if (path.Count < 2)
        {
            HideDestinationMarker();
            return;
        }

        // Create path points
        var points = new List<Vector3>();
        foreach (var cell in path)
        {
            var worldPos = new HexCoordinates(cell.Q, cell.R).ToWorldPosition(0);
            float yPos;
            if (cell.Elevation < HexMetrics.SeaLevel)
            {
                yPos = HexMetrics.SeaLevel * HexMetrics.ElevationStep + 0.15f;
            }
            else
            {
                yPos = cell.Elevation * HexMetrics.ElevationStep + 0.2f;
            }
            points.Add(new Vector3(worldPos.X, yPos, worldPos.Z));
        }

        // Create line mesh using ImmediateMesh
        var im = new ImmediateMesh();
        im.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        foreach (var point in points)
        {
            im.SurfaceAddVertex(point);
        }
        im.SurfaceEnd();

        _pathLine = new MeshInstance3D
        {
            Mesh = im,
            MaterialOverride = _pathMaterial
        };
        AddChild(_pathLine);

        // Show destination marker at end of path
        var lastCell = path[path.Count - 1];
        ShowDestinationMarker(lastCell);
    }

    /// <summary>
    /// Hide path preview.
    /// </summary>
    public void HidePath()
    {
        if (_pathLine != null)
        {
            _pathLine.QueueFree();
            _pathLine = null;
        }
        HideDestinationMarker();
    }

    /// <summary>
    /// Show destination marker at a cell.
    /// </summary>
    public void ShowDestinationMarker(HexCell cell)
    {
        if (_destinationMarker == null)
            return;

        var worldPos = new HexCoordinates(cell.Q, cell.R).ToWorldPosition(0);
        float yPos;
        if (cell.Elevation < HexMetrics.SeaLevel)
        {
            yPos = HexMetrics.SeaLevel * HexMetrics.ElevationStep + 0.12f;
        }
        else
        {
            yPos = cell.Elevation * HexMetrics.ElevationStep + 0.15f;
        }
        _destinationMarker.Position = new Vector3(worldPos.X, yPos, worldPos.Z);
        _destinationMarker.Visible = true;
    }

    /// <summary>
    /// Hide destination marker.
    /// </summary>
    public void HideDestinationMarker()
    {
        if (_destinationMarker != null)
        {
            _destinationMarker.Visible = false;
        }
    }

    /// <summary>
    /// Update path line color for valid/invalid paths.
    /// </summary>
    public void SetPathValid(bool valid)
    {
        if (_pathMaterial != null)
        {
            _pathMaterial.AlbedoColor = valid
                ? new Color(0.0f, 1.0f, 0.0f, 0.8f)
                : new Color(1.0f, 0.0f, 0.0f, 0.8f);
        }
    }
}
