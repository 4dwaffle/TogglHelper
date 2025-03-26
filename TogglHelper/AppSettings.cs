namespace TogglHelper;

public class AppSettings
{
    public TogglSettings Toggl { get; set; }
    public class TogglSettings
    {
        public string Token { get; set; }

        public Uri Url { get; set; }
    }
}