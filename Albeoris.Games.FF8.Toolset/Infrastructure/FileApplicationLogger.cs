using System.Globalization;
using System.Text;

namespace Albeoris.Games.FF8.Toolset.Infrastructure;

internal sealed class FileApplicationLogger : IApplicationLogger, IDisposable
{
    private readonly Object synchronizationRoot = new();
    private readonly StreamWriter writer;
    private Boolean disposed;

    private FileApplicationLogger(String logPath)
    {
        LogPath = logPath;
        writer = new StreamWriter(logPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true,
        };
    }

    public String LogPath { get; }

    public static FileApplicationLogger Create()
    {
        String logDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Albeoris",
            "FF8Toolset",
            "Logs");
        Directory.CreateDirectory(logDirectoryPath);

        String fileName = $"ff8-toolset-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Environment.ProcessId}.log";
        return new FileApplicationLogger(Path.Combine(logDirectoryPath, fileName));
    }

    public void Information(String message)
    {
        Write("INFO", message);
    }

    public void Warning(String message)
    {
        Write("WARN", message);
    }

    public void Error(String message, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write("ERROR", message);
        Write("ERROR", exception.ToString());
    }

    public void Dispose()
    {
        lock (synchronizationRoot)
        {
            if (disposed)
                return;

            writer.Dispose();
            disposed = true;
        }
    }

    private void Write(String level, String message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        lock (synchronizationRoot)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            using StringReader reader = new(message);
            String? line;
            while ((line = reader.ReadLine()) is not null)
            {
                writer.Write(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                writer.Write(" [");
                writer.Write(level);
                writer.Write("] ");
                writer.WriteLine(line);
            }
        }
    }
}
