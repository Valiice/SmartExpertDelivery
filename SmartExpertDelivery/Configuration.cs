using Dalamud.Configuration;
using Dalamud.Plugin;
using SmartExpertDelivery.Core;

namespace SmartExpertDelivery;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public KeepOptions Keep { get; set; } = new();

    public bool OverlayOnRight { get; set; }

    public static Configuration Load(IDalamudPluginInterface pluginInterface) =>
        pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

    public void Save() => Services.PluginInterface.SavePluginConfig(this);
}
