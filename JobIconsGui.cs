using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace JobIcons
{
    internal class JobIconsGui : IDisposable
    {
        private readonly JobIconsPlugin plugin;

#if DEBUG
        private bool isImguiConfigOpen = true;
#else
        private bool isImguiConfigOpen = false;
#endif

        public JobIconsGui(JobIconsPlugin plugin)
        {
            this.plugin = plugin;
            plugin.Interface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
            plugin.Interface.UiBuilder.Draw += OnBuildUi;
        }

        public void Dispose()
        {
            plugin.Interface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
            plugin.Interface.UiBuilder.Draw -= OnBuildUi;
        }

        public void ToggleConfigWindow()
        {
            isImguiConfigOpen = !isImguiConfigOpen;
        }

        private JobIconsConfiguration Configuration => plugin.Configuration;

        private void SaveConfiguration() => plugin.SaveConfiguration();

        private void OnOpenConfigUi() => isImguiConfigOpen = true;

        private unsafe void OnBuildUi()
        {
            OnBuildUi_Config();
#if DEBUG
            OnBuildUi_Debug();
#endif
        }

        private void OnBuildUi_Config()
        {
            if (isImguiConfigOpen)
            {
                var updateRequired = false;

                ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.FirstUseEver);
                if (ImGui.Begin("JobIcons Config", ref isImguiConfigOpen))
                {
#if DEBUG
                    if (ImGui.Button("Debug"))
                        isImguiDebugOpen = !isImguiDebugOpen;
#endif

                    if (ImGui.BeginTabBar("MainConfig"))
                    {
                        updateRequired |= OnBuildUi_SettingsTab();
                        updateRequired |= OnBuildUi_EditCustomIconSetTab("Custom Icon Set 1", Configuration.CustomIconSet1);
                        updateRequired |= OnBuildUi_EditCustomIconSetTab("Custom Icon Set 2", Configuration.CustomIconSet2);
                        OnBuildUi_AboutTab();

                        ImGui.EndTabBar();
                    }
                }

                ImGui.End();

                if (updateRequired)
                {
                    SaveConfiguration();
                    UpdateNamePlates();
                }
            }
        }

        private bool OnBuildUi_SettingsTab()
        {
            bool updateRequired = false;

            if (ImGui.BeginTabItem("Settings"))
            {
                var enabled = Configuration.Enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                {
                    Configuration.Enabled = enabled;
                    updateRequired = true;
                }

                ImGui.TextWrapped($"Changing these settings does not immediately reflect the actual position the icon may end up at. Move around a bit until the game updates your NamePlate.");

                var selfIcon = Configuration.SelfIcon;
                if (ImGui.Checkbox("Self Icon", ref selfIcon))
                {
                    Configuration.SelfIcon = selfIcon;
                    updateRequired = true;
                }

                var partyIcons = Configuration.PartyIcons;
                if (ImGui.Checkbox("Party Icons", ref partyIcons))
                {
                    Configuration.PartyIcons = partyIcons;
                    updateRequired = true;
                }

                var allianceIcons = Configuration.AllianceIcons;
                if (ImGui.Checkbox("Alliance Icons", ref allianceIcons))
                {
                    Configuration.AllianceIcons = allianceIcons;
                    updateRequired = true;
                }

                var everyoneIcons = Configuration.EveryoneElseIcons;
                if (ImGui.Checkbox("Everyone Else Icons", ref everyoneIcons))
                {
                    Configuration.EveryoneElseIcons = everyoneIcons;
                    updateRequired = true;
                }

                var scale = Configuration.Scale;
                if (ImGui.InputFloat("Scale", ref scale, .1f))
                {
                    if (scale < 0) scale = 0;
                    Configuration.Scale = scale;
                    updateRequired = true;
                }

                var locationAdjust = Configuration.LocationAdjust;
                if (ImGui.Checkbox("Adjust JobIcon Location", ref locationAdjust)) {
                    Configuration.LocationAdjust = locationAdjust;
                    updateRequired               = true;
                }

                if (locationAdjust) {

                    int xAdjust = Configuration.XAdjust;
                    if (ImGui.InputInt("X Adjust", ref xAdjust)) {
                        Configuration.XAdjust = (short)xAdjust;
                        updateRequired        = true;
                    }

                    int yAdjust = Configuration.YAdjust;
                    if (ImGui.InputInt("Y Adjust", ref yAdjust)) {
                        Configuration.YAdjust = (short)yAdjust;
                        updateRequired        = true;
                    }
                }

                var showIcon = Configuration.ShowIcon;
                if (ImGui.Checkbox("Show JobIcon", ref showIcon)) {
                    Configuration.ShowIcon = showIcon;
                    updateRequired         = true;
                }

                var showName = Configuration.ShowName;
                if (ImGui.Checkbox("Show Name", ref showName))
                {
                    if (!showName)
                        Configuration.ShowTitle = false;
                    Configuration.ShowName = showName;
                    updateRequired = true;
                }

                var jobName = Configuration.JobName;
                if (ImGui.Checkbox("Replace Name by Job (Requires Name)", ref jobName)) {
                    if (!jobName)
                        Configuration.ShowName = true;
                    Configuration.JobName = jobName;
                    updateRequired        = true;
                }

                var showTitle = Configuration.ShowTitle;
                if (ImGui.Checkbox("Show Title (Requires Name)", ref showTitle))
                {
                    if (showTitle)
                        Configuration.ShowName = true;
                    Configuration.ShowTitle = showTitle;
                    updateRequired = true;
                }

                var showFcName = Configuration.ShowFcName;
                if (ImGui.Checkbox("Show FC", ref showFcName))
                {
                    Configuration.ShowFcName = showFcName;
                    updateRequired = true;
                }

                updateRequired |= OnBuildUi_IconSetChoice("Tank Icon Set", Configuration.TankIconSetName, (name) => Configuration.TankIconSetName = name);
                updateRequired |= OnBuildUi_IconSetChoice("Heal Icon Set", Configuration.HealIconSetName, (name) => Configuration.HealIconSetName = name);
                updateRequired |= OnBuildUi_IconSetChoice("Melee Icon Set", Configuration.MeleeIconSetName, (name) => Configuration.MeleeIconSetName = name);
                updateRequired |= OnBuildUi_IconSetChoice("Ranged Icon Set", Configuration.RangedIconSetName, (name) => Configuration.RangedIconSetName = name);
                updateRequired |= OnBuildUi_IconSetChoice("Magical Icon Set", Configuration.MagicalIconSetName, (name) => Configuration.MagicalIconSetName = name);
                updateRequired |= OnBuildUi_IconSetChoice("Crafting Icon Set", Configuration.CraftingIconSetName, (name) => Configuration.CraftingIconSetName = name);
                updateRequired |= OnBuildUi_IconSetChoice("Gathering Icon Set", Configuration.GatheringIconSetName, (name) => Configuration.GatheringIconSetName = name);

                ImGui.EndTabItem();
            }

            return updateRequired;
        }

        private bool OnBuildUi_IconSetChoice(string label, string currentName, Action<string> setIconSetName)
        {
            var iconSetNames = IconSet.Names;
            var index = Array.IndexOf(iconSetNames, currentName);
            if (ImGui.Combo(label, ref index, iconSetNames, iconSetNames.Length))
            {
                var newName = iconSetNames[index];
                setIconSetName(newName);
                return true;
            }
            return false;
        }

        private bool OnBuildUi_EditCustomIconSetTab(string label, int[] customIconSet)
        {
            var updateRequired = false;

            if (ImGui.BeginTabItem(label))
            {
                ImGui.PushItemWidth(100);

                var roles = (JobRole[])Enum.GetValues(typeof(JobRole));
                foreach (var role in roles)
                {
                    var jobs = role.GetJobs();
                    for (int i = 0; i < jobs.Length; i++)
                    {
                        var job = jobs[i];
                        var customJobIndex = (int)job - 1;
                        var iconID = customIconSet[customJobIndex];
                        if (ImGui.InputInt(job.ToString(), ref iconID, 1, 100))
                        {
                            if (iconID < 0) iconID = 0;
                            customIconSet[customJobIndex] = iconID;
                            updateRequired = true;
                        }
                    }
                    ImGui.Separator();
                }

                ImGui.PopItemWidth();
                ImGui.EndTabItem();
            }

            return updateRequired;
        }

        private void OnBuildUi_AboutTab()
        {
            if (ImGui.BeginTabItem("About"))
            {
                //ImGui.TextWrapped(XivApi.RaptureAtkModulePtr.ToInt64().ToString("X"));
                ImGui.TextWrapped("");
                ImGui.TextWrapped("- The scale is 1.0 for 'default', 2.0 for double etc.");
                ImGui.TextWrapped("- Use X and Y Adjust to make the icon appear where you want, relative to the player.");
                ImGui.TextWrapped("- The icons only apply to party members.");
                ImGui.TextWrapped("- If there is a problem, let me (Haplo) know on Discord.");
                ImGui.TextWrapped("\nShoutouts:");
                ImGui.TextWrapped("Big shoutout to daemitus for the second re-write of the code. Without them, many features would have been much harder!");
                ImGui.TextWrapped("And shoutouts to aers, adam, caraxi, goat and many others for allowing me to pester them for simple issues.");
                ImGui.EndTabItem();
            }
        }

#if DEBUG

        private bool isImguiDebugOpen = false;

        private void OnBuildUi_Debug()
        {
            if (isImguiDebugOpen)
            {
                ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.FirstUseEver);
                if (ImGui.Begin("JobIcons Debug", ref isImguiDebugOpen))
                {
                    ImGui.PushItemWidth(-1);

                    if (ImGui.CollapsingHeader("PartyMembers"))
                    {
                        var headers = new string[] { "Address", "ActorID", "Name", "IsLocalPlayer", "IsParty", "isPC" };
                        var sizes = new float[headers.Length];

                        ImGui.Columns(headers.Length);
                        for (int i = 0; i < headers.Length; i++)
                        {
                            DebugTableCell(headers[i], sizes);
                        }

                        ImGui.Separator();

                        foreach (var actor in plugin.ObjectTable)
                        {
                            var isLocalPlayer = XivApi.IsLocalPlayer(actor.ObjectId);
                            var isParty = XivApi.IsPartyMember(actor.ObjectId);
                            var isPC = actor is Dalamud.Game.ClientState.Objects.SubKinds.PlayerCharacter;
                            if (isLocalPlayer || isParty)
                            {
                                DebugTableCell($"0x{actor.Address.ToInt64():X}", sizes);
                                DebugTableCell(actor.ObjectId.ToString(), sizes);
                                DebugTableCell(actor.Name.TextValue, sizes);
                                DebugTableCell(isLocalPlayer.ToString(), sizes);
                                DebugTableCell(isParty.ToString(), sizes);
                                DebugTableCell(isPC.ToString(), sizes);
                            }
                        }

                        for (int i = 0; i < sizes.Length; i++)
                        {
                            ImGui.SetColumnWidth(i, sizes[i] + 20);
                        }

                        ImGui.Columns(1);
                        ImGui.NewLine();
                    }

                    if (ImGui.CollapsingHeader("NamePlateObjects"))
                    {
                        var addon = XivApi.GetSafeAddonNamePlate();
                        if (addon.Pointer == IntPtr.Zero)
                        {
                            ImGui.Text("Addon not available");
                            ImGui.NewLine();
                        }
                        else
                        {
                            var headers = new string[] {
                                "Index",
                                "npObj", "Visible", "isLocalPlayer", "Layer", "XAdjust", "YAdjust", "XPos", "YPos", "XScale", "YScale", "Type",
                                "npInfo"
                              , "ActorID", "Name", "isPC", "isParty", "isAlliance", "JobID", "PrefixTitle", "Title", "FcName", "LevelText"
                            };
                            var sizes = new float[headers.Length];

                            ImGui.Columns(headers.Length);

                            for (int i = 0; i < headers.Length; i++)
                            {
                                DebugTableCell(headers[i], sizes);
                            }

                            ImGui.Separator();

                            for (int i = 0; i < 50; i++)
                            {
                                var npObject = addon.GetNamePlateObject(i);
                                if (npObject == null)
                                {
                                    for (int c = 0; c < headers.Length; c++)
                                        DebugTableCell("npObj=null", sizes);
                                    continue;
                                }

                                var npInfo = npObject.NamePlateInfo;
                                if (npInfo == null) {
                                    for (int c = 0; c < headers.Length; c++)
                                        DebugTableCell("npInfo=null", sizes);
                                    continue;
                                }

                                var imageNode = npObject.IconImageNode;
                                string imageX, imageY, scaleX, scaleY;

                                imageX = imageNode.AtkResNode.X.ToString();
                                imageY = imageNode.AtkResNode.Y.ToString();
                                scaleX = imageNode.AtkResNode.ScaleX.ToString();
                                scaleY = imageNode.AtkResNode.ScaleY.ToString();

                                DebugTableCell(i.ToString(), sizes);
                                DebugTableCell($"0x{npObject.Pointer.ToInt64():X}", sizes);
                                DebugTableCell(npObject.IsVisible.ToString(), sizes);
                                DebugTableCell(npObject.IsLocalPlayer.ToString(), sizes);
                                DebugTableCell(npObject.Data.Priority.ToString(), sizes);
                                DebugTableCell(npObject.Data.IconXAdjust.ToString(), sizes);
                                DebugTableCell(npObject.Data.IconYAdjust.ToString(), sizes);
                                DebugTableCell(imageX, sizes);
                                DebugTableCell(imageY, sizes);
                                DebugTableCell(scaleX, sizes);
                                DebugTableCell(scaleY, sizes);
                                DebugTableCell(npObject.Data.NameplateKind.ToString(), sizes);

                                DebugTableCell($"0x{npInfo.Pointer.ToInt64():X}", sizes);
                                //DebugTableCell(npInfo.Data.ActorID.ToString(), sizes);
                                DebugTableCell($"0x{npInfo.Data.ActorID:X}", sizes);
                                DebugTableCell(npInfo.Name, sizes);
                                DebugTableCell(XivApi.IsPlayerCharacter(npInfo.Data.ActorID).ToString(), sizes);
                                DebugTableCell(XivApi.IsPartyMember(npInfo.Data.ActorID).ToString(), sizes);
                                DebugTableCell(XivApi.IsAllianceMember(npInfo.Data.ActorID).ToString(), sizes);
                                DebugTableCell(XivApi.GetJobId(npInfo.Data.ActorID).ToString(), sizes);
                                DebugTableCell(npInfo.Data.IsPrefixTitle.ToString(), sizes);
                                DebugTableCell(npInfo.Title, sizes);
                                DebugTableCell(npInfo.FcName, sizes);
                                DebugTableCell(npInfo.LevelText, sizes);
                            }

                            for (int i = 0; i < sizes.Length; i++)
                            {
                                ImGui.SetColumnWidth(i, sizes[i] + 20);
                            }

                            ImGui.Columns(1);
                            ImGui.NewLine();
                        }
                    }

                    ImGui.PopItemWidth();
                }
                ImGui.End();
            }
        }

        private void DebugTableCell(string value, float[] sizes, bool nextColumn = true)
        {
            var width = ImGui.CalcTextSize(value).X;
            var columnIndex = ImGui.GetColumnIndex();
            var largest = sizes[columnIndex];
            if (width > largest)
                sizes[columnIndex] = width;
            ImGui.Text(value);

            if (nextColumn)
                ImGui.NextColumn();
        }

