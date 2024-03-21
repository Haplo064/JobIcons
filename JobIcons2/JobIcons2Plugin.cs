using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace JobIcons2;

public abstract class JobIcons2Plugin : IDalamudPlugin
{
    private const string Command1 = "/jicons2";
    private const string Command2 = "/jobicons2";

    internal readonly DalamudPluginInterface Interface;
    internal readonly JobIcons2Configuration Configuration;
    private readonly ICommandManager _commandManager;
    public readonly IClientState ClientState;
    public readonly IObjectTable ObjectTable;
    public static IDataManager DataManager { get; private set; }
    public readonly IGameGui GameGui;
    public static IPluginLog PluginLog;
    internal readonly PluginAddressResolver Address;
    private readonly JobIcons2Gui _pluginGui;

    private readonly Hook<SetNamePlateDelegate> _setNamePlateHook;

    private readonly IntPtr _emptySeStringPtr;

    private readonly OrderedDictionary _lastKnownJobId = new();
    private readonly IntPtr[] _jobStr = new IntPtr[Enum.GetValues(typeof(Job)).Length];

    protected JobIcons2Plugin(DalamudPluginInterface pluginInterface,
        IClientState clientState,
        ICommandManager commands,
        IDataManager data,
        IGameGui gameGui,
        IObjectTable objects,
        ISigScanner sigScanner,
        IGameInteropProvider hookProvider,
        IPluginLog pluginLog
    )
    {
        DataManager = data;
        ObjectTable = objects;
        ClientState = clientState;
        _commandManager = commands;
        Interface = pluginInterface;
        GameGui = gameGui;
        PluginLog = pluginLog;

        Configuration = Interface.GetPluginConfig() as JobIcons2Configuration ?? new JobIcons2Configuration();
            
        Address = new PluginAddressResolver();
        Address.Setup(sigScanner);

        XivApi.Initialize(this,Address);
        IconSet.Initialize(this);

        _setNamePlateHook = hookProvider.HookFromAddress<SetNamePlateDelegate>(Address.AddonNamePlateSetNamePlatePtr, SetNamePlateDetour);
        _setNamePlateHook.Enable();

        _emptySeStringPtr = XivApi.StringToSeStringPtr("");
        InitJobStr();
            
        var commandInfo = new CommandInfo(CommandHandler)
        {
            HelpMessage = "Opens Job Icons config.",
            ShowInHelp = true
        };
        _commandManager.AddHandler(Command1, commandInfo);
        _commandManager.AddHandler(Command2, commandInfo);

        Task.Run(() => FixNamePlates(_fixNonPlayerCharacterNamePlatesTokenSource.Token));

        _pluginGui = new JobIcons2Gui(this);
    }

    private void InitJobStr()
    {
        for (var index = 0; index < Enum.GetValues(typeof(Job)).Length; index++)
        {
            var jobName = ((Job)index).GetName();
            _jobStr[index] = XivApi.StringToSeStringPtr($"[{jobName}]");
        }
    }

    private void DisposeJobStr()
    {
        foreach (var seStrPtr in _jobStr)
        {
            Marshal.FreeHGlobal(seStrPtr);
        }
    }

    internal void SaveConfiguration() => Interface.SavePluginConfig(Configuration);

    public void Dispose()
    {
        _commandManager.RemoveHandler(Command1);
        _commandManager.RemoveHandler(Command2);

        _pluginGui.Dispose();

        _setNamePlateHook.Disable();
        _setNamePlateHook.Dispose();

        XivApi.DisposeInstance();
        _fixNonPlayerCharacterNamePlatesTokenSource.Cancel();
        Marshal.FreeHGlobal(_emptySeStringPtr);
        DisposeJobStr();
        GC.SuppressFinalize(this);
    }

    private void CommandHandler(string command, string arguments) => _pluginGui.ToggleConfigWindow();

    #region fix non-pc nameplates

    private readonly CancellationTokenSource _fixNonPlayerCharacterNamePlatesTokenSource = new();

    private void FixNamePlates(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                FixNamePlates();
                Task.Delay(100, token).Wait(token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Non-PC Updater loop has crashed");
        }
    }

