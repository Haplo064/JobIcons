using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;

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

            plugin.Interface.UiBuilder.OnOpenConfigUi += OnOpenConfigUi;
            plugin.Interface.UiBuilder.OnBuildUi += OnBuildUi;
        }

        public void Dispose()
        {
            plugin.Interface.UiBuilder.OnBuildUi -= OnBuildUi;
            plugin.Interface.UiBuilder.OnOpenConfigUi -= OnOpenConfigUi;
        }

        public void ToggleConfigWindow()
        {
            isImguiConfigOpen = !isImguiConfigOpen;
        }

        private JobIconsConfiguration Configuration => plugin.Configuration;

        private void SaveConfiguration() => plugin.SaveConfiguration();

        private void OnOpenConfigUi(object sender, EventArgs evt) => isImguiConfigOpen = true;

        private unsafe void OnBuildUi()
        {
            OnBuildUi_Config();
            OnBuildUi_Debug();
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

                if (updateRequired)
                {
                    SaveConfiguration();
                    UpdateNamePlates();
                }

                ImGui.End();
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

                var scale = Configuration.Scale;
                if (ImGui.InputFloat("Scale", ref scale, .1f))
                {
                    if (scale < 0) scale = 0;
                    Configuration.Scale = scale;
                    updateRequired = true;
                }

                int xAdjust = Configuration.XAdjust;
                if (ImGui.InputInt("X Adjust", ref xAdjust))
                {
                    Configuration.XAdjust = (short)xAdjust;
                    updateRequired = true;
                }

                int yAdjust = Configuration.YAdjust;
                if (ImGui.InputInt("Y Adjust", ref yAdjust))
                {
                    Configuration.YAdjust = (short)yAdjust;
                    updateRequired = true;
                }

                var showName = Configuration.ShowName;
                if (ImGui.Checkbox("Show Name", ref showName))
                {
                    if (!showName)
                        Configuration.ShowTitle = false;
                    Configuration.ShowName = showName;
                    updateRequired = true;
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
                ImGui.TextWrapped("Hi! Thanks for trying out my plugin. It's mostly done, but can always use improvement. Here's a quick guide to how the config works.");
                ImGui.TextWrapped("- The scale is 1.0 for 'default', 2.0 for double etc.");
                ImGui.TextWrapped("- Use X and Y Adjust to make the icon appear where you want, relative to the player.");
                ImGui.TextWrapped("- The icons only apply to party members.");
                ImGui.TextWrapped("- If there is a problem, let me (Haplo) know on Discord.");
                ImGui.EndTabItem();
            }
        }

