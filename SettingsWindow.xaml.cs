using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Windows.Media.Animation;
using PreviewEllipse = System.Windows.Shapes.Ellipse;
using PreviewLine = System.Windows.Shapes.Line;

namespace crosshair_y
{
    public partial class SettingsWindow : Window
    {
        private readonly MainWindow _overlay;
        private readonly List<CrosshairPreset> _presets = new();
        private readonly string _presetFilePath;
        private readonly string _communityUsageFilePath;
        private readonly List<CommunityCrosshairPreset> _communityPresets = new();
        private readonly Dictionary<string, int> _communityUses = new();
        private static readonly HttpClient CommunityHttpClient = new();
        private const string CommunityRepository = "obamah752-bit/Crosshair_y";
        private const string CommunityCatalogUrl = "https://raw.githubusercontent.com/obamah752-bit/Crosshair_y/main/catalog.json";
        private string _communitySort = "Top";
        private bool _browseLayoutExpanded;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public SettingsWindow(MainWindow overlay)
        {
            _overlay = overlay;
            _presetFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CrosshairY",
                "presets.json");
            _communityUsageFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CrosshairY",
                "community-usage.json");
            InitializeComponent();
            LoadPresets();
            LoadCommunityUses();
            RefreshSavedList();
        }

        private void DragArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void NumericValueBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var textBox = (TextBox)sender;
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            textBox.SelectAll();
            e.Handled = true;
        }

        private void NumericValueBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
            ((TextBox)sender).SelectAll();

        private void NumericValueBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textBox = (TextBox)sender;
            e.Handled = true;
            textBox.Focus();
            textBox.SelectAll();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Hide();

        private void SettingsTab_Click(object sender, RoutedEventArgs e)
        {
            SetBrowseLayoutExpanded(false);
            SettingsPanel.Visibility = Visibility.Visible;
            SavedPanel.Visibility = Visibility.Collapsed;
            CommunityPanel.Visibility = Visibility.Collapsed;
            SettingsTabButton.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68));
            SavedTabButton.Background = Brushes.Transparent;
            CommunityTabButton.Background = Brushes.Transparent;
        }

        private void SavedTab_Click(object sender, RoutedEventArgs e)
        {
            SetBrowseLayoutExpanded(true);
            SettingsPanel.Visibility = Visibility.Collapsed;
            SavedPanel.Visibility = Visibility.Visible;
            CommunityPanel.Visibility = Visibility.Collapsed;
            SavedTabButton.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68));
            SettingsTabButton.Background = Brushes.Transparent;
            CommunityTabButton.Background = Brushes.Transparent;
        }

        private async void CommunityTab_Click(object sender, RoutedEventArgs e)
        {
            SetBrowseLayoutExpanded(true);
            SettingsPanel.Visibility = Visibility.Collapsed;
            SavedPanel.Visibility = Visibility.Collapsed;
            CommunityPanel.Visibility = Visibility.Visible;
            CommunityTabButton.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68));
            SettingsTabButton.Background = Brushes.Transparent;
            SavedTabButton.Background = Brushes.Transparent;

            // Always check the GitHub catalog when the tab opens so a user sees
            // newly approved community crosshairs without having to press Refresh.
            await RefreshCommunityAsync();
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            var tag = (string)((Button)sender).Tag;
            _overlay.Settings.Color = (Color)ColorConverter.ConvertFromString(tag);
        }

        private void CustomColorButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.ColorDialog
            {
                FullOpen = true,
                Color = System.Drawing.Color.FromArgb(
                    _overlay.Settings.Color.A,
                    _overlay.Settings.Color.R,
                    _overlay.Settings.Color.G,
                    _overlay.Settings.Color.B)
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = dialog.Color;
                _overlay.Settings.Color = Color.FromArgb(c.A, c.R, c.G, c.B);
            }
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _overlay.Settings.ArmLength = e.NewValue;
        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _overlay.Settings.Thickness = e.NewValue;
        private void GapSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _overlay.Settings.Gap = e.NewValue;
        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _overlay.Settings.Opacity = e.NewValue;
        private void DotSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _overlay.Settings.DotSize = e.NewValue;
        private void DotOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _overlay.Settings.DotOpacity = e.NewValue;
        private void CircleRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _overlay.Settings.CircleRadius = e.NewValue;
        private void CircleOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _overlay.Settings.CircleOpacity = e.NewValue;

        private void DotCheck_Click(object sender, RoutedEventArgs e) => _overlay.Settings.ShowDot = DotCheck.IsChecked == true;
        private void CircleCheck_Click(object sender, RoutedEventArgs e) => _overlay.Settings.ShowCircle = CircleCheck.IsChecked == true;
        private void OutlineCheck_Click(object sender, RoutedEventArgs e) => _overlay.Settings.ShowOutline = OutlineCheck.IsChecked == true;

        private void LineCheck_Click(object sender, RoutedEventArgs e)
        {
            _overlay.Settings.ShowTop = TopCheck.IsChecked == true;
            _overlay.Settings.ShowBottom = BottomCheck.IsChecked == true;
            _overlay.Settings.ShowLeft = LeftCheck.IsChecked == true;
            _overlay.Settings.ShowRight = RightCheck.IsChecked == true;
        }

        private void SaveCurrentButton_Click(object sender, RoutedEventArgs e)
        {
            string name = string.IsNullOrWhiteSpace(PresetNameBox.Text)
                ? $"Crosshair {_presets.Count + 1}"
                : PresetNameBox.Text.Trim();

            var savedPreset = _overlay.Settings.ToPreset(name);
            _presets.Add(savedPreset);
            PresetNameBox.Text = "";
            SavePresets();
            RefreshSavedList();
        }

        private void RefreshSavedList()
        {
            SavedListPanel.Children.Clear();

            if (_presets.Count == 0)
            {
                SavedListPanel.Children.Add(new TextBlock
                {
                    Text = "No saved crosshairs yet.",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    FontStyle = FontStyles.Italic
                });
                return;
            }

            foreach (var preset in _presets)
            {
                var actions = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var loadButton = new Button { Content = "Load", Background = new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68)), Margin = new Thickness(0, 0, 0, 7) };
                loadButton.Click += (s, e) => { _overlay.Settings.ApplyPreset(preset); SyncControlsToSettings(); };
                var deleteButton = new Button { Content = "Delete", Background = new SolidColorBrush(Color.FromRgb(0x5A, 0x2A, 0x2A)) };
                deleteButton.Click += (s, e) => { _presets.Remove(preset); SavePresets(); RefreshSavedList(); };
                actions.Children.Add(loadButton);
                actions.Children.Add(deleteButton);

                SavedListPanel.Children.Add(CreatePresetCard(
                    preset,
                    "Saved on this PC",
                    $"{preset.ColorHex} · {preset.ArmLength:0.#} px arms · {preset.Thickness:0.#} px thickness",
                    actions));
            }
        }

        private void SyncControlsToSettings()
        {
            SizeSlider.Value = _overlay.Settings.ArmLength;
            ThicknessSlider.Value = _overlay.Settings.Thickness;
            GapSlider.Value = _overlay.Settings.Gap;
            OpacitySlider.Value = _overlay.Settings.Opacity;
            DotSizeSlider.Value = _overlay.Settings.DotSize;
            DotOpacitySlider.Value = _overlay.Settings.DotOpacity;
            CircleRadiusSlider.Value = _overlay.Settings.CircleRadius;
            CircleOpacitySlider.Value = _overlay.Settings.CircleOpacity;
            DotCheck.IsChecked = _overlay.Settings.ShowDot;
            CircleCheck.IsChecked = _overlay.Settings.ShowCircle;
            OutlineCheck.IsChecked = _overlay.Settings.ShowOutline;
            TopCheck.IsChecked = _overlay.Settings.ShowTop;
            BottomCheck.IsChecked = _overlay.Settings.ShowBottom;
            LeftCheck.IsChecked = _overlay.Settings.ShowLeft;
            RightCheck.IsChecked = _overlay.Settings.ShowRight;
        }

        private void LoadPresets()
        {
            if (!File.Exists(_presetFilePath)) return;

            try
            {
                var loaded = JsonSerializer.Deserialize<List<CrosshairPreset>>(
                    File.ReadAllText(_presetFilePath), JsonOptions);
                if (loaded != null) _presets.AddRange(loaded);
            }
            catch (Exception)
            {
                // Corrupt preset data should not stop the overlay from starting.
            }
        }

        private void SavePresets()
        {
            try
            {
                string? directory = Path.GetDirectoryName(_presetFilePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(_presetFilePath, JsonSerializer.Serialize(_presets, JsonOptions));
            }
            catch (Exception)
            {
                MessageBox.Show("The preset could not be saved.", "Crosshair Y");
            }
        }

        private void LoadCommunityUses()
        {
            try
            {
                if (!File.Exists(_communityUsageFilePath)) return;
                var loaded = JsonSerializer.Deserialize<Dictionary<string, int>>(
                    File.ReadAllText(_communityUsageFilePath), JsonOptions);
                if (loaded == null) return;
                foreach (var item in loaded) _communityUses[item.Key] = item.Value;
            }
            catch { }
        }

        private void SaveCommunityUses()
        {
            try
            {
                string? directory = Path.GetDirectoryName(_communityUsageFilePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(_communityUsageFilePath, JsonSerializer.Serialize(_communityUses, JsonOptions));
            }
            catch { }
        }

        private async void RefreshCommunityButton_Click(object sender, RoutedEventArgs e) => await RefreshCommunityAsync();

        private async System.Threading.Tasks.Task RefreshCommunityAsync()
        {
            CommunityStatusText.Text = "Loading community crosshairs…";
            _communityPresets.Clear();
            RefreshCommunityList();

            try
            {
                // The catalog is a normal static file hosted by GitHub; no backend is involved.
                // Raw GitHub responses can be cached for several minutes. A changing
                // query value ensures Refresh shows a newly published preset at once.
                string catalogUrl = $"{CommunityCatalogUrl}?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                using var response = await CommunityHttpClient.GetAsync(catalogUrl);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                var catalog = JsonSerializer.Deserialize<CommunityCatalog>(json, JsonOptions);
                if (catalog == null) throw new JsonException("The catalog is empty.");

                _communityPresets.AddRange(catalog.Crosshairs);
                CommunityStatusText.Text = _communityPresets.Count == 0
                    ? "No crosshairs have been published yet."
                    : $"Loaded {_communityPresets.Count} community crosshair(s).";
            }
            catch (Exception)
            {
                CommunityStatusText.Text = "Couldn't load catalog.json from the GitHub community repository. Check your internet connection or try again shortly.";
            }

            RefreshCommunityList();
        }

        private void RefreshCommunityList()
        {
            CommunityListPanel.Children.Clear();
            if (_communityPresets.Count == 0)
            {
                CommunityListPanel.Children.Add(new TextBlock
                {
                    Text = "Published crosshairs will appear here.",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    FontStyle = FontStyles.Italic
                });
                return;
            }

            List<CommunityCrosshairPreset> sorted = (_communitySort switch
            {
                "Week" => _communityPresets.OrderByDescending(p => p.WeeklyDownloads).ThenByDescending(p => p.TotalDownloads),
                "New" => _communityPresets.OrderByDescending(p => p.PublishedAt),
                _ => _communityPresets.OrderByDescending(p => p.TotalDownloads).ThenByDescending(GetLocalUseCount).ThenByDescending(p => p.PublishedAt)
            }).ToList();

            foreach (var preset in sorted)
            {
                var actions = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var apply = new Button { Content = "Use", Padding = new Thickness(16, 7, 16, 7), Background = new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68)) };
                apply.Click += (s, e) =>
                {
                    _overlay.Settings.ApplyPreset(preset);
                    _communityUses[GetCommunityKey(preset)] = GetLocalUseCount(preset) + 1;
                    SaveCommunityUses();
                    SyncControlsToSettings();
                    CommunityStatusText.Text = $"Now using {preset.Name}.";
                    RefreshCommunityList();
                };
                actions.Children.Add(apply);
                CommunityListPanel.Children.Add(CreatePresetCard(
                    preset,
                    string.IsNullOrWhiteSpace(preset.Description) ? $"By {preset.Author}" : $"By {preset.Author} · {preset.Description}",
                    $"{preset.TotalDownloads:N0} downloads · {preset.WeeklyDownloads:N0} this week · Used {GetLocalUseCount(preset)}× on this PC",
                    actions));
            }
        }

        private void SetBrowseLayoutExpanded(bool expanded)
        {
            if (_browseLayoutExpanded == expanded) return;

            _browseLayoutExpanded = expanded;
            var duration = new Duration(TimeSpan.FromMilliseconds(180));
            BeginAnimation(WidthProperty, new DoubleAnimation
            {
                To = expanded ? 840 : 540,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

        }

        private Border CreatePresetCard(CrosshairPreset preset, string subtitle, string details, UIElement actions)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x3D)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x5B, 0x4A, 0x70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(138) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var canvas = new Canvas { Width = 126, Height = 126, ClipToBounds = true };
            DrawPresetPreview(canvas, preset);
            var previewFrame = new Border
            {
                Width = 132, Height = 132, Background = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x1A)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x70, 0x5A, 0x88)), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6), Child = canvas, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center
            };

            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 12, 0) };
            text.Children.Add(new TextBlock { Text = preset.Name, Foreground = Brushes.White, FontSize = 16, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            text.Children.Add(new TextBlock { Text = subtitle, Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xA2, 0xFF)), FontSize = 11, Margin = new Thickness(0, 5, 0, 0), TextWrapping = TextWrapping.Wrap });
            text.Children.Add(new TextBlock { Text = details, Foreground = new SolidColorBrush(Color.FromRgb(0xB5, 0xA4, 0xCA)), FontSize = 11, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap });

            Grid.SetColumn(text, 1);
            Grid.SetColumn(actions, 2);
            grid.Children.Add(previewFrame);
            grid.Children.Add(text);
            grid.Children.Add(actions);
            card.Child = grid;
            return card;
        }

        private static void DrawPresetPreview(Canvas previewCanvas, CrosshairPreset preset)
        {
            previewCanvas.Children.Clear();
            double center = previewCanvas.Width / 2;

            double extent = Math.Max(preset.Gap + preset.ArmLength,
                Math.Max(preset.ShowCircle ? preset.CircleRadius + preset.Thickness : 0,
                         preset.ShowDot ? preset.DotSize + 1 : 0));
            double scale = Math.Clamp((previewCanvas.Width * 0.38) / Math.Max(extent, 1), 0.8, 9);
            double lineThickness = Math.Max(1, preset.Thickness * scale);
            var lineBrush = new SolidColorBrush(preset.Color) { Opacity = preset.Opacity };
            var outlineBrush = new SolidColorBrush(Colors.Black) { Opacity = preset.Opacity };

            void AddLine(double x1, double y1, double x2, double y2)
            {
                if (preset.ShowOutline)
                {
                    previewCanvas.Children.Add(new PreviewLine
                    {
                        X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                        Stroke = outlineBrush, StrokeThickness = lineThickness + 3,
                        StrokeStartLineCap = PenLineCap.Square, StrokeEndLineCap = PenLineCap.Square
                    });
                }
                previewCanvas.Children.Add(new PreviewLine
                {
                    X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                    Stroke = lineBrush, StrokeThickness = lineThickness,
                    StrokeStartLineCap = PenLineCap.Square, StrokeEndLineCap = PenLineCap.Square
                });
            }

            double gap = preset.Gap * scale;
            double arm = preset.ArmLength * scale;
            if (preset.ShowTop) AddLine(center, center - gap - arm, center, center - gap);
            if (preset.ShowBottom) AddLine(center, center + gap, center, center + gap + arm);
            if (preset.ShowLeft) AddLine(center - gap - arm, center, center - gap, center);
            if (preset.ShowRight) AddLine(center + gap, center, center + gap + arm, center);

            if (preset.ShowCircle)
            {
                double radius = preset.CircleRadius * scale;
                var circleBrush = new SolidColorBrush(preset.Color) { Opacity = preset.Opacity * preset.CircleOpacity };
                var circleOutlineBrush = new SolidColorBrush(Colors.Black) { Opacity = preset.Opacity * preset.CircleOpacity };
                var circle = new PreviewEllipse
                {
                    Width = radius * 2, Height = radius * 2,
                    Stroke = circleBrush, StrokeThickness = lineThickness * 0.75
                };
                if (preset.ShowOutline)
                {
                    var outline = new PreviewEllipse
                    {
                        Width = circle.Width + 3, Height = circle.Height + 3,
                        Stroke = circleOutlineBrush, StrokeThickness = lineThickness * 0.75 + 3
                    };
                    Canvas.SetLeft(outline, center - outline.Width / 2);
                    Canvas.SetTop(outline, center - outline.Height / 2);
                    previewCanvas.Children.Add(outline);
                }
                Canvas.SetLeft(circle, center - radius);
                Canvas.SetTop(circle, center - radius);
                previewCanvas.Children.Add(circle);
            }

            if (preset.ShowDot)
            {
                double radius = preset.DotSize * scale;
                var dotBrush = new SolidColorBrush(preset.Color) { Opacity = preset.Opacity * preset.DotOpacity };
                if (preset.ShowOutline)
                {
                    var dotOutlineBrush = new SolidColorBrush(Colors.Black) { Opacity = preset.Opacity * preset.DotOpacity };
                    var outline = new PreviewEllipse { Width = radius * 2 + 4, Height = radius * 2 + 4, Fill = dotOutlineBrush };
                    Canvas.SetLeft(outline, center - outline.Width / 2);
                    Canvas.SetTop(outline, center - outline.Height / 2);
                    previewCanvas.Children.Add(outline);
                }
                var dot = new PreviewEllipse { Width = radius * 2, Height = radius * 2, Fill = dotBrush };
                Canvas.SetLeft(dot, center - radius);
                Canvas.SetTop(dot, center - radius);
                previewCanvas.Children.Add(dot);
            }
        }

        private void CommunitySortButton_Click(object sender, RoutedEventArgs e)
        {
            _communitySort = (string)((Button)sender).Tag;
            CommunityTopButton.Background = _communitySort == "Top" ? new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68)) : new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x42));
            CommunityWeekButton.Background = _communitySort == "Week" ? new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68)) : new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x42));
            CommunityNewButton.Background = _communitySort == "New" ? new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68)) : new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x42));
            RefreshCommunityList();
        }

        private string GetCommunityKey(CommunityCrosshairPreset preset) => $"{preset.Author}|{preset.Name}|{preset.PublishedAt:O}";

        private int GetLocalUseCount(CommunityCrosshairPreset preset) =>
            _communityUses.TryGetValue(GetCommunityKey(preset), out int uses) ? uses : 0;

        private void PublishCommunityButton_Click(object sender, RoutedEventArgs e)
        {
            var submission = new CommunityCrosshairPreset
            {
                Name = string.IsNullOrWhiteSpace(CommunityPresetNameBox.Text) ? "My Crosshair" : CommunityPresetNameBox.Text.Trim(),
                Author = "GitHub username",
                Description = "Optional description",
                PublishedAt = DateTimeOffset.UtcNow
            };
            var settings = _overlay.Settings.ToPreset(submission.Name);
            submission.Color = settings.Color; submission.ArmLength = settings.ArmLength; submission.Thickness = settings.Thickness;
            submission.Gap = settings.Gap; submission.ShowDot = settings.ShowDot; submission.DotSize = settings.DotSize; submission.DotOpacity = settings.DotOpacity;
            submission.ShowCircle = settings.ShowCircle; submission.CircleRadius = settings.CircleRadius; submission.CircleOpacity = settings.CircleOpacity;
            submission.ShowOutline = settings.ShowOutline; submission.Opacity = settings.Opacity; submission.ShowTop = settings.ShowTop;
            submission.ShowBottom = settings.ShowBottom; submission.ShowLeft = settings.ShowLeft; submission.ShowRight = settings.ShowRight;

            string json = JsonSerializer.Serialize(submission, JsonOptions);
            string body = "Please add this crosshair to catalog.json. Replace the author placeholder with your GitHub username.\n\n```json\n" + json + "\n```";
            string url = $"https://github.com/{CommunityRepository}/issues/new?title={Uri.EscapeDataString("Community crosshair: " + submission.Name)}&body={Uri.EscapeDataString(body)}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            CommunityStatusText.Text = "GitHub opened with your preset ready to submit.";
        }
    }
}
