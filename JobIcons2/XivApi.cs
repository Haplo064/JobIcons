using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace JobIcons2;

internal class XivApi : IDisposable
{
    private static JobIcons2Plugin _plugin;
        
    private readonly GroupManagerIsObjectIdInPartyDelegate _isObjectIdInParty;
    private readonly AtkResNodeSetScaleDelegate _setNodeScale;

    public static void Initialize(JobIcons2Plugin plugin,PluginAddressResolver address)
    {
        _plugin ??= plugin;
        _instance ??= new XivApi(address);
    }

    private static XivApi _instance;

    private XivApi(PluginAddressResolver address)
    {
        _isObjectIdInParty = Marshal.GetDelegateForFunctionPointer<GroupManagerIsObjectIdInPartyDelegate>(address.GroupManagerIsObjectIdInPartyPtr);
        _setNodeScale = Marshal.GetDelegateForFunctionPointer<AtkResNodeSetScaleDelegate>(address.AtkResNodeSetScalePtr);

        _emptySeStringPtr = StringToSeStringPtr("");

        _plugin.ClientState.Logout += OnLogout_ResetRaptureAtkModule;
    }

    public static void DisposeInstance() => _instance.Dispose();

    public void Dispose()
    {
        _plugin.ClientState.Logout -= OnLogout_ResetRaptureAtkModule;
        Marshal.FreeHGlobal(_emptySeStringPtr);
    }

    #region RaptureAtkModule

    private static IntPtr _raptureAtkModulePtr = IntPtr.Zero;

    private static IntPtr RaptureAtkModulePtr
    {
        get
        {
            if (_raptureAtkModulePtr != IntPtr.Zero) return _raptureAtkModulePtr;

            unsafe
            {
                var framework = Framework.Instance();
                var uiModule = framework->GetUiModule();
                _raptureAtkModulePtr = new IntPtr(uiModule->GetRaptureAtkModule());
            }
                
            return _raptureAtkModulePtr;
        }
    }

    private static void OnLogout_ResetRaptureAtkModule() => _raptureAtkModulePtr = IntPtr.Zero;

    #endregion

    #region SeString

    private static IntPtr _emptySeStringPtr;

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

    internal static SafeAddonNamePlate GetSafeAddonNamePlate() => new();

    internal static bool IsLocalPlayer(uint actorId) => _plugin.ClientState.LocalPlayer?.ObjectId == actorId;

    private static bool IsPartyMember(uint actorId) => _instance._isObjectIdInParty(_plugin.Address.GroupManagerPtr, actorId) == 1;

    internal static bool IsAllianceMember(uint actorId) => _instance._isObjectIdInParty(_plugin.Address.GroupManagerPtr, actorId) == 1;

    private static bool IsPlayerCharacter(uint actorId)
    {
        return (from obj in _plugin.ObjectTable where obj.ObjectId == actorId select obj.ObjectKind == ObjectKind.Player).FirstOrDefault();
    }

    private static uint GetJobId(uint actorId)
    {
        //var address = Instance.LookupBattleCharaByObjectID(_plugin.Address.BattleCharaStorePtr, actorID);
        //if (address == IntPtr.Zero)
        //    return 0;
        //var actor = Marshal.PtrToStructure<PlayerCharacter>(address);

        //return actor == null ? 0: actor.ClassJob.Id;
        foreach (var obj in _plugin.ObjectTable)
        {
            if (obj == null) continue;
            if (obj.ObjectId == actorId && obj is PlayerCharacter character) {return character.ClassJob.Id;}
        }
        return 0;
    }

    internal class SafeAddonNamePlate
    {
        private static IntPtr Pointer => _plugin.GameGui.GetAddonByName("NamePlate");

