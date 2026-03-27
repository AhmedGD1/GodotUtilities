using Godot;

namespace Utilities.UIManagement;

public enum TransitionType { None, Fade, ScalePop, SlideRight, SlideLeft, SlideUp, SlideDown, }

public static class UITransition
{
    private const float FADE_DURATION      = 0.15f;
    private const float SLIDE_DURATION     = 0.25f;
    private const float SCALE_POP_DURATION = 0.18f;

    public static Tween Play(Control target, bool isShow, TransitionType type)
    {
        Tween tween = target.CreateTween();

        switch (type)
        {
            case TransitionType.Fade:     OnFade(target, isShow, tween);     break;
            case TransitionType.ScalePop: OnScalePop(target, isShow, tween); break;

            case TransitionType.SlideUp:
            case TransitionType.SlideDown:
            case TransitionType.SlideLeft:
            case TransitionType.SlideRight:
                OnSlide(target, isShow, tween, type); break;
        }   

        return tween;
    }

    private static void OnFade(Control target, bool isShow, Tween tween)
    {
        tween.TweenFade(target, isShow ? 1f : 0f, FADE_DURATION);
    }

    private static void OnSlide(Control target, bool isShow, Tween tween, TransitionType transition)
    {
        Vector2 direction = transition switch
        {
            TransitionType.SlideUp    => Vector2.Up,  
            TransitionType.SlideDown  => Vector2.Down,  
            TransitionType.SlideRight => Vector2.Right,  
            TransitionType.SlideLeft  => Vector2.Left,  

            _                         => throw new System.Exception("Invalid operation")
        };

        float factor = isShow ? 1f : -1f;

        Vector2 a = Vector2.Zero;
        Vector2 b = target.Size * direction * factor;

        tween.TweenPosition(target, isShow ? b : a, SLIDE_DURATION).From(isShow ? a : b);
    }

    private static void OnScalePop(Control target, bool isShow, Tween tween)
    {
        target.PivotOffsetRatio = Vector2.One / 2f;

        Vector2 from     = isShow ? Vector2.Zero : Vector2.One;
        Vector2 finalVal = isShow ? Vector2.One  : Vector2.Zero;

        tween.TweenScale(target, finalVal, SCALE_POP_DURATION).From(from);
    }
}   