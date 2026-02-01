using Godot;

/// <summary>
/// Applies wind shader material to vegetation meshes.
/// Attach to a tree/plant scene root to enable wind animation on leaves.
/// </summary>
public partial class VegetationWindApplier : Node3D
{
    [Export] public Material? LeavesMaterial;
    [Export] public string LeavesNodeName = "Leaves";
    [Export] public bool ApplyToAllMeshes = false;

    public override void _Ready()
    {
        if (LeavesMaterial == null)
        {
            // Try to load default wind material
            LeavesMaterial = GD.Load<Material>("res://materials/vegetation_leaves.tres");
        }

        if (LeavesMaterial == null)
        {
            GD.PrintErr("[VegetationWindApplier] No leaves material assigned or found");
            return;
        }

        ApplyWindMaterial(this);
    }

    private void ApplyWindMaterial(Node node)
    {
        if (node is MeshInstance3D mesh)
        {
            // Check node name for leaves/foliage keywords
            string nodeName = node.Name.ToString().ToLower();
            bool nodeIsLeaves = nodeName.Contains("leaves") || nodeName.Contains("foliage");

            // Apply to all surfaces and check each material
            for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
            {
                var existingMat = mesh.GetSurfaceOverrideMaterial(i);
                if (existingMat == null)
                {
                    existingMat = mesh.Mesh?.SurfaceGetMaterial(i);
                }

                // Get material name (check both ResourceName and ResourcePath)
                string matName = "";
                if (existingMat != null)
                {
                    matName = existingMat.ResourceName?.ToLower() ?? "";
                    if (string.IsNullOrEmpty(matName))
                    {
                        matName = existingMat.ResourcePath?.ToLower() ?? "";
                    }
                }

                bool materialIsLeaves = matName.Contains("leaves") || matName.Contains("foliage");

                // Apply wind material if node or material indicates leaves
                if (ApplyToAllMeshes || nodeIsLeaves || materialIsLeaves)
                {
                    mesh.SetSurfaceOverrideMaterial(i, LeavesMaterial);
                    GD.Print($"[VegetationWind] Applied wind material to {node.Name} surface {i} (mat: {matName})");
                }
            }
        }

        // Recurse through children
        foreach (var child in node.GetChildren())
        {
            ApplyWindMaterial(child);
        }
    }
}
