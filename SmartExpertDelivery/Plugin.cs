using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SmartExpertDelivery.Delivery;
using SmartExpertDelivery.Windows;

namespace SmartExpertDelivery;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/sed";

    private readonly WindowSystem windowSystem = new("SmartExpertDelivery");
    private readonly MainWindow mainWindow;
    private readonly DeliveryRunner runner;
    private bool wasLive;
    private bool openedByList;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();
        var config = Configuration.Load(pluginInterface);
        this.runner = new DeliveryRunner(config, new GearCatalog());
        this.mainWindow = new MainWindow(config, this.runner);
        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(new DeliveryOverlay(config, this.runner));

        try
        {
            pluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
            pluginInterface.UiBuilder.OpenMainUi += this.OpenMain;
            pluginInterface.UiBuilder.OpenConfigUi += this.OpenMain;
            Services.Framework.Update += this.OnUpdate;
            Services.CommandManager.AddHandler(CommandName, new CommandInfo((_, _) => this.mainWindow.Toggle())
            {
                HelpMessage = "Open the Smart Expert Delivery settings.",
            });
        }
        catch
        {
            this.Teardown();
            throw;
        }
    }

    public void Dispose() => this.Teardown();

    private void Teardown()
    {
        Services.CommandManager.RemoveHandler(CommandName);
        Services.Framework.Update -= this.OnUpdate;
        Services.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenMain;
        Services.PluginInterface.UiBuilder.OpenMainUi -= this.OpenMain;
        Services.PluginInterface.UiBuilder.Draw -= this.windowSystem.Draw;
        this.windowSystem.RemoveAllWindows();
    }

    private void OpenMain() => this.mainWindow.IsOpen = true;

    private void OnUpdate(IFramework framework)
    {
        this.runner.PreviewInventory = this.mainWindow.IsOpen;
        this.runner.Update();

        // The window comes and goes with the Expert Delivery list, unless the player opened it themselves.
        if (this.runner.Live != this.wasLive)
        {
            if (this.runner.Live && !this.mainWindow.IsOpen)
                this.mainWindow.IsOpen = this.openedByList = true;
            else if (!this.runner.Live && this.openedByList)
                this.mainWindow.IsOpen = this.openedByList = false;
            this.wasLive = this.runner.Live;
        }
    }
}
