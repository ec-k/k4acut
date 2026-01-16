using K4AdotNet.Record;
using ConsoleAppFramework;
using System.Diagnostics;


ConsoleApp.Run(args, (
    [Argument] string input,
    [Argument] string output,
    TimeSpan start,
    TimeSpan end
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
    using var recorder = new Recorder(output, config);

    Console.WriteLine($"Cutting: {start} -> {end} ...");

    playback.SeekTimestamp(start, PlaybackSeekOrigin.Begin);

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

            count++;
            if (count % 30 == 0) Console.Write("."); // 進捗表示
        }
    }

    sw.Stop();
    Console.WriteLine($"\nDone! Saved to: {output}");
    Console.WriteLine($"Processed {count} captures in {sw.Elapsed.TotalSeconds:F1}s");
});
