using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace JobIcons
{
    public partial class JobIconsPlugin : IDalamudPlugin
    {
        public string Name => "JobIcons";

        private const string Command1 = "/jicons";
        private const string Command2 = "/jobicons";

        internal DalamudPluginInterface Interface;
        internal JobIconsConfiguration Configuration;
        internal PluginAddressResolver Address;
        internal JobIconsGui PluginGui;

        private Hook<SetNamePlateDelegate> SetNamePlateHook;

        private IntPtr EmptySeStringPtr;

        public unsafe void Initialize(DalamudPluginInterface pluginInterface)
        {
            Interface = pluginInterface;

            Configuration = pluginInterface.GetPluginConfig() as JobIconsConfiguration ?? new JobIconsConfiguration();

            Address = new PluginAddressResolver();
            Address.Setup(pluginInterface.TargetModuleScanner);

            XivApi.Initialize(Interface, Address);
            IconSet.Initialize(this);

            SetNamePlateHook = new Hook<SetNamePlateDelegate>(Address.AddonNamePlate_SetNamePlatePtr, new SetNamePlateDelegate(SetNamePlateDetour), this);
            SetNamePlateHook.Enable();

            EmptySeStringPtr = XivApi.StringToSeStringPtr("");

            var commandInfo = new CommandInfo(CommandHandler)
            {
                HelpMessage = "Opens Job Icons config.",
                ShowInHelp = true
            };
            Interface.CommandManager.AddHandler(Command1, commandInfo);
            Interface.CommandManager.AddHandler(Command2, commandInfo);


            Task.Run(() => FixNonPlayerCharacterNamePlates(FixNonPlayerCharacterNamePlatesTokenSource.Token));

            PluginGui = new JobIconsGui(this);
        }

        internal void SaveConfiguration() => Interface.SavePluginConfig(Configuration);

        public void Dispose()
        {
            Interface.CommandManager.RemoveHandler(Command1);
            Interface.CommandManager.RemoveHandler(Command2);

            PluginGui.Dispose();

            SetNamePlateHook.Disable();
            SetNamePlateHook.Dispose();

            Marshal.FreeHGlobal(EmptySeStringPtr);
        }

        private void CommandHandler(string command, string arguments) => PluginGui.ToggleConfigWindow();


        #region fix non-pc nameplates

        private readonly CancellationTokenSource FixNonPlayerCharacterNamePlatesTokenSource = new CancellationTokenSource();

        private void FixNonPlayerCharacterNamePlates(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    FixNonPlayerCharacterNamePlates();
                    Task.Delay(100, token).Wait(token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Non-PC Updater loop has crashed");
            }
        }

        private void FixNonPlayerCharacterNamePlates()
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

                var actorID = npInfo.ActorID;
                if (actorID == -1)
                    continue;

                var isLocalPlayer = XivApi.IsLocalPlayer(actorID);
                var isPartyMember = XivApi.IsPartyMember(actorID);

                if (!isLocalPlayer && !isPartyMember)
                {
                    //npObject.SetIconScale(1);
                }
            }
        }

        #endregion

        internal unsafe IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconID)
        {
            //if (!Configuration.Enabled)
            return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);

            var npObject = new XivApi.SafeNamePlateObject(namePlateObjectPtr);

            var npInfo = npObject.NamePlateInfo;
            if (npInfo == null)
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);

            var actorID = npInfo.AsUnsafe()->ActorID;
            if (actorID == -1)
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);


            var pc = GetPlayerCharacter(actorID);
            if (pc == null)
            {
                npObject.SetIconScale(1);
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            }
            var isLocalPlayer = XivApi.IsLocalPlayer(actorID);
            var isPartyMember = XivApi.IsPartyMember(actorID);

            if ((Configuration.SelfIcon && isLocalPlayer) || isPartyMember)
            {
                var iconSet = Configuration.GetIconSet(pc.ClassJob.Id);
                var scale = Configuration.Scale * iconSet.ScaleMultiplier;
                iconID = iconSet.GetIconID(pc.ClassJob.Id);

                if (!Configuration.ShowName)
                    name = EmptySeStringPtr;

                if (!Configuration.ShowTitle)
                    title = EmptySeStringPtr;

                if (!Configuration.ShowFcName)
                    fcName = EmptySeStringPtr;

                npObject.SetIconScale(scale);
                var result = SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
                npObject.SetIconScale(scale);

                npObject.SetIconPosition(Configuration.XAdjust, Configuration.YAdjust);

                return result;
            }

            npObject.SetIconScale(1);
            return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        }

        internal string GetActorName(int actorId)
        {
            foreach (var actor in Interface.ClientState.Actors)
            {
                if (actor is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter pc)
                {
                    if (pc.ActorId == actorId)
                        return pc.Name;
                }
            }
            return "";
        }

        internal Dalamud.Game.ClientState.Actors.Types.PlayerCharacter GetPlayerCharacter(int actorID)
        {
            foreach (var actor in Interface.ClientState.Actors)
            {
                if (actor is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter pc)
                {
                    if (actorID == pc.ActorId)
                        return pc;
                }
            }
            return null;
        }

        internal bool IsPartyMember1(Dalamud.Game.ClientState.Actors.Types.Actor actor)
        {
            var flag = Marshal.ReadByte(actor.Address + 0x1980);
            return (flag & 16) > 0;
        }

    }
}