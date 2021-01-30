using Dalamud.Game;
using Dalamud.Game.Internal;
using System;
using System.Runtime.InteropServices;

namespace JobIcons
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    internal delegate IntPtr SetNamePlateDelegate(IntPtr addon, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconID);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    internal delegate IntPtr AtkResNode_SetScaleDelegate(IntPtr node, float x, float y);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    internal delegate IntPtr AtkResNode_SetPositionShortDelegate(IntPtr node, short x, short y);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    internal delegate IntPtr Framework_GetUIModuleDelegate(IntPtr framework);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    internal delegate IntPtr UIModule_GetRaptureAtkModuleDelegate(IntPtr uiModule);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    public delegate byte GroupManager_IsObjectIDInPartyDelegate(IntPtr groupManager, int actorId);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    public delegate byte GroupManager_IsObjectIDInAllianceDelegate(IntPtr groupManager, int actorId);

    internal sealed class PluginAddressResolver : BaseAddressResolver
    {
        private const string AddonNamePlate_SetNamePlateSignature = "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2";
        internal IntPtr AddonNamePlate_SetNamePlatePtr;

        private const string AtkResNode_SetScaleSignature = "8B 81 ?? ?? ?? ?? A8 01 75 ?? F3 0F 10 41 ?? 0F 2E C1 7A ?? 75 ?? F3 0F 10 41 ?? 0F 2E C2 7A ?? 74 ?? 83 C8 01 89 81 ?? ?? ?? ?? F3 0F 10 05 ?? ?? ?? ??";
        internal IntPtr AtkResNode_SetScalePtr;

        private const string AtkResNode_SetPositionShortSignature = "48 85 C9 74 4A 41 0F BF C0 66 0F 6E C8 0F BF C2 0F 5B C9 66 0F 6E D0";
        internal IntPtr AtkResNode_SetPositionShortPtr;

        private const string Framework_GetUIModuleSignature = "E8 ?? ?? ?? ?? 48 8B C8 48 8B 10 FF 92 ?? ?? ?? ?? 48 8B C8 BA ?? ?? ?? ??";
        internal IntPtr Framework_GetUIModulePtr;

        private const string GroupManagerSignature = "48 8D 0D ?? ?? ?? ?? 44 8B E7";
        internal IntPtr GroupManagerPtr;

        private const string GroupManager_IsObjectIDInPartySignature = "E8 ?? ?? ?? ?? EB B8 E8";
        internal IntPtr GroupManager_IsObjectIDInPartyPtr;

        private const string GroupManager_IsObjectIDInAllianceSignature = "33 C0 44 8B CA F6 81 ?? ?? ?? ?? ??";
        internal IntPtr GroupManager_IsObjectIDInAlliancePtr;

        protected override void Setup64Bit(SigScanner scanner)
        {
            AddonNamePlate_SetNamePlatePtr = scanner.ScanText(AddonNamePlate_SetNamePlateSignature);
            AtkResNode_SetScalePtr = scanner.ScanText(AtkResNode_SetScaleSignature);
            AtkResNode_SetPositionShortPtr = scanner.ScanText(AtkResNode_SetPositionShortSignature);
            Framework_GetUIModulePtr = scanner.ScanText(Framework_GetUIModuleSignature);
            GroupManagerPtr = scanner.GetStaticAddressFromSig(GroupManagerSignature);
            GroupManager_IsObjectIDInPartyPtr = scanner.ScanText(GroupManager_IsObjectIDInPartySignature);
            GroupManager_IsObjectIDInAlliancePtr = scanner.ScanText(GroupManager_IsObjectIDInAllianceSignature);
        }
    }
}
