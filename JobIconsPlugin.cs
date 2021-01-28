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

        private SetNamePlateDelegate SetNamePlate;
        private Framework_GetUIModuleDelegate GetUIModule;
        private GroupManager_IsObjectIDInPartyDelegate IsObjectIDInParty;
        private AtkResNode_SetScaleDelegate SetNodeScale;
        private AtkResNode_SetPositionShortDelegate SetNodePosition;
        private IntPtr EmptySeStringPtr;

        public unsafe void Initialize(DalamudPluginInterface pluginInterface)
        {
            Interface = pluginInterface;

            Configuration = pluginInterface.GetPluginConfig() as JobIconsConfiguration ?? new JobIconsConfiguration();

            Address = new PluginAddressResolver();
            Address.Setup(pluginInterface.TargetModuleScanner);

            IconSet.Initialize(this);

            SetNamePlate = Marshal.GetDelegateForFunctionPointer<SetNamePlateDelegate>(Address.AddonNamePlate_SetNamePlatePtr);
            GetUIModule = Marshal.GetDelegateForFunctionPointer<Framework_GetUIModuleDelegate>(Address.Framework_GetUIModulePtr);
            IsObjectIDInParty = Marshal.GetDelegateForFunctionPointer<GroupManager_IsObjectIDInPartyDelegate>(Address.GroupManager_IsObjectIDInPartyPtr);
            SetNodeScale = Marshal.GetDelegateForFunctionPointer<AtkResNode_SetScaleDelegate>(Address.AtkResNode_SetScalePtr);
            SetNodePosition = Marshal.GetDelegateForFunctionPointer<AtkResNode_SetPositionShortDelegate>(Address.AtkResNode_SetPositionShortPtr);

            Marshal.GetDelegateForFunctionPointer<SetNamePlateDelegate>(Address.AddonNamePlate_SetNamePlatePtr);
            SetNamePlateHook = new Hook<SetNamePlateDelegate>(Address.AddonNamePlate_SetNamePlatePtr, new SetNamePlateDelegate(SetNamePlateDetour), this);
            SetNamePlateHook.Enable();

            EmptySeStringPtr = StringToSeStringPtr("");

            var commandInfo = new CommandInfo(CommandHandler)
            {
                HelpMessage = "Opens Job Icons config.",
                ShowInHelp = true
            };
            Interface.CommandManager.AddHandler(Command1, commandInfo);
            Interface.CommandManager.AddHandler(Command2, commandInfo);

            Interface.ClientState.OnLogout += OnLogout;

            Task.Run(() => FixNonPlayerCharacterNamePlates(FixNonPlayerCharacterNamePlatesTokenSource.Token));

            PluginGui = new JobIconsGui(this);
        }

        internal void SaveConfiguration() => Interface.SavePluginConfig(Configuration);

        public void Dispose()
        {
            Interface.ClientState.OnLogout -= OnLogout;

            Interface.CommandManager.RemoveHandler(Command1);
            Interface.CommandManager.RemoveHandler(Command2);

            PluginGui.Dispose();

            SetNamePlateHook.Disable();
            SetNamePlateHook.Dispose();

            Marshal.FreeHGlobal(EmptySeStringPtr);
        }

        private void OnLogout(object sender, EventArgs evt)
        {
            _RaptureAtkModulePtr = IntPtr.Zero;
            _AddonNamePlatePtr = IntPtr.Zero;
        }

        private void CommandHandler(string command, string arguments) => PluginGui.ToggleConfigWindow();


        #region fix non-pc nameplates

        private CancellationTokenSource FixNonPlayerCharacterNamePlatesTokenSource = new CancellationTokenSource();

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

        private unsafe void FixNonPlayerCharacterNamePlates()
        {
            var partyMembers = Interface.ClientState.Actors
                .Where(a => IsPartyMember1(a) || IsPartyMember2(a.ActorId))
                .Select(a => a as Dalamud.Game.ClientState.Actors.Types.PlayerCharacter)
                .Where(a => a != null).ToArray();
            var partyMemberIDs = partyMembers.Select(pm => pm.ActorId).ToArray();

            var addonPtr = AddonNamePlatePtr;
            if (addonPtr == IntPtr.Zero)
                return;

            var addon = (AddonNamePlate*)addonPtr;

            var namePlateObjectArray = addon->NamePlateObjectArray;
            if (namePlateObjectArray == null)
                return;

            for (int i = 0; i < 50; i++)
            {
                var namePlateObject = &namePlateObjectArray[i];
                if (namePlateObject->ComponentNode == null || !namePlateObject->ComponentNode->AtkResNode.IsVisible)
                    continue;

                var namePlateInfo = GetNamePlateInfo(i);
                if (namePlateInfo == null)
                    continue;

                var actorID = namePlateInfo->ActorID;
                var isLocalPlayer = IsLocalPlayer(actorID);
                var isPartyMember = partyMemberIDs.Contains(actorID);

                if (!isLocalPlayer && !isPartyMember)
                {
                    AdjustIconScale(namePlateObject, 1.0001f);
                }
            }
        }

        #endregion


        #region internals

        private IntPtr _RaptureAtkModulePtr = IntPtr.Zero;

        internal IntPtr RaptureAtkModulePtr
        {
            get
            {
                if (_RaptureAtkModulePtr == IntPtr.Zero)
                {
                    var frameworkPtr = Interface.Framework.Address.BaseAddress;
                    var uiModulePtr = GetUIModule(frameworkPtr);

                    unsafe
                    {
                        var uiModule = (UIModule*)uiModulePtr;
                        var UIModule_GetRaptureAtkModuleAddress = new IntPtr(uiModule->vfunc[7]);
                        var GetRaptureAtkModule = Marshal.GetDelegateForFunctionPointer<UIModule_GetRaptureAtkModuleDelegate>(UIModule_GetRaptureAtkModuleAddress);
                        _RaptureAtkModulePtr = GetRaptureAtkModule(uiModulePtr);
                    }
                }
                return _RaptureAtkModulePtr;
            }
        }

        private IntPtr _AddonNamePlatePtr;

        internal IntPtr AddonNamePlatePtr
        {
            get
            {
                if (_AddonNamePlatePtr == IntPtr.Zero)
                {
                    _AddonNamePlatePtr = Interface.Framework.Gui.GetUiObjectByName("NamePlate", 1);
                }
                return _AddonNamePlatePtr;
            }
        }

        internal unsafe int GetNamePlateObjectIndex(AddonNamePlate.NamePlateObject* namePlateObject)
        {
            if (namePlateObject == null || AddonNamePlatePtr == null)
                return -1;

            var addon = (AddonNamePlate*)AddonNamePlatePtr;
            var namePlateObjectArray = addon->NamePlateObjectArray;
            if (namePlateObjectArray == null)
            {
                // Try it one more time, this shouldn't be null
                _AddonNamePlatePtr = IntPtr.Zero;
                if (AddonNamePlatePtr == IntPtr.Zero)
                    return -1;

                addon = (AddonNamePlate*)AddonNamePlatePtr;
                namePlateObjectArray = addon->NamePlateObjectArray;
                if (namePlateObjectArray == null)
                    return -1;
            }

            var baseNamePlateObjectAddr = (long)namePlateObjectArray;
            if (baseNamePlateObjectAddr == 0)
                return -1;

            var namePlateObjectAddr = (long)namePlateObject;
            var namePlateObjectSize = Marshal.SizeOf<AddonNamePlate.NamePlateObject>();
            var index = (namePlateObjectAddr - baseNamePlateObjectAddr) / namePlateObjectSize;

            return (int)index;
        }

        internal unsafe RaptureAtkModule.NamePlateInfo* GetNamePlateInfo(int namePlateObjectIndex)
        {
            if (namePlateObjectIndex == -1 || RaptureAtkModulePtr == IntPtr.Zero)
                return null;

            var raptureAtkModule = (RaptureAtkModule*)RaptureAtkModulePtr;
            var namePlateInfo = &(&raptureAtkModule->NamePlateInfoArray)[namePlateObjectIndex];

            return namePlateInfo;
        }

        internal unsafe RaptureAtkModule.NamePlateInfo* GetNamePlateInfo(AddonNamePlate.NamePlateObject* namePlateObject)
        {
            var namePlateObjectIndex = GetNamePlateObjectIndex(namePlateObject);
            if (namePlateObjectIndex == -1)
                return null;

            return GetNamePlateInfo(namePlateObjectIndex);
        }

        #endregion

        #region icons

        internal IconSet GetIconSet(uint jobID)
        {
            var job = (Job)jobID;
            var jobRole = job.GetRole();
            switch (jobRole)
            {
                case JobRole.Tank: return IconSet.Get(Configuration.TankIconSetName);
                case JobRole.Heal: return IconSet.Get(Configuration.HealIconSetName);
                case JobRole.Melee: return IconSet.Get(Configuration.MeleeIconSetName);
                case JobRole.Ranged: return IconSet.Get(Configuration.RangedIconSetName);
                case JobRole.Magical: return IconSet.Get(Configuration.MagicalIconSetName);
                case JobRole.Crafter: return IconSet.Get(Configuration.CraftingIconSetName);
                case JobRole.Gatherer: return IconSet.Get(Configuration.GatheringIconSetName);
                default: throw new ArgumentException($"Unknown jobID {(int)job}");
            }
        }

        internal int GetIconID(uint jobID)
        {
            return GetIconSet(jobID).GetIconID(jobID);
        }

        private unsafe void AdjustIconPos(AddonNamePlate.NamePlateObject* namePlateObject)
        {
            var imageNodePtr = new IntPtr(namePlateObject->ImageNode1);
            //SetNodePosition(imageNodePtr, Configuration.XAdjust, Configuration.YAdjust);
            namePlateObject->IconXAdjust = Configuration.XAdjust;
            namePlateObject->IconYAdjust = Configuration.YAdjust;
        }

        internal unsafe void AdjustIconScale(AddonNamePlate.NamePlateObject* namePlateObject, float scale)
        {
            var imageNodePtr = new IntPtr(namePlateObject->ImageNode1);
            SetNodeScale(imageNodePtr, scale, scale);
            //namePlateObject->ImageNode1->AtkResNode.ScaleX = scale;
            //namePlateObject->ImageNode1->AtkResNode.ScaleY = scale;
        }

        #endregion

        //internal unsafe IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconID)
        internal unsafe IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, string title, string name, string fcName, int iconID)
        {
            if (!Configuration.Enabled)
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);

            var namePlateObject = (AddonNamePlate.NamePlateObject*)namePlateObjectPtr;

            var namePlateInfo = GetNamePlateInfo(namePlateObject);
            if (namePlateInfo == null)
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);

            var actorID = namePlateInfo->ActorID;
            var pc = GetPlayerCharacter(actorID);
            if (pc == null)
            {
                AdjustIconScale(namePlateObject, 1);
                return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
            }

            var isLocalPlayer = IsLocalPlayer(actorID);
            var isPartyMember = IsPartyMember1(pc) || IsPartyMember2(actorID);

            if ((Configuration.SelfIcon && isLocalPlayer) || isPartyMember)
            {
                var iconSet = GetIconSet(pc.ClassJob.Id);
                var scale = Configuration.Scale * iconSet.ScaleMultiplier;

                iconID = iconSet.GetIconID(pc.ClassJob.Id);

                if (!Configuration.ShowName)
                    //name = EmptySeStringPtr;
                    name = "";

                if (!Configuration.ShowTitle)
                    //title = EmptySeStringPtr;
                    title = "";

                if (!Configuration.ShowFcName)
                    //fcName = EmptySeStringPtr;
                    fcName = "";

                var result = SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
                AdjustIconPos(namePlateObject);
                AdjustIconScale(namePlateObject, scale);
                return result;
            }

            AdjustIconScale(namePlateObject, 1);
            return SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
        }

        internal unsafe SeString GetSeStringFromPtr(byte* ptr)
        {
            var offset = 0;
            while (true)
            {
                var b = *(ptr + offset);
                if (b == 0) break;
                offset += 1;
            }

            var bytes = new byte[offset];
            Marshal.Copy(new IntPtr(ptr), bytes, 0, offset);
            return Interface.SeStringManager.Parse(bytes);
        }

        internal unsafe IntPtr StringToSeStringPtr(string rawText)
        {
            var seString = new SeString(new List<Payload>());
            seString.Payloads.Add(new TextPayload(rawText));
            var bytes = seString.Encode();
            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);
            return pointer;
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

        private Dalamud.Game.ClientState.Actors.Types.PlayerCharacter GetPlayerCharacter(int actorID)
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

        internal bool IsLocalPlayer(int actorID)
        {
            return Interface.ClientState.LocalPlayer?.ActorId == actorID;
        }

        internal bool IsPartyMember1(Dalamud.Game.ClientState.Actors.Types.Actor actor)
        {
            var flag = Marshal.ReadByte(actor.Address + 0x1980);
            return (flag & 16) > 0;
        }

        internal bool IsPartyMember2(int actorID)
        {
            return IsObjectIDInParty(Address.GroupManagerPtr, actorID) == 1;
        }
    }
}