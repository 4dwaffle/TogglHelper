Description:
  - A command-line utility designed to detect and remove overlaps/gaps in Toggl time entries within a specified threshold.

Setup:
  - [Toggl Token](https://support.toggl.com/en/articles/3116844-where-is-my-api-key-located) -> appsettings.json

Parameters:
  - [0] ***processingDate*** (yyMMdd) defaults to **DateTime.Today**
  - [1] ***processingThreshold*** (TimeSpan) defaults to appsettings value
