namespace Albeoris.Games.SteamLibrary.Tests.TestInfrastructure;

internal sealed class TemporarySteamInstallation : IDisposable
{
    public TemporarySteamInstallation()
    {
        RootPath = Path.Combine(Path.GetTempPath(), $"Albeoris.SteamLibrary.Tests.{Guid.NewGuid():N}");
        SecondLibraryPath = Path.Combine(RootPath, "SecondLibrary");

        Directory.CreateDirectory(Path.Combine(RootPath, "steamapps"));
        Directory.CreateDirectory(Path.Combine(SecondLibraryPath, "steamapps"));

        WriteDescriptor();
        WriteManifest(Path.Combine(RootPath, "steamapps"), 42, "Root Game", "RootGame");
        WriteManifest(Path.Combine(SecondLibraryPath, "steamapps"), 7, "Second Game", "SecondGame");
    }

    public String RootPath { get; }

    public String SecondLibraryPath { get; }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, recursive: true);
    }

    private void WriteDescriptor()
    {
        String root = Escape(RootPath);
        String second = Escape(SecondLibraryPath);
        String content = $$"""
            "libraryfolders"
            {
                "0"
                {
                    "path" "{{root}}"
                    "apps"
                    {
                        "7" "1"
                        "42" "2"
                        "999" "3"
                    }
                }
                "1"
                {
                    "path" "{{second}}"
                    "apps"
                    {
                        "7" "1"
                        "42" "2"
                    }
                }
            }
            """;
        File.WriteAllText(Path.Combine(RootPath, "steamapps", "libraryfolders.vdf"), content);
    }

    private static void WriteManifest(String steamAppsPath, UInt32 appId, String name, String directoryName)
    {
        String content = $$"""
            "AppState"
            {
                "appid" "{{appId}}"
                "universe" "1"
                "name" "{{name}}"
                "StateFlags" "4"
                "installdir" "{{directoryName}}"
            }
            """;
        File.WriteAllText(Path.Combine(steamAppsPath, $"appmanifest_{appId}.acf"), content);
    }

    private static String Escape(String path) => path.Replace(@"\", @"\\");
}
