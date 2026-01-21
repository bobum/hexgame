using Godot;
using System.Collections.Generic;

/// <summary>
/// Generates mesh geometry for hexagonal cells.
/// Ported from Catlike Coding Hex Map Tutorials 1-5.
/// </summary>
public partial class HexMesh : MeshInstance3D
{
    private ArrayMesh _hexMesh = null!;

    // Tutorial 6: Instance-based lists - terrain and rivers need separate data
    private List<Vector3> _vertices = new List<Vector3>();
    private List<Color> _colors = new List<Color>();
    private List<int> _triangles = new List<int>();
    private List<Vector2> _uvs = new List<Vector2>();

    // Tutorial 6: Configuration flags
    public bool UseColors = true;
    public bool UseUVCoordinates = false;

    // Tutorial 8: UV2 for estuary flow direction
    public bool UseUV2Coordinates = false;
    private List<Vector2> _uv2s = new List<Vector2>();

    // Debug: expose vertex count for debugging
    public int VertexCount => _vertices.Count;

    // Tutorial 6: Reference to rivers mesh for water surface triangulation
    private HexMesh? _rivers;

    // Tutorial 7: Reference to roads mesh for road surface triangulation
    private HexMesh? _roads;

    // Tutorial 8: References to water meshes for water body triangulation
    private HexMesh? _water;
    private HexMesh? _waterShore;
    private HexMesh? _estuaries;

    // Tutorial 9: Reference to feature manager for terrain feature placement
    private HexFeatureManager? _features;

    public override void _Ready()
    {
        // Initialize mesh if not already done
        EnsureInitialized();
    }

