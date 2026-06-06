using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using RaffleManager.Services;
using RaffleManager.UI;

namespace RaffleManager;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/rafflemanager";
    private const string SettingsCommandName = "/rafflemanagersettings";

    private readonly WindowSystem windowSystem = new("RaffleManager");
    private readonly Configuration config;
    private readonly PersistenceService persistence;
    private readonly RaffleService raffle;
    private readonly SoundService sound;
    private readonly LogoService logo;
    private readonly ChatCommandService chatCommands;
    private readonly AnnouncementService announcements;
    private readonly MainWindow mainWindow;
    private readonly SettingsWindow settingsWindow;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        DalamudServices.Initialize(pluginInterface);
        config = DalamudServices.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        persistence = new PersistenceService(config);
        raffle = new RaffleService(config, persistence);
        sound = new SoundService(config);
        logo = new LogoService(config);
        chatCommands = new ChatCommandService();
        announcements = new AnnouncementService(config, chatCommands);
        mainWindow = new MainWindow(config, persistence, raffle, sound, logo, announcements, OpenSettingsWindow) { IsOpen = config.WindowVisible };
        settingsWindow = new SettingsWindow(config, persistence, logo, announcements) { IsOpen = config.SettingsWindowVisible };

        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(settingsWindow);

        DalamudServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = "Toggle RaffleManager main window." });
        DalamudServices.CommandManager.AddHandler(SettingsCommandName, new CommandInfo(OnSettingsCommand) { HelpMessage = "Toggle RaffleManager settings window." });
        DalamudServices.PluginInterface.UiBuilder.Draw += DrawUi;
        DalamudServices.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        DalamudServices.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        persistence.SaveNow();
    }

    private void OnCommand(string command, string arguments) => ToggleMainUi();
    private void OnSettingsCommand(string command, string arguments) => ToggleConfigUi();

    private void OpenSettingsWindow()
    {
        config.SettingsWindowVisible = true;
        settingsWindow.IsOpen = true;
        persistence.SaveNow();
    }

    private void ToggleMainUi()
    {
        config.WindowVisible = !config.WindowVisible;
        mainWindow.IsOpen = config.WindowVisible;
        persistence.SaveNow();
    }

    private void ToggleConfigUi()
    {
        config.SettingsWindowVisible = !config.SettingsWindowVisible;
        settingsWindow.IsOpen = config.SettingsWindowVisible;
        persistence.SaveNow();
    }

    private void DrawUi()
    {
        windowSystem.Draw();
        if (config.WindowVisible != mainWindow.IsOpen || config.SettingsWindowVisible != settingsWindow.IsOpen)
        {
            config.WindowVisible = mainWindow.IsOpen;
            config.SettingsWindowVisible = settingsWindow.IsOpen;
            persistence.SaveNow();
        }
    }

    public void Dispose()
    {
        persistence.SaveNow();
        mainWindow.Dispose();
        sound.Dispose();
        logo.Dispose();
        DalamudServices.PluginInterface.UiBuilder.Draw -= DrawUi;
        DalamudServices.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        DalamudServices.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        DalamudServices.CommandManager.RemoveHandler(CommandName);
        DalamudServices.CommandManager.RemoveHandler(SettingsCommandName);
        windowSystem.RemoveAllWindows();
    }
}
