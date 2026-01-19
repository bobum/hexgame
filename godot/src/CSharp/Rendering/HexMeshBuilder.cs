namespace HexGame.Rendering;

using HexGame.Core;

/// <summary>
/// Builds hex mesh geometry with terraced slopes.
/// Direct port of hex_mesh_builder.gd
/// </summary>
public class HexMeshBuilder
{
    private List<Vector3> _vertices = new();
    private List<Color> _colors = new();
    private List<int> _indices = new();
    private int _vertexIndex = 0;

    // Splat blending data
    private List<Color> _splatColor1 = new();
    private List<Color> _splatColor2 = new();
    private List<Color> _splatColor3 = new();
    private List<Color> _splatWeights = new();

    // Current splat state
    private Color _currentSplatColor1 = Colors.White;
    private Color _currentSplatColor2 = Colors.White;
    private Color _currentSplatColor3 = Colors.White;
    private Vector3 _currentSplatWeights = new(1, 0, 0);

    // Pre-calculated corner offsets
    private Vector3[] _corners;

    // Mapping from edge index to neighbor direction
    private static readonly int[] EdgeToDirection = { 5, 4, 3, 2, 1, 0 };

    // Stats
    private int _cornerCount = 0;
    private int _edgeCount = 0;

    public HexMeshBuilder()
    {
        _corners = HexMetrics.GetCorners();
        Reset();
    }

    public void Reset()
    {
        _vertices = new List<Vector3>();
        _colors = new List<Color>();
        _indices = new List<int>();
        _splatColor1 = new List<Color>();
        _splatColor2 = new List<Color>();
        _splatColor3 = new List<Color>();
        _splatWeights = new List<Color>();
        _vertexIndex = 0;

        _currentSplatColor1 = Colors.White;
        _currentSplatColor2 = Colors.White;
        _currentSplatColor3 = Colors.White;
        _currentSplatWeights = new Vector3(1, 0, 0);
    }

    /// <summary>
    /// Build mesh for entire grid
    /// </summary>
    public ArrayMesh BuildGridMesh(HexGrid grid)
    {
        Reset();
        _cornerCount = 0;
        _edgeCount = 0;

        foreach (var cell in grid.GetAllCells())
        {
            BuildCell(cell, grid);
        }

        GD.Print($"Built mesh: {_vertices.Count} vertices, {_vertices.Count / 3} triangles");
        GD.Print($"Built {_edgeCount} edges, {_cornerCount} corners");
        GD.Print($"Splat data: colors1={_splatColor1.Count}, colors2={_splatColor2.Count}, weights={_splatWeights.Count}");
        return CommitMesh();
    }

