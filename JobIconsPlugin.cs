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
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling;

namespace JobIcons
{
    public class JobIconsPlugin : IDalamudPlugin
    {
        public string Name => "JobIcons";

        private const string Command1 = "/jicons";
        private const string Command2 = "/jobicons";

        internal DalamudPluginInterface Interface;
        internal JobIconsConfiguration Configuration;
        public CommandManager CommandManager;
        public Framework Framework;
        public ChatGui ChatGui;
        public SigScanner SigScanner;
        public ClientState ClientState;
        public ObjectTable ObjectTable;
        public static DataManager DataManager;
        public GameGui GameGui;
        public Dalamud.Game.ClientState.Conditions.Condition Condition;
        internal PluginAddressResolver Address;
        internal JobIconsGui PluginGui;

        
        private Hook<SetNamePlateDelegate> SetNamePlateHook;

        private IntPtr EmptySeStringPtr;

        private readonly OrderedDictionary LastKnownJobID = new OrderedDictionary();
        private readonly IntPtr[] JobStr = new IntPtr[Enum.GetValues(typeof(Job)).Length];
        //private readonly OrderedDictionary LastKnownJobID = new OrderedDictionary();

        public JobIconsPlugin(DalamudPluginInterface pluginInterface,
            //BuddyList buddies,
            ChatGui chat,
            //ChatHandlers chatHandlers,
            ClientState clientState,
            CommandManager commands,
            Condition condition,
            DataManager data,
            //FateTable fates,
            //FlyTextGui flyText,
            Framework framework,
            GameGui gameGui,
            //GameNetwork gameNetwork,
            //JobGauges gauges,
            //KeyState keyState,
            //LibcFunction libcFunction,
            ObjectTable objects,
            //PartyFinderGui pfGui,
            //PartyList party,
            //SeStringManager seStringManager,
            SigScanner sigScanner,
            TargetManager targets
            //ToastGui toasts
            )
        {
            Condition = condition;
            DataManager = data;
            ObjectTable = objects;
            ClientState = clientState;
            SigScanner = sigScanner;
            ChatGui = chat;
            Framework = framework;
            CommandManager = commands;
            Interface = pluginInterface;
            GameGui = gameGui;

            //Configuration.Init(this);

            Configuration = Interface.GetPluginConfig() as JobIconsConfiguration ?? new JobIconsConfiguration();



            //var testStr = XivApi.StringToSeStringPtr("TEst");
            //PluginLog.LogInformation(XivApi.GetSeStringFromPtr(testStr).ToString());
            //InitJobStr();
            Address = new PluginAddressResolver();
            Address.Setup(SigScanner);

            XivApi.Initialize(this,Address);
            IconSet.Initialize(this);

            SetNamePlateHook = new Hook<SetNamePlateDelegate>(Address.AddonNamePlate_SetNamePlatePtr, SetNamePlateDetour);
            SetNamePlateHook.Enable();

            EmptySeStringPtr = XivApi.StringToSeStringPtr("");
            InitJobStr();
            
            var commandInfo = new CommandInfo(CommandHandler)
            {
                HelpMessage = "Opens Job Icons config.",
                ShowInHelp = true
            };
            CommandManager.AddHandler(Command1, commandInfo);
            CommandManager.AddHandler(Command2, commandInfo);

            Task.Run(() => FixNamePlates(FixNonPlayerCharacterNamePlatesTokenSource.Token));

            PluginGui = new JobIconsGui(this);
        }

        private void InitJobStr()
        {
            for (var index = 0; index < Enum.GetValues(typeof(Job)).Length; index++)
            {
                var jobName = ((Job)index).GetName();
                JobStr[index] = XivApi.StringToSeStringPtr($"[{jobName}]");
                //PluginLog.LogInformation(JobStr[index].ToString("X"));
                //PluginLog.LogInformation(XivApi.GetSeStringFromPtr(JobStr[index]).ToString());
            }
        }

        private void DisposeJobStr()
        {
            foreach (var seStrPtr in JobStr)
            {
                Marshal.FreeHGlobal(seStrPtr);
            }
        }

        internal void SaveConfiguration() => Interface.SavePluginConfig(Configuration);

