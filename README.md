# k4acut

[日本語](./README.jp.md)

A CLI tool to trim Azure Kinect recording files (.mkv) by specifying a time range. It utilizes the `K4AdotNet` library to maintain device configurations and stream data.

## Usage

Execute the tool with the following arguments:

```bash
k4acut <input_path> <output_path> <start_time> <end_time> [--write-speed <multiplier>]
```

- `input_path`: Path to the source .mkv file.
- `output_path`: Path for the trimmed .mkv file.
- `start_time`: Start timestamp (format: `HH:mm:ss`).
- `end_time`: End timestamp (format: `HH:mm:ss`).
- `--write-speed`: (Optional) Maximum write speed as a real-time multiplier (e.g., `x1.0`, `x0.5`). Defaults to `x2.0`.

### Example

To extract a 10-second segment starting from 5 seconds into the recording:

```bash
k4acut input.mkv output.mkv 00:00:05 00:00:15
```

To limit write speed to real-time (e.g., for HDD):

```bash
k4acut input.mkv output.mkv 00:00:05 00:00:15 --write-speed x1.0
```

## Features

- **Configuration Preservation**: Copies color resolution, depth mode, and FPS settings from the source.
- **IMU Support**: Automatically detects and preserves IMU tracks if present.
- **Throttling**: Limits disk write speed to prevent frame drops. Defaults to x2.0 real-time; configurable via `--write-speed`.

## Current Limitations

- **IMU Synchronization**: The current implementation assumes a 1:1 ratio between captures and IMU samples. Since IMU data is typically recorded at a much higher frequency than video frames, some IMU samples will be lost in the current version. To preserve all IMU data, the logic for reading IMU samples needs to be decoupled from the capture loop.
