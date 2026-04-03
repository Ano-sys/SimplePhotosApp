using System;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Rendering.Composition;

namespace Photos.Animations;

public static class CardHoverEffectBehavior
{
    private static readonly TimeSpan _animationDuration = TimeSpan.FromMilliseconds(150);

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "IsEnabled",
            typeof(CardHoverEffectBehavior),
            false);

    public static readonly AttachedProperty<double> MaxRotationAngleProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "MaxRotationAngle",
            typeof(CardHoverEffectBehavior),
            5.5);

    public static readonly AttachedProperty<double> ScaleFactorProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "ScaleFactor",
            typeof(CardHoverEffectBehavior),
            0.99);

    public static readonly AttachedProperty<bool> SuppressPointerHoverProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "SuppressPointerHover",
            typeof(CardHoverEffectBehavior),
            false);

    static CardHoverEffectBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(AvaloniaObject obj) => obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(AvaloniaObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static double GetMaxRotationAngle(AvaloniaObject obj) => obj.GetValue(MaxRotationAngleProperty);
    public static void SetMaxRotationAngle(AvaloniaObject obj, double value) => obj.SetValue(MaxRotationAngleProperty, value);

    public static double GetScaleFactor(AvaloniaObject obj) => obj.GetValue(ScaleFactorProperty);
    public static void SetScaleFactor(AvaloniaObject obj, double value) => obj.SetValue(ScaleFactorProperty, value);

    public static bool GetSuppressPointerHover(AvaloniaObject obj) => obj.GetValue(SuppressPointerHoverProperty);

    public static void SetSuppressPointerHover(AvaloniaObject obj, bool value) => obj.SetValue(SuppressPointerHoverProperty, value);

    public static void ApplyHover(Control element)
    {
        element.ZIndex = 10;

        var visual = ElementComposition.GetElementVisual(element);
        var compositor = visual?.Compositor;

        if (visual is null || compositor is null) return;

        var scaleFactor = (float)GetScaleFactor(element);

        var width = (float)element.Bounds.Width;
        var height = (float)element.Bounds.Height;

        if (width <= 0 || height <= 0) return;

        visual.CenterPoint = new Vector3(width / 2f, height / 2f, 0f);

        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(scaleFactor, scaleFactor, 1f));
        scaleAnimation.Duration = _animationDuration;

        var orientationAnimation = compositor.CreateQuaternionKeyFrameAnimation();
        orientationAnimation.InsertKeyFrame(1.0f, Quaternion.Identity);
        orientationAnimation.Duration = _animationDuration;

        visual.StartAnimation("Scale", scaleAnimation);
        visual.StartAnimation(nameof(visual.Orientation), orientationAnimation);
    }

    public static void ResetHover(Control element)
    {
        element.ZIndex = 0;

        var visual = ElementComposition.GetElementVisual(element);
        var compositor = visual?.Compositor;

        if (visual is null || compositor is null) return;

        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1f, 1f, 1f));
        scaleAnimation.Duration = _animationDuration;

        var orientationAnimation = compositor.CreateQuaternionKeyFrameAnimation();
        orientationAnimation.InsertKeyFrame(1.0f, Quaternion.Identity);
        orientationAnimation.Duration = _animationDuration;

        visual.StartAnimation("Scale", scaleAnimation);
        visual.StartAnimation(nameof(visual.Orientation), orientationAnimation);
    }

    private static void OnIsEnabledChanged(Control c, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool enabled) return;

        if (enabled)
        {
            c.PointerEntered += Element_PointerEntered;
            c.PointerMoved += Element_PointerMoved;
            c.PointerExited += Element_PointerExited;
        }
        else
        {
            c.PointerEntered -= Element_PointerEntered;
            c.PointerMoved -= Element_PointerMoved;
            c.PointerExited -= Element_PointerExited;
        }
    }

    private static void Element_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Control element || GetSuppressPointerHover(element)) return;
        ApplyHover(element);
    }

    private static void Element_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is not Control element || GetSuppressPointerHover(element)) return;
        ResetHover(element);
    }

    private static void Element_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Control element || GetSuppressPointerHover(element)) return;

        var visual = ElementComposition.GetElementVisual(element);

        if (visual is null) return;

        var maxRotationAngle = (float)GetMaxRotationAngle(element);

        var width = element.Bounds.Width;
        var height = element.Bounds.Height;

        if (width <= 0 || height <= 0) return;

        var position = e.GetPosition(element);
        var centerX = width / 2.0;
        var centerY = height / 2.0;

        var normalizedX = (position.X - centerX) / centerX;
        var normalizedY = (position.Y - centerY) / centerY;

        var rotationY_deg = (float)(normalizedX * maxRotationAngle);
        var rotationX_deg = (float)(-normalizedY * maxRotationAngle);

        var rotationX_rad = (float)(Math.PI / 180.0 * rotationX_deg);
        var rotationY_rad = (float)(Math.PI / 180.0 * rotationY_deg);

        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rotationX_rad);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rotationY_rad);

        visual.Orientation = qy * qx;
    }
}