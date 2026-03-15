using System.Threading.Tasks;
using System;
using Godot;

namespace Utilities;

public static class TweenExtensions
{
    private const string PROPERTY_POSITION         = "position";
    private const string PROPERTY_GLOBAL_POSITION  = "global_position";
    private const string PROPERTY_SCALE            = "scale";
    private const string PROPERTY_ROTATION_DEGREES = "rotation_degrees";
    private const string PROPERTY_MODULATE_ALPHA   = "modulate:a";
    private const string PROPERTY_MODULATE         = "modulate";
    private const string PROPERTY_COLOR            = "color";
    private const string PROPERTY_VISIBLE_RATIO    = "visible_ratio";
    private const string PROPERTY_PIVOT_OFFSET     = "pivot_offset";
    private const string PROPERTY_VOLUME           = "volume";
    private const string PROPERTY_FOV              = "fov";

    #region Delay

    public static IntervalTweener Delay(this Tween tween, float duration) =>
        tween.TweenInterval(duration);

    #endregion

    #region UI

    public static PropertyTweener TweenPivot(this Tween tween, Control control, Vector2 value, float duration) =>
        tween.TweenProperty(control, PROPERTY_PIVOT_OFFSET, value, duration);

    #endregion

    #region Follow Path

    public static PropertyTweener TweenFollowPath(this Tween tween, PathFollow2D follower, float duration) =>
        tween.TweenProperty(follower, "progress_ratio", 1f, duration);

    #endregion

    #region Move

    public static PropertyTweener TweenPosition(this Tween tween, GodotObject target, Vector2 to, float duration) =>
        tween.TweenProperty(target, PROPERTY_POSITION, to, duration);
    
    public static PropertyTweener TweenGlobalPosition(this Tween tween, GodotObject target, Vector2 to, float duration) =>
        tween.TweenProperty(target, PROPERTY_GLOBAL_POSITION, to, duration);

    #endregion

    #region Rotation

    public static PropertyTweener TweenRotation(this Tween tween, GodotObject target, float degrees, float duration) =>
        tween.TweenProperty(target, PROPERTY_ROTATION_DEGREES, degrees, duration);

    #endregion

    #region Scale

    public static PropertyTweener TweenScale(this Tween tween, GodotObject target, Vector2 scale, float duration) =>
        tween.TweenProperty(target, PROPERTY_SCALE, scale, duration);
    
    #endregion

    #region Squish

    public enum SquishDir { Up, Down }

    public static Tween TweenSquish2D(this Tween tween, GodotObject target, float duration, float ratio = 0.2f, SquishDir dir = SquishDir.Up)
    {
        float stepDuration = duration / 3f;

        Vector2 up   = new Vector2(1f - ratio, 1f + ratio);
        Vector2 down = new Vector2(1f + ratio, 1f - ratio);

        tween.TweenProperty(target, PROPERTY_SCALE, dir == SquishDir.Up ? up : down, stepDuration).Sine().EaseIn();
        tween.TweenProperty(target, PROPERTY_SCALE, dir == SquishDir.Down ? down : up, stepDuration).Sine().EaseIn();
        tween.TweenProperty(target, PROPERTY_SCALE, Vector2.One, stepDuration).Sine().EaseOut();

        return tween;
    }

    public static Tween TweenSquish3D(this Tween tween, GodotObject target, float duration, float ratio = 0.2f, SquishDir dir = SquishDir.Up)
    {
        float stepDuration = duration / 3f;

        Vector3 up   = new Vector3(1f - ratio, 1f + ratio, 1f - ratio);
        Vector3 down = new Vector3(1f + ratio, 1f - ratio, 1f + ratio);

        tween.TweenProperty(target, PROPERTY_SCALE, dir == SquishDir.Up ? up : down, stepDuration);
        tween.TweenProperty(target, PROPERTY_SCALE, dir == SquishDir.Down ? down : up, stepDuration);
        tween.TweenProperty(target, PROPERTY_SCALE, Vector3.One, stepDuration);

        return tween;
    }

    #endregion

    #region Fade 

