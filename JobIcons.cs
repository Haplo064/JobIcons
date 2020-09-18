using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using ImGuiNET;
using Dalamud.Configuration;
using Num = System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using System.Linq;

namespace Job_Icons
{


    [StructLayout(LayoutKind.Explicit, Size = 0x60)]
    unsafe struct ObjectInfo
    {
        [FieldOffset(0x18)] public void* Actor;
        [FieldOffset(0x4E)] public byte NameplateIndex;
    }

    [StructLayout(LayoutKind.Explicit)]
    unsafe struct UI3DModule
    {
        [FieldOffset(0x20)] public fixed byte ObjectInfo[0x60 * 434];
        [FieldOffset(0xAC60)] public int ObjectInfoCount; // actor total count
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x248)]
    unsafe struct NamePlateInfo
    {
        [FieldOffset(0x00)] public int ActorID;
        [FieldOffset(0x52)] public fixed char ActorName[0x40];
    }

    [StructLayout(LayoutKind.Explicit)]
    unsafe struct RaptureAtkModule
    {
        [FieldOffset(0x19DE0)] public fixed byte NamePlateInfo[0x248 * 50];
    }

    [StructLayout(LayoutKind.Explicit)]
    unsafe struct AddonNamePlate
    {
        [FieldOffset(0x450)] public byte* NamePlateObjectArray;
    }

    public class JobIcons : IDalamudPlugin
    {
        public string Name => "JobIcons";
        private DalamudPluginInterface pluginInterface;
        public Config Configuration;

        public bool help = false;
        public bool enabled = true;
        public bool config = true;
        public bool dev = false;
        public bool debug = false;

        public int increase = 0;
        public float scaler = 1;

        public int[] role = { 0, 0, 0, 0, 0 };

        public int deepth = 1;
        public int xAdjust = -13;
        public int yAdjust = 55;

        public bool showName = false;
        public bool showFC = false;
        public bool showtitle = false;

        public string[] setNames = { "Gold", "Framed", "Glowing", "Grey", "Black", "Yellow", "Orange", "Red", "Purple", "Blue", "Green" };

        public int countdown = 1000;
        public List<PartyMem> partyList = new List<PartyMem>();
        public IntPtr me = IntPtr.Zero;
        public int meInt = 0;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetBaseUIObjDelegate();

        public GetBaseUIObjDelegate getBaseUIObj;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        public delegate IntPtr GetUI2ObjByNameDelegate(IntPtr getBaseUIObj, string uiName, int index);

        public GetUI2ObjByNameDelegate getUI2ObjByName;

        //void FUN_140e9f0e0(IntPtr this, bool isPrefixTitle, bool displayTitle, char* title, char* name, char* fcName, int iconId)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private unsafe delegate IntPtr SetNamePlate(IntPtr this_var, bool isPrefixTitle, bool displayTitle, string title, string name, string fcName, int iconId);
        private SetNamePlate setNamePlate;
        private Hook<SetNamePlate> setNamePlateHook;
        public IntPtr setNamePlatePtr;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr scaleIconFunc(IntPtr thisObj, float a, float b);
        private scaleIconFunc scaleIcon;
        public IntPtr scaleIconPtr;

        public IntPtr UIModulePtr = IntPtr.Zero;
        public IntPtr UI3DModule = IntPtr.Zero;
        private unsafe RaptureAtkModule* RaptureAtkModule;
        public IntPtr NameplateArray = IntPtr.Zero;
        public IntPtr baseUIObject = IntPtr.Zero;
        public IntPtr baseUiProperties = IntPtr.Zero;
        public IntPtr nameplateUIPtr = IntPtr.Zero;
        public unsafe byte* npObjArray;

        public unsafe void Initialize(DalamudPluginInterface pluginInterface)
        {
            setNamePlatePtr = pluginInterface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2");
            setNamePlate = new SetNamePlate(setNamePlateFunc);
            try
            { setNamePlateHook = new Hook<SetNamePlate>(setNamePlatePtr, setNamePlate, this); setNamePlateHook.Enable(); }
            catch (Exception e)
            { PluginLog.Log("BAD 1\n" + e.ToString()); }


            scaleIconPtr = pluginInterface.TargetModuleScanner.ScanText("8B 81 ?? ?? ?? ?? A8 01 75 ?? F3 0F 10 41 ?? 0F 2E C1 7A ?? 75 ?? F3 0F 10 41 ?? 0F 2E C2 7A ?? 74 ?? 83 C8 01 89 81 ?? ?? ?? ?? F3 0F 10 05 ?? ?? ?? ??");
            scaleIcon = Marshal.GetDelegateForFunctionPointer<scaleIconFunc>(scaleIconPtr);

            var GetBaseUIObject = pluginInterface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 41 b8 01 00 00 00 48 8d 15 ?? ?? ?? ?? 48 8b 48 20 e8 ?? ?? ?? ?? 48 8b cf");
            var GetUI2ObjByName = pluginInterface.TargetModuleScanner.ScanText("e8 ?? ?? ?? ?? 48 8b cf 48 89 87 ?? ?? 00 00 e8 ?? ?? ?? ?? 41 b8 01 00 00 00");

            getBaseUIObj = Marshal.GetDelegateForFunctionPointer<GetBaseUIObjDelegate>(GetBaseUIObject);
            getUI2ObjByName = Marshal.GetDelegateForFunctionPointer<GetUI2ObjByNameDelegate>(GetUI2ObjByName);

            this.pluginInterface = pluginInterface;
            Configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();

            role = Configuration.Role;
            enabled = Configuration.Enabled;
            scaler = Configuration.Scale;
            xAdjust = Configuration.XAdjust;
            yAdjust = Configuration.YAdjust;

            this.pluginInterface.CommandManager.AddHandler("/jicons", new CommandInfo(Command)
            {
                HelpMessage = "Opens Job Icons config."
            });

            baseUIObject = getBaseUIObj();
            baseUiProperties = Marshal.ReadIntPtr(baseUIObject, 0x20);

            UIModulePtr = Marshal.ReadIntPtr(pluginInterface.Framework.Address.BaseAddress, 0x29F8);
            UI3DModule = Marshal.ReadIntPtr(UIModulePtr, 0xA62C0);
            RaptureAtkModule = (RaptureAtkModule*)(Marshal.ReadIntPtr(pluginInterface.Framework.Address.BaseAddress, 0x29F8) + 0xB3780).ToPointer();

            nameplateUIPtr = getUI2ObjByName(baseUiProperties, "NamePlate", 1);
            if (nameplateUIPtr != IntPtr.Zero) { npObjArray = ((AddonNamePlate*)nameplateUIPtr)->NamePlateObjectArray; }
            

            this.pluginInterface.UiBuilder.OnOpenConfigUi += ConfigWindow;
            this.pluginInterface.UiBuilder.OnBuildUi += DrawWindow;

        }


        public void AdjustIconPos(IntPtr this_obj)
        {
            Marshal.WriteInt16(this_obj + 0x5A, (short)yAdjust);

            if (xAdjust < 0)
            {
                Marshal.WriteInt16(this_obj + 0x58, 0);
                Marshal.WriteInt16(this_obj + 0x54, (short)(Math.Abs(xAdjust)));
            }
            else
            {
                Marshal.WriteInt16(this_obj + 0x54, 0);
                Marshal.WriteInt16(this_obj + 0x58, (short)xAdjust);
            }


        }

        public unsafe IntPtr setNamePlateFunc(IntPtr this_var, bool isPrefixTitle, bool displayTitle, string title, string name, string fcName, int iconId)
        {
            
            if (enabled)
            {
                foreach (PartyMem pm in partyList)
                {
                    if (getActorFromNameplate(this_var) == pm.PC.ActorId)
                    {
                        
                        if (pm.NamePlatePtr != this_var)
                        {
                            if (pm.NamePlatePtr != IntPtr.Zero)
                            {
                                scaleIcon(Marshal.ReadIntPtr(pm.NamePlatePtr + 24), 1f, 1f);
                            }
                            pm.NamePlatePtr = this_var;
                        }

                        float scaled = scaler;
                        if (role[(int)pm.PC.ClassJob.GameData.Role] > 2) scaled *= 2;
                        scaleIcon(Marshal.ReadIntPtr(this_var + 24), scaled, scaled);

                        var x = setNamePlateHook.Original(this_var, true, true, "", "", "", ClassIcon((int)pm.PC.ClassJob.Id, role[(int)pm.PC.ClassJob.GameData.Role]));
                        AdjustIconPos(this_var);
                        return x;
                    }
                }


            }

            if (debug && pluginInterface.ClientState.LocalPlayer != null)
            {
                if (getActorFromNameplate(this_var) == pluginInterface.ClientState.LocalPlayer.ActorId)
                {
                    float scaled = scaler;

                    if (me != this_var)
                    {
                        if (me != IntPtr.Zero) { scaleIcon(Marshal.ReadIntPtr(me + 24), 1f, 1f); }
                        me = this_var;
                        meInt = getActorFromNameplate(this_var);
                    }

                    if ((role[(int)pluginInterface.ClientState.LocalPlayer.ClassJob.GameData.Role] > 2)) scaled *= 2;
                    scaleIcon(Marshal.ReadIntPtr(this_var + 24), scaled, scaled);


                    if (!showName) name = "";
                    if (!showtitle) title = "";
                    if (!showFC) fcName = "";

                    var x = setNamePlateHook.Original(this_var, isPrefixTitle, displayTitle, title, name, fcName, ClassIcon((int)pluginInterface.ClientState.LocalPlayer.ClassJob.Id, role[(int)pluginInterface.ClientState.LocalPlayer.ClassJob.GameData.Role]));
                    AdjustIconPos(this_var);
                    return x;
                }
            }
            
            return setNamePlateHook.Original(this_var, isPrefixTitle, displayTitle, title, name, fcName, iconId);
        }


        public unsafe int getActorFromNameplate(IntPtr this_var)
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
                    var npInfo = (NamePlateInfo*)(RaptureAtkModule->NamePlateInfo) + npIndex;

                    //PluginLog.Log($"SetNamePlate thisptr {(long)npObjPtr:X} index {npIndex} npinfo ptr {(long)npInfo:X} actorID {npInfo->ActorID:X}");
                    return npInfo->ActorID;
                }
            }

            return 0;
        }


        public void SaveConfig()
        {
            Configuration.Enabled = enabled;
            Configuration.Role = role;
            Configuration.Scale = scaler;
            Configuration.XAdjust = xAdjust;
            Configuration.YAdjust = yAdjust;
            this.pluginInterface.SavePluginConfig(Configuration);
        }

        public void Dispose()
        {
            pluginInterface.CommandManager.RemoveHandler("/jicons");
            this.pluginInterface.UiBuilder.OnBuildUi -= DrawWindow;
            this.pluginInterface.UiBuilder.OnOpenConfigUi -= ConfigWindow;
            setNamePlateHook.Disable();
            setNamePlateHook.Dispose();
        }

        private void Command(string command, string arguments)
        {
            config = !config;
        }

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
            if (set == 3) //grey
            {
                int num = 091021;
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
            if (set == 4) //black
            {
                int num = 091521;
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
            if (set == 5) //goldish
            {
                int num = 092021;
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
            if (set == 6) //orange
            {
                int num = 092521;
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
            if (set == 7) //red
            {
                int num = 093021;
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
            if (set == 8) //purple
            {
                int num = 093521;
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
            if (set == 9) //blue
            {
                int num = 094021;
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
            if (set == 10) //green
            {
                int num = 094521;
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

        private void ConfigWindow(object Sender, EventArgs args)
        {
            config = true;
        }

        private void DrawWindow()
        {
            if (config)
            {

                ImGui.SetNextWindowSize(new Num.Vector2(500, 500), ImGuiCond.FirstUseEver);
                ImGui.Begin("Config", ref config);

                if (ImGui.Button("Help"))
                {
                    help = true;
                }


                if (ImGui.Button("Debug Mode"))
                {
                    if (debug)
                    {
                        if (me != IntPtr.Zero)
                        {
                            scaleIcon(Marshal.ReadIntPtr(me + 24), 1f, 1f);
                            me = IntPtr.Zero;
                        }
                    }

                    debug = !debug;
                }

                ImGui.SameLine();
                ImGui.Text(debug.ToString());

                ImGui.Checkbox("Enable", ref enabled);
                ImGui.InputFloat("Scale", ref scaler);
                ImGui.InputInt("X Adjust", ref xAdjust);
                ImGui.InputInt("Y Adjust", ref yAdjust);

                //ImGui.Checkbox("Show Name", ref showName);
                //ImGui.Checkbox("Show Title", ref showtitle);
                //ImGui.Checkbox("Show FC", ref showFC);

                if (ImGui.BeginCombo("Tank Icon Set", setNames[role[1]]))
                {
                    for (int i = 0; i < setNames.Length; i++)
                    {
                        if (ImGui.Selectable(setNames[i]))
                        {
                            role[1] = i;
                        }
                    }
                    ImGui.EndCombo();
                }


                if (ImGui.BeginCombo("Heal Icon Set", setNames[role[4]]))
                {
                    for (int i = 0; i < setNames.Length; i++)
                    {
                        if (ImGui.Selectable(setNames[i]))
                        {
                            role[4] = i;
                        }
                    }
                    ImGui.EndCombo();
                }


                if (ImGui.BeginCombo("DPS Icon Set", setNames[role[2]]))
                {
                    for (int i = 0; i < setNames.Length; i++)
                    {
                        if (ImGui.Selectable(setNames[i]))
                        {
                            role[2] = i;
                            role[3] = i;
                        }
                    }
                    ImGui.EndCombo();
                }

                if (ImGui.Button("Save and Close Config"))
                {
                    SaveConfig();

                    config = false;
                }
                ImGui.End();
            }

            if (help)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(500, 500), ImGuiCond.FirstUseEver);
                ImGui.Begin("Help", ref help);
                ImGui.TextWrapped("Hi! Thanks for trying out my plugin. It's mostly done, but can always use improvement. Here's a quick guide to how the config works.");
                ImGui.TextWrapped("- The scale is 1.0 for 'default', 2.0 for double etc.");
                ImGui.TextWrapped("- X and Y Adjust to make the icon appear where you want, relative to the player.");
                ImGui.TextWrapped("- The icons only apply to party members.");
                ImGui.TextWrapped("- Make sure to save to keep your changes!");
                ImGui.TextWrapped("- If there is a problem, let me know on the discords.");
                ImGui.End();
            }

            if (enabled)
            {
                if (this.pluginInterface.ClientState.LocalPlayer != null)
                {
                    if (countdown == 0)
                    {

                        List<string> cleanup = new List<string>();

                        for (var k = 0; k < this.pluginInterface.ClientState.Actors.Length; k++)
                        {
                            var actor = this.pluginInterface.ClientState.Actors[k];

                            if (actor == null)
                                continue;


                            if (InParty(actor))
                            {
                                try
                                {
                                    bool exists = false;
                                    foreach (PartyMem pm in partyList)
                                    {
                                        if (pm.PC.Name == actor.Name) exists = true;
                                        cleanup.Add(actor.Name);
                                    }
                                    if (exists) continue;

                                    var pc = (Dalamud.Game.ClientState.Actors.Types.PlayerCharacter)actor;
                                    partyList.Add(new PartyMem(pc, IntPtr.Zero));
                                    cleanup.Add(actor.Name);

                                }
                                catch (Exception e)
                                {
                                    //PluginLog.LogError(e.ToString()); -- Noise spam, abusing use as filter.
                                }

                            }

                        }

                        for (int i = 0; i < partyList.Count; i++)
                        {
                            bool yes = false;
                            foreach (string cleaner in cleanup)
                            {
                                if (partyList[i].PC.Name == cleaner)
                                {
                                    yes = true;
                                }
                            }
                            if (!yes)
                            {
                                if (partyList[i].NamePlatePtr != IntPtr.Zero) { scaleIcon(Marshal.ReadIntPtr(partyList[i].NamePlatePtr + 24), 1f, 1f); }
                                partyList.RemoveAt(i);
                                i--;
                            }
                        }
                        countdown = 50;
                    }

                    countdown--;
                }

                int[] partyIDs = new int[partyList.Count];
                for (int i = 0; i < partyList.Count; i++)
                {
                    partyIDs[i] = partyList[i].PC.ActorId;
                }
                foreach (PartyMem pm in partyList)
                {
                    if (pm.NamePlatePtr != IntPtr.Zero)
                    {
                        if (partyIDs.Contains(getActorFromNameplate(pm.NamePlatePtr)))
                        {
                            continue;
                        }
                        else
                        {
                            scaleIcon(Marshal.ReadIntPtr(pm.NamePlatePtr + 24), 1f, 1f);
                            pm.NamePlatePtr = IntPtr.Zero;
                        }

                    }
                }
            }
            else
            {
                if (partyList.Count > 0)
                {
                    for (int i = 0; i < partyList.Count; i++)
                    {
                        if (partyList[i].NamePlatePtr != IntPtr.Zero) { scaleIcon(Marshal.ReadIntPtr(partyList[i].NamePlatePtr + 24), 1f, 1f); }
                    }
                    partyList = new List<PartyMem>();
                }
            }
        }


        private bool InParty(Dalamud.Game.ClientState.Actors.Types.Actor actor)
        {
            return (GetStatus(actor) & 16) > 0;
        }
        private static byte GetStatus(Dalamud.Game.ClientState.Actors.Types.Actor actor)
        {
            IntPtr statusPtr = actor.Address + 0x1906;
            return Marshal.ReadByte(statusPtr);
        }
    }

    public class PartyMem
    {
        public IntPtr NamePlatePtr { get; set; } = IntPtr.Zero;
        public Dalamud.Game.ClientState.Actors.Types.PlayerCharacter PC { get; set; }
        public PartyMem(Dalamud.Game.ClientState.Actors.Types.PlayerCharacter pc, IntPtr intptr)
        {
            PC = pc;
            NamePlatePtr = intptr;
        }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public float Scale { get; set; } = 1f;
        public int[] Role { get; set; } = { 0, 0, 0, 0, 0};
        public int XAdjust { get; set; } = -13;
        public int YAdjust { get; set; } = 55;
    }
}