#if DEBUG

        private bool isImguiDebugOpen = false;

        private unsafe void OnBuildUi_Debug()
        {
            if (isImguiDebugOpen)
            {
                ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.FirstUseEver);
                if (ImGui.Begin("JobIcons Debug", ref isImguiDebugOpen))
                {
                    ImGui.PushItemWidth(-1);

                    if (ImGui.CollapsingHeader("PartyMembers"))
                    {
                        var headers = new string[] { "Address", "ActorID", "Name", "IsLocalPlayer", "IsParty1", "IsParty2" };
                        var sizes = new float[headers.Length];

                        ImGui.Columns(headers.Length);
                        for (int i = 0; i < headers.Length; i++)
                        {
                            DebugTableCell(headers[i], sizes);
                        }

                        ImGui.Separator();

                        foreach (var actor in plugin.Interface.ClientState.Actors)
                        {
                            var isLocalPlayer = plugin.IsLocalPlayer(actor.ActorId);
                            var isParty1 = plugin.IsPartyMember1(actor);
                            var isParty2 = plugin.IsPartyMember2(actor.ActorId);
                            if (isLocalPlayer || isParty1 || isParty2)
                            {
                                DebugTableCell($"0x{actor.Address.ToInt64():X}", sizes);
                                DebugTableCell(actor.ActorId.ToString(), sizes);
                                DebugTableCell(actor.Name, sizes);
                                DebugTableCell(isLocalPlayer.ToString(), sizes);
                                DebugTableCell(isParty1.ToString(), sizes);
                                DebugTableCell(isParty2.ToString(), sizes);
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
                        var addonPtr = plugin.AddonNamePlatePtr;
                        if (addonPtr == IntPtr.Zero)
                        {
                            ImGui.Text("Addon not available");
                            ImGui.NewLine();
                        }
                        else
                        {
                            var addon = (AddonNamePlate*)addonPtr;
                            var namePlateObjectArray = addon->NamePlateObjectArray;
                            if (namePlateObjectArray == null)
                            {
                                ImGui.Text("NamePlateArray not available");
                                ImGui.NewLine();
                            }
                            else
                            {
                                var headers = new string[] { "Index", "npObj", "Visible", "Layer", "XAdjust", "YAdjust", "npInfo", "ActorID", "Name", "PrefixTitle", "Title", "FcName" };
                                var sizes = new float[headers.Length];

                                ImGui.Columns(headers.Length);

                                for (int i = 0; i < headers.Length; i++)
                                {
                                    DebugTableCell(headers[i], sizes);
                                }

                                ImGui.Separator();

                                for (int i = 0; i < 50; i++)
                                {
                                    var namePlateObject = &namePlateObjectArray[i];
                                    DebugTableCell(i.ToString(), sizes);
                                    DebugTableCell($"0x{(long)namePlateObject:X}", sizes);
                                    DebugTableCell(namePlateObject->ComponentNode->AtkResNode.IsVisible.ToString(), sizes);
                                    DebugTableCell(namePlateObject->Layer.ToString(), sizes);
                                    DebugTableCell(namePlateObject->IconXAdjust.ToString(), sizes);
                                    DebugTableCell(namePlateObject->IconYAdjust.ToString(), sizes);

                                    var namePlateInfo = plugin.GetNamePlateInfo(i);
                                    DebugTableCell($"0x{(long)namePlateInfo:X}", sizes);
                                    DebugTableCell(namePlateInfo->ActorID.ToString(), sizes);
                                    DebugTableCell(Marshal.PtrToStringAnsi(new IntPtr(namePlateInfo->Name.StringPtr)), sizes);
                                    DebugTableCell(namePlateInfo->IsPrefixTitle.ToString(), sizes);
                                    DebugTableCell(Marshal.PtrToStringAnsi(new IntPtr(namePlateInfo->Title.StringPtr)), sizes);
                                    DebugTableCell(Marshal.PtrToStringAnsi(new IntPtr(namePlateInfo->FcName.StringPtr)), sizes);
                                }

                                for (int i = 0; i < sizes.Length; i++)
                                {
                                    ImGui.SetColumnWidth(i, sizes[i] + 20);
                                }

                                ImGui.Columns(1);
                                ImGui.NewLine();
                            }
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

        private unsafe void UpdateNamePlates()
        {
            // So this doesn't work quite... exactly. Something else updates the NamePlate 
            // and resizes things which makes this cause objects to jump around.

            if (Configuration.Enabled)
            {
                var partyMembers = plugin.Interface.ClientState.Actors
                    .Where(a => plugin.IsPartyMember1(a) || plugin.IsPartyMember2(a.ActorId))
                    .Select(a => a as Dalamud.Game.ClientState.Actors.Types.PlayerCharacter)
                    .Where(a => a != null).ToArray();
                var partyMemberIDs = partyMembers.Select(pm => pm.ActorId).ToArray();

                var addonPtr = plugin.AddonNamePlatePtr;
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

                    var namePlateInfo = plugin.GetNamePlateInfo(i);
                    if (namePlateInfo == null)
                        continue;

                    var actorID = namePlateInfo->ActorID;
                    var isLocalPlayer = plugin.IsLocalPlayer(actorID);
                    var isPartyMember = partyMemberIDs.Contains(actorID);

                    if ((isLocalPlayer) || isPartyMember)
                    {
                        int iconID;
                        if (isLocalPlayer)
                        {
                            if (Configuration.SelfIcon)
                            {
                                var actor = plugin.Interface.ClientState.LocalPlayer;
                                iconID = plugin.GetIconID(actor.ClassJob.Id);
                            }
                            else
                            {
                                iconID = 0;
                            }
                        }
                        else
                        {
                            var actor = partyMembers[Array.IndexOf(partyMemberIDs, actorID)];
                            iconID = plugin.GetIconID(actor.ClassJob.Id);
                        }

                        var namePlateObjectPtr = new IntPtr(namePlateObject);
                        var isPrefixTitle = namePlateInfo->IsPrefixTitle;
                        var displayTitle = true;  // I couldn't find this in the NamePlateInfo, it'll fix itself the next time SetNamePlate is called by the game.
                        var title = Marshal.PtrToStringAnsi(new IntPtr(namePlateInfo->DisplayTitle.StringPtr));
                        var name = Marshal.PtrToStringAnsi(new IntPtr(namePlateInfo->Name.StringPtr));
                        var fcName = Marshal.PtrToStringAnsi(new IntPtr(namePlateInfo->FcName.StringPtr));

                        plugin.SetNamePlateDetour(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, iconID);
                    }
                    else
                    {
                        plugin.AdjustIconScale(namePlateObject, 1.0001f);
                    }
                }
            }
        }
    }
}