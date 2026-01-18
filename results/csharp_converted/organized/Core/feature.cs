using Godot;
using Godot.Collections;


//# Represents a decorative feature (tree, rock) on a hex cell
[GlobalClass]
public partial class Feature : Godot.RefCounted
{
	public enum Type {NONE, TREE, ROCK}

	public Type Type = Type.None;
	public Vector3 Position = Vector3.Zero;
	public double Rotation = 0.0;
	public double Scale = 1.0;


	public override void _Init(Type feature_type = Type.None, Vector3 pos = Vector3.Zero, double rot = 0.0, double feature_scale = 1.0)
	{
		Type = feature_type;
		Position = pos;
		Rotation = rot;
		Scale = feature_scale;
	}


}