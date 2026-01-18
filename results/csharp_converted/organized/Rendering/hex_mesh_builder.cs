using Godot;
using Godot.Collections;


//# Builds hex mesh geometry with terraced slopes

//# Matches web/src/rendering/HexMeshBuilder.ts
[GlobalClass]
public partial class HexMeshBuilder : Godot.RefCounted
{
	public PackedVector3Array Vertices;
	public PackedColorArray Colors;
	public PackedInt32Array Indices;
	public int VertexIndex = 0;


	// Splat blending data (3 colors + weights per vertex)
	public PackedColorArray SplatColor1;
	// Main terrain color (R weight)
	public PackedColorArray SplatColor2;
	// First neighbor color (G weight)
	public PackedColorArray SplatColor3;
	// Second neighbor color (B weight)
	public PackedColorArray SplatWeights;

	// RGB weights for blending (stored as Color for RGBA)
	// Current splat state (set before adding vertices)
	public Color CurrentSplatColor1 = Color.White;
	public Color CurrentSplatColor2 = Color.White;
	public Color CurrentSplatColor3 = Color.White;
	public Vector3 CurrentSplatWeights = new Vector3(1, 0, 0);


	// Pre-calculated corner offsets
	public Array<Vector3> Corners;


	// Mapping from edge index to neighbor direction
	public const Array<int> EDGE_TO_DIRECTION = new Array{5, 4, 3, 2, 1, 0, };


	public override void _Init()
	{
		Corners = HexMetrics.GetCorners();
		Reset();
	}


	public void Reset()
	{
		Vertices = new PackedVector3Array();
		Colors = new PackedColorArray();
		Indices = new PackedInt32Array();
		SplatColor1 = new PackedColorArray();
		SplatColor2 = new PackedColorArray();
		SplatColor3 = new PackedColorArray();
		SplatWeights = new PackedColorArray();
		VertexIndex = 0;

		// Reset splat state
		CurrentSplatColor1 = Color.White;
		CurrentSplatColor2 = Color.White;
		CurrentSplatColor3 = Color.White;
		CurrentSplatWeights = new Vector3(1, 0, 0);
	}


	protected int _CornerCount = 0;
	protected int _EdgeCount = 0;


	//# Build mesh for entire grid
	public Godot.ArrayMesh BuildGridMesh(Godot.HexGrid grid)
	{
		Reset();
		_CornerCount = 0;
		_EdgeCount = 0;

		foreach(HexCell cell in grid.GetAllCells())
		{
			BuildCell(cell, grid);
		}

		GD.Print("Built mesh: %d vertices, %d triangles" % new Array{Vertices.Size(), Vertices.Size() / 3, });
		GD.Print("Built %d edges, %d corners" % new Array{_EdgeCount, _CornerCount, });
		GD.Print("Splat data: colors1=%d, colors2=%d, weights=%d" % new Array{SplatColor1.Size(), SplatColor2.Size(), SplatWeights.Size(), });
		return _CreateMesh();
	}