        public SafeNamePlateObject GetNamePlateObject(int index)
        {
            if (!_plugin.ClientState.IsLoggedIn)
            {
                return null;
            }
                
            if (Pointer == IntPtr.Zero)
            {
                JobIcons2Plugin.PluginLog.Debug($"[{GetType().Name}] AddonNamePlate was null");
                return null;
            }

            var npObjectArrayPtrPtr = Pointer + Marshal.OffsetOf(typeof(AddonNamePlate), nameof(AddonNamePlate.NamePlateObjectArray)).ToInt32();
            var npObjectArrayPtr = Marshal.ReadIntPtr(npObjectArrayPtrPtr);
            if (npObjectArrayPtr == IntPtr.Zero)
            {
                JobIcons2Plugin.PluginLog.Debug($"[{GetType().Name}] NamePlateObjectArray was null");
                return null;
            }

            var npObjectPtr = npObjectArrayPtr + Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject)) * index;
            return new SafeNamePlateObject(npObjectPtr, index);
        }
    }

    internal class SafeNamePlateObject(IntPtr pointer, int index = -1)
    {
        private readonly IntPtr _pointer = pointer;
        private readonly AddonNamePlate.NamePlateObject _data = Marshal.PtrToStructure<AddonNamePlate.NamePlateObject>(pointer);

        private SafeNamePlateInfo _namePlateInfo;

        private int Index
        {
            get
            {
                if (index != -1) return index;
                    
                var addon = GetSafeAddonNamePlate();
                var npObject0 = addon.GetNamePlateObject(0);
                if (npObject0 == null)
                {
                    JobIcons2Plugin.PluginLog.Debug($"[{GetType().Name}] NamePlateObject0 was null");
                    return -1;
                }

                var npObjectBase = npObject0._pointer;
                var npObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
                var index1 = (_pointer.ToInt64() - npObjectBase.ToInt64()) / npObjectSize;
                if (index1 is < 0 or >= 50)
                {
                    JobIcons2Plugin.PluginLog.Debug($"[{GetType().Name}] NamePlateObject index was out of bounds");
                    return -1;
                }

                index = (int)index1;
                return index;
            }
        }

        public SafeNamePlateInfo NamePlateInfo
        {
            get
            {
                if (_namePlateInfo != null) return _namePlateInfo;
                    
                var rapturePtr = RaptureAtkModulePtr;
                if (rapturePtr == IntPtr.Zero)
                {
                    JobIcons2Plugin.PluginLog.Debug($"[{GetType().Name}] RaptureAtkModule was null");
                    return null;
                }

                var npInfoArrayPtr = RaptureAtkModulePtr + Marshal.OffsetOf(typeof(RaptureAtkModule), nameof(RaptureAtkModule.NamePlateInfoArray)).ToInt32();
                var npInfoPtr = npInfoArrayPtr + Marshal.SizeOf(typeof(RaptureAtkModule.NamePlateInfo)) * Index;
                _namePlateInfo = new SafeNamePlateInfo(npInfoPtr);
                return _namePlateInfo;
            }
        }

        #region Getters

        private IntPtr IconImageNodeAddress => Marshal.ReadIntPtr(_pointer + Marshal.OffsetOf(typeof(AddonNamePlate.NamePlateObject), nameof(AddonNamePlate.NamePlateObject.IconImageNode)).ToInt32());

        private AtkImageNode IconImageNode => Marshal.PtrToStructure<AtkImageNode>(IconImageNodeAddress);

        #endregion

        public bool IsVisible => _data.IsVisible;

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public bool IsLocalPlayer => _data.IsLocalPlayer;

        public bool IsPlayer => _data.NameplateKind == 0;

        public void SetIconScale(float scale, bool force = false)
        {
            // Leaving this conditional may help with XIVCombo not flickering
            if (force || Math.Abs(IconImageNode.AtkResNode.ScaleX - scale) > 0.01 || Math.Abs(IconImageNode.AtkResNode.ScaleY - scale) > 0.01)
                _instance._setNodeScale(IconImageNodeAddress, scale, scale);

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
            var iconXAdjustPtr = _pointer + Marshal.OffsetOf(typeof(AddonNamePlate.NamePlateObject), nameof(AddonNamePlate.NamePlateObject.IconXAdjust)).ToInt32();
            var iconYAdjustPtr = _pointer + Marshal.OffsetOf(typeof(AddonNamePlate.NamePlateObject), nameof(AddonNamePlate.NamePlateObject.IconYAdjust)).ToInt32();
            Marshal.WriteInt16(iconXAdjustPtr, x);
            Marshal.WriteInt16(iconYAdjustPtr, y);

            //Instance.SetNodePosition(IconImageNodeAddress, x, y);
            //npObject->ImageNode1->AtkResNode.X = 0;
            //npObject->ImageNode1->AtkResNode.Y = 0;
            //npObject->IconXAdjust = x;
            //npObject->IconYAdjust = y;
        }
    }

    internal class SafeNamePlateInfo(IntPtr pointer)
    {
        public readonly RaptureAtkModule.NamePlateInfo Data = Marshal.PtrToStructure<RaptureAtkModule.NamePlateInfo>(pointer);

        public bool IsPlayerCharacter() => XivApi.IsPlayerCharacter(Data.ObjectID.ObjectID);

        public bool IsPartyMember() => XivApi.IsPartyMember(Data.ObjectID.ObjectID);

        public bool IsAllianceMember() => XivApi.IsAllianceMember(Data.ObjectID.ObjectID);

        public uint GetJobId() => XivApi.GetJobId(Data.ObjectID.ObjectID);
    }
}