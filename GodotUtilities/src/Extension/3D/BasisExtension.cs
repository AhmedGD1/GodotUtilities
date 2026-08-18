using Godot;

namespace GodotUtilities;

public static class BasisExtension
{
    public static Vector3 Forward(this Basis basis) => -basis.Z;
    
    public static Vector3 Back(this Basis basis) => basis.Z;
    
    public static Vector3 Right(this Basis basis) => basis.X;
    
    public static Vector3 Left(this Basis basis) => -basis.X;
    
    public static Vector3 Up(this Basis basis) => basis.Y;
    
    public static Vector3 Down(this Basis basis) => -basis.Y;
}
