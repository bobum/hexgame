using Godot;
using System.Collections.Generic;

/// <summary>
/// Generates mesh geometry for hexagonal cells.
/// Ported exactly from Catlike Coding Hex Map Tutorial 1.
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
        Vector3 center = cell.Position;
        for (int i = 0; i < 6; i++)
        {
            AddTriangle(
                center,
                center + HexMetrics.Corners[i],
                center + HexMetrics.Corners[i + 1]
            );
            AddTriangleColor(cell.Color);
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
}
