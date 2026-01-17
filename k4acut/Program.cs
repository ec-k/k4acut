using ConsoleAppFramework;
using K4AdotNet.Record;
using K4AdotNet.Sensor;
using System.Diagnostics;


ConsoleApp.Run(args, async (
    [Argument] string input,
    [Argument] string output,
    [Argument] TimeSpan start,
    [Argument] TimeSpan end,
    double speedLimit = 2.0
) =>
{
    if (!File.Exists(input))
    {
        Console.Error.WriteLine($"Error: Input file '{input}' not found.");
        return;
    }

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
            var realElapsed = sw.Elapsed;
            if (virtualElapsed.TotalSeconds > realElapsed.TotalSeconds * speedLimit)
            {
                await Task.Delay(1);
            }
            else if (count % 10 == 0)
            {
                await Task.Yield();
            }

            if (count % 30 == 0) Console.Write("."); // progression indicator
        }
    }

    sw.Stop();
    Console.WriteLine($"\nDone! Saved to: {output}");
    Console.WriteLine($"Processed {count} captures in {sw.Elapsed.TotalSeconds:F1}s");
});
