namespace TogglHelper;

public class AppSettings
{
    public TimeSpan Threshold { get; set; }
    public TogglSettings Toggl { get; set; }
    public class TogglSettings
    {
        public string Token { get; set; }
        public Uri Url { get; set; }
        public int LimitDays { get; set; }
    }
}