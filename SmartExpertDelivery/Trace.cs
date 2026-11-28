namespace SmartExpertDelivery;

internal static class Trace
{
    // Own file because dalamud.log stops at its size cap until the next game start.
    // shortcut: always on and never trimmed, a few lines per delivery run; add a setting or rotation if it grows.
    public static readonly string Path = System.IO.Path.Combine(Services.PluginInterface.ConfigDirectory.FullName, "trace.log");

    public static void Event(string what)
    {
        try
        {
            File.AppendAllText(Path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {what}{Environment.NewLine}");
        }
        catch (IOException e)
        {
            Services.Log.Warning(e, "Could not write the Smart Expert Delivery trace");
        }
    }
}
