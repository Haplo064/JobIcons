using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Num = System.Numerics;

namespace JobIcons
{
    public class Draw
    {
        public static unsafe void DrawWindow()
        {
            if (Job_Icons.JobIconsPlugin.config)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(500, 500), ImGuiCond.FirstUseEver);
                ImGui.Begin("Config", ref Job_Icons.JobIconsPlugin.config);

#if Debug                
                if (ImGui.Button("Party"))
                {
                    Job_Icons.JobIcons.help = true;
                }


                if (ImGui.Button("Debug Mode"))
                {
                    if (Job_Icons.JobIcons.debug)
                    {
                        if (Job_Icons.JobIcons.me != IntPtr.Zero)
                        {
                            Job_Icons.JobIcons.scaleIcon(Marshal.ReadIntPtr(Job_Icons.JobIcons.me + 24), 1f, 1f);
                            Job_Icons.JobIcons.me = IntPtr.Zero;
                        }
                    }

                    Job_Icons.JobIcons.debug = !Job_Icons.JobIcons.debug;
                }
                ImGui.SameLine();
                ImGui.Text(Job_Icons.JobIcons.debug.ToString());
#endif                
                ImGui.Checkbox("Enable", ref Job_Icons.JobIconsPlugin.enabled);
                ImGui.InputFloat("Scale", ref Job_Icons.JobIconsPlugin.scaler);
                ImGui.InputInt("X Adjust", ref Job_Icons.JobIconsPlugin.xAdjust);
                ImGui.InputInt("Y Adjust", ref Job_Icons.JobIconsPlugin.yAdjust);

                ImGui.Checkbox("Show Name", ref Job_Icons.JobIconsPlugin.showName);
                ImGui.Checkbox("Show Title", ref Job_Icons.JobIconsPlugin.showtitle);
                ImGui.Checkbox("Show FC", ref Job_Icons.JobIconsPlugin.showFC);

                if (ImGui.BeginCombo("Tank Icon Set", Job_Icons.JobIconsPlugin.setNames[Job_Icons.JobIconsPlugin.role[1]]))
                {
                    for (int i = 0; i < Job_Icons.JobIconsPlugin.setNames.Length; i++)
                    {
                        if (ImGui.Selectable(Job_Icons.JobIconsPlugin.setNames[i]))
                        {
                            Job_Icons.JobIconsPlugin.role[1] = i;
                        }
                    }

                    ImGui.EndCombo();
                }

                if (ImGui.BeginCombo("Heal Icon Set", Job_Icons.JobIconsPlugin.setNames[Job_Icons.JobIconsPlugin.role[4]]))
                {
                    for (int i = 0; i < Job_Icons.JobIconsPlugin.setNames.Length; i++)
                    {
                        if (ImGui.Selectable(Job_Icons.JobIconsPlugin.setNames[i]))
                        {
                            Job_Icons.JobIconsPlugin.role[4] = i;
                        }
                    }

                    ImGui.EndCombo();
                }

                if (ImGui.BeginCombo("DPS Icon Set", Job_Icons.JobIconsPlugin.setNames[Job_Icons.JobIconsPlugin.role[2]]))
                {
                    for (int i = 0; i < Job_Icons.JobIconsPlugin.setNames.Length; i++)
                    {
                        if (ImGui.Selectable(Job_Icons.JobIconsPlugin.setNames[i]))
                        {
                            Job_Icons.JobIconsPlugin.role[2] = i;
                            Job_Icons.JobIconsPlugin.role[3] = i;
                        }
                    }

                    ImGui.EndCombo();
                }

                if (ImGui.Button("Save and Close Config"))
                {
                    Job_Icons.JobIconsPlugin.SaveConfig();

                    Job_Icons.JobIconsPlugin.config = false;
                }

                ImGui.End();
            }

            if (Job_Icons.JobIconsPlugin.help)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(500, 500), ImGuiCond.FirstUseEver);
                ImGui.Begin("Help", ref Job_Icons.JobIconsPlugin.help);
                foreach (int actorId in Job_Icons.JobIconsPlugin.partyList)
                {
                    ImGui.Text(actorId.ToString());
                    ImGui.Text(Job_Icons.JobIconsPlugin.GetActorName(actorId));
                    ImGui.Text(Job_Icons.JobIconsPlugin.isObjectIDInParty(Job_Icons.JobIconsPlugin.groupManager, actorId).ToString());
                }

                ImGui.End();
            }

            if (Job_Icons.JobIconsPlugin.enabled)
            {
                if (Job_Icons.JobIconsPlugin.pluginInterface.ClientState.LocalPlayer != null)
                {
                    for (var k = 0; k < Job_Icons.JobIconsPlugin.pluginInterface.ClientState.Actors.Length; k++)
                    {
                        var actor = Job_Icons.JobIconsPlugin.pluginInterface.ClientState.Actors[k];

                        if (actor == null) continue;

                        if (Job_Icons.JobIconsPlugin.InParty(actor))
                        {
                            try
                            {
                                if (!Job_Icons.JobIconsPlugin.partyList.Contains(actor.ActorId))
                                {
                                    if (Job_Icons.JobIconsPlugin.pluginInterface.ClientState.Actors[k] is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter pc) Job_Icons.JobIconsPlugin.partyList.Add(pc.ActorId);
                                }
                            }
                            catch (Exception e)
                            {
                                // PluginLog.LogError(e.ToString()); -- Noise spam, abusing use as filter for non PCs.
                            }
                        }
                    }

                    for (int i = 0; i < Job_Icons.JobIconsPlugin.partyList.Count; i++)
                    {
                        if (Job_Icons.JobIconsPlugin.isObjectIDInParty(Job_Icons.JobIconsPlugin.groupManager, Job_Icons.JobIconsPlugin.partyList[i]) == 0)
                        {
                            Job_Icons.JobIconsPlugin.partyList.RemoveAt(i);
                            i--;
                        }
                    }

                    if ((int)Job_Icons.JobIconsPlugin.npObjArray != 0)
                    {
                        Job_Icons.JobIconsPlugin.NPObjects = new List<IntPtr>();
                        for (int i = 0; i < 50; i++)
                        {
                            Job_Icons.JobIconsPlugin.NPObjects.Add((IntPtr)Job_Icons.JobIconsPlugin.npObjArray + (0x70 * i));
                        }

                        foreach (IntPtr x in Job_Icons.JobIconsPlugin.NPObjects)
                        {
                            var test = (AddonNamePlate.BakePlateRenderer.NamePlateObject*)x;
                            if (!Job_Icons.JobIconsPlugin.partyList.Contains(Job_Icons.JobIconsPlugin.GetActorFromNameplate(x)))
                            {
                                Job_Icons.JobIconsPlugin.scaleIcon(Marshal.ReadIntPtr(x + 24), 1.0001f, 1.0001f);
                                test->ImageNode1->AtkResNode.ScaleY = 1f;
                                test->ImageNode1->AtkResNode.ScaleX = 1f;
                            }
                        }
                    }
                }
            }
        }
    }
}