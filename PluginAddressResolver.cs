using Dalamud.Game;
using System;
using System.Runtime.InteropServices;

namespace JobIcons
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    internal delegate IntPtr SetNamePlateDelegate(IntPtr addon, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconId);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    internal delegate IntPtr AtkResNodeSetScaleDelegate(IntPtr node, float x, float y);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    internal delegate byte GroupManagerIsObjectIdInPartyDelegate(IntPtr groupManager, uint actorId);

    internal sealed class PluginAddressResolver : BaseAddressResolver
    {
        private const string AddonNamePlateSetNamePlateSignature = "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2";
        internal IntPtr AddonNamePlateSetNamePlatePtr;

        private const string AtkResNodeSetScaleSignature = "8B 81 ?? ?? ?? ?? A8 01 75 ?? F3 0F 10 41 ?? 0F 2E C1 7A ?? 75 ?? F3 0F 10 41 ?? 0F 2E C2 7A ?? 74 ?? 83 C8 01 89 81 ?? ?? ?? ?? F3 0F 10 05 ?? ?? ?? ??";
        internal IntPtr AtkResNodeSetScalePtr;

        private const string GroupManagerSignature = "48 8D 0D ?? ?? ?? ?? 44 8B E7";
        internal IntPtr GroupManagerPtr;

        private const string GroupManagerIsObjectIdInPartySignature = "E8 ?? ?? ?? ?? EB B8 E8";
        internal IntPtr GroupManagerIsObjectIdInPartyPtr;

        protected override void Setup64Bit(SigScanner scanner)
        {
            AddonNamePlateSetNamePlatePtr = scanner.ScanText(AddonNamePlateSetNamePlateSignature);
            AtkResNodeSetScalePtr = scanner.ScanText(AtkResNodeSetScaleSignature);
            GroupManagerPtr = scanner.GetStaticAddressFromSig(GroupManagerSignature);
            GroupManagerIsObjectIdInPartyPtr = scanner.ScanText(GroupManagerIsObjectIdInPartySignature);
        }
    }
}