    /// <summary>
    /// Build geometry for a single cell (Catlike Coding pattern)
    /// </summary>
    public void BuildCell(HexCell cell, HexGrid grid)
    {
        var center = cell.GetWorldPosition();
        var baseColor = cell.GetColor();

        // Gather all 6 neighbors and their colors
        var neighbors = new HexCell?[6];
        var neighborColors = new Color[6];

        for (int dir = 0; dir < 6; dir++)
        {
            var neighbor = grid.GetNeighbor(cell, (HexDirection)dir);
            neighbors[dir] = neighbor;
            neighborColors[dir] = neighbor?.GetColor() ?? baseColor;
        }

        // Check if we're using full hexes (no gaps) or Catlike Coding style (gaps for edges)
        var useFullHexes = HexMetrics.SolidFactor >= 0.99f;

        if (useFullHexes)
        {
            // Simple mode: full hex tops with walls for elevation changes
            BuildFullHex(center, baseColor, neighborColors);

            // Build walls for elevation drops
            for (int dir = 0; dir < 6; dir++)
            {
                var neighbor = neighbors[dir];
                var edgeIndex = GetEdgeIndexForDirection(dir);

                if (neighbor == null)
                {
                    // Map edge - build cliff down
                    var wallHeight = (cell.Elevation + 3) * HexMetrics.ElevationStep;
                    BuildCliff(center, edgeIndex, wallHeight, baseColor);
                }
                else if (cell.Elevation > neighbor.Elevation)
                {
                    // We're higher - build wall down to neighbor
                    var wallHeight = (cell.Elevation - neighbor.Elevation) * HexMetrics.ElevationStep;
                    BuildCliff(center, edgeIndex, wallHeight, baseColor);
                }
            }
        }
        else
        {
            // Catlike Coding mode: solid center with edge/corner connections
            BuildTopFace(center, baseColor, neighborColors);

            // Build edges for each direction
            for (int dir = 0; dir < 6; dir++)
            {
                var neighbor = neighbors[dir];
                var edgeIndex = GetEdgeIndexForDirection(dir);

                if (neighbor == null)
                {
                    // Map edge - build a cliff down
                    var wallHeight = (cell.Elevation + 3) * HexMetrics.ElevationStep;
                    BuildCliff(center, edgeIndex, wallHeight, baseColor);
                }
                else
                {
                    var elevationDiff = cell.Elevation - neighbor.Elevation;
                    var neighborCenter = neighbor.GetWorldPosition();
                    var neighborColor = neighborColors[dir];

                    if (elevationDiff == 1)
                    {
                        // Single level slope - build terraces
                        BuildTerracedSlope(center, neighborCenter, edgeIndex, baseColor, neighborColor);
                    }
                    else if (elevationDiff > 1)
                    {
                        // Multi-level cliff
                        BuildFlatCliff(center, neighborCenter, edgeIndex, baseColor, neighborColor);
                    }
                    else if (elevationDiff == 0 && dir <= 2)
                    {
                        // Same level - build flat edge bridge (only dirs 0-2 to avoid duplication)
                        BuildFlatEdge(center, neighborCenter, edgeIndex, baseColor, neighborColor);
                    }
                }

                // Build corners (where three hexes meet)
                if (dir <= 1)
                {
                    var prevDir = (dir + 5) % 6;
                    var prevNeighbor = neighbors[prevDir];
                    if (neighbor != null && prevNeighbor != null)
                    {
                        BuildCorner(cell, center, baseColor, dir, neighbor, prevNeighbor);
                    }
                }
            }
        }
    }

    private int GetEdgeIndexForDirection(int dir)
    {
        int[] dirToEdge = { 5, 4, 3, 2, 1, 0 };
        return dirToEdge[dir];
    }

    private void BuildFullHex(Vector3 center, Color color, Color[] neighborColors)
    {
        bool hasNeighbors = neighborColors.Length == 6;

        for (int i = 0; i < 6; i++)
        {
            var c1 = _corners[i];
            var c2 = _corners[(i + 1) % 6];

            var v1 = center;
            var v2 = new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z);
            var v3 = new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z);

