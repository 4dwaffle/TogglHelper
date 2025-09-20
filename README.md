Description:
  - A command-line utility designed to detect and remove overlaps/gaps in Toggl time entries within a specified threshold.
  - Extract images from RVF (Rich Visual Format) files and convert them to JPEG format.

Setup:
  - [Toggl Token](https://support.toggl.com/en/articles/3116844-where-is-my-api-key-located) -> appsettings.json

Parameters:
  - [0] ***processingDate*** (yyMMdd) defaults to **DateTime.Today**
  - [1] ***processingThreshold*** (TimeSpan) defaults to appsettings value

Options:
  - ***--last-month*** or ***-m*** : Process all time entries for the last month instead of a single day
    - When used with a threshold parameter, provide it as the first non-flag argument
    - Example: `TogglHelper --last-month 00:10:00` (process last month with 10-minute threshold)

RVF Image Extraction:
  - ***--rvf*** or ***--extract-images*** : Extract images from RVF files and convert to JPEG
    - Usage: `TogglHelper --rvf <file.rvf> [--output <directory>]`
    - Supports JSON, HTML-like, and RTF-like RVF formats
    - All extracted images are converted to JPEG format with 90% quality
    - Example: `TogglHelper --rvf document.rvf --output ./images/`
