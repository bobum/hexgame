namespace HexGame.Core;

/// <summary>
/// Centralized game constants for balance tuning and configuration.
/// Modify values here to adjust game behavior across the entire codebase.
/// </summary>
public static class GameConstants
{
    #region Unit Stats

    public static class UnitStats
    {
        // Infantry
        public const int InfantryHealth = 100;
        public const int InfantryMovement = 2;
        public const int InfantryAttack = 10;
        public const int InfantryDefense = 8;

        // Cavalry
        public const int CavalryHealth = 80;
        public const int CavalryMovement = 4;
        public const int CavalryAttack = 12;
        public const int CavalryDefense = 5;

        // Archer
        public const int ArcherHealth = 60;
        public const int ArcherMovement = 2;
        public const int ArcherAttack = 15;
        public const int ArcherDefense = 3;

        // Settler
        public const int SettlerHealth = 50;
        public const int SettlerMovement = 2;
        public const int SettlerAttack = 0;
        public const int SettlerDefense = 2;

        // Tank
        public const int TankHealth = 150;
        public const int TankMovement = 3;
        public const int TankAttack = 20;
        public const int TankDefense = 12;

        // Ship
        public const int ShipHealth = 120;
        public const int ShipMovement = 4;
        public const int ShipAttack = 15;
        public const int ShipDefense = 8;

        // Naval Transport
        public const int TransportHealth = 70;
        public const int TransportMovement = 3;
        public const int TransportAttack = 0;
        public const int TransportDefense = 4;

        // Galley (light naval)
        public const int GalleyHealth = 80;
        public const int GalleyMovement = 3;
        public const int GalleyAttack = 8;
        public const int GalleyDefense = 6;

        // Warship (heavy naval)
        public const int WarshipHealth = 150;
        public const int WarshipMovement = 2;
        public const int WarshipAttack = 20;
        public const int WarshipDefense = 12;

        // Marine (amphibious)
        public const int MarineHealth = 70;
        public const int MarineMovement = 2;
        public const int MarineAttack = 8;
        public const int MarineDefense = 6;
    }

    #endregion

    #region Movement Costs

    public static class Movement
    {
        // Base terrain costs
        public const float BaseCost = 1.0f;
        public const float ForestCost = 1.5f;
        public const float JungleCost = 2.0f;
        public const float HillsCost = 2.0f;
        public const float SnowCost = 2.5f;
        public const float MarshCost = 3.0f;

        // Naval terrain costs
        public const float OceanCost = 1.0f;
        public const float CoastCost = 1.5f;

        // Special movement costs
        public const float RiverCrossingCost = 1.0f;
        public const float EmbarkDisembarkCost = 1.0f;
        public const float ClimbingPenaltyMultiplier = 0.5f;

        // Elevation thresholds
        public const int CliffElevationDifference = 2;
    }

    #endregion

    #region Map Generation

    public static class Generation
    {
        // Noise parameters
        public const float NoiseScale = 0.02f;
        public const int NoiseOctaves = 4;
        public const float NoisePersistence = 0.5f;
        public const float NoiseLacunarity = 2.0f;

        // Moisture noise
        public const int MoistureNoiseSeedOffset = 1000;
        public const float MoistureNoiseFrequency = 0.03f;

        // Height thresholds
        public const float SeaLevelThreshold = 0.35f;
        public const float MountainLevelThreshold = 0.75f;

        // Biome elevation thresholds
        public const int MountainElevation = 6;
        public const int HillElevation = 4;

        // Biome moisture thresholds
        public const float DesertMoisture = 0.2f;
        public const float GrasslandMoisture = 0.4f;
        public const float ForestMoisture = 0.6f;
        public const float JungleMoisture = 0.8f;

        // River generation
        public const float RiverPercentage = 0.1f;
        public const int RiverSeedOffset = 7777;
        public const int MinRiverLength = 3;
        public const float RiverSourceFitnessThreshold = 0.25f;
        public const int RiverTracingSafetyLimit = 100;
        public const float RiverSteepnessWeight = 3.0f;

        // Weighted selection thresholds
        public const float WeightedSelectionHigh = 0.75f;
        public const float WeightedSelectionMedium = 0.5f;
        public const float WeightHighPriority = 4.0f;
        public const float WeightMediumPriority = 2.0f;
        public const float WeightLowPriority = 1.0f;

        // Feature generation
        public const int FeatureSeedOffset = 2000;
    }

    #endregion

    #region Feature Placement

    public static class Features
    {
        // Tree placement
        public const int MinTreesPerCell = 1;
        public const int MaxTreesPerCell = 4;
        public const float TreeOffsetMin = -0.3f;
        public const float TreeOffsetMax = 0.3f;
        public const float TreeScaleMin = 0.8f;
        public const float TreeScaleMax = 1.2f;

        // Rock placement
        public const int MinRocksPerCell = 1;
        public const int MaxRocksPerCell = 3;
        public const float RockOffsetMin = -0.35f;
        public const float RockOffsetMax = 0.35f;
        public const float RockScaleMin = 0.6f;
        public const float RockScaleMax = 1.4f;

        // Feature chances by terrain (tree, rock)
        public const float ForestTreeChance = 0.7f;
        public const float ForestRockChance = 0.1f;
        public const float JungleTreeChance = 0.85f;
        public const float JungleRockChance = 0.05f;
        public const float GrasslandTreeChance = 0.15f;
        public const float GrasslandRockChance = 0.1f;
        public const float HillsTreeChance = 0.2f;
        public const float HillsRockChance = 0.2f;
        public const float MountainTreeChance = 0.05f;
        public const float MountainRockChance = 0.3f;
        public const float DesertTreeChance = 0.0f;
        public const float DesertRockChance = 0.15f;
        public const float TundraTreeChance = 0.1f;
        public const float TundraRockChance = 0.2f;
        public const float SnowTreeChance = 0.05f;
        public const float SnowRockChance = 0.1f;

        // Savanna
        public const float SavannaTreeChance = 0.1f;
        public const float SavannaRockChance = 0.15f;

        // Taiga
        public const float TaigaTreeChance = 0.4f;
        public const float TaigaRockChance = 0.1f;
    }

    #endregion

    #region Pooling & Systems

    public static class Pooling
    {
        public const int DefaultPoolMaxSize = 1000;
        public const int UnitPoolMaxSize = 500;
        public const float SpatialHashGridSize = 2.0f;
    }

    #endregion

    #region Rendering

    public static class Rendering
    {
        public const int ChunkSize = 16;

        // Render distances
        public const float TerrainRenderDistance = 200.0f;
        public const float WaterRenderDistance = 200.0f;
        public const float FeatureRenderDistance = 100.0f;
        public const float UnitRenderDistance = 150.0f;

        // Fade settings
        public const float FadeStartPercentage = 0.8f;

        // Default player colors (R, G, B)
        public static readonly (float R, float G, float B)[] PlayerColors =
        {
            (0.2f, 0.4f, 0.8f),  // Player 1: Blue
            (0.8f, 0.2f, 0.2f),  // Player 2: Red
            (0.2f, 0.8f, 0.2f),  // Player 3: Green
            (0.8f, 0.8f, 0.2f),  // Player 4: Yellow
            (0.8f, 0.2f, 0.8f),  // Player 5: Purple
            (0.2f, 0.8f, 0.8f),  // Player 6: Cyan
        };
    }

    #endregion

    #region Game State

    public static class GameState
    {
        public const int HumanPlayerId = 1;
        public const int FirstAIPlayerId = 2;
        public const int DefaultPlayerCount = 2;
        public const int MaxHistorySize = 100;
    }

    #endregion
}
