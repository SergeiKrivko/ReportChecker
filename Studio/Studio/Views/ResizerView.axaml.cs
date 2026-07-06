using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace ReportChecker.Studio.Views;

public partial class ResizerView : UserControl
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ResizerView, Orientation>(nameof(Orientation));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public static readonly StyledProperty<double> CurrentSizeProperty =
        AvaloniaProperty.Register<ResizerView, double>(nameof(CurrentSize), 200);

    public double CurrentSize
    {
        get => GetValue(CurrentSizeProperty);
        set => SetValue(CurrentSizeProperty, value);
    }

    public static readonly StyledProperty<int> BorderWidthProperty =
        AvaloniaProperty.Register<ResizerView, int>(nameof(BorderWidth), 8);

    public int BorderWidth
    {
        get => GetValue(BorderWidthProperty);
        set => SetValue(BorderWidthProperty, value);
    }

    public static readonly StyledProperty<bool> IsVerticalProperty =
        AvaloniaProperty.Register<ResizerView, bool>(nameof(IsVertical));

    public bool IsVertical
    {
        get => GetValue(IsVerticalProperty);
        set => SetValue(IsVerticalProperty, value);
    }

    public static readonly StyledProperty<bool> ReversedProperty =
        AvaloniaProperty.Register<ResizerView, bool>(nameof(Reversed));

    public bool Reversed
    {
        get => GetValue(ReversedProperty);
        set => SetValue(ReversedProperty, value);
    }

    public ResizerView()
    {
        InitializeComponent();
        DataContext = this;
        Panel.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
        PropertyChanged += (_, e) =>
        {
            if (e.Property == OrientationProperty)
            {
                IsVertical = Orientation == Orientation.Vertical;
                Panel.Cursor = new Cursor(IsVertical ? StandardCursorType.SizeWestEast : StandardCursorType.SizeNorthSouth);
            }
        };
    }

    private Point? _lastMousePosition;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _lastMousePosition = e.GetCurrentPoint(this).Position;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _lastMousePosition = null;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_lastMousePosition.HasValue)
        {
            var point = e.GetCurrentPoint(this).Position;
            var vector = _lastMousePosition.Value - point;
            var delta = IsVertical ? vector.X : vector.Y;
            if (Reversed)
                delta = -delta;
            CurrentSize -= delta;
            if (Parent is Control control)
                control.Width = CurrentSize;
        }
    }
}