using ConsoleAppFramework;
using K4AdotNet.Record;
using K4AdotNet.Sensor;
using System.Diagnostics;


ConsoleApp.Run(args, async (
    [Argument] string input,
    [Argument] string output,
    [Argument] TimeSpan start,
    [Argument] TimeSpan end,
    string? writeSpeed = null
) =>
{
    if (!File.Exists(input))
    {
        Console.Error.WriteLine($"Error: Input file '{input}' not found.");
        return;
    }

    // Parse --write-speed option (e.g., "x1.0", "x0.5"). Defaults to x2.0.
    double writeSpeedValue = 2.0;
    if (writeSpeed != null)
    {
        var s = writeSpeed.TrimStart('x', 'X');
        if (!double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) || v <= 0)
        {
            Console.Error.WriteLine($"Error: Invalid --write-speed '{writeSpeed}'. Use format like 'x1.0'.");
            return;
        }
        writeSpeedValue = v;
    }
    Console.WriteLine($"Write speed limit: x{writeSpeedValue:F2} (real-time)");

    Console.WriteLine($"Opening: {input}");
    using var playback = new Playback(input);

    playback.GetRecordConfiguration(out var config);
    var deviceConfig = new DeviceConfiguration()
    {
        ColorFormat = config.ColorFormat,
        ColorResolution = config.ColorResolution,
        DepthMode = config.DepthMode,
        CameraFps = config.CameraFps,
    };
    var isImuEnabled = config.ImuTrackEnabled;

    using var recorder = new Recorder(output, null, deviceConfig);
    var rawCalibration = playback.GetRawCalibration();
    recorder.AddTag("CUSTOM_CALIBRATION_RAW", Convert.ToBase64String(rawCalibration));

    Console.WriteLine($"Cutting: {start} -> {end} ...");

    playback.SeekTimestamp(start, PlaybackSeekOrigin.Begin);
    if (isImuEnabled)
        recorder.AddImuTrack();
    recorder.WriteHeader();

    var count = 0;
    var sw = Stopwatch.StartNew();

    while (playback.TryGetNextCapture(out var capture))
    {
        using (capture)
        {
            var currentPos = capture.DepthImage?.DeviceTimestamp
                            ?? capture.ColorImage?.DeviceTimestamp
                            ?? TimeSpan.Zero;

            if (currentPos > end) break;

            recorder.WriteCapture(capture);

            if (isImuEnabled
                && playback.TryGetNextImuSample(out var imuSample))
            {
                recorder.WriteImuSample(imuSample);
            }

            count++;


            // throttling
            var virtualElapsed = currentPos.ToTimeSpan() - start;
            var targetElapsed = virtualElapsed / writeSpeedValue;
            var delay = targetElapsed - sw.Elapsed;
            if (delay > TimeSpan.FromMilliseconds(1))
                await Task.Delay(delay);

            if (count % 30 == 0) Console.Write("."); // progression indicator
        }
    }

    sw.Stop();
    Console.WriteLine($"\nDone! Saved to: {output}");
    Console.WriteLine($"Processed {count} captures in {sw.Elapsed.TotalSeconds:F1}s");
});