    public static PropertyTweener TweenFade(this Tween tween, GodotObject target, float value, float duration) =>
        tween.TweenProperty(target, PROPERTY_MODULATE_ALPHA, value, duration);

    public static PropertyTweener TweenFadeIn(this Tween tween, GodotObject target, float duration) =>
        tween.TweenFade(target, 1f, duration);

    public static PropertyTweener TweenFadeOut(this Tween tween, GodotObject target, float duration) =>
        tween.TweenFade(target, 0f, duration);

    #endregion

    #region Color

    public static PropertyTweener TweenModulate(this Tween tween, GodotObject target, Color color, float duration) =>
        tween.TweenProperty(target, PROPERTY_MODULATE, color, duration);

    public static PropertyTweener TweenColor(this Tween tween, ColorRect colorRect, Color color, float duration) =>
        tween.TweenProperty(colorRect, PROPERTY_COLOR, color, duration);

    public static PropertyTweener TweenColor(this Tween tween, CanvasModulate canvasModulate, Color color, float duration) =>
        tween.TweenProperty(canvasModulate, PROPERTY_COLOR, color, duration);

    #endregion

    #region Shake
 
    public static Tween TweenShake(this Tween tween, Node2D target, float duration, float strength = 10f, float speed = 8f)
    {
        var shaker = ShakePositionTweener.Get();
        shaker.Setup(target, strength, speed);
 
        tween.TweenMethod(Callable.From<float>(shaker.Tick), 0f, 1f, duration);
 
        return tween;
    }
 
    public static Tween TweenShake(this Tween tween, Node3D target, float duration, float strength = 10f, float speed = 8f)
    {
        var shaker = ShakePosition3DTweener.Get();
        shaker.Setup(target, strength, speed);
 
        tween.TweenMethod(Callable.From<float>(shaker.Tick), 0f, 1f, duration);
 
        return tween;
    }
 
    #endregion
 
    #region Punch
 
    public static Tween TweenPunch(this Tween tween, Node2D target, float duration, Vector2 position = default, Vector2 scale = default)
    {
        var punch = PunchTweener2D.Get();
        punch.Setup(target, position, scale);
 
        tween.TweenMethod(Callable.From<float>(punch.Tick), 0f, 1f, duration);
 
        return tween;
    }
 
    public static Tween TweenPunch(this Tween tween, Node3D target, float duration, Vector3 position = default, Vector3 scale = default)
    {
        var punch = PunchTweener3D.Get();
        punch.Setup(target, position, scale);
 
        tween.TweenMethod(Callable.From<float>(punch.Tick), 0f, 1f, duration);
 
        return tween;
    }
 
    #endregion

    #region Wiggle 

    public static Tween TweenWiggle(this Tween tween, GodotObject target, float degrees, float duration)
    {
        float stepDuration = duration / 3f;

        tween.TweenProperty(target, PROPERTY_ROTATION_DEGREES,  degrees, stepDuration).Sine().EaseIn();
        tween.TweenProperty(target, PROPERTY_ROTATION_DEGREES, -degrees, stepDuration).Sine().EaseIn();
        tween.TweenProperty(target, PROPERTY_ROTATION_DEGREES,    0f,    stepDuration).Sine().EaseOut();

        return tween;
    }

    #endregion

    #region Blink

    public static Tween TweenBlink(this Tween tween, CanvasItem canvasItem, int blinks, float blinkDuration = 0.1f)
    {
        for (int i = 0; i < blinks; i++)
            tween.TweenProperty(canvasItem, PROPERTY_MODULATE_ALPHA, (float)(i % 2 == 0 ? 0f : 1f), blinkDuration);
        return tween;
    }

    #endregion

    #region Flicker

    public static MethodTweener TweenFlicker(this Tween tween, CanvasItem target, float duration, float minInterval = 0.05f, float maxInterval = 0.2f, float threshold = 0.3f)
    {
        if (threshold <= minInterval || threshold >= maxInterval)
        {
            GD.PushError("[FlickerTween] threshold must be between min and max");
            return null;
        }

        var flicker = FlickerTweener.Get();
        flicker.Setup(target, minInterval, maxInterval, threshold);
        return tween.TweenMethod(Callable.From<float>(flicker.Tick), 0f, 1f, duration);
    }