#endif

        private void UpdateNamePlates()
        {
            // So this doesn't work quite... exactly. Something else updates the NamePlate 
            // and resizes things which makes this cause objects to jump around.

            if (Configuration.Enabled)
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

                    var actorID = npInfo.Data.ActorID;
                    if (actorID == 0xE0000000)
                        continue;

                    if (!npInfo.IsPlayerCharacter())  // Only PlayerCharacters can have icons
                        continue;

                    var jobID = npInfo.GetJobID();
                    if (jobID < 1 || jobID >= Enum.GetValues(typeof(Job)).Length)
                        continue;

                    var isLocalPlayer = XivApi.IsLocalPlayer(actorID);
                    var isPartyMember = XivApi.IsLocalPlayer(actorID);
                    var isAllianceMember = XivApi.IsAllianceMember(actorID);

                    var updateLocalPlayer = Configuration.SelfIcon && isLocalPlayer;
                    var updatePartyMember = Configuration.PartyIcons && isPartyMember;
                    var updateAllianceMember = Configuration.AllianceIcons && isAllianceMember;
                    var updateEveryoneElse = Configuration.EveryoneElseIcons && !isLocalPlayer && !isPartyMember && !isAllianceMember;

                    if (updateLocalPlayer || updatePartyMember || updateAllianceMember || updateEveryoneElse)
                    {
                        var iconSet = Configuration.GetIconSet(jobID);
                        // var iconID = iconSet.GetIconID(jobID);
                        var scaleMult = iconSet.ScaleMultiplier;

                        npObject.SetIconScale(Configuration.Scale * scaleMult);
                        npObject.SetIconPosition(Configuration.XAdjust, Configuration.YAdjust);

                        //var isPrefixTitle = npInfo.Data.IsPrefixTitle;

                        // I couldn't find this in the NamePlateInfo, it'll fix itself the next time SetNamePlate is called by the game.
                        //var displayTitle = true;


                        // plugin.SetNamePlateDetour(npObject.Pointer, isPrefixTitle, displayTitle, npInfo.TitleAddress, npInfo.NameAddress, npInfo.FcNameAddress, iconID);

                        //var title = Encoding.UTF8.GetBytes(Marshal.PtrToStringAnsi(new IntPtr(npi->DisplayTitle.StringPtr)));
                        //var name = Encoding.UTF8.GetBytes(Marshal.PtrToStringAnsi(new IntPtr(npi->Name.StringPtr)));
                        //var fcName = Encoding.UTF8.GetBytes(Marshal.PtrToStringAnsi(new IntPtr(npi->FcName.StringPtr)));

                        //var titlePtr = Marshal.AllocHGlobal(title.Length + 1);
                        //var namePtr = Marshal.AllocHGlobal(name.Length + 1);
                        //var fcNamePtr = Marshal.AllocHGlobal(fcName.Length + 1);

                        //Marshal.Copy(title, 0, titlePtr, title.Length);
                        //Marshal.Copy(name, 0, namePtr, name.Length);
                        //Marshal.Copy(fcName, 0, fcNamePtr, fcName.Length);

                        //Marshal.WriteByte(titlePtr + title.Length, 0);
                        //Marshal.WriteByte(namePtr + name.Length, 0);
                        //Marshal.WriteByte(fcNamePtr + fcName.Length, 0);

                        //plugin.SetNamePlateDetour(npObject.Pointer, isPrefixTitle, displayTitle, titlePtr, namePtr, fcNamePtr, iconID);

                        //Marshal.FreeHGlobal(titlePtr);
                        //Marshal.FreeHGlobal(namePtr);
                        //Marshal.FreeHGlobal(fcNamePtr);
                    }
                }
            }
        }
    }
}