using Godot;
using Godot.Collections;


//# Runtime unit instance data.

//# Matches web UnitData interface.
[GlobalClass]
public partial class Unit : Godot.RefCounted
{
	public int Id = 0;
	public UnitTypes.Type Type;
	public int Q = 0;
	// Hex position Q
	public int R = 0;
	// Hex position R
	public int Health = 100;
	public int MaxHealth = 100;
	public int Movement = 2;
	// Current movement points this turn
	public int MaxMovement = 2;
	// Movement points per turn
	public int Attack = 10;
	public int Defense = 8;
	public int PlayerId = 0;
	// 0 = neutral, 1 = player, 2+ = AI
	public bool HasMoved = false;


	public override void _Init(UnitTypes.Type unit_type = UnitTypes.Type.INFANTRY)
	{
		Type = unit_type;
		_ApplyStats();
	}


	protected void _ApplyStats()
	{
		var stats = UnitTypes.GetStats(Type);
		Health = stats["health"];
		MaxHealth = stats["health"];
		Movement = stats["movement"];
		MaxMovement = stats["movement"];
		Attack = stats["attack"];
		Defense = stats["defense"];
	}


	public void ResetMovement()
	{
		Movement = MaxMovement;
		HasMoved = false;
	}


	public bool SpendMovement(int cost)
	{
		if(Movement < cost)
		{
			return false;
		}
		Movement -= cost;
		HasMoved = true;
		return true;
	}


	public bool CanMove()
	{
		return Movement > 0;
	}


	public Vector3 GetWorldPosition()
	{
		var coords = HexCoordinates.New(Q, R);
		return coords.ToWorldPosition(0);
	}


	public String GetTypeName()
	{
		return UnitTypes.GetName(Type);
	}


	public UnitTypes.Domain GetDomain()
	{
		return UnitTypes.GetDomain(Type);
	}


	public bool CanTraverseLand()
	{
		return UnitTypes.CanTraverseLand(Type);
	}


	public bool CanTraverseWater()
	{
		return UnitTypes.CanTraverseWater(Type);
	}


	//# Reset unit state for reuse from object pool.
	public void ResetForPool()
	{
		Id = 0;
		Type = UnitTypes.Type.INFANTRY;
		Q = 0;
		R = 0;
		Health = 100;
		MaxHealth = 100;
		Movement = 2;
		MaxMovement = 2;
		Attack = 10;
		Defense = 8;
		PlayerId = 0;
		HasMoved = false;
	}


	//# Initialize unit with new values (used when acquiring from pool).
	public void InitWith(UnitTypes.Type unit_type, int unit_q, int unit_r, int unit_player_id)
	{
		Type = unit_type;
		Q = unit_q;
		R = unit_r;
		PlayerId = unit_player_id;
		HasMoved = false;
		_ApplyStats();
	}


}