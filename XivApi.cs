﻿using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;

namespace JobIcons
{
    internal class XivApi : IDisposable
    {
        public static int ThreadID => System.Threading.Thread.CurrentThread.ManagedThreadId;

        private static JobIconsPlugin _plugin;

        private readonly SetNamePlateDelegate SetNamePlate;
        private readonly Framework_GetUIModuleDelegate GetUIModule;
        private readonly GroupManager_IsObjectIDInPartyDelegate IsObjectIDInParty;
        private readonly GroupManager_IsObjectIDInAllianceDelegate IsObjectIDInAlliance;
        private readonly AtkResNode_SetScaleDelegate SetNodeScale;
        private readonly AtkResNode_SetPositionShortDelegate SetNodePosition;
        private readonly BattleCharaStore_LookupBattleCharaByObjectIDDelegate LookupBattleCharaByObjectID;

        public static void Initialize(JobIconsPlugin plugin,PluginAddressResolver address)
        {
            _plugin ??= plugin;
            Instance ??= new XivApi(plugin.Interface, address);
        }

        private static XivApi Instance;

        private XivApi(DalamudPluginInterface pluginInterface, PluginAddressResolver address)
        {

            SetNamePlate = Marshal.GetDelegateForFunctionPointer<SetNamePlateDelegate>(address.AddonNamePlate_SetNamePlatePtr);
            GetUIModule = Marshal.GetDelegateForFunctionPointer<Framework_GetUIModuleDelegate>(address.Framework_GetUIModulePtr);
            IsObjectIDInParty = Marshal.GetDelegateForFunctionPointer<GroupManager_IsObjectIDInPartyDelegate>(address.GroupManager_IsObjectIDInPartyPtr);
            IsObjectIDInAlliance = Marshal.GetDelegateForFunctionPointer<GroupManager_IsObjectIDInAllianceDelegate>(address.GroupManager_IsObjectIDInAlliancePtr);
            SetNodeScale = Marshal.GetDelegateForFunctionPointer<AtkResNode_SetScaleDelegate>(address.AtkResNode_SetScalePtr);
            SetNodePosition = Marshal.GetDelegateForFunctionPointer<AtkResNode_SetPositionShortDelegate>(address.AtkResNode_SetPositionShortPtr);
            LookupBattleCharaByObjectID = Marshal.GetDelegateForFunctionPointer<BattleCharaStore_LookupBattleCharaByObjectIDDelegate>(address.BattleCharaStore_LookupBattleCharaByObjectIDPtr);

            EmptySeStringPtr = StringToSeStringPtr("");

            _plugin.ClientState.Logout += OnLogout_ResetRaptureAtkModule;



        }

        public static void DisposeInstance() => Instance.Dispose();

        public void Dispose()
        {
            _plugin.ClientState.Logout -= OnLogout_ResetRaptureAtkModule;
            Marshal.FreeHGlobal(EmptySeStringPtr);
        }

        #region RaptureAtkModule

        private static IntPtr _RaptureAtkModulePtr = IntPtr.Zero;

        internal static IntPtr RaptureAtkModulePtr
        {
            get
            {
                if (_RaptureAtkModulePtr == IntPtr.Zero)
                {
                    var frameworkPtr = _plugin.Framework.Address.BaseAddress;
                    var uiModulePtr = Instance.GetUIModule(frameworkPtr);

                    unsafe
                    {
                        var uiModule = *(UIModule*)uiModulePtr;
                        var UIModule_GetRaptureAtkModuleAddress = new IntPtr(uiModule.vfunc[7]);
                        var GetRaptureAtkModule = Marshal.GetDelegateForFunctionPointer<UIModule_GetRaptureAtkModuleDelegate>(UIModule_GetRaptureAtkModuleAddress);
                        _RaptureAtkModulePtr = GetRaptureAtkModule(uiModulePtr);
                    }
                }
                return _RaptureAtkModulePtr;
            }
        }

        private void OnLogout_ResetRaptureAtkModule(object sender, EventArgs evt) => _RaptureAtkModulePtr = IntPtr.Zero;

        #endregion

        #region SeString

        internal static IntPtr EmptySeStringPtr;

        internal static SeString GetSeStringFromPtr(IntPtr seStringPtr)
        {
            byte b;
            var offset = 0;
            unsafe
            {
                while ((b = *(byte*)(seStringPtr + offset)) != 0)
                    offset++;
            }
            var bytes = new byte[offset];
            Marshal.Copy(seStringPtr, bytes, 0, offset);
            return Dalamud.Game.Text.SeStringHandling.SeString.Parse(bytes);
        }

        internal static IntPtr StringToSeStringPtr(string rawText)
        {
            var seString = new SeString(new List<Payload>());
            seString.Payloads.Add(new TextPayload(rawText));
            var bytes = seString.Encode();
            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);
            return pointer;
        }

        #endregion