    #endregion

    #region Orbit

    public static MethodTweener TweenOrbit(this Tween tween, Node2D target, Vector2 center, float radius, float duration, bool clockwise = true)
    {
        var orbitTweener = OrbitTweener.Get();
        orbitTweener.Setup(target, center, radius, clockwise ? 1f : -1f);
        return tween.TweenMethod(Callable.From<float>(orbitTweener.Tick), 0f, 1f, duration);
    }

    #endregion

    #region Typewriter 

    public static PropertyTweener TweenTypewriter(this Tween tween, Label label, float duration) =>
        tween.TweenProperty(label, PROPERTY_VISIBLE_RATIO, 1f, duration).From(0f);

    #endregion

    #region Counter

    public static MethodTweener TweenCounter(this Tween tween, Label label, float from, float to, float duration) =>
        tween.TweenMethod(Callable.From<float>(value => label.Text = Mathf.RoundToInt(value).ToString()), from, to, duration);

    #endregion

    #region Shader

    public static PropertyTweener TweenShader(this Tween tween, ShaderMaterial material, string paramName, Variant value, float duration) =>
        tween.TweenProperty(material, $"shader_parameter/{paramName}", value, duration);

    #endregion

    #region Audio

    public static PropertyTweener TweenVolume(this Tween tween, AudioStreamPlayer player, float db, float duration) =>
        tween.TweenProperty(player, PROPERTY_VOLUME, db, duration);

    #endregion

    #region Camera 3D

    public static PropertyTweener TweenFov(this Tween tween, Camera3D camera, float value, float duration) =>
        tween.TweenProperty(camera, PROPERTY_FOV, value, duration);

    #endregion

    #region Signals & Awaiters

    public static void OnComplete(this Tween tweener, Action action) =>
        tweener.Connect(Tween.SignalName.Finished, Callable.From(action), (uint)GodotObject.ConnectFlags.OneShot);

    public static void OnComplete(this Tweener tweener, Action action) =>
        tweener.Connect(Tweener.SignalName.Finished, Callable.From(action), (uint)GodotObject.ConnectFlags.OneShot);

    public static async Task AwaitAsync(this Tween tween) =>
        await tween.ToSignal(tween, Tween.SignalName.Finished);

    public static async Task AwaitAsync(this Tweener tween) =>
        await tween.ToSignal(tween, Tweener.SignalName.Finished);

    #endregion

    #region Other

    public static void KillIfValid(this Tween tween)
    {   
        if (GodotObject.IsInstanceValid(tween) && tween.IsValid())
            tween.Kill();
    }
    
    #endregion

    #region Transitions & Ease

    public static Tween Linear(this Tween tween)  => tween.SetTrans(Tween.TransitionType.Linear);
    public static Tween Sine(this Tween tween)    => tween.SetTrans(Tween.TransitionType.Sine);
    public static Tween Back(this Tween tween)    => tween.SetTrans(Tween.TransitionType.Back);
    public static Tween Bounce(this Tween tween)  => tween.SetTrans(Tween.TransitionType.Bounce);
    public static Tween Circ(this Tween tween)    => tween.SetTrans(Tween.TransitionType.Circ);
    public static Tween Spring(this Tween tween)  => tween.SetTrans(Tween.TransitionType.Spring);
    public static Tween Quad(this Tween tween)    => tween.SetTrans(Tween.TransitionType.Quad);
    public static Tween Quart(this Tween tween)   => tween.SetTrans(Tween.TransitionType.Quart);
    public static Tween Expo(this Tween tween)    => tween.SetTrans(Tween.TransitionType.Expo);
    public static Tween Quint(this Tween tween)   => tween.SetTrans(Tween.TransitionType.Quint);
    public static Tween Elastic(this Tween tween) => tween.SetTrans(Tween.TransitionType.Elastic);
    public static Tween Cubic(this Tween tween)   => tween.SetTrans(Tween.TransitionType.Cubic);

