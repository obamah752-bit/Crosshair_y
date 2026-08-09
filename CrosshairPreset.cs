using System.Windows.Media;

using System.Text.Json.Serialization;

namespace crosshair_y
{
    public class CrosshairPreset
    {
        public string Name { get; set; } = "";
        [JsonIgnore]
        public Color Color { get; set; }

        // Persist WPF colors as ARGB because System.Text.Json cannot recreate
        // the immutable Color struct from its individual components.
        [JsonPropertyName("color")]
        public string ColorHex
        {
            get => $"#{Color.A:X2}{Color.R:X2}{Color.G:X2}{Color.B:X2}";
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Color = (Color)ColorConverter.ConvertFromString(value);
                }
            }
        }
        public double ArmLength { get; set; }
        public double Thickness { get; set; }
        public double Gap { get; set; }
        public bool ShowDot { get; set; }
        public double DotSize { get; set; }
        public double DotOpacity { get; set; } = 1.0;
        public bool ShowCircle { get; set; }
        public double CircleRadius { get; set; }
        public double CircleOpacity { get; set; } = 1.0;
        public bool ShowOutline { get; set; }
        public double Opacity { get; set; }
        public bool ShowTop { get; set; }
        public bool ShowBottom { get; set; }
        public bool ShowLeft { get; set; }
        public bool ShowRight { get; set; }
    }
}
