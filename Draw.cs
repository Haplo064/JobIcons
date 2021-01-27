using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Num = System.Numerics;
using Dalamud.Plugin;

namespace JobIcons
{
    public class Draw
    {
        public static unsafe void DrawWindow()
        {
            try
            {
                if (Job_Icons.JobIcons.pluginInterface.ClientState.LocalPlayer != null)
                {

                    if (Job_Icons.JobIcons.config)
                    {
                        ImGui.SetNextWindowSize(new Num.Vector2(500, 500), ImGuiCond.FirstUseEver);
                        ImGui.Begin("Config", ref Job_Icons.JobIcons.config);

#if Debug
                if (ImGui.Button("Party"))
                {
                    Job_Icons.JobIcons.help = true;
                }
#endif

                        if (ImGui.Button("Testing Mode"))
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

                        ImGui.Checkbox("Enable", ref Job_Icons.JobIcons.enabled);
                        ImGui.InputFloat("Scale", ref Job_Icons.JobIcons.scaler);
                        ImGui.InputInt("X Adjust", ref Job_Icons.JobIcons.xAdjust);
                        ImGui.InputInt("Y Adjust", ref Job_Icons.JobIcons.yAdjust);

                        ImGui.Checkbox("Show Name", ref Job_Icons.JobIcons.showName);
                        ImGui.Checkbox("Show Title", ref Job_Icons.JobIcons.showtitle);
                        ImGui.Checkbox("Show FC", ref Job_Icons.JobIcons.showFC);

                        if (ImGui.BeginCombo("Tank Icon Set", Job_Icons.JobIcons.setNames[Job_Icons.JobIcons.role[1]]))
                        {
                            for (int i = 0; i < Job_Icons.JobIcons.setNames.Length; i++)
                            {
                                if (ImGui.Selectable(Job_Icons.JobIcons.setNames[i]))
                                {
                                    Job_Icons.JobIcons.role[1] = i;
                                }
                            }

                            ImGui.EndCombo();
                        }

                        if (ImGui.BeginCombo("Heal Icon Set", Job_Icons.JobIcons.setNames[Job_Icons.JobIcons.role[4]]))
                        {
                            for (int i = 0; i < Job_Icons.JobIcons.setNames.Length; i++)
                            {
                                if (ImGui.Selectable(Job_Icons.JobIcons.setNames[i]))
                                {
                                    Job_Icons.JobIcons.role[4] = i;
                                }
                            }

                            ImGui.EndCombo();
                        }

                        if (ImGui.BeginCombo("DPS Icon Set", Job_Icons.JobIcons.setNames[Job_Icons.JobIcons.role[2]]))
                        {
                            for (int i = 0; i < Job_Icons.JobIcons.setNames.Length; i++)
                            {
                                if (ImGui.Selectable(Job_Icons.JobIcons.setNames[i]))
                                {
                                    Job_Icons.JobIcons.role[2] = i;
                                    Job_Icons.JobIcons.role[3] = i;
                                }
                            }

                            ImGui.EndCombo();
                        }

                        if (ImGui.Button("Save and Close Config"))
                        {
                            Job_Icons.JobIcons.SaveConfig();

                            Job_Icons.JobIcons.config = false;
                        }

                        ImGui.End();
                    }

                    if (Job_Icons.JobIcons.help)
                    {
                        ImGui.SetNextWindowSize(new Num.Vector2(500, 500), ImGuiCond.FirstUseEver);
                        ImGui.Begin("Help", ref Job_Icons.JobIcons.help);
                        foreach (int actorId in Job_Icons.JobIcons.partyList)
                        {
                            ImGui.Text(actorId.ToString());
                            ImGui.Text(Job_Icons.JobIcons.GetActorName(actorId));
                            ImGui.Text(Job_Icons.JobIcons.isObjectIDInParty(Job_Icons.JobIcons.groupManager, actorId).ToString());
                        }

                        ImGui.End();
                    }

                    if (Job_Icons.JobIcons.enabled)
                    {
                        if (Job_Icons.JobIcons.pluginInterface.ClientState.LocalPlayer != null)
                        {
                            for (var k = 0; k < Job_Icons.JobIcons.pluginInterface.ClientState.Actors.Length; k++)
                            {
                                var actor = Job_Icons.JobIcons.pluginInterface.ClientState.Actors[k];

                                if (actor == null) continue;

                                if (Job_Icons.JobIcons.InParty(actor))
                                {
                                    try
                                    {
                                        if (!Job_Icons.JobIcons.partyList.Contains(actor.ActorId))
                                        {
                                            if (Job_Icons.JobIcons.pluginInterface.ClientState.Actors[k] is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter pc) Job_Icons.JobIcons.partyList.Add(pc.ActorId);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        // PluginLog.LogError(e.ToString()); -- Noise spam, abusing use as filter for non PCs.
                                    }
                                }
                            }

                            for (int i = 0; i < Job_Icons.JobIcons.partyList.Count; i++)
                            {
                                if (Job_Icons.JobIcons.isObjectIDInParty(Job_Icons.JobIcons.groupManager, Job_Icons.JobIcons.partyList[i]) == 0)
                                {
                                    Job_Icons.JobIcons.partyList.RemoveAt(i);
                                    i--;
                                }
                            }

                            if ((int)Job_Icons.JobIcons.npObjArray != 0)
                            {
                                Job_Icons.JobIcons.NPObjects = new List<IntPtr>();
                                for (int i = 0; i < 50; i++)
                                {
                                    Job_Icons.JobIcons.NPObjects.Add((IntPtr)Job_Icons.JobIcons.npObjArray + (0x70 * i));
                                }

                                foreach (IntPtr x in Job_Icons.JobIcons.NPObjects)
                                {
                                    var test = (AddonNamePlate.BakePlateRenderer.NamePlateObject*)x;
                                    if (!Job_Icons.JobIcons.partyList.Contains(Job_Icons.JobIcons.GetActorFromNameplate(x)) && !Job_Icons.JobIcons.debug)
                                    {
                                        Job_Icons.JobIcons.scaleIcon(Marshal.ReadIntPtr(x + 24), 1.0001f, 1.0001f);
                                        test->ImageNode1->AtkResNode.ScaleY = 1f;
                                        test->ImageNode1->AtkResNode.ScaleX = 1f;
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                PluginLog.Log(e.ToString());
            }
        }
    }
}