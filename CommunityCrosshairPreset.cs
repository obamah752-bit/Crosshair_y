namespace crosshair_y
{
    // This is the JSON shape stored in the public GitHub catalog. CrosshairPreset
    // supplies all rendering properties, while these fields are only for display.
    public class CommunityCrosshairPreset : CrosshairPreset
    {
        public string Author { get; set; } = "Community";
        public string Description { get; set; } = "";
        public int TotalDownloads { get; set; }
        public int WeeklyDownloads { get; set; }
        public System.DateTimeOffset PublishedAt { get; set; }
    }

    public class CommunityCatalog
    {
        public System.Collections.Generic.List<CommunityCrosshairPreset> Crosshairs { get; set; } = new();
    }
}