            if (hasNeighbors)
            {
                var edgeDir = EdgeToDirection[i];
                var prevDir = (edgeDir + 1) % 6;
                var nextDir = (edgeDir + 5) % 6;

                var neighborColor1 = neighborColors[edgeDir];
                var neighborColor2 = neighborColors[prevDir];
                var neighborColorRight = neighborColors[nextDir];

                var centerColor = color;
                var corner2Color = BlendColors(color, neighborColor1, neighborColor2);
                var corner3Color = BlendColors(color, neighborColor1, neighborColorRight);

                AddTriangleWithColors(v1, centerColor, v2, corner2Color, v3, corner3Color);
            }
            else
            {
                SetSplatSolid(color);
                AddTriangle(v1, v2, v3, color);
            }
        }
    }

    private void BuildTopFace(Vector3 center, Color color, Color[] neighborColors)
    {
        float solid = HexMetrics.SolidFactor;
        bool hasNeighbors = neighborColors.Length == 6;

        for (int i = 0; i < 6; i++)
        {
            var c1 = _corners[i];
            var c2 = _corners[(i + 1) % 6];

            var v1 = center;
            var v2 = new Vector3(center.X + c1.X * solid, center.Y, center.Z + c1.Z * solid);
            var v3 = new Vector3(center.X + c2.X * solid, center.Y, center.Z + c2.Z * solid);

            if (hasNeighbors)
            {
                var edgeDir = EdgeToDirection[i];
                var prevDir = (edgeDir + 1) % 6;
                var nextDir = (edgeDir + 5) % 6;

                var neighborColor1 = neighborColors[edgeDir];
                var neighborColor2 = neighborColors[prevDir];
                var neighborColorRight = neighborColors[nextDir];

                var centerColor = color;
                var corner2Color = BlendColors(color, neighborColor1, neighborColor2);
                var corner3Color = BlendColors(color, neighborColor1, neighborColorRight);

                AddTriangleWithColors(v1, centerColor, v2, corner2Color, v3, corner3Color);
            }
            else
            {
                SetSplatSolid(color);
                AddTriangleWithColors(v1, color, v2, color, v3, color);
            }
        }
    }

    private void BuildCliff(Vector3 center, int edgeIndex, float height, Color color)
    {
        var wallColor = color.Darkened(0.45f);
        SetSplatSolid(wallColor);

        var c1 = _corners[edgeIndex];
        var c2 = _corners[(edgeIndex + 1) % 6];

        var topLeft = new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z);
        var topRight = new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z);
        var bottomLeft = new Vector3(topLeft.X, center.Y - height, topLeft.Z);
        var bottomRight = new Vector3(topRight.X, center.Y - height, topRight.Z);

        AddTriangle(topLeft, bottomRight, bottomLeft, wallColor);
        AddTriangle(topLeft, topRight, bottomRight, wallColor);
    }

    private void BuildTerracedSlope(Vector3 center, Vector3 neighborCenter, int edgeIndex, Color beginColor, Color endColor)
    {
        _edgeCount++;
        SetSplatSolid(beginColor);

        float solid = HexMetrics.SolidFactor;
        var c1 = _corners[edgeIndex];
        var c2 = _corners[(edgeIndex + 1) % 6];

        var topLeft = new Vector3(center.X + c1.X * solid, center.Y, center.Z + c1.Z * solid);
        var topRight = new Vector3(center.X + c2.X * solid, center.Y, center.Z + c2.Z * solid);

        var oppositeEdge = (edgeIndex + 3) % 6;
        var oppC1 = _corners[oppositeEdge];
        var oppC2 = _corners[(oppositeEdge + 1) % 6];

        var bottomLeft = new Vector3(neighborCenter.X + oppC2.X * solid, neighborCenter.Y, neighborCenter.Z + oppC2.Z * solid);
        var bottomRight = new Vector3(neighborCenter.X + oppC1.X * solid, neighborCenter.Y, neighborCenter.Z + oppC1.Z * solid);

        var v1 = topLeft;
        var v2 = topRight;
        var color1 = beginColor;
        var color2 = beginColor;

        int terraceSteps = HexMetrics.TerraceSteps;
        for (int step = 1; step <= terraceSteps; step++)
        {
            var v3 = HexMetrics.TerraceLerp(topLeft, bottomLeft, step);
            var v4 = HexMetrics.TerraceLerp(topRight, bottomRight, step);
            var c3 = HexMetrics.TerraceColorLerp(beginColor, endColor, step);
            var c4 = HexMetrics.TerraceColorLerp(beginColor, endColor, step);

            var avgColor = new Color(
                (color1.R + color2.R + c3.R + c4.R) / 4.0f,
                (color1.G + color2.G + c3.G + c4.G) / 4.0f,
                (color1.B + color2.B + c3.B + c4.B) / 4.0f
            );

            AddTriangle(v1, v4, v3, avgColor);
            AddTriangle(v1, v2, v4, avgColor);

            v1 = v3;
            v2 = v4;
            color1 = c3;
            color2 = c4;
        }
    }

    private void BuildFlatCliff(Vector3 center, Vector3 neighborCenter, int edgeIndex, Color color, Color neighborColor)
    {
        _edgeCount++;
        SetSplatSolid(color);

        float solid = HexMetrics.SolidFactor;
        var c1 = _corners[edgeIndex];
        var c2 = _corners[(edgeIndex + 1) % 6];

        var v1 = new Vector3(center.X + c1.X * solid, center.Y, center.Z + c1.Z * solid);
        var v2 = new Vector3(center.X + c2.X * solid, center.Y, center.Z + c2.Z * solid);

        var oppositeEdge = (edgeIndex + 3) % 6;
        var oc1 = _corners[oppositeEdge];
        var oc2 = _corners[(oppositeEdge + 1) % 6];

        var v3 = new Vector3(neighborCenter.X + oc2.X * solid, neighborCenter.Y, neighborCenter.Z + oc2.Z * solid);
        var v4 = new Vector3(neighborCenter.X + oc1.X * solid, neighborCenter.Y, neighborCenter.Z + oc1.Z * solid);

        var blendColor = color.Lerp(neighborColor, 0.5f);
        AddTriangle(v1, v2, v4, blendColor);
        AddTriangle(v1, v4, v3, blendColor);
    }

    private void BuildFlatEdge(Vector3 center, Vector3 neighborCenter, int edgeIndex, Color color, Color neighborColor)
    {
        _edgeCount++;
        SetSplatSolid(color);

        float solid = HexMetrics.SolidFactor;
        var c1 = _corners[edgeIndex];
        var c2 = _corners[(edgeIndex + 1) % 6];

        var v1 = new Vector3(center.X + c1.X * solid, center.Y, center.Z + c1.Z * solid);
        var v2 = new Vector3(center.X + c2.X * solid, center.Y, center.Z + c2.Z * solid);

        var oppositeEdge = (edgeIndex + 3) % 6;
        var oc1 = _corners[oppositeEdge];
        var oc2 = _corners[(oppositeEdge + 1) % 6];

        var v3 = new Vector3(neighborCenter.X + oc2.X * solid, neighborCenter.Y, neighborCenter.Z + oc2.Z * solid);
        var v4 = new Vector3(neighborCenter.X + oc1.X * solid, neighborCenter.Y, neighborCenter.Z + oc1.Z * solid);

        var blendColor = color.Lerp(neighborColor, 0.5f);
        AddTriangle(v1, v2, v4, blendColor);
        AddTriangle(v1, v4, v3, blendColor);
    }

    private void BuildCorner(HexCell cell, Vector3 center, Color color, int dir, HexCell neighbor1, HexCell neighbor2)
    {
        _cornerCount++;
        SetSplatSolid(color);

        float solid = HexMetrics.SolidFactor;
        var edgeIndex = GetEdgeIndexForDirection(dir);

        var cornerIdx = (edgeIndex + 1) % 6;
        var cornerOffset = _corners[cornerIdx];
        var P = new Vector3(center.X + cornerOffset.X, 0, center.Z + cornerOffset.Z);

        var n1Center = neighbor1.GetWorldPosition();
        var n2Center = neighbor2.GetWorldPosition();

        var v1 = new Vector3(center.X + cornerOffset.X * solid, center.Y, center.Z + cornerOffset.Z * solid);
        var v2 = new Vector3(n1Center.X + (P.X - n1Center.X) * solid, n1Center.Y, n1Center.Z + (P.Z - n1Center.Z) * solid);
        var v3 = new Vector3(n2Center.X + (P.X - n2Center.X) * solid, n2Center.Y, n2Center.Z + (P.Z - n2Center.Z) * solid);

        var c1 = color;
        var c2 = neighbor1.GetColor();
        var c3 = neighbor2.GetColor();

        var e1 = cell.Elevation;
        var e2 = neighbor1.Elevation;
        var e3 = neighbor2.Elevation;

        // Sort by elevation
        if (e1 <= e2)
        {
            if (e1 <= e3)
                TriangulateCorner(v1, c1, e1, v2, c2, e2, v3, c3, e3);
            else
                TriangulateCorner(v3, c3, e3, v1, c1, e1, v2, c2, e2);
        }
        else
        {
            if (e2 <= e3)
                TriangulateCorner(v2, c2, e2, v3, c3, e3, v1, c1, e1);
            else
                TriangulateCorner(v3, c3, e3, v1, c1, e1, v2, c2, e2);
        }
    }

    private void TriangulateCorner(
        Vector3 bottom, Color bottomColor, int bottomElev,
        Vector3 left, Color leftColor, int leftElev,
        Vector3 right, Color rightColor, int rightElev)
    {
        var leftEdgeType = GetEdgeType(bottomElev, leftElev);
        var rightEdgeType = GetEdgeType(bottomElev, rightElev);

        if (leftEdgeType == "slope")
        {
            if (rightEdgeType == "slope")
            {
                var topEdgeType = GetEdgeType(leftElev, rightElev);
                if (topEdgeType == "flat")
                    TriangulateCornerSsf(bottom, bottomColor, left, leftColor, right, rightColor);
                else
                    TriangulateCornerTerraces(bottom, bottomColor, left, leftColor, right, rightColor);
            }
            else if (rightEdgeType == "cliff")
            {
                TriangulateCornerTerracesCliff(bottom, bottomColor, bottomElev, left, leftColor, leftElev, right, rightColor, rightElev);
            }
            else
            {
                TriangulateCornerTerraces(left, leftColor, right, rightColor, bottom, bottomColor);
            }
        }
        else if (leftEdgeType == "cliff")
        {
            if (rightEdgeType == "slope")
            {
                TriangulateCornerCliffTerraces(bottom, bottomColor, bottomElev, left, leftColor, leftElev, right, rightColor, rightElev);
            }
            else if (rightEdgeType == "cliff")
            {
                var topEdgeType = GetEdgeType(leftElev, rightElev);
                if (topEdgeType == "slope")
                {
                    if (leftElev < rightElev)
                        TriangulateCornerCcsr(bottom, bottomColor, left, leftColor, right, rightColor);
                    else
                        TriangulateCornerCcsl(bottom, bottomColor, left, leftColor, right, rightColor);
                }
                else
                {
                    AddTriangleWithColors(bottom, bottomColor, left, leftColor, right, rightColor);
                }
            }
            else
            {
                AddTriangleWithColors(bottom, bottomColor, left, leftColor, right, rightColor);
            }
        }
        else
        {
            if (rightEdgeType == "slope")
                TriangulateCornerTerraces(right, rightColor, bottom, bottomColor, left, leftColor);
            else
                AddTriangleWithColors(bottom, bottomColor, left, leftColor, right, rightColor);
        }
    }

    private string GetEdgeType(int e1, int e2)
    {
        var diff = Mathf.Abs(e1 - e2);
        if (diff == 0) return "flat";
        if (diff == 1) return "slope";
        return "cliff";
    }

    private void TriangulateCornerTerraces(Vector3 bottom, Color bottomColor, Vector3 left, Color leftColor, Vector3 right, Color rightColor)
    {
        var v3 = HexMetrics.TerraceLerp(bottom, left, 1);
        var v4 = HexMetrics.TerraceLerp(bottom, right, 1);
        var c3 = HexMetrics.TerraceColorLerp(bottomColor, leftColor, 1);
        var c4 = HexMetrics.TerraceColorLerp(bottomColor, rightColor, 1);

        AddTriangleWithColors(bottom, bottomColor, v3, c3, v4, c4);

        int terraceSteps = HexMetrics.TerraceSteps;
        for (int i = 2; i < terraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            var c1 = c3;
            var c2 = c4;
            v3 = HexMetrics.TerraceLerp(bottom, left, i);
            v4 = HexMetrics.TerraceLerp(bottom, right, i);
            c3 = HexMetrics.TerraceColorLerp(bottomColor, leftColor, i);
            c4 = HexMetrics.TerraceColorLerp(bottomColor, rightColor, i);
            AddQuadWithColors(v1, c1, v2, c2, v3, c3, v4, c4);
        }

        AddQuadWithColors(v3, c3, v4, c4, left, leftColor, right, rightColor);
    }

    private void TriangulateCornerTerracesCliff(
        Vector3 bottom, Color bottomColor, int bottomElev,
        Vector3 left, Color leftColor, int leftElev,
        Vector3 right, Color rightColor, int rightElev)
    {
        float b = 1.0f / (rightElev - bottomElev);
        var boundary = bottom.Lerp(right, b);
        var boundaryColor = bottomColor.Lerp(rightColor, b);

        TriangulateBoundaryTriangle(bottom, bottomColor, left, leftColor, boundary, boundaryColor);

        if (GetEdgeType(leftElev, rightElev) == "slope")
            TriangulateBoundaryTriangle(left, leftColor, right, rightColor, boundary, boundaryColor);
        else
            AddTriangleWithColors(left, leftColor, right, rightColor, boundary, boundaryColor);
    }

    private void TriangulateCornerCliffTerraces(
        Vector3 bottom, Color bottomColor, int bottomElev,
        Vector3 left, Color leftColor, int leftElev,
        Vector3 right, Color rightColor, int rightElev)
    {
        float b = 1.0f / (leftElev - bottomElev);
        var boundary = bottom.Lerp(left, b);
        var boundaryColor = bottomColor.Lerp(leftColor, b);

        TriangulateBoundaryTriangle(bottom, bottomColor, right, rightColor, boundary, boundaryColor);

        if (GetEdgeType(leftElev, rightElev) == "slope")
            TriangulateBoundaryTriangle(right, rightColor, left, leftColor, boundary, boundaryColor);
        else
            AddTriangleWithColors(right, rightColor, left, leftColor, boundary, boundaryColor);
    }

    private void TriangulateCornerSsf(Vector3 bottom, Color bottomColor, Vector3 left, Color leftColor, Vector3 right, Color rightColor)
    {
        var v3 = HexMetrics.TerraceLerp(bottom, left, 1);
        var c3 = HexMetrics.TerraceColorLerp(bottomColor, leftColor, 1);
        var v4 = HexMetrics.TerraceLerp(bottom, right, 1);
        var c4 = HexMetrics.TerraceColorLerp(bottomColor, rightColor, 1);

        AddTriangleWithColors(bottom, bottomColor, v3, c3, v4, c4);

        int terraceSteps = HexMetrics.TerraceSteps;
        for (int i = 2; i <= terraceSteps; i++)
        {
            var v3prev = v3;
            var c3prev = c3;
            var v4prev = v4;
            var c4prev = c4;

            v3 = HexMetrics.TerraceLerp(bottom, left, i);
            c3 = HexMetrics.TerraceColorLerp(bottomColor, leftColor, i);
            v4 = HexMetrics.TerraceLerp(bottom, right, i);
            c4 = HexMetrics.TerraceColorLerp(bottomColor, rightColor, i);

            AddTriangleWithColors(v3prev, c3prev, v3, c3, v4prev, c4prev);
            AddTriangleWithColors(v3, c3, v4, c4, v4prev, c4prev);
        }

        AddTriangleWithColors(v3, c3, left, leftColor, v4, c4);
        AddTriangleWithColors(left, leftColor, right, rightColor, v4, c4);
    }

    private void TriangulateCornerCcsr(Vector3 bottom, Color bottomColor, Vector3 left, Color leftColor, Vector3 right, Color rightColor)
    {
        float rightCliffHeight = right.Y - bottom.Y;
        float leftHeight = left.Y - bottom.Y;
        float b = leftHeight / rightCliffHeight;

        var boundary = bottom.Lerp(right, b);
        var boundaryColor = bottomColor.Lerp(rightColor, b);

        AddTriangleWithColors(bottom, bottomColor, left, leftColor, boundary, boundaryColor);
        TriangulateBoundaryTriangle(left, leftColor, right, rightColor, boundary, boundaryColor);
    }

    private void TriangulateCornerCcsl(Vector3 bottom, Color bottomColor, Vector3 left, Color leftColor, Vector3 right, Color rightColor)
    {
        float leftCliffHeight = left.Y - bottom.Y;
        float rightHeight = right.Y - bottom.Y;
        float b = rightHeight / leftCliffHeight;

        var boundary = bottom.Lerp(left, b);
        var boundaryColor = bottomColor.Lerp(leftColor, b);

        AddTriangleWithColors(bottom, bottomColor, boundary, boundaryColor, right, rightColor);
        TriangulateBoundaryTriangle(right, rightColor, left, leftColor, boundary, boundaryColor);
    }

    private void TriangulateBoundaryTriangle(Vector3 begin, Color beginColor, Vector3 left, Color leftColor, Vector3 boundary, Color boundaryColor)
    {
        var v2 = HexMetrics.TerraceLerp(begin, left, 1);
        var c2 = HexMetrics.TerraceColorLerp(beginColor, leftColor, 1);

        AddTriangleWithColors(begin, beginColor, v2, c2, boundary, boundaryColor);

        int terraceSteps = HexMetrics.TerraceSteps;
        for (int i = 2; i < terraceSteps; i++)
        {
            var v1 = v2;
            var c1 = c2;
            v2 = HexMetrics.TerraceLerp(begin, left, i);
            c2 = HexMetrics.TerraceColorLerp(beginColor, leftColor, i);
            AddTriangleWithColors(v1, c1, v2, c2, boundary, boundaryColor);
        }

        AddTriangleWithColors(v2, c2, left, leftColor, boundary, boundaryColor);
    }

    private void AddTriangleWithColors(Vector3 v1, Color c1, Vector3 v2, Color c2, Vector3 v3, Color c3)
    {
        var edge1 = v2 - v1;
        var edge2 = v3 - v1;
        var normal = edge1.Cross(edge2);

        // Godot needs opposite winding from Three.js
        if (normal.Y > 0)
        {
            _vertices.Add(v1);
            _vertices.Add(v3);
            _vertices.Add(v2);
            _colors.Add(c1);
            _colors.Add(c3);
            _colors.Add(c2);
        }
        else
        {
            _vertices.Add(v1);
            _vertices.Add(v2);
            _vertices.Add(v3);
            _colors.Add(c1);
            _colors.Add(c2);
            _colors.Add(c3);
        }

        _indices.Add(_vertexIndex);
        _indices.Add(_vertexIndex + 1);
        _indices.Add(_vertexIndex + 2);
        _vertexIndex += 3;
    }

    private void AddQuadWithColors(Vector3 v1, Color c1, Vector3 v2, Color c2, Vector3 v3, Color c3, Vector3 v4, Color c4)
    {
        var avgColor = new Color(
            (c1.R + c2.R + c3.R + c4.R) / 4.0f,
            (c1.G + c2.G + c3.G + c4.G) / 4.0f,
            (c1.B + c2.B + c3.B + c4.B) / 4.0f
        );

        AddTriangleWithColors(v1, avgColor, v2, avgColor, v3, avgColor);
        AddTriangleWithColors(v2, avgColor, v4, avgColor, v3, avgColor);
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color)
    {
        var edge1 = v2 - v1;
        var edge2 = v3 - v1;
        var normal = edge1.Cross(edge2);

        if (normal.Y > 0)
        {
            _vertices.Add(v1);
            _vertices.Add(v3);
            _vertices.Add(v2);
        }
        else
        {
            _vertices.Add(v1);
            _vertices.Add(v2);
            _vertices.Add(v3);
        }

        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);

        var weightColor = new Color(_currentSplatWeights.X, _currentSplatWeights.Y, _currentSplatWeights.Z, 1.0f);
        for (int i = 0; i < 3; i++)
        {
            _splatColor1.Add(_currentSplatColor1);
            _splatColor2.Add(_currentSplatColor2);
            _splatColor3.Add(_currentSplatColor3);
            _splatWeights.Add(weightColor);
        }

        _indices.Add(_vertexIndex);
        _indices.Add(_vertexIndex + 1);
        _indices.Add(_vertexIndex + 2);
        _vertexIndex += 3;
    }

    private void SetSplatSolid(Color color)
    {
        _currentSplatColor1 = color;
        _currentSplatColor2 = color;
        _currentSplatColor3 = color;
        _currentSplatWeights = new Vector3(1, 0, 0);
    }

    private Color BlendColors(Color mainColor, Color neighbor1, Color neighbor2)
    {
        var w = new Vector3(0.7f, 0.15f, 0.15f);
        return new Color(
            mainColor.R * w.X + neighbor1.R * w.Y + neighbor2.R * w.Z,
            mainColor.G * w.X + neighbor1.G * w.Y + neighbor2.G * w.Z,
            mainColor.B * w.X + neighbor1.B * w.Y + neighbor2.B * w.Z
        );
    }

    /// <summary>
    /// Commit the mesh after building cells
    /// </summary>
    public ArrayMesh CommitMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        int numTriangles = _vertices.Count / 3;
        for (int i = 0; i < numTriangles; i++)
        {
            int idx = i * 3;
            var v0 = _vertices[idx];
            var v1 = _vertices[idx + 1];
            var v2 = _vertices[idx + 2];

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var normal = edge1.Cross(edge2).Normalized();

            st.SetNormal(normal);
            st.SetColor(_colors[idx]);
            st.AddVertex(v0);

            st.SetNormal(normal);
            st.SetColor(_colors[idx + 1]);
            st.AddVertex(v1);

            st.SetNormal(normal);
            st.SetColor(_colors[idx + 2]);
            st.AddVertex(v2);
        }

        return st.Commit();
    }
}