    /// <summary>
    /// Ensures the mesh is initialized. Safe to call multiple times.
    /// Called automatically by _Ready() or can be called explicitly for
    /// programmatically created HexMesh instances.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_hexMesh == null)
        {
            _hexMesh = new ArrayMesh();
            Mesh = _hexMesh;
        }
    }

    /// <summary>
    /// Clears the mesh and lists for new triangulation.
    /// </summary>
    public void Clear()
    {
        _hexMesh.ClearSurfaces();
        _vertices.Clear();
        _triangles.Clear();
        if (UseColors)
        {
            _colors.Clear();
        }
        if (UseUVCoordinates)
        {
            _uvs.Clear();
        }
        if (UseUV2Coordinates)
        {
            _uv2s.Clear();
        }
    }

    /// <summary>
    /// Applies the accumulated geometry to create the final mesh.
    /// </summary>
    public void Apply()
    {
        if (_vertices.Count == 0)
        {
            GD.Print($"[Apply] {Name}: No vertices, skipping");
            return;
        }

        GD.Print($"[Apply] {Name}: Building mesh with {_vertices.Count} vertices, {_triangles.Count} triangle indices, {_uvs.Count} UVs, UseUVCoordinates={UseUVCoordinates}");

        // Build the mesh using SurfaceTool with flat normals per-triangle
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        // Process triangles in groups of 3 to calculate flat face normals
        for (int i = 0; i < _triangles.Count; i += 3)
        {
            int idx0 = _triangles[i];
            int idx1 = _triangles[i + 1];
            int idx2 = _triangles[i + 2];

            Vector3 v0 = _vertices[idx0];
            Vector3 v1 = _vertices[idx1];
            Vector3 v2 = _vertices[idx2];

            // Calculate flat face normal
            // Using edge2 x edge1 for correct terrain normals (pointing up)
            // Wall quads have their vertex order swapped to get correct outward normals
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 normal = edge2.Cross(edge1).Normalized();

            // Add all 3 vertices with the SAME flat normal
            st.SetNormal(normal);
            if (UseColors)
            {
                st.SetColor(_colors[idx0]);
            }
            if (UseUVCoordinates)
            {
                st.SetUV(_uvs[idx0]);
            }
            if (UseUV2Coordinates)
            {
                st.SetUV2(_uv2s[idx0]);
            }
            st.AddVertex(v0);

            st.SetNormal(normal);
            if (UseColors)
            {
                st.SetColor(_colors[idx1]);
            }
            if (UseUVCoordinates)
            {
                st.SetUV(_uvs[idx1]);
            }
            if (UseUV2Coordinates)
            {
                st.SetUV2(_uv2s[idx1]);
            }
            st.AddVertex(v1);

            st.SetNormal(normal);
            if (UseColors)
            {
                st.SetColor(_colors[idx2]);
            }
            if (UseUVCoordinates)
            {
                st.SetUV(_uvs[idx2]);
            }
            if (UseUV2Coordinates)
            {
                st.SetUV2(_uv2s[idx2]);
            }
            st.AddVertex(v2);
        }

        _hexMesh = st.Commit();
        Mesh = _hexMesh;

        // Debug: Check the final mesh
        if (_hexMesh != null && _hexMesh.GetSurfaceCount() > 0)
        {
            var arrays = _hexMesh.SurfaceGetArrays(0);
            var verts = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            GD.Print($"[Apply] {Name}: Committed mesh has {verts.Length} vertices, {_hexMesh.GetSurfaceCount()} surfaces");
        }
        else
        {
            GD.Print($"[Apply] {Name}: WARNING - Committed mesh has no surfaces!");
        }
    }

    /// <summary>
    /// Triangulates all cells in the array.
    /// </summary>
    /// <param name="cells">Array of cells to triangulate</param>
    /// <param name="rivers">Optional rivers mesh for water surface triangulation</param>
    /// <param name="roads">Optional roads mesh for road surface triangulation</param>
    /// <param name="water">Optional water mesh for open water triangulation</param>
    /// <param name="waterShore">Optional water shore mesh for shore foam triangulation</param>
    /// <param name="estuaries">Optional estuaries mesh for river-water transitions</param>
    /// <param name="features">Optional feature manager for terrain feature placement</param>
    public void Triangulate(
        HexCell[] cells,
        HexMesh? rivers = null,
        HexMesh? roads = null,
        HexMesh? water = null,
        HexMesh? waterShore = null,
        HexMesh? estuaries = null,
        HexFeatureManager? features = null)
    {
        EnsureInitialized();
        Clear();

        _rivers = rivers;
        if (_rivers != null)
        {
            _rivers.Clear();
        }

        _roads = roads;
        if (_roads != null)
        {
            _roads.Clear();
        }

        // Tutorial 8: Initialize water meshes
        _water = water;
        if (_water != null)
        {
            _water.Clear();
        }

        _waterShore = waterShore;
        if (_waterShore != null)
        {
            _waterShore.Clear();
        }

        _estuaries = estuaries;
        if (_estuaries != null)
        {
            _estuaries.Clear();
        }

        // Tutorial 9: Initialize feature manager
        _features = features;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null)
            {
                Triangulate(cells[i]);
            }
        }

        Apply();
        if (_rivers != null)
        {
            _rivers.Apply();
        }
        if (_roads != null)
        {
            GD.Print($"[ROAD DEBUG] Before _roads.Apply(): vertices={_roads.VertexCount}");
            _roads.Apply();
            GD.Print($"[ROAD DEBUG] After _roads.Apply(): Mesh={_roads.Mesh != null}, Material={_roads.MaterialOverride != null}");
        }

        // Tutorial 8: Apply water meshes
        if (_water != null)
        {
            _water.Apply();
        }
        if (_waterShore != null)
        {
            _waterShore.Apply();
        }
        if (_estuaries != null)
        {
            _estuaries.Apply();
        }
    }

    private void Triangulate(HexCell cell)
    {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }

        // Tutorial 8: Triangulate water if cell is underwater
        if (cell.IsUnderwater)
        {
            TriangulateWater(cell);
        }

        // Tutorial 11: Add special feature at cell center (replaces regular features)
        if (_features != null && !cell.IsUnderwater && cell.IsSpecial)
        {
            _features.AddSpecialFeature(cell, cell.Position);
        }
        // Tutorial 9: Add regular features at cell center
        // Features are NOT placed on underwater cells, cells with rivers, or cells with roads
        else if (_features != null && !cell.IsUnderwater && !cell.HasRiver && !cell.HasRoads)
        {
            _features.AddFeature(cell, cell.Position);
        }
    }

    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        AddTriangle(center, edge.V1, edge.V2);
        AddTriangleColor(color);
        AddTriangle(center, edge.V2, edge.V3);
        AddTriangleColor(color);
        AddTriangle(center, edge.V3, edge.V4);
        AddTriangleColor(color);
        AddTriangle(center, edge.V4, edge.V5);
        AddTriangleColor(color);
    }

    private void TriangulateEdgeStrip(
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2,
        bool hasRoad = false)
    {
        AddQuad(e1.V1, e1.V2, e2.V1, e2.V2);
        AddQuadColor(c1, c2);
        AddQuad(e1.V2, e1.V3, e2.V2, e2.V3);
        AddQuadColor(c1, c2);
        AddQuad(e1.V3, e1.V4, e2.V3, e2.V4);
        AddQuadColor(c1, c2);
        AddQuad(e1.V4, e1.V5, e2.V4, e2.V5);
        AddQuadColor(c1, c2);

        // Tutorial 7: Add road segment across edge connection
        if (hasRoad)
        {
            TriangulateRoadSegment(e1.V2, e1.V3, e1.V4, e2.V2, e2.V3, e2.V4);
        }
    }

    private void Triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.Position;
        EdgeVertices e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        // DEBUG: Check what's happening with roads vs rivers
        if (cell.HasRoads)
        {
            GD.Print($"[TRIANGULATE] Cell {cell.Coordinates} dir={direction} HasRoads=true HasRiver={cell.HasRiver}");
        }

        // Tutorial 6-7: Check for rivers and route to appropriate triangulation
        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                e.V3.Y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else
                {
                    TriangulateWithRiver(direction, cell, center, e);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }
        }
        else
        {
            TriangulateWithoutRiver(direction, cell, center, e);
        }

        // Edge connection (bridge + corners) - only for NE, E, SE to avoid duplicates
        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, e);
        }
    }

    /// <summary>
    /// Triangulates a cell section that has no river.
    /// Handles road triangulation if cell has roads.
    /// </summary>
    private void TriangulateWithoutRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        TriangulateEdgeFan(center, e, cell.Color);

        if (cell.HasRoads)
        {
            GD.Print($"  [ROAD DEBUG] Cell {cell.Coordinates} dir={direction} HasRoads=true, HasRoadThroughEdge={cell.HasRoadThroughEdge(direction)}");
            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            TriangulateRoad(
                center,
                center.Lerp(e.V1, interpolators.X),
                center.Lerp(e.V5, interpolators.Y),
                e,
                cell.HasRoadThroughEdge(direction)
            );
        }

        // Tutorial 9: Add features at triangle centers (6 per cell, one per direction)
        // Features skip underwater cells and directions with roads through them
        if (_features != null && !cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
        {
            Vector3 featurePosition = (center + e.V1 + e.V5) * (1f / 3f);
            _features.AddFeature(cell, featurePosition);
        }
    }

    /// <summary>
    /// Triangulates a cell section where a river begins or ends.
    /// Creates a terminating triangle for the water surface.
    /// Tutorial 8: Skips river surface when cell is underwater.
    /// </summary>
    private void TriangulateWithRiverBeginOrEnd(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        EdgeVertices m = new EdgeVertices(
            center.Lerp(e.V1, 0.5f),
            center.Lerp(e.V5, 0.5f)
        );
        m.V3.Y = e.V3.Y;

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);

        // Tutorial 6/8: Add water surface if rivers mesh available and cell is not underwater
        if (_rivers != null && !cell.IsUnderwater)
        {
            bool reversed = cell.HasIncomingRiver;
            TriangulateRiverQuad(m.V2, m.V4, e.V2, e.V4, cell.RiverSurfaceY, 0.6f, reversed);
            Vector3 waterCenter = center;
            waterCenter.Y = cell.RiverSurfaceY;
            Vector3 wm2 = m.V2;
            wm2.Y = cell.RiverSurfaceY;
            Vector3 wm4 = m.V4;
            wm4.Y = cell.RiverSurfaceY;
            // Perturb vertices to match terrain (matches Unity's rivers.AddTriangle behavior)
            waterCenter = HexMetrics.Perturb(waterCenter);
            wm2 = HexMetrics.Perturb(wm2);
            wm4 = HexMetrics.Perturb(wm4);
            _rivers.AddTriangleUnperturbed(waterCenter, wm2, wm4);
            if (reversed)
            {
                _rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(1f, 0.2f),
                    new Vector2(0f, 0.2f)
                );
            }
            else
            {
                _rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(0f, 0.6f),
                    new Vector2(1f, 0.6f)
                );
            }
        }
    }

    /// <summary>
    /// Triangulates a cell section where a river flows through.
    /// Handles straight, zigzag, and curved river configurations.
    /// Tutorial 8: Skips river surface when cell is underwater.
    /// </summary>
    private void TriangulateWithRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;

        // Determine center vertices based on river flow configuration
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            // Straight river: stretch center into a line
            centerL = center +
                HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center +
                HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            // Sharp right turn (zigzag)
            centerL = center;
            centerR = center.Lerp(e.V5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            // Sharp left turn (zigzag)
            centerL = center.Lerp(e.V1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            // Gentle right curve
            centerL = center;
            centerR = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Next()) *
                (0.5f * HexMetrics.InnerToOuter);
        }
        else
        {
            // Gentle left curve (direction.Previous2())
            centerL = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Previous()) *
                (0.5f * HexMetrics.InnerToOuter);
            centerR = center;
        }
        center = (centerL + centerR) * 0.5f;

        // Middle edge with narrower channel (1/6 outer step)
        EdgeVertices m = new EdgeVertices(
            centerL.Lerp(e.V1, 0.5f),
            centerR.Lerp(e.V5, 0.5f),
            1f / 6f
        );
        m.V3.Y = center.Y = e.V3.Y;

        // Terrain triangulation (channel banks)
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

        AddTriangle(centerL, m.V1, m.V2);
        AddTriangleColor(cell.Color);
        AddQuad(centerL, center, m.V2, m.V3);
        AddQuadColor(cell.Color, cell.Color);
        AddQuad(center, centerR, m.V3, m.V4);
        AddQuadColor(cell.Color, cell.Color);
        AddTriangle(centerR, m.V4, m.V5);
        AddTriangleColor(cell.Color);

        // Tutorial 6/8: Add water surface if rivers mesh available and cell is not underwater
        if (_rivers != null && !cell.IsUnderwater)
        {
            bool reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centerL, centerR, m.V2, m.V4, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.V2, m.V4, e.V2, e.V4, cell.RiverSurfaceY, 0.6f, reversed);
        }
    }

    /// <summary>
    /// Triangulates a cell section that is adjacent to (but not crossed by) a river.
    /// Adjusts center position to align with river channel geometry.
    /// </summary>
    private void TriangulateAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        // Offset center based on river position to maintain smooth geometry
        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                // River on both adjacent sides - center offset toward edge middle
                center += HexMetrics.GetSolidEdgeMiddle(direction) *
                    (HexMetrics.InnerToOuter * 0.5f);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
            {
                // River one side adjacent, other side two steps away
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            if (cell.HasRiverThroughEdge(direction.Next2()))
            {
                center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
            }
        }

        EdgeVertices m = new EdgeVertices(
            center.Lerp(e.V1, 0.5f),
            center.Lerp(e.V5, 0.5f)
        );

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);

        // Tutorial 7: Handle roads adjacent to rivers
        if (cell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(direction, cell, center, e);
        }

        // Tutorial 9: Add features at triangle centers adjacent to rivers
        // Features can appear in triangles adjacent to rivers (just not in river channel itself)
        if (_features != null && !cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
        {
            Vector3 featurePosition = (center + e.V1 + e.V5) * (1f / 3f);
            _features.AddFeature(cell, featurePosition);
        }
    }

    /// <summary>
    /// Triangulates roads in a cell section adjacent to a river.
    /// Handles 6 configurations based on where river flows relative to this direction.
    /// </summary>
    private void TriangulateRoadAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        if (_roads == null) return;

        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());

        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 roadCenter = center;

        // 6 configurations based on where river flows relative to this direction
        if (cell.HasRiverBeginOrEnd)
        {
            // Config 1: River begin/end - push road toward direction away from river
            roadCenter += HexMetrics.GetSolidEdgeMiddle(
                cell.RiverBeginOrEndDirection.Opposite()
            ) * (1f / 3f);
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
        {
            // Config 2: Straight river - push road perpendicular
            Vector3 corner;
            if (previousHasRiver)
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next()))
                {
                    return; // No road here
                }
                corner = HexMetrics.GetSecondSolidCorner(direction);
            }
            else
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous()))
                {
                    return; // No road here
                }
                corner = HexMetrics.GetFirstSolidCorner(direction);
            }
            roadCenter += corner * 0.5f;
            // Extend center toward edge since we're pushed far from center
            center += corner * 0.25f;

            // Tutorial 11: Add bridge across straight river
            // Only add from one side to prevent duplicates
            if (_features != null &&
                cell.IncomingRiver == direction.Next() &&
                (cell.HasRoadThroughEdge(direction.Next2()) ||
                 cell.HasRoadThroughEdge(direction.Opposite())))
            {
                _features.AddBridge(
                    roadCenter,
                    center - corner * 0.5f
                );
            }
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
        {
            // Config 3: River curves tightly left
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;

            // Tutorial 11: Add bridge at curve - only from middle direction
            HexDirection middle = cell.IncomingRiver.Next();
            if (_features != null &&
                direction == middle &&
                cell.HasRoadThroughEdge(direction.Opposite()))
            {
                Vector3 bridgeOffset = HexMetrics.GetSolidEdgeMiddle(direction);
                _features.AddBridge(
                    roadCenter + bridgeOffset * 0.25f,
                    roadCenter + bridgeOffset * (HexMetrics.InnerToOuter * 0.7f)
                );
            }
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
        {
            // Config 4: River curves tightly right
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;

            // Tutorial 11: Add bridge at curve - only from middle direction
            HexDirection middle = cell.IncomingRiver.Previous();
            if (_features != null &&
                direction == middle &&
                cell.HasRoadThroughEdge(direction.Opposite()))
            {
                Vector3 bridgeOffset = HexMetrics.GetSolidEdgeMiddle(direction);
                _features.AddBridge(
                    roadCenter + bridgeOffset * 0.25f,
                    roadCenter + bridgeOffset * (HexMetrics.InnerToOuter * 0.7f)
                );
            }
        }
        else if (previousHasRiver && nextHasRiver)
        {
            // Config 5: V-shaped river bend - river on both adjacent sides
            if (!hasRoadThroughEdge)
            {
                return;
            }
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.InnerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        else if (previousHasRiver)
        {
            // Config 6: River at previous direction only - outside of curve
            if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next()))
            {
                return;
            }
            roadCenter += HexMetrics.GetSolidEdgeMiddle(direction) * 0.25f;
        }
        else if (nextHasRiver)
        {
            // Config 7: River at next direction only - outside of curve
            if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous()))
            {
                return;
            }
            roadCenter += HexMetrics.GetSolidEdgeMiddle(direction) * 0.25f;
        }

        Vector3 mL = roadCenter.Lerp(e.V1, interpolators.X);
        Vector3 mR = roadCenter.Lerp(e.V5, interpolators.Y);
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

        // Add road edge triangles at river edges
        if (previousHasRiver)
        {
            TriangulateRoadEdge(roadCenter, center, mL);
        }
        if (nextHasRiver)
        {
            TriangulateRoadEdge(roadCenter, mR, center);
        }
    }

    private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.Y = neighbor.Position.Y - cell.Position.Y;

        EdgeVertices e2 = new EdgeVertices(
            e1.V1 + bridge,
            e1.V5 + bridge
        );

        // Tutorial 6: Handle rivers through the edge
        bool hasRiver = cell.HasRiverThroughEdge(direction);
        // Tutorial 7: Handle roads through the edge (roads can't coexist with rivers)
        bool hasRoad = cell.HasRoadThroughEdge(direction);

        if (hasRiver)
        {
            e2.V3.Y = neighbor.StreamBedY;

            // Tutorial 6/8: Add water surface across connection
            // Handle various underwater scenarios for river-water body transitions
            if (_rivers != null)
            {
                bool reversed = cell.HasIncomingRiver && cell.IncomingRiver == direction;

                if (!cell.IsUnderwater && !neighbor.IsUnderwater)
                {
                    // Case 1: Neither cell underwater - normal river connection
                    TriangulateRiverQuad(
                        e1.V2, e1.V4, e2.V2, e2.V4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        0.8f, reversed
                    );
                }
                else if (!cell.IsUnderwater && neighbor.IsUnderwater)
                {
                    // Case 2: Cell above water, neighbor underwater
                    if (cell.Elevation > neighbor.WaterLevel)
                    {
                        // Case 2a: Cell floor above water level - waterfall dropping into water
                        // Use interpolation to clip river quad at water surface
                        TriangulateWaterfallInWater(
                            e1.V2, e1.V4, e2.V2, e2.V4,
                            cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                            neighbor.WaterSurfaceY, reversed
                        );
                    }
                    else
                    {
                        // Case 2b: Cell floor at or below water level
                        // Draw river quad from cell's river surface to neighbor's water surface
                        TriangulateRiverQuad(
                            e1.V2, e1.V4, e2.V2, e2.V4,
                            cell.RiverSurfaceY, neighbor.WaterSurfaceY,
                            0.8f, reversed
                        );
                    }
                }
                else if (cell.IsUnderwater && !neighbor.IsUnderwater)
                {
                    // Case 3: Cell underwater, neighbor above water
                    if (neighbor.Elevation > cell.WaterLevel)
                    {
                        // Case 3a: Neighbor floor above water level - waterfall from neighbor into water
                        // Process from underwater side with swapped vertices
                        TriangulateWaterfallInWater(
                            e2.V4, e2.V2, e1.V4, e1.V2,
                            neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                            cell.WaterSurfaceY, !reversed
                        );
                    }
                    else
                    {
                        // Case 3b: Neighbor floor at or below water level
                        // Draw river quad from cell's water surface to neighbor's river surface
                        TriangulateRiverQuad(
                            e1.V2, e1.V4, e2.V2, e2.V4,
                            cell.WaterSurfaceY, neighbor.RiverSurfaceY,
                            0.8f, reversed
                        );
                    }
                }
                // Case 4: Both cells underwater - no river surface needed (water mesh handles it)
            }
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(e1, cell, e2, neighbor, hasRoad);
        }
        else
        {
            TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color, hasRoad);
        }

        // Tutorial 10: Add walls along edge
        _features?.AddWall(e1, cell, e2, neighbor, hasRiver, hasRoad);

        // Corner triangle (only for NE and E to avoid duplicates)
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = e1.V5 + HexMetrics.GetBridge(direction.Next());
            v5.Y = nextNeighbor.Position.Y;

            // Sort by elevation to determine bottom, left, right
            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(e1.V5, cell, e2.V5, neighbor, v5, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(v5, nextNeighbor, e1.V5, cell, e2.V5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(e2.V5, neighbor, v5, nextNeighbor, e1.V5, cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, e1.V5, cell, e2.V5, neighbor);
            }
        }
    }

    private void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell,
        bool hasRoad = false)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        TriangulateEdgeStrip(begin, beginCell.Color, e2, c2, hasRoad);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2, hasRoad);
        }

        TriangulateEdgeStrip(e2, c2, end, endCell.Color, hasRoad);
    }

    private void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }
        }
        else
        {
            AddTriangle(bottom, left, right);
            AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }

        // Tutorial 10: Add corner walls
        _features?.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
    }

    private void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        AddTriangle(begin, v3, v4);
        AddTriangleColor(beginCell.Color, c3, c4);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }

        AddQuad(v3, v4, left, right);
        AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
    }

    private void TriangulateBoundaryTriangle(
        Vector3 begin, Color beginColor,
        Vector3 left, Color leftColor,
        Vector3 boundary, Color boundaryColor)
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginColor, leftColor, 1);

        AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        AddTriangleColor(beginColor, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginColor, leftColor, i);
            AddTriangleUnperturbed(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }

        AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        AddTriangleColor(c2, leftColor, boundaryColor);
    }

    private void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = HexMetrics.Perturb(begin).Lerp(HexMetrics.Perturb(right), b);
        Color boundaryColor = beginCell.Color.Lerp(rightCell.Color, b);

        TriangulateBoundaryTriangle(begin, beginCell.Color, left, leftCell.Color, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell.Color, right, rightCell.Color, boundary, boundaryColor);
        }
        else
        {
            AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = HexMetrics.Perturb(begin).Lerp(HexMetrics.Perturb(left), b);
        Color boundaryColor = beginCell.Color.Lerp(leftCell.Color, b);

        TriangulateBoundaryTriangle(right, rightCell.Color, begin, beginCell.Color, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell.Color, right, rightCell.Color, boundary, boundaryColor);
        }
        else
        {
            AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = _vertices.Count;
        _vertices.Add(HexMetrics.Perturb(v1));
        _vertices.Add(HexMetrics.Perturb(v2));
        _vertices.Add(HexMetrics.Perturb(v3));
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = _vertices.Count;
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
    }

    private void AddTriangleColor(Color color)
    {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }

    private void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c3);
    }

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = _vertices.Count;
        _vertices.Add(HexMetrics.Perturb(v1));
        _vertices.Add(HexMetrics.Perturb(v2));
        _vertices.Add(HexMetrics.Perturb(v3));
        _vertices.Add(HexMetrics.Perturb(v4));
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 3);
    }

    private void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c3);
        _colors.Add(c4);
    }

    private void AddQuadColor(Color c1, Color c2)
    {
        _colors.Add(c1);
        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c2);
    }

    // Tutorial 6: UV coordinate methods for river flow animation

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        _uvs.Add(uv1);
        _uvs.Add(uv2);
        _uvs.Add(uv3);
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        _uvs.Add(uv1);
        _uvs.Add(uv2);
        _uvs.Add(uv3);
        _uvs.Add(uv4);
    }

    /// <summary>
    /// Adds UV coordinates for a quad using min/max U and V values.
    /// Convenient for river quads where UVs are based on flow position.
    /// </summary>
    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        _uvs.Add(new Vector2(uMin, vMin));
        _uvs.Add(new Vector2(uMax, vMin));
        _uvs.Add(new Vector2(uMin, vMax));
        _uvs.Add(new Vector2(uMax, vMax));
    }

    // Tutorial 8: UV2 coordinate methods for estuary flow direction

    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        _uv2s.Add(uv1);
        _uv2s.Add(uv2);
        _uv2s.Add(uv3);
    }

    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        _uv2s.Add(uv1);
        _uv2s.Add(uv2);
        _uv2s.Add(uv3);
        _uv2s.Add(uv4);
    }

    /// <summary>
    /// Adds UV2 coordinates for a quad using min/max U and V values.
    /// </summary>
    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
    {
        _uv2s.Add(new Vector2(uMin, vMin));
        _uv2s.Add(new Vector2(uMax, vMin));
        _uv2s.Add(new Vector2(uMin, vMax));
        _uv2s.Add(new Vector2(uMax, vMax));
    }

    // Tutorial 6: River water surface triangulation

    /// <summary>
    /// Triangulates a river water surface quad.
    /// Vertices are set to the water surface height and UVs are set for flow animation.
    /// </summary>
    /// <param name="v1">First vertex (left, near)</param>
    /// <param name="v2">Second vertex (right, near)</param>
    /// <param name="v3">Third vertex (left, far)</param>
    /// <param name="v4">Fourth vertex (right, far)</param>
    /// <param name="y">Water surface Y height</param>
    /// <param name="v">V coordinate for UV mapping (flow position)</param>
    /// <param name="reversed">True if flow is reversed (incoming river)</param>
    private void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y, float v, bool reversed)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }

    /// <summary>
    /// Triangulates a river water surface quad with different heights for near and far edges.
    /// Used for waterfalls where elevation changes between cells.
    /// </summary>
    private void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float v, bool reversed)
    {
        if (_rivers == null) return;

        v1.Y = v2.Y = y1;
        v3.Y = v4.Y = y2;

        // Perturb vertices to match terrain banks (matches Unity's rivers.AddQuad behavior)
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);

        _rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        if (reversed)
        {
            _rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
        }
        else
        {
            _rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
        }
    }

    /// <summary>
    /// Adds a quad without perturbation - for water surface triangulation.
    /// </summary>
    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = _vertices.Count;
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _vertices.Add(v4);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 3);
    }

    // Tutorial 8: Waterfall triangulation

    /// <summary>
    /// Triangulates a waterfall where river flows into a water body.
    /// Clips the river surface quad at the water surface level using interpolation.
    /// Formula: t = (waterY - y2) / (y1 - y2), then lerp vertices by t.
    /// </summary>
    /// <param name="v1">First vertex (left, near - higher)</param>
    /// <param name="v2">Second vertex (right, near - higher)</param>
    /// <param name="v3">Third vertex (left, far - at or below water)</param>
    /// <param name="v4">Fourth vertex (right, far - at or below water)</param>
    /// <param name="y1">Y height of near edge (river surface)</param>
    /// <param name="y2">Y height of far edge (would be below water)</param>
    /// <param name="waterY">Y height of water surface to clip to</param>
    /// <param name="reversed">True if flow is reversed (incoming river)</param>
    private void TriangulateWaterfallInWater(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float waterY, bool reversed)
    {
        if (_rivers == null) return;

        // Set initial heights
        v1.Y = v2.Y = y1;
        v3.Y = v4.Y = y2;

        // Perturb vertices BEFORE interpolation (matches tutorial)
        // This ensures waterfall edges align with surrounding perturbed terrain
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);

        // Interpolation factor: how far from near to far does water surface intersect
        float t = (waterY - y2) / (y1 - y2);

        // Clip far vertices to water surface
        v3 = v3.Lerp(v1, t);
        v4 = v4.Lerp(v2, t);

        // Add quad with fixed UVs (matches tutorial)
        _rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        if (reversed)
        {
            _rivers.AddQuadUV(1f, 0f, 0.8f, 1f);
        }
        else
        {
            _rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
        }
    }

    // Tutorial 7: Road triangulation methods

    /// <summary>
    /// Triangulates a road segment as two quads forming the road surface.
    /// UV coordinates: U=1 at center, U=0 at edges for alpha blending.
    /// Perturbs X,Z to match terrain, then adds Y offset.
    /// </summary>
    private void TriangulateRoadSegment(
        Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 v4, Vector3 v5, Vector3 v6)
    {
        if (_roads == null) return;

        // Perturb X,Z coordinates to match terrain displacement
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);
        v5 = HexMetrics.Perturb(v5);
        v6 = HexMetrics.Perturb(v6);

        // Elevate road vertices to prevent z-fighting with terrain
        v1.Y += HexMetrics.RoadElevationOffset;
        v2.Y += HexMetrics.RoadElevationOffset;
        v3.Y += HexMetrics.RoadElevationOffset;
        v4.Y += HexMetrics.RoadElevationOffset;
        v5.Y += HexMetrics.RoadElevationOffset;
        v6.Y += HexMetrics.RoadElevationOffset;

        // Use unperturbed method since we already perturbed above
        _roads.AddQuadUnperturbed(v1, v2, v4, v5);
        _roads.AddQuadUV(0f, 1f, 0f, 0f);
        _roads.AddQuadUnperturbed(v2, v3, v5, v6);
        _roads.AddQuadUV(1f, 0f, 0f, 0f);
    }

    /// <summary>
    /// Triangulates a road from the cell center to the edge.
    /// If road goes through edge, creates full road segment.
    /// Otherwise, creates just a road edge termination.
    /// </summary>
    private void TriangulateRoad(
        Vector3 center, Vector3 mL, Vector3 mR,
        EdgeVertices e, bool hasRoadThroughCellEdge)
    {
        if (_roads == null)
        {
            GD.Print($"  [ROAD DEBUG] TriangulateRoad called but _roads is NULL!");
            return;
        }

        GD.Print($"  [ROAD DEBUG] TriangulateRoad: hasRoadThroughCellEdge={hasRoadThroughCellEdge}, _roads vertex count before={_roads.VertexCount}");

        if (hasRoadThroughCellEdge)
        {
            Vector3 mC = mL.Lerp(mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, e.V2, e.V3, e.V4);

            // Perturb X,Z then add elevation offset
            Vector3 cR = HexMetrics.Perturb(center);
            cR.Y += HexMetrics.RoadElevationOffset;
            Vector3 mLR = HexMetrics.Perturb(mL);
            mLR.Y += HexMetrics.RoadElevationOffset;
            Vector3 mCR = HexMetrics.Perturb(mC);
            mCR.Y += HexMetrics.RoadElevationOffset;
            Vector3 mRR = HexMetrics.Perturb(mR);
            mRR.Y += HexMetrics.RoadElevationOffset;

            _roads.AddTriangleUnperturbed(cR, mLR, mCR);
            _roads.AddTriangleUV(
                new Vector2(1f, 0f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            );
            _roads.AddTriangleUnperturbed(cR, mCR, mRR);
            _roads.AddTriangleUV(
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f)
            );
        }
        else
        {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    /// <summary>
    /// Triangulates a road edge termination (where road ends within cell).
    /// Perturbs X,Z to match terrain, then adds Y offset.
    /// </summary>
    private void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
    {
        if (_roads == null) return;

        // Perturb X,Z then add elevation offset
        center = HexMetrics.Perturb(center);
        mL = HexMetrics.Perturb(mL);
        mR = HexMetrics.Perturb(mR);

        center.Y += HexMetrics.RoadElevationOffset;
        mL.Y += HexMetrics.RoadElevationOffset;
        mR.Y += HexMetrics.RoadElevationOffset;

        _roads.AddTriangleUnperturbed(center, mL, mR);
        _roads.AddTriangleUV(
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f)
        );
    }

    /// <summary>
    /// Gets the interpolation factors for road edges based on adjacent roads.
    /// Returns (left interpolator, right interpolator).
    /// 0.5 if road exists in that direction, 0.25 otherwise.
    /// </summary>
    private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
    {
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(direction))
        {
            interpolators.X = interpolators.Y = 0.5f;
        }
        else
        {
            interpolators.X = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.Y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }

    // Tutorial 8: Water triangulation methods

    /// <summary>
    /// Triangulates water surface for an underwater cell.
    /// Called for each underwater cell after terrain triangulation.
    /// </summary>
    private void TriangulateWater(HexCell cell)
    {
        if (_water == null) return;

        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            TriangulateWater(d, cell);
        }
    }

    /// <summary>
    /// Triangulates water for a specific direction of an underwater cell.
    /// Routes to open water or shore triangulation based on neighbor state.
    /// </summary>
    private void TriangulateWater(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.Position;
        center.Y = cell.WaterSurfaceY;

        HexCell neighbor = cell.GetNeighbor(direction);

        if (neighbor != null && !neighbor.IsUnderwater)
        {
            // Water-to-land transition: shore triangulation (Phase 8)
            TriangulateWaterShore(direction, cell, neighbor, center);
        }
        else
        {
            // Water-to-water or edge of map: open water
            TriangulateOpenWater(direction, cell, neighbor, center);
        }
    }

    /// <summary>
    /// Triangulates open water hexagon section and connections to other water cells.
    /// Uses smaller water corners (WaterFactor = 0.6) to leave room for shore blending.
    /// </summary>
    private void TriangulateOpenWater(
        HexDirection direction, HexCell cell, HexCell? neighbor, Vector3 center)
    {
        if (_water == null) return;

        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

        // Triangulate the water hexagon section (fan from center)
        // Must use perturbed geometry to match terrain edges
        _water.AddTriangle(center, c1, c2);

        // Water connections (bridges to neighbors) - only NE, E, SE to avoid duplicates
        if (direction <= HexDirection.SE && neighbor != null)
        {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            // Set neighbor's water surface height
            e1.Y = e2.Y = neighbor.WaterSurfaceY;

            // Must use perturbed geometry to match terrain edges
            _water.AddQuad(c1, c2, e1, e2);

            // Corner triangle (only NE and E to avoid duplicates)
            if (direction <= HexDirection.E)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor != null && nextNeighbor.IsUnderwater)
                {
                    Vector3 nextBridge = HexMetrics.GetWaterBridge(direction.Next());
                    Vector3 v3 = c2 + nextBridge;
                    v3.Y = nextNeighbor.WaterSurfaceY;
                    _water.AddTriangle(c2, e2, v3);
                }
            }
        }
    }

    /// <summary>
    /// Triangulates water shore where water meets land.
    /// Uses UV coordinates for shore foam effect: V=0 at water, V=1 at land.
    /// Tutorial 8: Handles estuaries where rivers flow through the shore.
    /// </summary>
    private void TriangulateWaterShore(
        HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        if (_waterShore == null) return;

        // Water edge (inner, at water level)
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction)
        );

        // Shore edge (outer, at land level)
        // Calculate from neighbor's center using solid corners (Tutorial 8)
        // This ensures shore edges align with the land cell's actual boundary
        Vector3 center2 = neighbor.Position;
        center2.Y = center.Y;  // Keep at water surface height
        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
        );

        // Check for river through this edge (estuary case)
        bool hasRiver = cell.HasRiverThroughEdge(direction);
        if (hasRiver)
        {
            // River flows through shore - create estuary
            TriangulateEstuary(e1, e2, cell.IncomingRiver == direction);
        }
        else
        {
            // Normal shore quad strip - must use perturbed geometry to match terrain
            _waterShore.AddQuad(e1.V1, e1.V2, e2.V1, e2.V2);
            _waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            _waterShore.AddQuad(e1.V2, e1.V3, e2.V2, e2.V3);
            _waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            _waterShore.AddQuad(e1.V3, e1.V4, e2.V3, e2.V4);
            _waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            _waterShore.AddQuad(e1.V4, e1.V5, e2.V4, e2.V5);
            _waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }

        // Water center section - proper triangle fan using all 5 edge vertices
        // Must use perturbed geometry to match shore edges
        if (_water != null)
        {
            _water.AddTriangle(center, e1.V1, e1.V2);
            _water.AddTriangle(center, e1.V2, e1.V3);
            _water.AddTriangle(center, e1.V3, e1.V4);
            _water.AddTriangle(center, e1.V4, e1.V5);
        }

        // Shore corners - triangulate the corner between this cell (water),
        // the current neighbor (land), and the next neighbor (water or land)
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (nextNeighbor != null)
        {
            // Calculate corner vertex position from nextNeighbor's center
            // Use water corner if underwater, solid corner if land (Tutorial 8)
            Vector3 v3 = nextNeighbor.Position + (nextNeighbor.IsUnderwater ?
                HexMetrics.GetFirstWaterCorner(direction.Previous()) :
                HexMetrics.GetFirstSolidCorner(direction.Previous()));
            v3.Y = center.Y;  // Keep at water surface height

            // Must use perturbed geometry to match shore edges
            _waterShore.AddTriangle(e1.V5, e2.V5, v3);
            _waterShore.AddTriangleUV(
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)
            );
        }
    }

    /// <summary>
    /// Triangulates an estuary where a river flows into a water body at shore level.
    /// Uses UV for shore blending (V=0 at water, V=1 at land) and
    /// UV2 for river flow direction (X=flow intensity, Y=flow position).
    /// </summary>
    /// <param name="e1">Water edge vertices (inner, at water level)</param>
    /// <param name="e2">Shore edge vertices (outer, at land level)</param>
    /// <param name="incomingRiver">True if river flows from land into water</param>
    private void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver)
    {
        if (_estuaries == null || _waterShore == null) return;

        // Left shore triangle (water side) - must use perturbed geometry to match river
        _waterShore.AddTriangle(e2.V1, e1.V2, e1.V1);
        _waterShore.AddTriangleUV(
            new Vector2(0f, 1f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f)
        );

        // Right shore triangle (water side) - must use perturbed geometry to match river
        _waterShore.AddTriangle(e2.V5, e1.V5, e1.V4);
        _waterShore.AddTriangleUV(
            new Vector2(0f, 1f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f)
        );

        // Estuary trapezoid (center section where river meets water)
        // Uses both UV for shore blending and UV2 for river flow
        // Must use perturbed geometry to match river quads which are perturbed
        _estuaries.AddQuad(e2.V1, e1.V2, e2.V2, e1.V3);
        _estuaries.AddQuadUV(
            new Vector2(0f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f)
        );
        if (incomingRiver)
        {
            _estuaries.AddQuadUV2(
                new Vector2(1.5f, 1f),
                new Vector2(0.7f, 1.15f),
                new Vector2(1f, 0.8f),
                new Vector2(0.5f, 1.1f)
            );
        }
        else
        {
            _estuaries.AddQuadUV2(
                new Vector2(-0.5f, -0.2f),
                new Vector2(0.3f, -0.35f),
                new Vector2(0f, 0f),
                new Vector2(0.5f, -0.3f)
            );
        }

        _estuaries.AddTriangle(e1.V3, e2.V2, e2.V4);
        _estuaries.AddTriangleUV(
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f)
        );
        if (incomingRiver)
        {
            _estuaries.AddTriangleUV2(
                new Vector2(0.5f, 1.1f),
                new Vector2(1f, 0.8f),
                new Vector2(0f, 0.8f)
            );
        }
        else
        {
            _estuaries.AddTriangleUV2(
                new Vector2(0.5f, -0.3f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            );
        }

        _estuaries.AddQuad(e1.V3, e1.V4, e2.V4, e2.V5);
        _estuaries.AddQuadUV(
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        );
        if (incomingRiver)
        {
            _estuaries.AddQuadUV2(
                new Vector2(0.5f, 1.1f),
                new Vector2(0.3f, 1.15f),
                new Vector2(0f, 0.8f),
                new Vector2(-0.5f, 1f)
            );
        }
        else
        {
            _estuaries.AddQuadUV2(
                new Vector2(0.5f, -0.3f),
                new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f),
                new Vector2(1.5f, -0.2f)
            );
        }
    }
}