    private void FixNamePlates()
    {
        var addon = XivApi.GetSafeAddonNamePlate();
        for (var i = 0; i < 50; i++)
        {
            var npObject = addon.GetNamePlateObject(i);
            if (npObject is not { IsVisible: true })
                continue;

            var npInfo = npObject.NamePlateInfo;
            if (npInfo == null)
                continue;

            var actorId = npInfo.Data.ObjectID.ObjectID;
            if (actorId == 0xE0000000)
                continue;

            var isPc = npInfo.IsPlayerCharacter();
            var isLocalPlayer = npObject.IsLocalPlayer;
            var isPartyMember = npInfo.IsPartyMember();
            var isAllianceMember = npInfo.IsAllianceMember();

            var updateLocalPlayer = Configuration.SelfIcon && isLocalPlayer;
            var updatePartyMember = Configuration.PartyIcons && isPartyMember;
            var updateAllianceMember = Configuration.AllianceIcons && isAllianceMember;
            var updateEveryoneElse = Configuration.EveryoneElseIcons && !isLocalPlayer && !isPartyMember && !isAllianceMember;

            if (!isPc || !(updateLocalPlayer || updatePartyMember || updateAllianceMember || updateEveryoneElse))
                npObject.SetIconScale(1);
        }
    }

    #endregion

    private IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, IntPtr prefixOrWhatever, int iconId)
    {
        try
        {
            return SetNamePlate(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefixOrWhatever, iconId);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"SetNamePlateDetour encountered a critical error");

            var npObject = new XivApi.SafeNamePlateObject(namePlateObjectPtr);
            npObject.SetIconScale(1f);

            return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefixOrWhatever, iconId);
        }
    }

    private IntPtr SetNamePlate(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, IntPtr prefixOrWhatever, int iconId)
    {
        var npObject = new XivApi.SafeNamePlateObject(namePlateObjectPtr);

        npObject.SetIconScale(1f);

        if (!Configuration.Enabled || ClientState.IsPvP)
            return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefixOrWhatever, iconId);

        var npInfo = npObject.NamePlateInfo;
        if (npInfo == null)
            return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefixOrWhatever, iconId);
            
        var actorId = npInfo.Data.ObjectID.ObjectID;
        if (actorId == 0xE0000000)
            return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefixOrWhatever, iconId);

        if (!npObject.IsPlayer)  // Only PlayerCharacters can have icons
        {
            npObject.SetIconScale(1);
            return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefixOrWhatever, iconId);
        }
        var jobId = npInfo.GetJobId();
        if (jobId < 1 || jobId >= Enum.GetValues(typeof(Job)).Length)
        {
            // This may not necessarily be needed anymore, but better safe than sorry.
            var cache = _lastKnownJobId[actorId];
            if (cache == null)
                return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefixOrWhatever, iconId);
            jobId = (uint)cache;
        }

        // Cache this actor's job
        _lastKnownJobId[actorId] = jobId;

        // Prune the pool a little.
        while (_lastKnownJobId.Count > 500)
            _lastKnownJobId.RemoveAt(0);
            
        var isLocalPlayer = npObject.IsLocalPlayer;
        var isPartyMember = npInfo.IsPartyMember();
        var isAllianceMember = npInfo.IsAllianceMember();

        var updateLocalPlayer = Configuration.SelfIcon && isLocalPlayer;
        var updatePartyMember = Configuration.PartyIcons && isPartyMember;
        var updateAllianceMember = Configuration.AllianceIcons && isAllianceMember;
        var updateEveryoneElse = Configuration.EveryoneElseIcons && !isLocalPlayer && !isPartyMember && !isAllianceMember;

        if (updateLocalPlayer || updatePartyMember || updateAllianceMember || updateEveryoneElse)
        {
            if (Configuration.ShowIcon)
            {
                var iconSet = Configuration.GetIconSet(jobId);
                var scale = Configuration.Scale * iconSet.ScaleMultiplier;
                if (iconId != 061545 && iconId != 061503/* && iconID != 061523*/)
                    iconId = iconSet.GetIconId(jobId);
                npObject.SetIconScale(scale);
            }

            if (!Configuration.ShowName)
                name = _emptySeStringPtr;
            else
            {
                if (Configuration.JobName)
                {
                    name = _jobStr[jobId];
                }
            }

            if (!Configuration.ShowTitle)
                title = _emptySeStringPtr;

            if (!Configuration.ShowFcName)
                fcName = _emptySeStringPtr;

            var result = _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefixOrWhatever, iconId);
            if (Configuration.LocationAdjust)
            {
                npObject.SetIconPosition(Configuration.XAdjust, Configuration.YAdjust);
            }

            return result;
        }

        npObject.SetIconScale(1);
        return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefixOrWhatever, iconId);
    }
}