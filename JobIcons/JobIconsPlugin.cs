using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using JobIcons;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Job_Icons
{
    public class JobIconsPlugin : IDalamudPlugin
    {
        public string Name => "JobIcons";
        public static DalamudPluginInterface pluginInterface;
        public static Config Configuration;

        public static bool help;
        public static bool enabled = true;
        public static bool config;
        public static bool dev;
        public static bool debug;

        public static int increase;
        public static float scaler = 1;

        public static int[] role = { 0, 0, 0, 0, 0 };
        public int[] sets = { 091021, 091521, 092021, 092521, 093021, 093521, 094021, 094521 };
        public static int deepth = 1;
        public static int xAdjust = -13;
        public static int yAdjust = 55;

        public static bool showName = true;
        public static bool showFC = true;
        public static bool showtitle = true;

        public static IntPtr emptyPointer;
        public static IntPtr namePointer;
        public static List<IntPtr> NPObjects;

        public static string[] setNames = { "Gold", "Framed", "Glowing", "Grey", "Black", "Yellow", "Orange", "Red", "Purple", "Blue", "Green" };

        public int countdown = 1000;
        public static List<int> partyList = new List<int>();
        public static IntPtr me = IntPtr.Zero;
        public int meInt;
        public static IntPtr raptk = IntPtr.Zero;
        public static IntPtr groupManager = IntPtr.Zero;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetBaseUIObjDelegate();

        public GetBaseUIObjDelegate getBaseUIObj;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        public delegate IntPtr GetUI2ObjByNameDelegate(IntPtr getBaseUIObj, string uiName, int index);

        public static GetUI2ObjByNameDelegate getUI2ObjByName;

        // E8 ?? ?? ?? ?? EB B8 E8 bool IsObjectIDInParty(groupManager, id)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        public delegate byte IsObjectIDInParty(IntPtr groupManager, int ActorId);

        public static IsObjectIDInParty isObjectIDInParty;
        public IntPtr isObjectIDInPartyPtr;

        // void FUN_140e9f0e0(IntPtr this, bool isPrefixTitle, bool displayTitle, char* title, char* name, char* fcName, int iconId)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private unsafe delegate IntPtr SetNamePlate(IntPtr this_var, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconId);

        private SetNamePlate setNamePlate;
        private Hook<SetNamePlate> setNamePlateHook;
        public IntPtr setNamePlatePtr;

        // void FUN_1405C5E50 raptureatkmodule::whocares(raptureatkmodule * this, intptr what, intptr ever, intptr it, intptr dont, uint matter, uint really)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        public unsafe delegate IntPtr RaptureAtkThing(RaptureAtkModule* this_var, IntPtr what, IntPtr ever, IntPtr it, IntPtr dont, uint matter, uint really);

        private RaptureAtkThing raptureAtkThing;
        private Hook<RaptureAtkThing> raptureAtkThingHook;
        public IntPtr raptureAtkThingPtr;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        public delegate IntPtr scaleIconFunc(IntPtr thisObj, float a, float b);

        public static scaleIconFunc scaleIcon;
        public IntPtr scaleIconPtr;

        public IntPtr UIModulePtr = IntPtr.Zero;
        public IntPtr UI3DModule = IntPtr.Zero;
        public static unsafe RaptureAtkModule* RaptureAtkModule;
        public IntPtr NameplateArray = IntPtr.Zero;
        public static IntPtr baseUIObject = IntPtr.Zero;
        public static IntPtr baseUiProperties = IntPtr.Zero;
        public static IntPtr nameplateUIPtr = IntPtr.Zero;
        public static unsafe void* npObjArray;

        public bool nparray = true;

        public unsafe void Initialize(DalamudPluginInterface pluginInterface)
        {
            setNamePlatePtr = pluginInterface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2");
            setNamePlate = new SetNamePlate(SetNamePlateFunc);
            try
            { setNamePlateHook = new Hook<SetNamePlate>(setNamePlatePtr, setNamePlate, this); setNamePlateHook.Enable(); }
            catch (Exception e)
            { PluginLog.Log("BAD 1\n" + e.ToString()); }

            groupManager = pluginInterface.TargetModuleScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 44 8B E7");

            isObjectIDInPartyPtr = pluginInterface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? EB B8 E8");
            isObjectIDInParty = Marshal.GetDelegateForFunctionPointer<IsObjectIDInParty>(isObjectIDInPartyPtr);

            scaleIconPtr = pluginInterface.TargetModuleScanner.ScanText("8B 81 ?? ?? ?? ?? A8 01 75 ?? F3 0F 10 41 ?? 0F 2E C1 7A ?? 75 ?? F3 0F 10 41 ?? 0F 2E C2 7A ?? 74 ?? 83 C8 01 89 81 ?? ?? ?? ?? F3 0F 10 05 ?? ?? ?? ??");
            scaleIcon = Marshal.GetDelegateForFunctionPointer<scaleIconFunc>(scaleIconPtr);

            raptureAtkThingPtr = pluginInterface.TargetModuleScanner.ScanText("40 53 55 56 41 56 48 81 EC ?? ?? ?? ?? 48 8B 84 24 ?? ?? ?? ??");
            raptureAtkThing = new RaptureAtkThing(RaptureAtkThingFunc);
            try
            { raptureAtkThingHook = new Hook<RaptureAtkThing>(raptureAtkThingPtr, raptureAtkThing, this); raptureAtkThingHook.Enable(); }
            catch (Exception e)
            { PluginLog.Log("BAD 2\n" + e.ToString()); }

            var GetBaseUIObject = pluginInterface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 41 b8 01 00 00 00 48 8d 15 ?? ?? ?? ?? 48 8b 48 20 e8 ?? ?? ?? ?? 48 8b cf");
            var GetUI2ObjByName = pluginInterface.TargetModuleScanner.ScanText("e8 ?? ?? ?? ?? 48 8b cf 48 89 87 ?? ?? 00 00 e8 ?? ?? ?? ?? 41 b8 01 00 00 00");
            getBaseUIObj = Marshal.GetDelegateForFunctionPointer<GetBaseUIObjDelegate>(GetBaseUIObject);
            getUI2ObjByName = Marshal.GetDelegateForFunctionPointer<GetUI2ObjByNameDelegate>(GetUI2ObjByName);

            JobIconsPlugin.pluginInterface = pluginInterface;
            Configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();

            emptyPointer = StringToSeStringPtr("");
            namePointer = StringToSeStringPtr("Banana Maiden");

            role = Configuration.Role;
            enabled = Configuration.Enabled;
            scaler = Configuration.Scale;
            xAdjust = Configuration.XAdjust;
            yAdjust = Configuration.YAdjust;
            showName = Configuration.ShowName;
            showtitle = Configuration.ShowTitle;
            showFC = Configuration.ShowFC;

            JobIconsPlugin.pluginInterface.CommandManager.AddHandler("/jicons", new CommandInfo(Command)
            {
                HelpMessage = "Opens Job Icons config."
            });

            baseUIObject = getBaseUIObj();
            baseUiProperties = Marshal.ReadIntPtr(baseUIObject, 0x20);

            UIModulePtr = Marshal.ReadIntPtr(pluginInterface.Framework.Address.BaseAddress, 0x29F8);
            UI3DModule = Marshal.ReadIntPtr(UIModulePtr, 0xA62C0);
            // RaptureAtkModule = (RaptureAtkModule*)(Marshal.ReadIntPtr(pluginInterface.Framework.Address.BaseAddress, 0x29F8) + 0xB47D0).ToPointer();
            // PluginLog.Log($"RaptureATK: {(long)RaptureAtkModule:X}");
            nameplateUIPtr = getUI2ObjByName(baseUiProperties, "NamePlate", 1);
            if (nameplateUIPtr != IntPtr.Zero) { npObjArray = ((AddonNamePlate*)nameplateUIPtr)->NamePlateObjectArray; }

            JobIconsPlugin.pluginInterface.UiBuilder.OnOpenConfigUi += ConfigWindow;
            JobIconsPlugin.pluginInterface.UiBuilder.OnBuildUi += Draw.DrawWindow;
        }

        public unsafe IntPtr RaptureAtkThingFunc(RaptureAtkModule* this_var, IntPtr what, IntPtr ever, IntPtr it, IntPtr dont, uint matter, uint really)
        {
            if (raptk == IntPtr.Zero)
            {
                raptk = (IntPtr)this_var;
                RaptureAtkModule = this_var;
            }

            return raptureAtkThingHook.Original(this_var, what, ever, it, dont, matter, really);
        }

        public void AdjustIconPos(IntPtr this_obj)
        {
            Marshal.WriteInt16(this_obj + 0x5A, (short)yAdjust);
            Marshal.WriteInt16(this_obj + 0x58, (short)xAdjust);
        }

        public unsafe void AdjustIconScale(IntPtr this_obj, float scale)
        {
            ((AddonNamePlate.NamePlateObject*)this_obj)->ImageNode1->AtkResNode.ScaleY = scale;
            ((AddonNamePlate.NamePlateObject*)this_obj)->ImageNode1->AtkResNode.ScaleX = scale;
        }

        public unsafe IntPtr SetNamePlateFunc(IntPtr this_var, bool isPrefixTitle, bool displayTitle, IntPtr title, IntPtr name, IntPtr fcName, int iconId)
        {
            var actorID = GetActorFromNameplate(this_var);
            if (pluginInterface.ClientState.LocalPlayer != null)
            {
                if (partyList.Contains(actorID))
                {
                    float scaled = scaler;
                    var pc = GetPC(actorID);
                    if (!showName) name = emptyPointer;
                    if (!showtitle) title = emptyPointer;
                    if (!showFC) fcName = emptyPointer;

                    if ((role[pc.ClassJob.GameData.Role] > 2)) scaled *= 2;
                    scaleIcon(Marshal.ReadIntPtr(this_var + 24), 1.0001f, 1.0001f);

                    var x = setNamePlateHook.Original(this_var, isPrefixTitle, displayTitle, title, name, fcName, ClassIcon(
                        (int)pc.ClassJob.Id,
                        role[pc.ClassJob.GameData.Role]));
                    AdjustIconPos(this_var);
                    AdjustIconScale(this_var, scaled);
                    return x;
                }
            }

            if (actorID == pluginInterface.ClientState.LocalPlayer.ActorId && debug)
            {
                float scaled = scaler;

                if (!showName) name = emptyPointer;
                if (!showtitle) title = emptyPointer;
                if (!showFC) fcName = emptyPointer;

                if ((role[pluginInterface.ClientState.LocalPlayer.ClassJob.GameData.Role] > 2)) scaled *= 2;

                scaleIcon(Marshal.ReadIntPtr(this_var + 24), 1.0001f, 1.0001f);
                var x = setNamePlateHook.Original(this_var, isPrefixTitle, displayTitle, title, name, fcName, ClassIcon((int)pluginInterface.ClientState.LocalPlayer.ClassJob.Id, role[(int)pluginInterface.ClientState.LocalPlayer.ClassJob.GameData.Role]));
                AdjustIconPos(this_var);
                AdjustIconScale(this_var, scaled);

                return x;
            }

            scaleIcon(Marshal.ReadIntPtr(this_var + 24), 1.0001f, 1.0001f);
            AdjustIconScale(this_var, 1f);
            return setNamePlateHook.Original(this_var, isPrefixTitle, displayTitle, title, name, fcName, iconId);
        }

        public unsafe SeString GetSeStringFromPtr(byte* ptr)
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
            return pluginInterface.SeStringManager.Parse(bytes);
        }

        public unsafe IntPtr StringToSeStringPtr(string rawText)
        {
            var seString = new SeString(new List<Payload>());
            seString.Payloads.Add(new TextPayload(rawText));
            var bytes = seString.Encode();
            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);
            return pointer;
        }

        public static unsafe int GetActorFromNameplate(IntPtr this_var)
        {
            if (raptk != IntPtr.Zero)
            {
                var npObjPtr = this_var.ToPointer();
                if (nameplateUIPtr == IntPtr.Zero)
                {
                    nameplateUIPtr = getUI2ObjByName(baseUiProperties, "NamePlate", 1);
                }

                if (nameplateUIPtr != IntPtr.Zero)
                {
                    npObjArray = ((AddonNamePlate*)nameplateUIPtr)->NamePlateObjectArray;
                }

                if (baseUIObject != IntPtr.Zero)
                {
                    if (baseUiProperties != IntPtr.Zero)
                    {
                        var npIndex = ((long)npObjPtr - (long)npObjArray) / 0x70;
                        var npInfo = (&RaptureAtkModule->NamePlateInfoArray)[npIndex];

                        // PluginLog.Log($"SetNamePlate thisptr {(long)npObjPtr:X} index {npIndex} npinfo ptr {(long)npInfo:X} actorID {npInfo->ActorID:X}");
                        return npInfo.ActorID;
                    }
                }
            }

            return 0;
        }

        public static void SaveConfig()
        {
            JobIconsPlugin.Configuration.Enabled = enabled;
            Configuration.Role = role;
            Configuration.Scale = scaler;
            Configuration.XAdjust = xAdjust;
            Configuration.YAdjust = yAdjust;
            Configuration.ShowName = showName;
            Configuration.ShowTitle = showtitle;
            Configuration.ShowFC = showFC;
            pluginInterface.SavePluginConfig(Configuration);
        }

        public void Dispose()
        {
            pluginInterface.CommandManager.RemoveHandler("/jicons");
            JobIconsPlugin.pluginInterface.UiBuilder.OnBuildUi -= Draw.DrawWindow;
            JobIconsPlugin.pluginInterface.UiBuilder.OnOpenConfigUi -= ConfigWindow;
            setNamePlateHook.Disable();
            setNamePlateHook.Dispose();
            Marshal.FreeHGlobal(emptyPointer);
            Marshal.FreeHGlobal(namePointer);
        }

        private void Command(string command, string arguments) => config = !config;

        public int ClassIcon(int jobid, int set)
        {
            if (set == 0) { return 062000 + jobid; } //Standard
            if (set == 1) { return 062100 + jobid; } //Framed
            if (set == 2) //Glowy
            {
                if (jobid < 8) return 062300 + jobid;
                if (jobid == 26) return 062308;//arc
                if (jobid == 29) return 062309;//rog
                if (jobid < 19) return 062502 - 8 + jobid; //crafting
                if (jobid < 26) return 062401 - 19 + jobid;
                if (jobid < 29) return 062408 - 27 + jobid;
                else return 062410 - 30 + jobid;
            }

            if (set > 2 && set < 11) //grey
            {
                int num = sets[set - 3];
                if (jobid < 6) return num + jobid;
                if (jobid < 8) return num + 1 + jobid;
                if (jobid == 26) return num + 9;
                if (jobid < 19) return num + 2 + jobid;
                if (jobid < 26) return num + 39 + jobid;
                if (jobid < 29) return num + 38 + jobid;
                if (jobid < 31) return num + 71 + jobid;
                if (jobid == 31) return num + 104;
                if (jobid < 34) return num + 70 + jobid;
                else return num + 72 + jobid;
            }
            else return 0;
        }

        private void ConfigWindow(object Sender, EventArgs args) => config = true;

        public static string GetActorName(int actorId)
        {
            for (var k = 0; k < pluginInterface.ClientState.Actors.Length; k++)
            {
                if (pluginInterface.ClientState.Actors[k] is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter pc)
                {
                    if (pc.ActorId == actorId) return pc.Name;
                }
            }

            return "";
        }

        public Dalamud.Game.ClientState.Actors.Types.PlayerCharacter GetPC(int actorId)
        {
            for (var k = 0; k < pluginInterface.ClientState.Actors.Length; k++)
            {
                if (pluginInterface.ClientState.Actors[k] is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter pc)
                {
                    if (pc.ActorId == actorId) return pc;
                }
            }

            return null;
        }

        public static bool InParty(Dalamud.Game.ClientState.Actors.Types.Actor actor)
        {
            return (GetStatus(actor) & 16) > 0;
        }

        private static byte GetStatus(Dalamud.Game.ClientState.Actors.Types.Actor actor)
        {
            IntPtr statusPtr = actor.Address + 0x1980;
            return Marshal.ReadByte(statusPtr);
        }
    }
}