using Godot;
using Godot.Collections;


//# Terrain type definitions

//# Matches web/src/types/index.ts
[GlobalClass]
public partial class TerrainType : Godot.RefCounted
{
	public enum Type {OCEAN, COAST, PLAINS, FOREST, HILLS, MOUNTAINS, SNOW, DESERT, TUNDRA, JUNGLE, SAVANNA, TAIGA}


	// Terrain colors - stylized low-poly palette
	public const Dictionary COLORS = new Dictionary{
			{Type.Ocean, new Color(0.102, 0.298, 0.431)},
			// 0x1a4c6e - Deep blue
			{Type.Coast, new Color(0.176, 0.545, 0.788)},
			// 0x2d8bc9 - Light blue
			{Type.Plains, new Color(0.52, 0.75, 0.28)},
			// Brighter grass green
			{Type.Forest, new Color(0.18, 0.49, 0.196)},
			// 0x2e7d32 - Dark green
			{Type.Hills, new Color(0.553, 0.431, 0.388)},
			// 0x8d6e63 - Brown
			{Type.Mountains, new Color(0.459, 0.459, 0.459)},
			// 0x757575 - Gray
			{Type.Snow, new Color(0.925, 0.937, 0.945)},
			// 0xeceff1 - White
			{Type.Desert, new Color(0.902, 0.784, 0.431)},
			// 0xe6c86e - Sand yellow
			{Type.Tundra, new Color(0.565, 0.643, 0.682)},
			// 0x90a4ae - Blue-gray
			{Type.Jungle, new Color(0.106, 0.369, 0.125)},
			// 0x1b5e20 - Deep green
			{Type.Savanna, new Color(0.773, 0.659, 0.333)},
			// 0xc5a855 - Golden brown
			{Type.Taiga, new Color(0.29, 0.388, 0.365)},
			// 0x4a635d - Dark teal-green
			};


	public static Color GetColor(Type terrain)
	{
		return COLORS.Get(terrain, Color.White);
	}


	public static bool IsWater(Type terrain)
	{
		return terrain == Type.Ocean || terrain == Type.Coast;
	}


	public static String GetTerrainName(Type terrain)
	{

		if(terrain == Type.Ocean)
		{return "Ocean";
		}
		if(terrain == Type.Coast)
		{return "Coast";
		}
		if(terrain == Type.Plains)
		{return "Plains";
		}
		if(terrain == Type.Forest)
		{return "Forest";
		}
		if(terrain == Type.Hills)
		{return "Hills";
		}
		if(terrain == Type.Mountains)
		{return "Mountains";
		}
		if(terrain == Type.Snow)
		{return "Snow";
		}
		if(terrain == Type.Desert)
		{return "Desert";
		}
		if(terrain == Type.Tundra)
		{return "Tundra";
		}
		if(terrain == Type.Jungle)
		{return "Jungle";
		}
		if(terrain == Type.Savanna)
		{return "Savanna";
		}
		if(terrain == Type.Taiga)
		{return "Taiga";
		}
		else 
		{return "Unknown";
		}
	}
}

