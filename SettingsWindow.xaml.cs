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
            SettingsPanel.Visibility = Visibility.Visible;
            SavedPanel.Visibility = Visibility.Collapsed;
            CommunityPanel.Visibility = Visibility.Collapsed;
            SettingsTabButton.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68));
            SavedTabButton.Background = Brushes.Transparent;
            CommunityTabButton.Background = Brushes.Transparent;
        }

        private void SavedTab_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            SavedPanel.Visibility = Visibility.Visible;
            CommunityPanel.Visibility = Visibility.Collapsed;
            SavedTabButton.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68));
            SettingsTabButton.Background = Brushes.Transparent;
            CommunityTabButton.Background = Brushes.Transparent;
        }

        private async void CommunityTab_Click(object sender, RoutedEventArgs e)
        {
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

            _presets.Add(_overlay.Settings.ToPreset(name));
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
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var nameText = new TextBlock
                {
                    Text = preset.Name,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameText, 0);

                var loadButton = new Button
                {
                    Content = "Load",
                    Margin = new Thickness(4, 0, 4, 0),
                    Padding = new Thickness(8, 2, 8, 2),
                    Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };
                loadButton.Click += (s, e) => { _overlay.Settings.ApplyPreset(preset); SyncControlsToSettings(); };
                Grid.SetColumn(loadButton, 1);

                var deleteButton = new Button
                {
                    Content = "Delete",
                    Padding = new Thickness(8, 2, 8, 2),
                    Background = new SolidColorBrush(Color.FromRgb(0x5A, 0x2A, 0x2A)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0)
                };
                deleteButton.Click += (s, e) =>
                {
                    _presets.Remove(preset);
                    SavePresets();
                    RefreshSavedList();
                };
                Grid.SetColumn(deleteButton, 2);

                row.Children.Add(nameText);
                row.Children.Add(loadButton);
                row.Children.Add(deleteButton);
                SavedListPanel.Children.Add(row);
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

            IEnumerable<CommunityCrosshairPreset> sorted = _communitySort switch
            {
                "Week" => _communityPresets.OrderByDescending(p => p.WeeklyDownloads).ThenByDescending(p => p.TotalDownloads),
                "New" => _communityPresets.OrderByDescending(p => p.PublishedAt),
                _ => _communityPresets.OrderByDescending(p => p.TotalDownloads).ThenByDescending(GetLocalUseCount).ThenByDescending(p => p.PublishedAt)
            };

            foreach (var preset in sorted)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                text.Children.Add(new TextBlock { Text = preset.Name, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
                text.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(preset.Description) ? $"By {preset.Author}" : $"By {preset.Author} · {preset.Description}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xB5, 0xA4, 0xCA)), FontSize = 11, TextWrapping = TextWrapping.Wrap
                });
                text.Children.Add(new TextBlock
                {
                    Text = $"{preset.TotalDownloads:N0} downloads · {preset.WeeklyDownloads:N0} this week · Used {GetLocalUseCount(preset)}× on this PC",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), FontSize = 10, Margin = new Thickness(0, 3, 0, 0)
                });

                var apply = new Button { Content = "Use", Padding = new Thickness(12, 5, 12, 5), Background = new SolidColorBrush(Color.FromRgb(0x4C, 0x3A, 0x68)) };
                apply.Click += (s, e) =>
                {
                    _overlay.Settings.ApplyPreset(preset);
                    _communityUses[GetCommunityKey(preset)] = GetLocalUseCount(preset) + 1;
                    SaveCommunityUses();
                    SyncControlsToSettings();
                    CommunityStatusText.Text = $"Now using {preset.Name}.";
                    RefreshCommunityList();
                };
                Grid.SetColumn(apply, 1);
                row.Children.Add(text);
                row.Children.Add(apply);
                CommunityListPanel.Children.Add(row);
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
