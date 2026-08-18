namespace GodotUtilities;

public static class BooleanExtension
{
    public static float ToSingle(this bool value) => value ? 1f : 0f;

    public static int ToInt(this bool value) => value ? 1 : 0;
    
    public static int ToSign(this bool value) => value ? 1 : -1;
}
