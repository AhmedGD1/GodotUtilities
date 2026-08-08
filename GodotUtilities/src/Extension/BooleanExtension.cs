namespace GodotUtilities;

public static class BooleanExtension
{
    public static float ToSingle(this bool value) => value ? 1f : 0f;
    
    public static float ToSign(this bool value) => value ? 1f : -1f;
}
