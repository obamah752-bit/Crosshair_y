using System.ComponentModel;
using System.Windows.Media;
using System.Collections.Generic;

namespace crosshair_y
{
    public class CrosshairSettings : INotifyPropertyChanged
    {
        private Color _color = Colors.Lime;
        private double _armLength = 5;
        private double _thickness = 2;
        private double _gap = 4;
        private bool _showDot = false;
        private double _dotSize = 3;
        private double _dotOpacity = 1.0;
        private bool _showCircle = false;
        private double _circleRadius = 15;
        private double _circleOpacity = 1.0;
        private bool _showOutline = true;
        private double _opacity = 1.0;
        private bool _showTop = true;
        private bool _showBottom = true;
        private bool _showLeft = true;
        private bool _showRight = true;

        public Color Color { get => _color; set => SetField(ref _color, value, nameof(Color)); }
        public double ArmLength { get => _armLength; set => SetField(ref _armLength, value, nameof(ArmLength)); }
        public double Thickness { get => _thickness; set => SetField(ref _thickness, value, nameof(Thickness)); }
        public double Gap { get => _gap; set => SetField(ref _gap, value, nameof(Gap)); }
        public bool ShowDot { get => _showDot; set => SetField(ref _showDot, value, nameof(ShowDot)); }
        public double DotSize { get => _dotSize; set => SetField(ref _dotSize, value, nameof(DotSize)); }
        public double DotOpacity { get => _dotOpacity; set => SetField(ref _dotOpacity, value, nameof(DotOpacity)); }
        public bool ShowCircle { get => _showCircle; set => SetField(ref _showCircle, value, nameof(ShowCircle)); }
        public double CircleRadius { get => _circleRadius; set => SetField(ref _circleRadius, value, nameof(CircleRadius)); }
        public double CircleOpacity { get => _circleOpacity; set => SetField(ref _circleOpacity, value, nameof(CircleOpacity)); }
        public bool ShowOutline { get => _showOutline; set => SetField(ref _showOutline, value, nameof(ShowOutline)); }
        public double Opacity { get => _opacity; set => SetField(ref _opacity, value, nameof(Opacity)); }
        public bool ShowTop { get => _showTop; set => SetField(ref _showTop, value, nameof(ShowTop)); }
        public bool ShowBottom { get => _showBottom; set => SetField(ref _showBottom, value, nameof(ShowBottom)); }
        public bool ShowLeft { get => _showLeft; set => SetField(ref _showLeft, value, nameof(ShowLeft)); }
        public bool ShowRight { get => _showRight; set => SetField(ref _showRight, value, nameof(ShowRight)); }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void SetField<T>(ref T field, T value, string name)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnChanged(name);
        }

        public CrosshairPreset ToPreset(string name)
        {
            return new CrosshairPreset
            {
                Name = name,
                Color = Color,
                ArmLength = ArmLength,
                Thickness = Thickness,
                Gap = Gap,
                ShowDot = ShowDot,
                DotSize = DotSize,
                DotOpacity = DotOpacity,
                ShowCircle = ShowCircle,
                CircleRadius = CircleRadius,
                CircleOpacity = CircleOpacity,
                ShowOutline = ShowOutline,
                Opacity = Opacity,
                ShowTop = ShowTop,
                ShowBottom = ShowBottom,
                ShowLeft = ShowLeft,
                ShowRight = ShowRight
            };
        }

        public void ApplyPreset(CrosshairPreset preset)
        {
            _color = preset.Color;
            _armLength = preset.ArmLength;
            _thickness = preset.Thickness;
            _gap = preset.Gap;
            _showDot = preset.ShowDot;
            _dotSize = preset.DotSize;
            _dotOpacity = preset.DotOpacity;
            _showCircle = preset.ShowCircle;
            _circleRadius = preset.CircleRadius;
            _circleOpacity = preset.CircleOpacity;
            _showOutline = preset.ShowOutline;
            _opacity = preset.Opacity;
            _showTop = preset.ShowTop;
            _showBottom = preset.ShowBottom;
            _showLeft = preset.ShowLeft;
            _showRight = preset.ShowRight;

            // A preset should redraw the overlay once, rather than once per property.
            OnChanged(string.Empty);
        }
    }
}
