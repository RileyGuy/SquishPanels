﻿using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using FrooxEngine.LogiX;
using FrooxEngine.LogiX.Color;
using FrooxEngine.LogiX.Input;
using BaseX;
using CodeX;
using System;
using System.Reflection;
using System.Collections.Generic;



namespace YourNamespaceHere;
public class ModClass : NeosMod
{
    public override string Author => "Cyro";
    public override string Name => "SquishPanels";
    public override string Version => "2.0.0";

    public static float DopplerLevel = 0.0f;
    public static AudioDistanceSpace DistSpace = AudioDistanceSpace.Global;
    public static string OpenSoundURL = "neosdb:///bbdf36b8f036a5c30f7019d68c1fbdd4032bb1d4c9403bcb926bb21cd0ca3c1a.wav";
    public static string CloseSoundURL = "neosdb:///e600ed8a6895325613b82a50fd2a8ea2ac64151adc5c48c913d33d584fdf75d5.wav";
    public static float TweenSpeed = 0.22f;
    public override void OnEngineInit()
    {
        Harmony harmony = new Harmony("net.Author.ModClass");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(NeosPanel), "OnAttach")]
    public static class NeosPanel_OnAttach_Patch
    {
        public static void PlayOpenSound(NeosPanel __instance)
        {
            StaticAudioClip clip = __instance.World.GetSharedComponentOrCreate<StaticAudioClip>(OpenSoundURL, a => a.URL.Value = new Uri(OpenSoundURL));
            AudioOutput audio = __instance.World.PlayOneShot(__instance.Slot.GlobalPosition, clip, 1f, true, 1f, __instance.Slot, AudioDistanceSpace.Local, false);
            audio.DopplerLevel.Value = DopplerLevel;
            audio.DistanceSpace.Value = DistSpace;
        }

        public static void PlayCloseSound(NeosPanel __instance)
        {
            StaticAudioClip clip = __instance.World.GetSharedComponentOrCreate<StaticAudioClip>(CloseSoundURL, a => a.URL.Value = new Uri(CloseSoundURL));
            AudioOutput audio = __instance.World.PlayOneShot(__instance.Slot.GlobalPosition, clip, 1f, true, 1f, __instance.Slot, AudioDistanceSpace.Local, false);
            audio.DopplerLevel.Value = DopplerLevel;
            audio.DistanceSpace.Value = DistSpace;
        }

        public static void Prefix(NeosPanel __instance)
        {
            float3 Orig = __instance.Slot.LocalScale;

            SyncListElementsEvent<SyncRef<IBounded>> CanvasListener = null;
            
            CanvasListener = (SyncElementList<SyncRef<IBounded>> list, int StartIndex, int count) => {
                __instance.RunInUpdates(0, () => {
                    if (list[StartIndex] == null)
                        return;
                    
                    if (list[StartIndex].Target is Canvas)
                    {
                        Canvas c = list[StartIndex].Target as Canvas;
                        
                        if (c != null)
                        {
                            float2 OrigSize = c.Size.Value;
                            PlayOpenSound(__instance);
                            c.Size.TweenFrom(new float2(OrigSize.x, 0f), TweenSpeed);
                        }
                    }
                    __instance.WhiteList.ElementsAdded -= CanvasListener;
                });
            };
            __instance.RunInUpdates(0, () => {
                if (__instance.WhiteList.Count < 1)
                {
                    float3 OrigSize = __instance.Slot.LocalScale;
                    PlayOpenSound(__instance);
                    __instance.Slot.Scale_Field.TweenFrom(new float3(OrigSize.x, 0f, OrigSize.z), TweenSpeed);
                    __instance.WhiteList.ElementsAdded -= CanvasListener;
                }
            });
            __instance.WhiteList.ElementsAdded += CanvasListener;
        }
    }

    [HarmonyPatch]
    public static class NeosPanel_OnClose_Snapshot
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(NeosPanel), "OnClose")]
        public static void OnClose(object instance, NeosPanel.TitleButton button)
        {
            throw new NotImplementedException();
        }
    }

    [HarmonyPatch(typeof(NeosPanel), "OnClose")]
    public static class NeosPanel_AddCloseButton_Patch
    {
        public static bool Prefix(NeosPanel __instance, NeosPanel.TitleButton button)
        {
            Action OnTweenDoneAction = delegate() { NeosPanel_OnClose_Snapshot.OnClose(__instance, button); };

            if (__instance.WhiteList.Count < 1)
            {
                float3 OrigSize = __instance.Slot.LocalScale;
                NeosPanel_OnAttach_Patch.PlayCloseSound(__instance);
                __instance.Slot.Scale_Field.TweenTo(new float3(OrigSize.x, 0f, OrigSize.z), TweenSpeed, default, null, OnTweenDoneAction);
                return false;
            }

            if (__instance.WhiteList[0] == null)
                return true;

            if (__instance.WhiteList[0] is Canvas)
            {
                Canvas c = __instance.WhiteList[0] as Canvas;

                if (c == null)
                    return true;
                
                NeosPanel_OnAttach_Patch.PlayCloseSound(__instance);
                c.Size.TweenTo(new float2(c.Size.Value.x, 0f), TweenSpeed, default, null, OnTweenDoneAction);
                return false;
            }
            return true;
        }
    }
}