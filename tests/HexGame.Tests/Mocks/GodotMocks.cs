// Mock types for Godot dependencies when running tests outside Godot
// These provide minimal implementations to allow core logic testing

namespace Godot;

/// <summary>
/// Mock Vector2 for testing.
/// </summary>
public struct Vector2
{
    public float X;
    public float Y;

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);

    public float Length() => MathF.Sqrt(X * X + Y * Y);
    public Vector2 Normalized()
    {
        var len = Length();
        return len > 0 ? new Vector2(X / len, Y / len) : Zero;
    }

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 v, float s) => new(v.X * s, v.Y * s);
    public static Vector2 operator /(Vector2 v, float s) => new(v.X / s, v.Y / s);
}

/// <summary>
/// Mock Vector2I for testing.
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

    public static Vector2I Zero => new(0, 0);
    public static Vector2I One => new(1, 1);

    public static bool operator ==(Vector2I a, Vector2I b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vector2I a, Vector2I b) => !(a == b);

    public override bool Equals(object? obj) => obj is Vector2I v && this == v;
    public override int GetHashCode() => HashCode.Combine(X, Y);
}

/// <summary>
/// Mock Vector3 for testing.
/// </summary>
public struct Vector3
{
    public float X;
    public float Y;
    public float Z;

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 Zero => new(0, 0, 0);
    public static Vector3 One => new(1, 1, 1);
    public static Vector3 Up => new(0, 1, 0);
    public static Vector3 Down => new(0, -1, 0);
    public static Vector3 Forward => new(0, 0, -1);
    public static Vector3 Back => new(0, 0, 1);

    public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator *(Vector3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);
}

/// <summary>
/// Mock Color for testing.
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

    public static Color White => new(1, 1, 1, 1);
    public static Color Black => new(0, 0, 0, 1);
    public static Color Red => new(1, 0, 0, 1);
    public static Color Green => new(0, 1, 0, 1);
    public static Color Blue => new(0, 0, 1, 1);

    public Color Lerp(Color to, float weight)
    {
        return new Color(
            R + (to.R - R) * weight,
            G + (to.G - G) * weight,
            B + (to.B - B) * weight,
            A + (to.A - A) * weight
        );
    }
}

/// <summary>
/// Mock GD static class for logging.
/// </summary>
public static class GD
{
    public static void Print(params object[] args)
    {
        Console.WriteLine(string.Join(" ", args));
    }

    public static void PrintErr(params object[] args)
    {
        Console.Error.WriteLine(string.Join(" ", args));
    }

    public static void PushError(string message)
    {
        Console.Error.WriteLine($"ERROR: {message}");
    }

    public static void PushWarning(string message)
    {
        Console.WriteLine($"WARNING: {message}");
    }
}

/// <summary>
/// Mock Mathf for testing.
/// </summary>
public static class Mathf
{
    public const float Pi = MathF.PI;
    public const float Tau = MathF.Tau;
    public const float Epsilon = 1e-6f;

    public static float Abs(float x) => MathF.Abs(x);
    public static float Sin(float x) => MathF.Sin(x);
    public static float Cos(float x) => MathF.Cos(x);
    public static float Sqrt(float x) => MathF.Sqrt(x);
    public static float Floor(float x) => MathF.Floor(x);
    public static float Ceil(float x) => MathF.Ceiling(x);
    public static float Round(float x) => MathF.Round(x);
    public static int RoundToInt(float x) => (int)MathF.Round(x);
    public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
    public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
    public static float Min(float a, float b) => MathF.Min(a, b);
    public static float Max(float a, float b) => MathF.Max(a, b);
    public static int Min(int a, int b) => Math.Min(a, b);
    public static int Max(int a, int b) => Math.Max(a, b);
    public static bool IsEqualApprox(float a, float b) => MathF.Abs(a - b) < Epsilon;
}
