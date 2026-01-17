using K4AdotNet.Record;
using ConsoleAppFramework;
using System.Diagnostics;
using K4AdotNet.Sensor;


ConsoleApp.Run(args, async (
    [Argument] string input,
    [Argument] string output,
    [Argument] TimeSpan start,
    [Argument] TimeSpan end
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
            if (count % 30 == 0) Console.Write("."); // progression indicator


            await Task.Delay(TimeSpan.FromMilliseconds(1)); // throttling
        }
    }

    sw.Stop();
    Console.WriteLine($"\nDone! Saved to: {output}");
    Console.WriteLine($"Processed {count} captures in {sw.Elapsed.TotalSeconds:F1}s");
});
