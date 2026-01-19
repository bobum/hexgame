using Godot;
using System.Collections.Generic;

/// <summary>
/// Generates mesh geometry for hexagonal cells.
/// Ported exactly from Catlike Coding Hex Map Tutorials 1-2.
/// </summary>
public partial class HexMesh : MeshInstance3D
{
    private ArrayMesh _hexMesh = null!;
    private List<Vector3> _vertices = null!;
    private List<Color> _colors = null!;
    private List<int> _triangles = null!;

    public override void _Ready()
    {
        _hexMesh = new ArrayMesh();
        Mesh = _hexMesh;
        _vertices = new List<Vector3>();
        _colors = new List<Color>();
        _triangles = new List<int>();
    }

    public void Triangulate(HexCell[] cells)
    {
        _hexMesh.ClearSurfaces();
        _vertices.Clear();
        _colors.Clear();
        _triangles.Clear();

        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }

        GD.Print($"HexMesh: Generated {_vertices.Count} vertices, {_triangles.Count} indices for {cells.Length} cells");

        // Build the mesh using SurfaceTool for proper normal generation
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        for (int i = 0; i < _triangles.Count; i++)
        {
            int idx = _triangles[i];
            st.SetColor(_colors[idx]);
            st.AddVertex(_vertices[idx]);
        }

        st.GenerateNormals();
        _hexMesh = st.Commit();
        Mesh = _hexMesh;

        GD.Print($"HexMesh: Mesh created with {_hexMesh.GetSurfaceCount()} surfaces");
    }

    private void Triangulate(HexCell cell)
    {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }
    }

    private void Triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.Position;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

        // Solid inner triangle
        AddTriangle(center, v1, v2);
        AddTriangleColor(cell.Color);

        // Edge connection (bridge + corners)
        TriangulateConnection(direction, cell, v1, v2);
    }

    private void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(direction);
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;

        AddQuad(v1, v2, v3, v4);
        AddQuadColor(cell.Color, neighbor.Color);

        // Corner triangle (only for NE and E to avoid duplicates)
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            AddTriangle(v2, v4, v2 + HexMetrics.GetBridge(direction.Next()));
            AddTriangleColor(cell.Color, neighbor.Color, nextNeighbor.Color);
        }
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
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
}