	//# Build geometry for a single cell (Catlike Coding pattern)
	public void BuildCell(Godot.HexCell cell, Godot.HexGrid grid)
	{
		var center = cell.GetWorldPosition();

		// Use flat colors - no variation for crisp look
		var base_color = cell.GetColor();


		// Gather all 6 neighbors and their colors
		var neighbors = new Array{};
		var neighbor_colors = new Array{};
		neighbors.Resize(6);
		neighbor_colors.Resize(6);

		foreach(int dir in GD.Range(6))
		{
			var neighbor = grid.GetNeighbor(cell, dir);
			neighbors[dir] = neighbor;
			if(neighbor)
			{

				// Use flat neighbor colors - no variation
				neighbor_colors[dir] = neighbor.GetColor();
			}
			else
			{
				neighbor_colors[dir] = base_color;
			}
		}


		// Check if we're using full hexes (no gaps) or Catlike Coding style (gaps for edges)
		var use_full_hexes = HexMetrics.SOLID_FACTOR >= 0.99;

		if(use_full_hexes)
		{

			// Simple mode: full hex tops with walls for elevation changes
			_BuildFullHex(center, base_color, neighbor_colors);


			// Build walls for elevation drops
			foreach(int dir in GD.Range(6))
			{
				var neighbor = neighbors[dir];
				var edge_index = _GetEdgeIndexForDirection(dir);

				if(!neighbor)
				{

					// Map edge - build cliff down
					var wall_height = (cell.Elevation + 3) * HexMetrics.ELEVATION_STEP;
					_BuildCliff(center, edge_index, wall_height, base_color);
				}
				else if(cell.Elevation > neighbor.Elevation)
				{

					// We're higher - build wall down to neighbor
					var wall_height = (cell.Elevation - neighbor.Elevation) * HexMetrics.ELEVATION_STEP;
					_BuildCliff(center, edge_index, wall_height, base_color);
				}
			}
		}
		else
		{

			// Catlike Coding mode: solid center with edge/corner connections
			_BuildTopFace(center, base_color, neighbor_colors);


			// Build edges for each direction
			foreach(int dir in GD.Range(6))
			{
				var neighbor = neighbors[dir];
				var edge_index = _GetEdgeIndexForDirection(dir);

				if(!neighbor)
				{

					// Map edge - build a cliff down
					var wall_height = (cell.Elevation + 3) * HexMetrics.ELEVATION_STEP;
					_BuildCliff(center, edge_index, wall_height, base_color);
				}
				else
				{
					var elevation_diff = cell.Elevation - neighbor.Elevation;
					var neighbor_center = neighbor.GetWorldPosition();
					var neighbor_color = neighbor_colors[dir];

					if(elevation_diff == 1)
					{

						// Single level slope - build terraces
						_BuildTerracedSlope(center, neighbor_center, edge_index, base_color, neighbor_color);
					}
					else if(elevation_diff > 1)
					{

						// Multi-level cliff
						_BuildFlatCliff(center, neighbor_center, edge_index, base_color, neighbor_color);
					}
					else if(elevation_diff == 0 && dir <= 2)
					{

						// Same level - build flat edge bridge (only dirs 0-2 to avoid duplication)
						_BuildFlatEdge(center, neighbor_center, edge_index, base_color, neighbor_color);
					}
				}


				// Build corners (where three hexes meet)
				if(dir <= 1)
				{
					var prev_dir = (dir + 5) % 6;
					var prev_neighbor = neighbors[prev_dir];
					if(neighbor && prev_neighbor)
					{
						_BuildCorner(cell, center, base_color, dir, neighbor, prev_neighbor);
					}
				}
			}
		}
	}


	//# Vary a color slightly for visual interest
	protected Color _VaryColor(Color color, double amount)
	{
		var variation = GD.RandRange( - amount, amount);


		return new Color(, Mathf.Clamp(color.R + variation, 0.0, 1.0), Mathf.Clamp(color.G + variation, 0.0, 1.0), Mathf.Clamp(color.B + variation, 0.0, 1.0));
	}


	protected int _GetEdgeIndexForDirection(int dir)
	{
		var dir_to_edge = new Array{5, 4, 3, 2, 1, 0, };
		return dir_to_edge[dir];


		//# Get the corner index of neighbor that touches the shared corner
		//# For dir=0: neighbor1 (NE) corner=2, neighbor2 (NW, prev_dir=5) corner=4

	}//# For dir=1: neighbor1 (E) corner=1, neighbor2 (NE, prev_dir=0) corner=3
	protected int _GetNeighborCornerIndex(int dir, bool is_prev_neighbor = false)
	{
		if(dir == 0)
		{
			return ( is_prev_neighbor ? 4 : 2 );
		}
		else if(dir == 1)
		{
			return ( is_prev_neighbor ? 3 : 1 );
		}
		else if(dir == 5)
		{
			// prev_dir for dir=0
			return 4;
		}
		else
		{
			// dir == 0 as prev_dir for dir=1
			return 3;
		}
	}


	//# Build full hexagon at full radius (no solid/blend regions)
	protected void _BuildFullHex(Vector3 center, Color color, Array<Color> neighbor_colors = new Array{})
	{
		var has_neighbors = neighbor_colors.Size() == 6;

		foreach(int i in GD.Range(6))
		{
			var c1 = Corners[i];
			var c2 = Corners[(i + 1) % 6];

			var v1 = center;
			var v2 = new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z);
			var v3 = new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z);

