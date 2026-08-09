using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace crosshair_y
{
    public partial class MainWindow : Window
    {
        public CrosshairSettings Settings { get; } = new CrosshairSettings();

        public MainWindow()
        {
            InitializeComponent();
            Settings.PropertyChanged += (s, e) => DrawCrosshair();
            Loaded += (s, e) =>
            {
                PositionWindowCenterActiveScreen();
                DrawCrosshair();
                SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;
            };
            Closed += (s, e) => SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        }

        private void PositionWindowCenterActiveScreen()
        {
            this.Width = 300;
            this.Height = 300;

            // Put the crosshair on the display the player is currently using.
            var screen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
            var area = screen.Bounds;
            this.Left = area.Left + (area.Width - this.Width) / 2;
            this.Top = area.Top + (area.Height - this.Height) / 2;
        }

        private void DisplaySettingsChanged(object? sender, EventArgs e) =>
            Dispatcher.BeginInvoke(PositionWindowCenterActiveScreen);

        public void DrawCrosshair()
        {
            CrosshairCanvas.Children.Clear();

            double centerX = this.Width / 2;
            double centerY = this.Height / 2;
            var lineBrush = new SolidColorBrush(Settings.Color) { Opacity = Settings.Opacity };
            var lineOutlineBrush = new SolidColorBrush(Colors.Black) { Opacity = Settings.Opacity };
            var circleBrush = new SolidColorBrush(Settings.Color) { Opacity = Settings.Opacity * Settings.CircleOpacity };
            var circleOutlineBrush = new SolidColorBrush(Colors.Black) { Opacity = Settings.Opacity * Settings.CircleOpacity };
            var dotBrush = new SolidColorBrush(Settings.Color) { Opacity = Settings.Opacity * Settings.DotOpacity };
            var dotOutlineBrush = new SolidColorBrush(Colors.Black) { Opacity = Settings.Opacity * Settings.DotOpacity };

            void AddLine(double x1, double y1, double x2, double y2)
            {
                if (Settings.ShowOutline)
                {
                    CrosshairCanvas.Children.Add(new Line
                    {
                        X1 = x1,
                        Y1 = y1,
                        X2 = x2,
                        Y2 = y2,
                        Stroke = lineOutlineBrush,
                        StrokeThickness = Settings.Thickness + 2,
                        StrokeStartLineCap = PenLineCap.Square,
                        StrokeEndLineCap = PenLineCap.Square,
                        SnapsToDevicePixels = true
                    });
                }
                CrosshairCanvas.Children.Add(new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = lineBrush,
                    StrokeThickness = Settings.Thickness,
                    StrokeStartLineCap = PenLineCap.Square,
                    StrokeEndLineCap = PenLineCap.Square,
                    SnapsToDevicePixels = true
                });
            }

            if (Settings.ShowTop) AddLine(centerX, centerY - Settings.Gap - Settings.ArmLength, centerX, centerY - Settings.Gap);
            if (Settings.ShowBottom) AddLine(centerX, centerY + Settings.Gap, centerX, centerY + Settings.Gap + Settings.ArmLength);
            if (Settings.ShowLeft) AddLine(centerX - Settings.Gap - Settings.ArmLength, centerY, centerX - Settings.Gap, centerY);
            if (Settings.ShowRight) AddLine(centerX + Settings.Gap, centerY, centerX + Settings.Gap + Settings.ArmLength, centerY);

            if (Settings.ShowCircle)
            {
                var circle = new Ellipse
                {
                    Width = Settings.CircleRadius * 2,
                    Height = Settings.CircleRadius * 2,
                    Stroke = circleBrush,
                    StrokeThickness = Settings.Thickness
                };
                Canvas.SetLeft(circle, centerX - Settings.CircleRadius);
                Canvas.SetTop(circle, centerY - Settings.CircleRadius);
                if (Settings.ShowOutline)
                {
                    var outline = new Ellipse
                    {
                        // Increase the geometry so the black ring remains visible
                        // on the outside of the coloured circle.
                        Width = circle.Width + 2,
                        Height = circle.Height + 2,
                        Stroke = circleOutlineBrush,
                        StrokeThickness = Settings.Thickness
                    };
                    Canvas.SetLeft(outline, centerX - Settings.CircleRadius - 1);
                    Canvas.SetTop(outline, centerY - Settings.CircleRadius - 1);
                    CrosshairCanvas.Children.Add(outline);
                }
                CrosshairCanvas.Children.Add(circle);
            }

            if (Settings.ShowDot)
            {
                if (Settings.ShowOutline)
                {
                    var outline = new Ellipse
                    {
                        Width = (Settings.DotSize + 1) * 2,
                        Height = (Settings.DotSize + 1) * 2,
                        Fill = dotOutlineBrush
                    };
                    Canvas.SetLeft(outline, centerX - Settings.DotSize - 1);
                    Canvas.SetTop(outline, centerY - Settings.DotSize - 1);
                    CrosshairCanvas.Children.Add(outline);
                }
                var dot = new Ellipse { Width = Settings.DotSize * 2, Height = Settings.DotSize * 2, Fill = dotBrush };
                Canvas.SetLeft(dot, centerX - Settings.DotSize);
                Canvas.SetTop(dot, centerY - Settings.DotSize);
                CrosshairCanvas.Children.Add(dot);
            }
        }

        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_LAYERED = 0x80000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }
    }
}