        internal static SafeAddonNamePlate GetSafeAddonNamePlate() => new SafeAddonNamePlate(_plugin.Interface);

        internal static bool IsLocalPlayer(uint actorID) => _plugin.ClientState.LocalPlayer?.ObjectId == actorID;

        internal static bool IsPartyMember(uint actorID) => Instance.IsObjectIDInParty(_plugin.Address.GroupManagerPtr, actorID) == 1;

        internal static bool IsAllianceMember(uint actorID) => Instance.IsObjectIDInParty(_plugin.Address.GroupManagerPtr, actorID) == 1;

        internal static bool IsPlayerCharacter(uint actorID)
        {
            foreach (var obj in _plugin.ObjectTable)
            {
                if (obj == null) continue;
                if (obj.ObjectId == actorID) return obj.ObjectKind == ObjectKind.Player;
            }

            return false;
        }

        internal static uint GetJobId(uint actorID)
        {
            //var address = Instance.LookupBattleCharaByObjectID(_plugin.Address.BattleCharaStorePtr, actorID);
            //if (address == IntPtr.Zero)
            //    return 0;
            //var actor = Marshal.PtrToStructure<PlayerCharacter>(address);

            //return actor == null ? 0: actor.ClassJob.Id;
            foreach (var obj in _plugin.ObjectTable)
            {
                if (obj == null) continue;
                if (obj.ObjectId == actorID && obj is PlayerCharacter character) {return character.ClassJob.Id;}
            }
            return 0;
        }

        internal class SafeAddonNamePlate
        {
            private readonly DalamudPluginInterface Interface;

            public IntPtr Pointer => _plugin.GameGui.GetAddonByName("NamePlate", 1);

            public SafeAddonNamePlate(DalamudPluginInterface pluginInterface)
            {
                Interface = pluginInterface;
            }