			if(has_neighbors)
			{

				// Splat blending: get neighbor directions for this edge
				var edge_dir = EDGE_TO_DIRECTION[i];
				var prev_dir = (edge_dir + 1) % 6;
				var next_dir = (edge_dir + 5) % 6;

				var neighbor_color1 = neighbor_colors[edge_dir];
				var neighbor_color2 = neighbor_colors[prev_dir];
				var neighbor_color_right = neighbor_colors[next_dir];


				// Pre-blend colors for corner vertices
				var center_color = color;
				// 100% main color
				var corner2_color = _BlendColors(color, neighbor_color1, neighbor_color2);
				var corner3_color = _BlendColors(color, neighbor_color1, neighbor_color_right);


				// Use existing triangle function for proper winding
				_AddTriangleWithColors(v1, center_color, v2, corner2_color, v3, corner3_color);
			}
			else
			{

				// CW winding for upward-facing in Godot
				_SetSplatSolid(color);
				_AddTriangle(v1, v2, v3, color);
			}
		}
	}


	protected void _BuildTopFace(Vector3 center, Color color, Array<Color> neighbor_colors = new Array{})
	{
		var solid = HexMetrics.SOLID_FACTOR;
		var has_neighbors = neighbor_colors.Size() == 6;

		foreach(int i in GD.Range(6))
		{
			var c1 = Corners[i];
			var c2 = Corners[(i + 1) % 6];

			var v1 = center;
			var v2 = new Vector3(center.X + c1.X * solid, center.Y, center.Z + c1.Z * solid);
			var v3 = new Vector3(center.X + c2.X * solid, center.Y, center.Z + c2.Z * solid);

			if(has_neighbors)
			{

				// Splat blending: get neighbor directions for this edge
				var edge_dir = EDGE_TO_DIRECTION[i];
				var prev_dir = (edge_dir + 1) % 6;
				var next_dir = (edge_dir + 5) % 6;

				var neighbor_color1 = neighbor_colors[edge_dir];
				var neighbor_color2 = neighbor_colors[prev_dir];
				var neighbor_color_right = neighbor_colors[next_dir];


				// Pre-blend colors for corner vertices
				var center_color = color;
				// 100% main color
				var corner2_color = _BlendColors(color, neighbor_color1, neighbor_color2);
				var corner3_color = _BlendColors(color, neighbor_color1, neighbor_color_right);


				// Use existing triangle function for proper winding
				_AddTriangleWithColors(v1, center_color, v2, corner2_color, v3, corner3_color);
			}
			else
			{

				// No splatting - use solid colors
				_SetSplatSolid(color);
				_AddTriangleWithColors(v1, color, v2, color, v3, color);


				//# Add a single vertex with pre-blended splat colors (for top face splatting)

			}
		}
	}//# Instead of passing splat data to shader, we blend colors here in the mesh builder
	protected void _AddVertexWithSplat(Vector3 v, Color base_color)
	{
		Vertices.Append(v);


		// TEMPORARILY use solid base color to debug geometry
		Colors.Append(base_color);
		VertexIndex += 1;
	}


	protected void _BuildCliff(Vector3 center, int edge_index, double height, Color color)
	{
		var wall_color = color.Darkened(0.45);
		_SetSplatSolid(wall_color);
		// Walls don't blend
		var c1 = Corners[edge_index];
		var c2 = Corners[(edge_index + 1) % 6];

		var top_left = new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z);
		var top_right = new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z);
		var bottom_left = new Vector3(top_left.X, center.Y - height, top_left.Z);
		var bottom_right = new Vector3(top_right.X, center.Y - height, top_right.Z);

		_AddTriangle(top_left, bottom_right, bottom_left, wall_color);
		_AddTriangle(top_left, top_right, bottom_right, wall_color);
	}


	protected void _BuildTerracedSlope(Vector3 center, Vector3 neighbor_center, int edge_index, Color begin_color, Color end_color)
	{
		_EdgeCount += 1;
		_SetSplatSolid(begin_color);
		// Terraces don't blend
		var solid = HexMetrics.SOLID_FACTOR;
		var c1 = Corners[edge_index];
		var c2 = Corners[(edge_index + 1) % 6];


		// Top edge: outer boundary of THIS cell's solid region (higher elevation)


		var top_left = new Vector3(, center.X + c1.X * solid, center.Y, center.Z + c1.Z * solid);


		var top_right = new Vector3(, center.X + c2.X * solid, center.Y, center.Z + c2.Z * solid);


		// The neighbor's edge that faces us is the opposite edge
		var opposite_edge = (edge_index + 3) % 6;
		var opp_c1 = Corners[opposite_edge];
		var opp_c2 = Corners[(opposite_edge + 1) % 6];


		// Bottom edge: outer boundary of NEIGHBOR's solid region (lower elevation)
		// Note: corners are swapped to align the edges properly


		var bottom_left = new Vector3(, neighbor_center.X + opp_c2.X * solid, neighbor_center.Y, neighbor_center.Z + opp_c2.Z * solid);


		var bottom_right = new Vector3(, neighbor_center.X + opp_c1.X * solid, neighbor_center.Y, neighbor_center.Z + opp_c1.Z * solid);


		// Build terraces from top to bottom
		var v1 = top_left;
		var v2 = top_right;
		var c_1 = begin_color;
		var c_2 = begin_color;

		foreach(int step in GD.Range(1, HexMetrics.GetTerraceSteps() + 1))
		{

			// Interpolate to the next terrace level
			var v3 = HexMetrics.TerraceLerp(top_left, bottom_left, step);
			var v4 = HexMetrics.TerraceLerp(top_right, bottom_right, step);
			var c_3 = HexMetrics.TerraceColorLerp(begin_color, end_color, step);
			var c_4 = HexMetrics.TerraceColorLerp(begin_color, end_color, step);


			// Build quad for this terrace step


			var avg_color = new Color(, (c_1.R + c_2.R + c_3.R + c_4.R) / 4.0, (c_1.G + c_2.G + c_3.G + c_4.G) / 4.0, (c_1.B + c_2.B + c_3.B + c_4.B) / 4.0);


			// Two triangles for the quad
			_AddTriangle(v1, v4, v3, avg_color);
			_AddTriangle(v1, v2, v4, avg_color);


			// Move to next step
			v1 = v3;
			v2 = v4;
			c_1 = c_3;
			c_2 = c_4;
		}
	}


	protected void _BuildFlatCliff(Vector3 center, Vector3 neighbor_center, int edge_index, Color color, Color neighbor_color)
	{
		_EdgeCount += 1;
		_SetSplatSolid(color);
		// Cliffs don't blend
		var solid = HexMetrics.SOLID_FACTOR;
		var c1 = Corners[edge_index];
		var c2 = Corners[(edge_index + 1) % 6];


		// This cell's solid edge corners (higher elevation)
		var v1 = new Vector3(center.X + c1.X * solid, center.Y, center.Z + c1.Z * solid);
		var v2 = new Vector3(center.X + c2.X * solid, center.Y, center.Z + c2.Z * solid);


		// Neighbor's solid edge corners (lower elevation)
		var opposite_edge = (edge_index + 3) % 6;
		var oc1 = Corners[opposite_edge];
		var oc2 = Corners[(opposite_edge + 1) % 6];


		// Note: oc2 aligns with v1, oc1 aligns with v2 (corners are swapped)
		var v3 = new Vector3(neighbor_center.X + oc2.X * solid, neighbor_center.Y, neighbor_center.Z + oc2.Z * solid);
		var v4 = new Vector3(neighbor_center.X + oc1.X * solid, neighbor_center.Y, neighbor_center.Z + oc1.Z * solid);

		var blend_color = color.Lerp(neighbor_color, 0.5);
		_AddTriangle(v1, v2, v4, blend_color);
		_AddTriangle(v1, v4, v3, blend_color);
	}


	protected void _BuildFlatEdge(Vector3 center, Vector3 neighbor_center, int edge_index, Color color, Color neighbor_color)
	{
		_EdgeCount += 1;
		_SetSplatSolid(color);
		// Flat edges don't blend
		var solid = HexMetrics.SOLID_FACTOR;
		var c1 = Corners[edge_index];
		var c2 = Corners[(edge_index + 1) % 6];


		// This cell's solid edge corners
		var v1 = new Vector3(center.X + c1.X * solid, center.Y, center.Z + c1.Z * solid);
		var v2 = new Vector3(center.X + c2.X * solid, center.Y, center.Z + c2.Z * solid);


		// Neighbor's solid edge corners (opposite edge)
		var opposite_edge = (edge_index + 3) % 6;
		var oc1 = Corners[opposite_edge];
		var oc2 = Corners[(opposite_edge + 1) % 6];


		// Note: oc2 aligns with v1, oc1 aligns with v2 (corners are swapped)
		var v3 = new Vector3(neighbor_center.X + oc2.X * solid, neighbor_center.Y, neighbor_center.Z + oc2.Z * solid);
		var v4 = new Vector3(neighbor_center.X + oc1.X * solid, neighbor_center.Y, neighbor_center.Z + oc1.Z * solid);

		var blend_color = color.Lerp(neighbor_color, 0.5);


		// Build quad - CCW winding for upward-facing
		_AddTriangle(v1, v2, v4, blend_color);
		_AddTriangle(v1, v4, v3, blend_color);
	}


	//# Build corner geometry where three hexes meet
	protected void _BuildCorner(Godot.HexCell cell, Vector3 center, Color color, int dir, Godot.HexCell neighbor1, Godot.HexCell neighbor2)
	{
		_CornerCount += 1;
		_SetSplatSolid(color);
		// Corner geometry doesn't blend
		var solid = HexMetrics.SOLID_FACTOR;
		var edge_index = _GetEdgeIndexForDirection(dir);


		// The shared corner position P (at full radius) - where all three cells meet
		var corner_idx = (edge_index + 1) % 6;
		var corner_offset = Corners[corner_idx];
		var P = new Vector3(center.X + corner_offset.X, 0, center.Z + corner_offset.Z);


		// Get neighbor centers
		var n1_center = neighbor1.GetWorldPosition();
		var n2_center = neighbor2.GetWorldPosition();


		// Calculate solid corner vertices for each cell
		// Each vertex is solid% of the way from cell center toward the shared corner P


		var v1 = new Vector3(, center.X + corner_offset.X * solid, center.Y, center.Z + corner_offset.Z * solid);


		// For neighbors, calculate direction from their center to P, then scale by solid


		var v2 = new Vector3(, n1_center.X + (P.X - n1_center.X) * solid, n1_center.Y, n1_center.Z + (P.Z - n1_center.Z) * solid);


		var v3 = new Vector3(, n2_center.X + (P.X - n2_center.X) * solid, n2_center.Y, n2_center.Z + (P.Z - n2_center.Z) * solid);


		// Get colors
		var c1 = color;
		var c2 = neighbor1.GetColor();
		var c3 = neighbor2.GetColor();


		// Get elevations
		var e1 = cell.Elevation;
		var e2 = neighbor1.Elevation;
		var e3 = neighbor2.Elevation;


		// Sort by elevation to find bottom, left, right (Catlike Coding approach)
		if(e1 <= e2)
		{
			if(e1 <= e3)
			{

				// e1 is lowest - current cell is bottom
				_TriangulateCorner(v1, c1, e1, v2, c2, e2, v3, c3, e3);
			}
			else
			{

				// e3 is lowest - neighbor2 is bottom, rotate CCW
				_TriangulateCorner(v3, c3, e3, v1, c1, e1, v2, c2, e2);
			}
		}
		else
		{
			if(e2 <= e3)
			{

				// e2 is lowest - neighbor1 is bottom, rotate CW
				_TriangulateCorner(v2, c2, e2, v3, c3, e3, v1, c1, e1);
			}
			else
			{

				// e3 is lowest - neighbor2 is bottom, rotate CCW
				_TriangulateCorner(v3, c3, e3, v1, c1, e1, v2, c2, e2);
			}
		}
	}


	//# Triangulate corner with bottom vertex first, then left and right
	protected void _TriangulateCorner(Vector3 bottom, Color bottom_color, int bottom_elev, Vector3 left, Color left_color, int left_elev, Vector3 right, Color right_color, int right_elev)
	{

		var left_edge_type = _GetEdgeType(bottom_elev, left_elev);
		var right_edge_type = _GetEdgeType(bottom_elev, right_elev);

		if(left_edge_type == "slope")
		{
			if(right_edge_type == "slope")
			{

				// Check if left and right are at same elevation (SSF case)
				var top_edge_type = _GetEdgeType(left_elev, right_elev);
				if(top_edge_type == "flat")
				{
					_TriangulateCornerSsf(bottom, bottom_color, left, left_color, right, right_color);
				}
				else
				{
					_TriangulateCornerTerraces(bottom, bottom_color, left, left_color, right, right_color);
				}
			}
			else if(right_edge_type == "cliff")
			{


				_TriangulateCornerTerracesCliff(, bottom, bottom_color, bottom_elev, left, left_color, left_elev, right, right_color, right_elev);
			}
			else
			{

				// Slope-Flat: terraces fan from left
				_TriangulateCornerTerraces(left, left_color, right, right_color, bottom, bottom_color);
			}
		}

		else if(left_edge_type == "cliff")
		{
			if(right_edge_type == "slope")
			{


				_TriangulateCornerCliffTerraces(, bottom, bottom_color, bottom_elev, left, left_color, left_elev, right, right_color, right_elev);
			}
			else if(right_edge_type == "cliff")
			{
				var top_edge_type = _GetEdgeType(left_elev, right_elev);
				if(top_edge_type == "slope")
				{
					if(left_elev < right_elev)
					{
						_TriangulateCornerCcsr(bottom, bottom_color, left, left_color, right, right_color);
					}
					else
					{
						_TriangulateCornerCcsl(bottom, bottom_color, left, left_color, right, right_color);
					}
				}
				else
				{
					_AddTriangleWithColors(bottom, bottom_color, left, left_color, right, right_color);
				}
			}
			else
			{
				_AddTriangleWithColors(bottom, bottom_color, left, left_color, right, right_color);
			}
		}

		else
		{

			// Left is flat
			if(right_edge_type == "slope")
			{
				_TriangulateCornerTerraces(right, right_color, bottom, bottom_color, left, left_color);
			}
			else
			{
				_AddTriangleWithColors(bottom, bottom_color, left, left_color, right, right_color);
			}
		}
	}


	protected String _GetEdgeType(int e1, int e2)
	{
		var diff = Mathf.Abs(e1 - e2);
		if(diff == 0)
		{
			return "flat";
		}
		else if(diff == 1)
		{
			return "slope";
		}
		else
		{
			return "cliff";
		}
	}


	//# Slope-Slope corner: terraced fan from bottom
	protected void _TriangulateCornerTerraces(Vector3 bottom, Color bottom_color, Vector3 left, Color left_color, Vector3 right, Color right_color)
	{

		var v3 = HexMetrics.TerraceLerp(bottom, left, 1);
		var v4 = HexMetrics.TerraceLerp(bottom, right, 1);
		var c3 = HexMetrics.TerraceColorLerp(bottom_color, left_color, 1);
		var c4 = HexMetrics.TerraceColorLerp(bottom_color, right_color, 1);

		_AddTriangleWithColors(bottom, bottom_color, v3, c3, v4, c4);

		foreach(int i in GD.Range(2, HexMetrics.GetTerraceSteps()))
		{
			var v1 = v3;
			var v2 = v4;
			var c1 = c3;
			var c2 = c4;
			v3 = HexMetrics.TerraceLerp(bottom, left, i);
			v4 = HexMetrics.TerraceLerp(bottom, right, i);
			c3 = HexMetrics.TerraceColorLerp(bottom_color, left_color, i);
			c4 = HexMetrics.TerraceColorLerp(bottom_color, right_color, i);
			_AddQuadWithColors(v1, c1, v2, c2, v3, c3, v4, c4);
		}

		_AddQuadWithColors(v3, c3, v4, c4, left, left_color, right, right_color);
	}


	//# Slope-Cliff corner: terraces on left, boundary triangles to cliff
	protected void _TriangulateCornerTerracesCliff(Vector3 bottom, Color bottom_color, int bottom_elev, Vector3 left, Color left_color, int left_elev, Vector3 right, Color right_color, int right_elev)
	{

		var b = 1.0 / (right_elev - bottom_elev);
		var boundary = bottom.Lerp(right, b);
		var boundary_color = bottom_color.Lerp(right_color, b);

		_TriangulateBoundaryTriangle(bottom, bottom_color, left, left_color, boundary, boundary_color);

		if(_GetEdgeType(left_elev, right_elev) == "slope")
		{
			_TriangulateBoundaryTriangle(left, left_color, right, right_color, boundary, boundary_color);
		}
		else
		{
			_AddTriangleWithColors(left, left_color, right, right_color, boundary, boundary_color);
		}
	}


	//# Cliff-Slope corner: boundary triangles on cliff, terraces on right
	protected void _TriangulateCornerCliffTerraces(Vector3 bottom, Color bottom_color, int bottom_elev, Vector3 left, Color left_color, int left_elev, Vector3 right, Color right_color, int right_elev)
	{

		var b = 1.0 / (left_elev - bottom_elev);
		var boundary = bottom.Lerp(left, b);
		var boundary_color = bottom_color.Lerp(left_color, b);

		_TriangulateBoundaryTriangle(bottom, bottom_color, right, right_color, boundary, boundary_color);

		if(_GetEdgeType(left_elev, right_elev) == "slope")
		{
			_TriangulateBoundaryTriangle(right, right_color, left, left_color, boundary, boundary_color);
		}
		else
		{
			_AddTriangleWithColors(right, right_color, left, left_color, boundary, boundary_color);
		}
	}


	//# SSF: Slope-Slope-Flat - both slopes, flat on top
	protected void _TriangulateCornerSsf(Vector3 bottom, Color bottom_color, Vector3 left, Color left_color, Vector3 right, Color right_color)
	{

		var v3 = HexMetrics.TerraceLerp(bottom, left, 1);
		var c3 = HexMetrics.TerraceColorLerp(bottom_color, left_color, 1);
		var v4 = HexMetrics.TerraceLerp(bottom, right, 1);
		var c4 = HexMetrics.TerraceColorLerp(bottom_color, right_color, 1);

		_AddTriangleWithColors(bottom, bottom_color, v3, c3, v4, c4);

		foreach(int i in GD.Range(2, HexMetrics.GetTerraceSteps() + 1))
		{
			var v3prev = v3;
			var c3prev = c3;
			var v4prev = v4;
			var c4prev = c4;

			v3 = HexMetrics.TerraceLerp(bottom, left, i);
			c3 = HexMetrics.TerraceColorLerp(bottom_color, left_color, i);
			v4 = HexMetrics.TerraceLerp(bottom, right, i);
			c4 = HexMetrics.TerraceColorLerp(bottom_color, right_color, i);

			_AddTriangleWithColors(v3prev, c3prev, v3, c3, v4prev, c4prev);
			_AddTriangleWithColors(v3, c3, v4, c4, v4prev, c4prev);
		}

		_AddTriangleWithColors(v3, c3, left, left_color, v4, c4);
		_AddTriangleWithColors(left, left_color, right, right_color, v4, c4);
	}


	//# CCSR: Cliff-Cliff with Slope, Right higher
	protected void _TriangulateCornerCcsr(Vector3 bottom, Color bottom_color, Vector3 left, Color left_color, Vector3 right, Color right_color)
	{

		var right_cliff_height = right.Y - bottom.Y;
		var left_height = left.Y - bottom.Y;
		var b = left_height / right_cliff_height;

		var boundary = bottom.Lerp(right, b);
		var boundary_color = bottom_color.Lerp(right_color, b);

		_AddTriangleWithColors(bottom, bottom_color, left, left_color, boundary, boundary_color);
		_TriangulateBoundaryTriangle(left, left_color, right, right_color, boundary, boundary_color);
	}


	//# CCSL: Cliff-Cliff with Slope, Left higher
	protected void _TriangulateCornerCcsl(Vector3 bottom, Color bottom_color, Vector3 left, Color left_color, Vector3 right, Color right_color)
	{

		var left_cliff_height = left.Y - bottom.Y;
		var right_height = right.Y - bottom.Y;
		var b = right_height / left_cliff_height;

		var boundary = bottom.Lerp(left, b);
		var boundary_color = bottom_color.Lerp(left_color, b);

		_AddTriangleWithColors(bottom, bottom_color, boundary, boundary_color, right, right_color);
		_TriangulateBoundaryTriangle(right, right_color, left, left_color, boundary, boundary_color);
	}


	//# Boundary triangle: fan from begin toward left, all pointing to boundary
	protected void _TriangulateBoundaryTriangle(Vector3 begin, Color begin_color, Vector3 left, Color left_color, Vector3 boundary, Color boundary_color)
	{

		var v2 = HexMetrics.TerraceLerp(begin, left, 1);
		var c2 = HexMetrics.TerraceColorLerp(begin_color, left_color, 1);

		_AddTriangleWithColors(begin, begin_color, v2, c2, boundary, boundary_color);

		foreach(int i in GD.Range(2, HexMetrics.GetTerraceSteps()))
		{
			var v1 = v2;
			var c1 = c2;
			v2 = HexMetrics.TerraceLerp(begin, left, i);
			c2 = HexMetrics.TerraceColorLerp(begin_color, left_color, i);
			_AddTriangleWithColors(v1, c1, v2, c2, boundary, boundary_color);
		}

		_AddTriangleWithColors(v2, c2, left, left_color, boundary, boundary_color);
	}


	//# Add triangle with per-vertex colors (auto-corrects winding for upward normal)
	protected void _AddTriangleWithColors(Vector3 v1, Color c1, Vector3 v2, Color c2, Vector3 v3, Color c3)
	{

		var edge1 = v2 - v1;
		var edge2 = v3 - v1;
		var normal = edge1.Cross(edge2);


		// Godot needs opposite winding from Three.js
		// Reverse when normal points UP (opposite of Three.js logic)
		if(normal.Y > 0)
		{
			Vertices.Append(v1);
			Vertices.Append(v3);
			Vertices.Append(v2);
			Colors.Append(c1);
			Colors.Append(c3);
			Colors.Append(c2);
		}
		else
		{
			Vertices.Append(v1);
			Vertices.Append(v2);
			Vertices.Append(v3);
			Colors.Append(c1);
			Colors.Append(c2);
			Colors.Append(c3);
		}

		Indices.Append(VertexIndex);
		Indices.Append(VertexIndex + 1);
		Indices.Append(VertexIndex + 2);
		VertexIndex += 3;
	}


	//# Add quad with per-vertex colors
	protected void _AddQuadWithColors(Vector3 v1, Color c1, Vector3 v2, Color c2, Vector3 v3, Color c3, Vector3 v4, Color c4)
	{


		var avg_color = new Color(, (c1.R + c2.R + c3.R + c4.R) / 4.0, (c1.G + c2.G + c3.G + c4.G) / 4.0, (c1.B + c2.B + c3.B + c4.B) / 4.0);

		_AddTriangleWithColors(v1, avg_color, v2, avg_color, v3, avg_color);
		_AddTriangleWithColors(v2, avg_color, v4, avg_color, v3, avg_color);
	}


	protected void _AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color)
	{

		// Calculate normal to determine correct winding for Godot
		var edge1 = v2 - v1;
		var edge2 = v3 - v1;
		var normal = edge1.Cross(edge2);


		// Godot needs opposite winding from Three.js for cull_back to work correctly
		// Reverse when normal points UP (y > 0)
		if(normal.Y > 0)
		{
			Vertices.Append(v1);
			Vertices.Append(v3);
			Vertices.Append(v2);
		}
		else
		{
			Vertices.Append(v1);
			Vertices.Append(v2);
			Vertices.Append(v3);
		}

		Colors.Append(color);
		Colors.Append(color);
		Colors.Append(color);


		// Add splat data for each vertex
		var weight_color = new Color(CurrentSplatWeights.X, CurrentSplatWeights.Y, CurrentSplatWeights.Z, 1.0);
		foreach(int _i in GD.Range(3))
		{
			SplatColor1.Append(CurrentSplatColor1);
			SplatColor2.Append(CurrentSplatColor2);
			SplatColor3.Append(CurrentSplatColor3);
			SplatWeights.Append(weight_color);
		}

		Indices.Append(VertexIndex);
		Indices.Append(VertexIndex + 1);
		Indices.Append(VertexIndex + 2);
		VertexIndex += 3;
	}


	//# Set splat state for next vertices (no blending - 100% main color)
	protected void _SetSplatSolid(Color color)
	{
		CurrentSplatColor1 = color;
		CurrentSplatColor2 = color;
		CurrentSplatColor3 = color;
		CurrentSplatWeights = new Vector3(1, 0, 0);
	}


	//# Set splat state for blended corner vertex
	protected void _SetSplatBlended(Color main_color, Color neighbor1_color, Color neighbor2_color)
	{
		CurrentSplatColor1 = main_color;
		CurrentSplatColor2 = neighbor1_color;
		CurrentSplatColor3 = neighbor2_color;

		// More conservative blend - main color dominates with subtle neighbor influence
		CurrentSplatWeights = new Vector3(0.7, 0.15, 0.15);
	}


	//# Blend three colors with fixed weights for corner vertices
	protected Color _BlendColors(Color main_color, Color neighbor1, Color neighbor2)
	{

		// 70% main, 15% each neighbor for subtle but visible blending
		var w = new Vector3(0.7, 0.15, 0.15);


		return new Color(, main_color.R * w.X + neighbor1.R * w.Y + neighbor2.R * w.Z, main_color.G * w.X + neighbor1.G * w.Y + neighbor2.G * w.Z, main_color.B * w.X + neighbor1.B * w.Y + neighbor2.B * w.Z);
	}


	protected Godot.ArrayMesh _CreateMesh()
	{

		// Use SurfaceTool to build the mesh with FLAT normals for crisp edges
		// Splat blending is pre-computed in vertex colors, so no custom attributes needed
		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);


		// Add each triangle with explicit flat normals
		var num_triangles = Vertices.Size() / 3;
		foreach(int i in GD.Range(num_triangles))
		{
			var idx = i * 3;
			var v0 = Vertices[idx];
			var v1 = Vertices[idx + 1];
			var v2 = Vertices[idx + 2];


			// Calculate flat face normal
			var edge1 = v1 - v0;
			var edge2 = v2 - v0;
			var normal = edge1.Cross(edge2).Normalized();


			// Add all 3 vertices with the SAME flat normal
			st.SetNormal(normal);
			st.SetColor(Colors[idx]);
			st.AddVertex(v0);

			st.SetNormal(normal);
			st.SetColor(Colors[idx + 1]);
			st.AddVertex(v1);

			st.SetNormal(normal);
			st.SetColor(Colors[idx + 2]);
			st.AddVertex(v2);
		}


		// Don't call generate_normals() - we set them manually for flat shading
		return st.Commit();
	}


}