        public void Dispose()
        {
            CommandManager.RemoveHandler(Command1);
            CommandManager.RemoveHandler(Command2);

            PluginGui.Dispose();

            SetNamePlateHook.Disable();
            SetNamePlateHook.Dispose();

            XivApi.DisposeInstance();
            FixNonPlayerCharacterNamePlatesTokenSource.Cancel();
            Marshal.FreeHGlobal(EmptySeStringPtr);
            DisposeJobStr();
        }

        private void CommandHandler(string command, string arguments) => PluginGui.ToggleConfigWindow();

        #region fix non-pc nameplates

        private readonly CancellationTokenSource FixNonPlayerCharacterNamePlatesTokenSource = new CancellationTokenSource();

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
            for (int i = 0; i < 50; i++)
            {
                var npObject = addon.GetNamePlateObject(i);
                if (npObject == null || !npObject.IsVisible)
                    continue;

                var npInfo = npObject.NamePlateInfo;
                if (npInfo == null)
                    continue;

                var actorID = npInfo.Data.ObjectID.ObjectID;
                if (actorID == 0xE0000000)
                    continue;

                var isPC = npInfo.IsPlayerCharacter();
                var isLocalPlayer = npObject.IsLocalPlayer;
                var isPartyMember = npInfo.IsPartyMember();
                var isAllianceMember = npInfo.IsAllianceMember();

                var updateLocalPlayer = Configuration.SelfIcon && isLocalPlayer;
                var updatePartyMember = Configuration.PartyIcons && isPartyMember;
                var updateAllianceMember = Configuration.AllianceIcons && isAllianceMember;
                var updateEveryoneElse = Configuration.EveryoneElseIcons && !isLocalPlayer && !isPartyMember && !isAllianceMember;

                if (!isPC || !(updateLocalPlayer || updatePartyMember || updateAllianceMember || updateEveryoneElse))
                    npObject.SetIconScale(1);
            }
        }

        #endregion

        internal IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconID)
        {
            try
            {
                return SetNamePlate(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"SetNamePlateDetour encountered a critical error");

                var npObject = new XivApi.SafeNamePlateObject(namePlateObjectPtr);
                if (npObject != null)
                    npObject.SetIconScale(1f);

                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            }
        }

        internal IntPtr SetNamePlate(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconID)
        {
            var npObject = new XivApi.SafeNamePlateObject(namePlateObjectPtr);

            if (npObject == null)
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            else
                npObject.SetIconScale(1f);

            if (!Configuration.Enabled || ClientState.IsPvP)
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);

            var npInfo = npObject.NamePlateInfo;
            if (npInfo == null)
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            
            var actorID = npInfo.Data.ObjectID.ObjectID;
            if (actorID == 0xE0000000)
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);

            if (!npObject.IsPlayer)  // Only PlayerCharacters can have icons
            {
                npObject.SetIconScale(1);
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            }
            var jobID = npInfo.GetJobID();
            if (jobID < 1 || jobID >= Enum.GetValues(typeof(Job)).Length)
            {
                // This may not necessarily be needed anymore, but better safe than sorry.
                var cache = LastKnownJobID[(object)actorID];
                if (cache == null)
                    return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
                else
                    jobID = (uint)cache;
            }

            // Cache this actor's job
            LastKnownJobID[(object)actorID] = jobID;

            // Prune the pool a little.
            while (LastKnownJobID.Count > 500)
                LastKnownJobID.RemoveAt(0);
            
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
                    var iconSet = Configuration.GetIconSet(jobID);
                    var scale = Configuration.Scale * iconSet.ScaleMultiplier;
                    if (iconID != 061545 && iconID != 061503/* && iconID != 061523*/)
                        iconID = iconSet.GetIconID(jobID);
                    npObject.SetIconScale(scale);
                }


                if (!Configuration.ShowName)
                    name = EmptySeStringPtr;
                else
                {
                    if (Configuration.JobName)
                    {
                        name = JobStr[jobID];
                    }
                }

                if (!Configuration.ShowTitle)
                    title = EmptySeStringPtr;

                if (!Configuration.ShowFcName)
                    fcName = EmptySeStringPtr;

                var result = SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
                if (Configuration.LocationAdjust)
                {
                    npObject.SetIconPosition(Configuration.XAdjust, Configuration.YAdjust);
                }

                return result;
            }
            else
            {
                npObject.SetIconScale(1);
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            }
        }
    }
}