            public unsafe SafeNamePlateObject GetNamePlateObject(int index)
            {
                if (Pointer == IntPtr.Zero)
                {
                    PluginLog.Debug($"[{GetType().Name}] AddonNamePlate was null");
                    return null;
                }

                var npObjectArrayPtrPtr = Pointer + Marshal.OffsetOf(typeof(AddonNamePlate), nameof(AddonNamePlate.NamePlateObjectArray)).ToInt32();
                var npObjectArrayPtr = Marshal.ReadIntPtr(npObjectArrayPtrPtr);
                if (npObjectArrayPtr == IntPtr.Zero)
                {
                    PluginLog.Debug($"[{GetType().Name}] NamePlateObjectArray was null");
                    return null;
                }

                var npObjectPtr = npObjectArrayPtr + Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject)) * index;
                return new SafeNamePlateObject(npObjectPtr, index);
            }
        }

        internal class SafeNamePlateObject
        {
            public readonly IntPtr Pointer;
            public readonly AddonNamePlate.NamePlateObject Data;

            private int _Index;
            private SafeNamePlateInfo _NamePlateInfo;

            public SafeNamePlateObject(IntPtr pointer, int index = -1)
            {
                Pointer = pointer;
                Data = Marshal.PtrToStructure<AddonNamePlate.NamePlateObject>(pointer);
                _Index = index;
            }

            public int Index
            {
                get
                {
                    if (_Index == -1)
                    {
                        var addon = XivApi.GetSafeAddonNamePlate();
                        var npObject0 = addon.GetNamePlateObject(0);
                        if (npObject0 == null)
                        {
                            PluginLog.Debug($"[{GetType().Name}] NamePlateObject0 was null");
                            return -1;
                        }

                        var npObjectBase = npObject0.Pointer;
                        var npObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
                        var index = (Pointer.ToInt64() - npObjectBase.ToInt64()) / npObjectSize;
                        if (index < 0 || index >= 50)
                        {
                            PluginLog.Debug($"[{GetType().Name}] NamePlateObject index was out of bounds");
                            return -1;
                        }

                        _Index = (int)index;
                    }
                    return _Index;
                }
            }

            public SafeNamePlateInfo NamePlateInfo
            {
                get
                {
                    if (_NamePlateInfo == null)
                    {
                        var rapturePtr = XivApi.RaptureAtkModulePtr;
                        if (rapturePtr == IntPtr.Zero)
                        {
                            PluginLog.Debug($"[{GetType().Name}] RaptureAtkModule was null");
                            return null;
                        }

                        var npInfoArrayPtr = XivApi.RaptureAtkModulePtr + Marshal.OffsetOf(typeof(RaptureAtkModule), nameof(RaptureAtkModule.NamePlateInfoArray)).ToInt32();
                        var npInfoPtr = npInfoArrayPtr + Marshal.SizeOf(typeof(RaptureAtkModule.NamePlateInfo)) * Index;
                        _NamePlateInfo = new SafeNamePlateInfo(npInfoPtr);
                    }
                    return _NamePlateInfo;
                }
            }

            #region Getters

            public unsafe IntPtr IconImageNodeAddress => Marshal.ReadIntPtr(Pointer + Marshal.OffsetOf(typeof(AddonNamePlate.NamePlateObject), nameof(AddonNamePlate.NamePlateObject.IconImageNode)).ToInt32());

            public AtkImageNode IconImageNode => Marshal.PtrToStructure<AtkImageNode>(IconImageNodeAddress);

            #endregion

            public unsafe bool IsVisible => Data.IsVisible;

            public unsafe bool IsLocalPlayer => Data.IsLocalPlayer;

            public bool IsPlayer => Data.NameplateKind == 0;

            public void SetIconScale(float scale, bool force = false)
            {
                // Leaving this conditional may help with XIVCombo not flickering
                if (force || IconImageNode.AtkResNode.ScaleX != scale || IconImageNode.AtkResNode.ScaleY != scale)
                    Instance.SetNodeScale(IconImageNodeAddress, scale, scale);

                //var imageNodePtr = IconImageNodeAddress;
                //var resNodePtr = imageNodePtr + Marshal.OffsetOf(typeof(AtkImageNode), nameof(AtkImageNode.AtkResNode)).ToInt32();
                //var scaleXPtr = resNodePtr + Marshal.OffsetOf(typeof(AtkResNode), nameof(AtkResNode.ScaleX)).ToInt32();
                //var scaleYPtr = resNodePtr + Marshal.OffsetOf(typeof(AtkResNode), nameof(AtkResNode.ScaleY)).ToInt32();

                // sizeof(float) == sizeof(int)
                //var scaleBytes = BitConverter.GetBytes(scale);
                //var scaleInt = BitConverter.ToInt32(scaleBytes, 0);
                //Marshal.WriteInt32(scaleXPtr, scaleInt);
                //Marshal.WriteInt32(scaleYPtr, scaleInt);
                //imageNode->AtkResNode.ScaleX = scale;
                //imageNode->AtkResNode.ScaleY = scale;
            }

            public void SetIconPosition(short x, short y)
            {
                // This must always be updated, or icons will jump around
                var iconXAdjustPtr = Pointer + Marshal.OffsetOf(typeof(AddonNamePlate.NamePlateObject), nameof(AddonNamePlate.NamePlateObject.IconXAdjust)).ToInt32();
                var iconYAdjustPtr = Pointer + Marshal.OffsetOf(typeof(AddonNamePlate.NamePlateObject), nameof(AddonNamePlate.NamePlateObject.IconYAdjust)).ToInt32();
                Marshal.WriteInt16(iconXAdjustPtr, x);
                Marshal.WriteInt16(iconYAdjustPtr, y);

                //Instance.SetNodePosition(IconImageNodeAddress, x, y);
                //npObject->ImageNode1->AtkResNode.X = 0;
                //npObject->ImageNode1->AtkResNode.Y = 0;
                //npObject->IconXAdjust = x;
                //npObject->IconYAdjust = y;
            }
        }

        internal class SafeNamePlateInfo
        {
            public readonly IntPtr Pointer;
            public readonly RaptureAtkModule.NamePlateInfo Data;

            public SafeNamePlateInfo(IntPtr pointer)
            {
                Pointer = pointer;//-0x10;
                Data = Marshal.PtrToStructure<RaptureAtkModule.NamePlateInfo>(Pointer);
            }

            #region Getters

            public IntPtr NameAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.Name));

            public string Name => GetString(NameAddress);

            public IntPtr FcNameAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.FcName));

            public string FcName => GetString(FcNameAddress);

            public IntPtr TitleAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.Title));

            public string Title => GetString(TitleAddress);

            public IntPtr DisplayTitleAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.DisplayTitle));

            public string DisplayTitle => GetString(DisplayTitleAddress);

            public IntPtr LevelTextAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.LevelText));

            public string LevelText => GetString(LevelTextAddress);

            #endregion

            public bool IsPlayerCharacter() => XivApi.IsPlayerCharacter(Data.ObjectID.ObjectID);

            public bool IsPartyMember() => XivApi.IsPartyMember(Data.ObjectID.ObjectID);

            public bool IsAllianceMember() => XivApi.IsAllianceMember(Data.ObjectID.ObjectID);

            public uint GetJobID() => GetJobId(Data.ObjectID.ObjectID);

            private unsafe IntPtr GetStringPtr(string name)
            {
                var namePtr = Pointer + Marshal.OffsetOf(typeof(RaptureAtkModule.NamePlateInfo), name).ToInt32();
                var stringPtrPtr = namePtr + Marshal.OffsetOf(typeof(Utf8String), nameof(Utf8String.StringPtr)).ToInt32();
                var stringPtr = Marshal.ReadIntPtr(stringPtrPtr);
                return stringPtr;
            }

            private string GetString(IntPtr stringPtr) => Marshal.PtrToStringUTF8(stringPtr);
        }
    }
}