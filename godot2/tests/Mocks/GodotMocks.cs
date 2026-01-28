namespace Godot;

/// <summary>
/// Mock GD for testing without Godot runtime.
/// Provides no-op implementations of Print methods.
/// </summary>
public static class GD
{
    public static void Print(params object[] what) { }
    public static void PrintErr(params object[] what) { }
}

/// <summary>
/// Mock Mathf for testing without Godot runtime.
/// </summary>
public static class Mathf
{
    public static int RoundToInt(float value) => (int)Math.Round(value);
    public static float Abs(float value) => Math.Abs(value);
    public static float Floor(float value) => (float)Math.Floor(value);
    public static float Sqrt(float value) => (float)Math.Sqrt(value);
}

/// <summary>
/// Mock Vector3 for testing without Godot runtime.
/// Replicates essential Godot.Vector3 functionality.
/// </summary>
public struct Vector3
{
    public float X;
    public float Y;
    public float Z;

    public static readonly Vector3 Zero = new Vector3(0, 0, 0);

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 operator +(Vector3 a, Vector3 b) =>
        new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3 operator -(Vector3 a, Vector3 b) =>
        new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3 operator *(Vector3 v, float s) =>
        new Vector3(v.X * s, v.Y * s, v.Z * s);

    public static Vector3 operator *(float s, Vector3 v) =>
        new Vector3(v.X * s, v.Y * s, v.Z * s);

    public override string ToString() => $"({X}, {Y}, {Z})";

    public bool Equals(Vector3 other, float tolerance = 0.0001f) =>
        Math.Abs(X - other.X) < tolerance &&
        Math.Abs(Y - other.Y) < tolerance &&
        Math.Abs(Z - other.Z) < tolerance;

    public Vector3 Lerp(Vector3 to, float weight) =>
        new Vector3(
            X + (to.X - X) * weight,
            Y + (to.Y - Y) * weight,
            Z + (to.Z - Z) * weight
        );
}

/// <summary>
/// Mock Vector2I for testing without Godot runtime.
/// </summary>
public struct Vector2I
{
    public int X;
    public int Y;

    public Vector2I(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"({X}, {Y})";
}

/// <summary>
/// Mock Color for testing without Godot runtime.
/// </summary>
public struct Color
{
    public float R;
    public float G;
    public float B;
    public float A;

    public Color(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public override string ToString() => $"Color({R}, {G}, {B}, {A})";

    public Color Lerp(Color to, float weight) =>
        new Color(
            R + (to.R - R) * weight,
            G + (to.G - G) * weight,
            B + (to.B - B) * weight,
            A + (to.A - A) * weight
        );
}
