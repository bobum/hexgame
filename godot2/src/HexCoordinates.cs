using Godot;

/// <summary>
/// Hexagonal cube coordinates.
/// Ported exactly from Catlike Coding Hex Map Tutorial 1.
/// </summary>
[System.Serializable]
public struct HexCoordinates
{
    private int x, z;

    public int X => x;
    public int Z => z;
    public int Y => -X - Z;

    public HexCoordinates(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - z / 2, z);
    }

    public static HexCoordinates FromPosition(Vector3 position)
    {
        float x = position.X / (HexMetrics.InnerRadius * 2f);
        float y = -x;
        float offset = position.Z / (HexMetrics.OuterRadius * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public override string ToString()
    {
        return "(" + X + ", " + Y + ", " + Z + ")";
    }

    public string ToStringOnSeparateLines()
    {
        return X + "\n" + Y + "\n" + Z;
    }

    /// <summary>
    /// Calculates the distance to another hex cell in cube coordinates.
    /// Tutorial 15: Uses the formula (|x1-x2| + |y1-y2| + |z1-z2|) / 2
    /// </summary>
    public int DistanceTo(HexCoordinates other)
    {
        return ((X < other.X ? other.X - X : X - other.X) +
                (Y < other.Y ? other.Y - Y : Y - other.Y) +
                (Z < other.Z ? other.Z - Z : Z - other.Z)) / 2;
    }
}
