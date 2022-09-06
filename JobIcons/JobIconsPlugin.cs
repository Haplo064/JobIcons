using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.Logging;

namespace JobIcons
{
    public class JobIconsPlugin : IDalamudPlugin
    {
        public string Name => "JobIcons";

        private const string Command1 = "/jicons";
        private const string Command2 = "/jobicons";

        internal readonly DalamudPluginInterface Interface;
        internal readonly JobIconsConfiguration Configuration;
        private readonly CommandManager _commandManager;
        public readonly ClientState ClientState;
        public readonly ObjectTable ObjectTable;
        public static DataManager DataManager { get; private set; }
        public readonly GameGui GameGui;
        internal readonly PluginAddressResolver Address;
        private readonly JobIconsGui _pluginGui;

        
        private readonly Hook<SetNamePlateDelegate> _setNamePlateHook;

        private readonly IntPtr _emptySeStringPtr;

        private readonly OrderedDictionary _lastKnownJobId = new();
        private readonly IntPtr[] _jobStr = new IntPtr[Enum.GetValues(typeof(Job)).Length];

        public JobIconsPlugin(DalamudPluginInterface pluginInterface,
            ClientState clientState,
            CommandManager commands,
            DataManager data,
            GameGui gameGui,
            ObjectTable objects,
            SigScanner sigScanner
            )
        {
            DataManager = data;
            ObjectTable = objects;
            ClientState = clientState;
            _commandManager = commands;
            Interface = pluginInterface;
            GameGui = gameGui;

            Configuration = Interface.GetPluginConfig() as JobIconsConfiguration ?? new JobIconsConfiguration();
            
            Address = new PluginAddressResolver();
            Address.Setup(sigScanner);

            XivApi.Initialize(this,Address);
            IconSet.Initialize(this);

            _setNamePlateHook = Hook<SetNamePlateDelegate>.FromAddress(Address.AddonNamePlateSetNamePlatePtr, SetNamePlateDetour);
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

            _pluginGui = new JobIconsGui(this);
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

        private IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconId)
        {
            try
            {
                return SetNamePlate(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconId);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"SetNamePlateDetour encountered a critical error");

                var npObject = new XivApi.SafeNamePlateObject(namePlateObjectPtr);
                npObject.SetIconScale(1f);

                return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconId);
            }
        }

        private IntPtr SetNamePlate(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconId)
        {
            var npObject = new XivApi.SafeNamePlateObject(namePlateObjectPtr);

            npObject.SetIconScale(1f);

            if (!Configuration.Enabled || ClientState.IsPvP)
                return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconId);

            var npInfo = npObject.NamePlateInfo;
            if (npInfo == null)
                return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconId);
            
            var actorId = npInfo.Data.ObjectID.ObjectID;
            if (actorId == 0xE0000000)
                return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconId);

            if (!npObject.IsPlayer)  // Only PlayerCharacters can have icons
            {
                npObject.SetIconScale(1);
                return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconId);
            }
            var jobId = npInfo.GetJobId();
            if (jobId < 1 || jobId >= Enum.GetValues(typeof(Job)).Length)
            {
                // This may not necessarily be needed anymore, but better safe than sorry.
                var cache = _lastKnownJobId[actorId];
                if (cache == null)
                    return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconId);
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

                var result = _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconId);
                if (Configuration.LocationAdjust)
                {
                    npObject.SetIconPosition(Configuration.XAdjust, Configuration.YAdjust);
                }

                return result;
            }

            npObject.SetIconScale(1);
            return _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconId);
        }
    }
}