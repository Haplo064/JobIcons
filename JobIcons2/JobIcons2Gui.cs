using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Numerics;

namespace JobIcons2
{
    internal class JobIcons2Gui : IDisposable
    {
        private readonly JobIcons2Plugin _plugin;

#if DEBUG
        private bool _isImguiConfigOpen = true;
#else
        private bool _isImguiConfigOpen;
#endif

        public JobIcons2Gui(JobIcons2Plugin plugin)
        {
            _plugin = plugin;
            plugin.Interface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
            plugin.Interface.UiBuilder.Draw += OnBuildUi;
        }

        public void Dispose()
        {
            _plugin.Interface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
            _plugin.Interface.UiBuilder.Draw -= OnBuildUi;
        }

        public void ToggleConfigWindow()
        {
            _isImguiConfigOpen = !_isImguiConfigOpen;
        }

        private JobIcons2Configuration Configuration => _plugin.Configuration;

        private void SaveConfiguration() => _plugin.SaveConfiguration();

        private void OnOpenConfigUi() => _isImguiConfigOpen = true;

        private void OnBuildUi()
        {
            OnBuildUi_Config();
        }

        private void OnBuildUi_Config()
        {
            if (_isImguiConfigOpen)
            {
                var updateRequired = false;

                ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.FirstUseEver);
                if (ImGui.Begin("JobIcons2 Config", ref _isImguiConfigOpen))
                {

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

                ImGui.Indent(25 * ImGuiHelpers.GlobalScale);

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

                ImGui.Indent(-25 * ImGuiHelpers.GlobalScale);

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

        private static bool OnBuildUi_EditCustomIconSetTab(string label, int[] customIconSet)
        {
            var updateRequired = false;

            if (!ImGui.BeginTabItem(label)) return false;
            ImGui.PushItemWidth(100);

            var roles = (JobRole[])Enum.GetValues(typeof(JobRole));
            foreach (var role in roles)
            {
                var jobs = role.GetJobs();
                foreach (var job in jobs)
                {
                    var customJobIndex = (int)job - 1;
                    var iconId = customIconSet[customJobIndex];
                    if (!ImGui.InputInt(job.ToString(), ref iconId, 1, 100)) continue;
                        
                    if (iconId < 0) iconId = 0;
                    customIconSet[customJobIndex] = iconId;
                    updateRequired = true;
                }
                ImGui.Separator();
            }

            ImGui.PopItemWidth();
            ImGui.EndTabItem();

            return updateRequired;
        }

        private static void OnBuildUi_AboutTab()
        {
            if (!ImGui.BeginTabItem("About")) return;
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

                    var actorId = npInfo.Data.ObjectID.ObjectID;
                    if (actorId == 0xE0000000)
                        continue;

                    if (!npInfo.IsPlayerCharacter())  // Only PlayerCharacters can have icons
                        continue;

                    var jobId = npInfo.GetJobId();
                    if (jobId < 1 || jobId >= Enum.GetValues(typeof(Job)).Length)
                        continue;

                    var isLocalPlayer = XivApi.IsLocalPlayer(actorId);
                    var isPartyMember = XivApi.IsLocalPlayer(actorId);
                    var isAllianceMember = XivApi.IsAllianceMember(actorId);

                    var updateLocalPlayer = Configuration.SelfIcon && isLocalPlayer;
                    var updatePartyMember = Configuration.PartyIcons && isPartyMember;
                    var updateAllianceMember = Configuration.AllianceIcons && isAllianceMember;
                    var updateEveryoneElse = Configuration.EveryoneElseIcons && !isLocalPlayer && !isPartyMember && !isAllianceMember;

                    if (!updateLocalPlayer && !updatePartyMember && !updateAllianceMember && !updateEveryoneElse) continue;
                    
                    var iconSet = Configuration.GetIconSet(jobId);
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