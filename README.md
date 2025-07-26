Description:
  - A command-line utility designed to detect and remove overlaps/gaps in Toggl time entries within a specified threshold.

Setup:
  - [Toggl Token](https://support.toggl.com/en/articles/3116844-where-is-my-api-key-located) -> appsettings.json

Parameters:
  - [0] ***processingDate*** (yyMMdd) defaults to **DateTime.Today**
  - [1] ***processingThreshold*** (TimeSpan) defaults to **TimeSpan.FromMinutes(5)**

Options:
  - ***--last-month*** or ***-m*** : Process all time entries for the last month instead of a single day
    - When used with a threshold parameter, provide it as the first non-flag argument
    - Example: `TogglHelper --last-month 00:10:00` (process last month with 10-minute threshold)
