namespace Zacs
{
    public class ZacsConfig
    {
        public bool IsEnabled { get; set; } = true;

        public int ClickSettingId { get; set; } = 77021;

        public string ClickLabel { get; set; } = "ZACS Interact";

        public string ClickHint { get; set; } = "Press while looking at the monitor.";

        public string SuggestedKey { get; set; } = "E";

        public float MaxDistance { get; set; } = 3.2f;

        public float ReleaseDelay { get; set; } = 4f;

        public float CursorDiameter { get; set; } = 0.018f;

        public float TextCanvasScale { get; set; } = 1f;

        public bool MirrorText { get; set; } = false;

        public int VirtualHeight { get; set; } = 1080;

        public int MaxPlates { get; set; } = 128;

        public int MaxGlyphs { get; set; } = 96;

        public int FieldLimit { get; set; } = 48;
    }
}
