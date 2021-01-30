using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JobIcons
{
    internal class XivApi : IDisposable
    {
        public static int ThreadID => System.Threading.Thread.CurrentThread.ManagedThreadId;

        private readonly DalamudPluginInterface Interface;
        private readonly PluginAddressResolver Address;

        private readonly SetNamePlateDelegate SetNamePlate;
        private readonly Framework_GetUIModuleDelegate GetUIModule;
        private readonly GroupManager_IsObjectIDInPartyDelegate IsObjectIDInParty;
        private readonly AtkResNode_SetScaleDelegate SetNodeScale;
        private readonly AtkResNode_SetPositionShortDelegate SetNodePosition;

        public static void Initialize(DalamudPluginInterface pluginInterface, PluginAddressResolver address)
        {
            Instance ??= new XivApi(pluginInterface, address);
        }

        private static XivApi Instance;

        private XivApi(DalamudPluginInterface pluginInterface, PluginAddressResolver address)
        {
            Interface = pluginInterface;
            Address = address;

            SetNamePlate = Marshal.GetDelegateForFunctionPointer<SetNamePlateDelegate>(address.AddonNamePlate_SetNamePlatePtr);
            GetUIModule = Marshal.GetDelegateForFunctionPointer<Framework_GetUIModuleDelegate>(address.Framework_GetUIModulePtr);
            IsObjectIDInParty = Marshal.GetDelegateForFunctionPointer<GroupManager_IsObjectIDInPartyDelegate>(address.GroupManager_IsObjectIDInPartyPtr);
            SetNodeScale = Marshal.GetDelegateForFunctionPointer<AtkResNode_SetScaleDelegate>(address.AtkResNode_SetScalePtr);
            SetNodePosition = Marshal.GetDelegateForFunctionPointer<AtkResNode_SetPositionShortDelegate>(address.AtkResNode_SetPositionShortPtr);
            EmptySeStringPtr = StringToSeStringPtr("");

            Interface.ClientState.OnLogout += OnLogout_ResetRaptureAtkModule;
        }

        public static void DisposeInstance() => Instance.Dispose();

        public void Dispose()
        {
            Interface.ClientState.OnLogout -= OnLogout_ResetRaptureAtkModule;
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
                    var frameworkPtr = Instance.Interface.Framework.Address.BaseAddress;
                    var uiModulePtr = Instance.GetUIModule(frameworkPtr);

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
            return Instance.Interface.SeStringManager.Parse(bytes);
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

        internal static SafeAddonNamePlate GetSafeAddonNamePlate() => new SafeAddonNamePlate(Instance.Interface);

        internal static bool IsLocalPlayer(int actorID)
        {
            PluginLog.Information($"[{ThreadID}][IsLocalPlayer] Enter");
            var result = Instance.Interface.ClientState.LocalPlayer?.ActorId == actorID;
            PluginLog.Information($"[{ThreadID}][] Exit");
            return result;
        }

        internal static bool IsPartyMember(int actorID)
        {
            PluginLog.Information($"[{ThreadID}]IsPartyMember] Enter");
            var result = Instance.IsObjectIDInParty(Instance.Address.GroupManagerPtr, actorID) == 1;
            PluginLog.Information($"[{ThreadID}][IsPartyMember] Exit");
            return result;
        }

        internal class SafeAddonNamePlate
        {
            private readonly DalamudPluginInterface Interface;

            public IntPtr Pointer
            {
                get
                {
                    PluginLog.Information($"[{ThreadID}]SafeAddonNamePlate.Pointer] Enter");
                    var result = Interface.Framework.Gui.GetUiObjectByName("NamePlate", 1);
                    PluginLog.Information($"[{ThreadID}][SafeAddonNamePlate.Pointer] Exit");
                    return result;
                }
            }

            public SafeAddonNamePlate(DalamudPluginInterface pluginInterface)
            {
                Interface = pluginInterface;
            }

            public unsafe SafeNamePlateObject GetNamePlateObject(int index)
            {
                PluginLog.Information($"[{ThreadID}]SafeAddonNamePlate.GetNamePlateObject] Enter");
                var addon = AsUnsafe();
                if (addon == null)
                {
                    PluginLog.Debug($"[{GetType().Name}] AddonNamePlate was null");
                    return null;
                }

                var npObjectArray = addon->NamePlateObjectArray;
                if (npObjectArray == null)
                {
                    PluginLog.Debug($"[{GetType().Name}] NamePlateObjectArray was null");
                    return null;
                }

                var npObject = &npObjectArray[index];
                if (npObject == null)
                {
                    PluginLog.Debug($"[{GetType().Name}] NamePlateObject was null");
                    return null;
                }

                var result = new SafeNamePlateObject(new IntPtr(npObject), index);
                PluginLog.Information($"[{ThreadID}][SafeAddonNamePlate.GetNamePlateObject] Exit");
                return result;
            }

            public unsafe AddonNamePlate* AsUnsafe()
            {
                PluginLog.Information($"[{ThreadID}][SafeAddonNamePlate.AsUnsafe] Enter");
                var addonPtr = Pointer;
                if (addonPtr == null)
                {
                    PluginLog.Debug($"[{GetType().Name}] AddonNamePlate was null");
                    return null;
                }
                var result = (AddonNamePlate*)addonPtr;
                PluginLog.Information($"[{ThreadID}][SafeAddonNamePlate.AsUnsafe] Exit");
                return result;
            }
        }

        internal class SafeNamePlateObject
        {
            public IntPtr Pointer { get; private set; }
            private int _Index;
            private SafeNamePlateInfo _NamePlateInfo;

            public SafeNamePlateObject(IntPtr pointer, int index = -1)
            {
                Pointer = pointer;
                _Index = index;
            }

            public int Index
            {
                get
                {
                    PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.Index] Enter");
                    if (_Index == -1)
                    {
                        var addon = GetSafeAddonNamePlate();
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
                    var result = _Index;
                    PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.Index] Exit");
                    return result;
                }
            }

            public SafeNamePlateInfo NamePlateInfo
            {
                get
                {
                    PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.NamePlateInfo] Enter");
                    if (_NamePlateInfo == null)
                    {
                        var rapturePtr = RaptureAtkModulePtr;
                        if (rapturePtr == IntPtr.Zero)
                        {
                            PluginLog.Debug($"[{GetType().Name}] RaptureAtkModule was null");
                            return null;
                        }

                        unsafe
                        {
                            var rapture = (RaptureAtkModule*)rapturePtr;
                            var npInfo = &(&rapture->NamePlateInfoArray)[Index];
                            _NamePlateInfo = new SafeNamePlateInfo(new IntPtr(npInfo));
                        }

                        /*
                        var npInfoArrayPtr = rapture + Marshal.OffsetOf(typeof(RaptureAtkModule), nameof(RaptureAtkModule.NamePlateInfoArray)).ToInt32();
                        var npInfoPtr = npInfoArrayPtr + Marshal.SizeOf(typeof(RaptureAtkModule.NamePlateInfo)) * Index;
                        _NamePlateInfo = new SafeNamePlateInfo(npInfoPtr);
                        */

                    }
                    var result = _NamePlateInfo;
                    PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.NamePlateInfo] Exit");
                    return result;
                }
            }

            public unsafe bool IsVisible
            {
                get
                {
                    PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.IsVisible] Enter");
                    var npObject = AsUnsafe();
                    if (npObject == null)
                    {
                        PluginLog.Debug($"[{GetType().Name}] NamePlateObject was null");
                        return false;
                    }

                    var result = npObject->IsVisible;
                    PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.IsVisible] Exit");
                    return result;
                }
            }

            public unsafe bool IsLocalPlayer
            {
                get
                {
                    PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.IsLocalPlayer] Enter");
                    var npObject = AsUnsafe();
                    if (npObject == null)
                    {
                        PluginLog.Debug($"[{GetType().Name}] NamePlateObject was null");
                        return false;
                    }

                    var result = npObject->IsLocalPlayer;
                    PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.IsLocalPlayer] Exit");
                    return result;
                }
            }

            public unsafe void SetIconScale(float scale)
            {
                PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.SetIconScale] Enter");
                var npObject = AsUnsafe();
                if (npObject == null)
                {
                    PluginLog.Debug($"[{GetType().Name}] NamePlateObject was null");
                    return;
                }

                var imageNode = npObject->ImageNode1;
                if (imageNode == null)
                {
                    PluginLog.Debug($"[{GetType().Name}] ImageNode1 was null");
                    return;
                }

                Instance.SetNodeScale(new IntPtr(imageNode), scale, scale);
                //imageNode->AtkResNode.ScaleX = scale;
                //imageNode->AtkResNode.ScaleY = scale;
                PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.SetIconScale] Exit");
            }

            public unsafe void SetIconPosition(short x, short y)
            {
                PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.SetIconPosition] Enter");
                var npObject = AsUnsafe();
                if (npObject == null)
                {
                    PluginLog.Debug($"[{GetType().Name}] NamePlateObject was null");
                    return;
                }

                var imageNode = npObject->ImageNode1;
                if (imageNode == null)
                {
                    PluginLog.Debug($"[{GetType().Name}] ImageNode1 was null");
                    return;
                }

                //npObject->ImageNode1->AtkResNode.X = 0;
                //npObject->ImageNode1->AtkResNode.Y = 0;
                npObject->IconXAdjust = x;
                npObject->IconYAdjust = y;
                //Instance.SetNodePosition(new IntPtr(imageNode), x, y);
                PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.SetIconPosition] Exit");
            }

            public unsafe AddonNamePlate.NamePlateObject* AsUnsafe()
            {
                PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.AsUnsafe] Enter");
                var npObjectPtr = Pointer;
                if (npObjectPtr == null)
                {
                    PluginLog.Debug($"[{GetType().Name}] NamePlateObject was null");
                    return null;
                }
                var result = (AddonNamePlate.NamePlateObject*)npObjectPtr;
                PluginLog.Information($"[{ThreadID}][SafeNamePlateObject.AsUnsafe] Exit");
                return result;
            }
        }

        internal class SafeNamePlateInfo
        {
            public IntPtr Pointer { get; private set; }

            public SafeNamePlateInfo(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public unsafe int ActorID
            {
                get
                {
                    PluginLog.Information($"[{ThreadID}][SafeNamePlateInfo.ActorID] Enter");
                    var npInfo = AsUnsafe();
                    if (npInfo == null)
                    {
                        PluginLog.Debug($"[{GetType().Name}] NamePlateInfo was null");
                        return -1;
                    }
                    var result = npInfo->ActorID;
                    PluginLog.Information($"[{ThreadID}][SafeNamePlateInfo.ActorID] Exit");
                    return result;
                }
            }

            public unsafe RaptureAtkModule.NamePlateInfo* AsUnsafe()
            {
                PluginLog.Information($"[{ThreadID}][SafeNamePlateInfo.ActorID] Enter");
                var npInfoPtr = Pointer;
                if (npInfoPtr == null)
                {
                    PluginLog.Debug($"[{GetType().Name}] NamePlateInfo was null");
                    return null;
                }
                var result = (RaptureAtkModule.NamePlateInfo*)npInfoPtr;
                PluginLog.Information($"[{ThreadID}][SafeNamePlateInfo.ActorID] Exit");
                return result;
            }
        }
    }
}