    public static Tween EaseIn(this Tween tween)    => tween.SetEase(Tween.EaseType.In); 
    public static Tween EaseOut(this Tween tween)   => tween.SetEase(Tween.EaseType.Out); 
    public static Tween EaseOutIn(this Tween tween) => tween.SetEase(Tween.EaseType.OutIn); 
    public static Tween EaseInOut(this Tween tween) => tween.SetEase(Tween.EaseType.InOut); 
    
    public static PropertyTweener Linear(this PropertyTweener propertytweener)  => propertytweener.SetTrans(Tween.TransitionType.Linear);
    public static PropertyTweener Sine(this PropertyTweener propertytweener)    => propertytweener.SetTrans(Tween.TransitionType.Sine);
    public static PropertyTweener Back(this PropertyTweener propertytweener)    => propertytweener.SetTrans(Tween.TransitionType.Back);
    public static PropertyTweener Bounce(this PropertyTweener propertytweener)  => propertytweener.SetTrans(Tween.TransitionType.Bounce);
    public static PropertyTweener Circ(this PropertyTweener propertytweener)    => propertytweener.SetTrans(Tween.TransitionType.Circ);
    public static PropertyTweener Spring(this PropertyTweener propertytweener)  => propertytweener.SetTrans(Tween.TransitionType.Spring);
    public static PropertyTweener Quad(this PropertyTweener propertytweener)    => propertytweener.SetTrans(Tween.TransitionType.Quad);
    public static PropertyTweener Quart(this PropertyTweener propertytweener)   => propertytweener.SetTrans(Tween.TransitionType.Quart);
    public static PropertyTweener Expo(this PropertyTweener propertytweener)    => propertytweener.SetTrans(Tween.TransitionType.Expo);
    public static PropertyTweener Quint(this PropertyTweener propertytweener)   => propertytweener.SetTrans(Tween.TransitionType.Quint);
    public static PropertyTweener Elastic(this PropertyTweener propertytweener) => propertytweener.SetTrans(Tween.TransitionType.Elastic);
    public static PropertyTweener Cubic(this PropertyTweener propertytweener)   => propertytweener.SetTrans(Tween.TransitionType.Cubic);

    public static PropertyTweener EaseIn(this PropertyTweener tween)    => tween.SetEase(Tween.EaseType.In); 
    public static PropertyTweener EaseOut(this PropertyTweener tween)   => tween.SetEase(Tween.EaseType.Out); 
    public static PropertyTweener EaseOutIn(this PropertyTweener tween) => tween.SetEase(Tween.EaseType.OutIn); 
    public static PropertyTweener EaseInOut(this PropertyTweener tween) => tween.SetEase(Tween.EaseType.InOut); 

    #endregion
}

#region Virtual Tween

public static class TweenVirtual
{
    public static Tween Float(float from, float to, float duration, Action<float> action)
    {
        Tween tween = GameUtilities.Instance.CreateTween();
        tween.TweenMethod(Callable.From(action), from, to, duration);
        return tween;
    }

    public static Tween Int(int from, int to, float duration, Action<int> action)
    {
        Tween tween = GameUtilities.Instance.CreateTween();
        tween.TweenMethod(Callable.From(action), from, to, duration);
        return tween;
    }

    public static Tween Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> action)
    {
        Tween tween = GameUtilities.Instance.CreateTween();
        tween.TweenMethod(Callable.From(action), from, to, duration);
        return tween;
    }

    public static Tween Vector3(Vector3 from, Vector3 to, float duration, Action<Vector3> action)
    {
        Tween tween = GameUtilities.Instance.CreateTween();
        tween.TweenMethod(Callable.From(action), from, to, duration);
        return tween;
    }
    
    public static Tween Color(Color from, Color to, float duration, Action<Color> action)
    {
        Tween tween = GameUtilities.Instance.CreateTween();
        tween.TweenMethod(Callable.From(action), from, to, duration);
        return tween;
    }
}

#endregion
