using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TacoVengeance
{
    public class TriggermanIntegrations : MVRScript
    {
        readonly bool logMessages = true;

        delegate void ActionOnPlugin(JSONStorable plugin);

        public override void Init()
        {
            var triggerman = SearchForLocalPluginBySuffix("TriggermanPlugin") as TriggermanPlugin;
            if (triggerman == null)
            {
                LogError("Triggerman plugin not found on this atom; disabling");
                return;
            }

            IntegrateTo("MacGruber.Breathing", plugin => {
                //make breaths less sparse
                plugin.SetFloatParamValue("Rhythm Randomness", 0.02f);
                plugin.SetFloatParamValue("Rhythm Damping", 0.05f);
                plugin.SetFloatParamValue("Audio Variance", 1f);

                triggerman.OnArousalUpdate += (arousal, kissing, sucking, orgasming) =>
                {
                    if   (arousal < 0.05f) plugin.SetFloatParamValue("Intensity", .25f);
                    else                   plugin.SetFloatParamValue("Intensity", Modulate(0.45f, 1f, arousal));
                };

                triggerman.OnOrgasmStart += () => plugin.CallAction("QueueOrgasm");
            });

            IntegrateTo("EasyGaze", plugin => {
                triggerman.OnArousalUpdate += (arousal, kissing, sucking, orgasming) =>
                {
                    //increasing random head movement
                    plugin.SetFloatParamValue("Random head roll timer Maximum",    Modulate(6f,  3f, arousal));
                    plugin.SetFloatParamValue("Random roll Angle Maximum",         Modulate(2f, 25f, arousal));
                    plugin.SetFloatParamValue("Random head turn range Vertical",   Modulate(2f, 25f, arousal));
                    plugin.SetFloatParamValue("Random head turn range Horizontal", Modulate(2f, 25f, arousal));
                };
            });

            IntegrateTo("BreatheStandalone", plugin => {
                triggerman.OnArousalUpdate += (arousal, kissing, sucking, orgasming) =>
                {
                    plugin.SetFloatParamValue("Breath Intensity", Modulate(0.2f,  1f, arousal));
                    plugin.SetFloatParamValue("Chest Movement",   Modulate( 15f, 30f, arousal));
                };
            });

            IntegrateTo("VAMMoan", plugin => {
                triggerman.OnArousalUpdate += (arousal, kissing, sucking, orgasming) =>
                {
                    //NOTE: these are more "delay" than speed", thus here they are decreased with arousal
                    plugin.SetFloatParamValue("Kissing Speed", Modulate(1f, 0.25f, arousal));
                    plugin.SetFloatParamValue("Blowjob Speed", Modulate(1f, 0.75f, arousal));

                    //these take precedence over regular moaning, so while they take place, don't do anything here
                    if (sucking || kissing || orgasming) return;
                    
                    if      (arousal < 0.01f) plugin.CallAction("setVoiceBreathing");
                    else if (arousal < 0.15f) plugin.CallAction("setVoiceIntensity0");
                    else if (arousal < 0.35f) plugin.CallAction("setVoiceIntensity1");
                    else if (arousal < 0.65f) plugin.CallAction("setVoiceIntensity2");
                    else if (arousal < 0.85f) plugin.CallAction("setVoiceIntensity3");
                    else                      plugin.CallAction("setVoiceIntensity4");
                };

                triggerman.OnSuckingStart += () => plugin.CallAction("setVoiceBlowjob");
                triggerman.OnKissingStart += () => plugin.CallAction("setVoiceKissing");
                triggerman.OnOrgasmStart += ()  => plugin.CallAction("setVoiceOrgasm");
            });

            IntegrateTo("SexHelper", plugin => {
                plugin.SetFloatParamValue("Thrust Time Range", .10f);

                triggerman.OnArousalUpdate += (arousal, kissing, sucking, orgasming) =>
                {
                    //slow, long movement at first; shorter and faster with arousal
                    plugin.SetFloatParamValue("Thrust Time (lower = faster)",   Modulate(.80f, .25f, arousal));
                    plugin.SetFloatParamValue("Position One (higher = deeper)", Modulate(.80f, .25f, arousal));
                    plugin.SetFloatParamValue("Hip Position One",               Modulate(.80f, .25f, arousal));
                };
            });

            IntegrateTo("ExpressionRandomizer", plugin => {
                plugin.SetFloatParamValue("Morphing speed", 3f);
                plugin.SetBoolParamValue("Trigger transitions manually", true);
                plugin.SetBoolParamValue("Auto reset transitions if manual", true);
                plugin.SetBoolParamValue("Play", true);

                triggerman.OnArousalUpdate += (arousal, kissing, sucking, orgasming) =>
                {
                    //faster, more intense gestures with arousal
                    plugin.SetFloatParamValue("Multiplier",   Modulate(0.8f, 1.5f,  arousal));
                    plugin.SetFloatParamValue("Master speed", Modulate(1f,   1.25f, arousal));
                };

                triggerman.OnPenetration += () => plugin.CallAction("Trigger transition");
                triggerman.OnPump +=        () => plugin.CallAction("Trigger transition");
                triggerman.OnBreastTouch += () => plugin.CallAction("Trigger transition");
                triggerman.OnOrgasmStart += () => plugin.CallAction("Trigger transition");
            });
        }

        //if the containing atom also has an enabled plugin with this suffix, then do stuff with it
        void IntegrateTo(string pluginNameSuffix, ActionOnPlugin action)
        {
            var plugin = SearchForLocalPluginBySuffix(pluginNameSuffix);

            if (plugin && plugin.enabled)
            {
                action(plugin);
            }
        }

        JSONStorable SearchForLocalPluginBySuffix(string pluginNameSuffix)
        {
            foreach (var sid in containingAtom.GetStorableIDs().Where(id => id.StartsWith("plugin#")))
            {
                if (sid.EndsWith(pluginNameSuffix))
                {
                    LogMessage("integrating to " + pluginNameSuffix);
                    return containingAtom.GetStorableByID(sid);
                }
            }

            return null;
        }

        #region helpers

        //returns a value between starting and final, based on arousal ratio
        float Modulate(float startingValue, float finalValue, float arousalRatio)
        {
            return (finalValue - startingValue) * arousalRatio + startingValue;
        }

        #endregion

        #region logging

        void LogMessage(string message)
        {
            if (logMessages)
            {
                SuperController.LogMessage($"Triggerman: {message}");
            }
        }

        void LogError(string message)
        {
            if (logMessages)
            {
                SuperController.LogError($"Triggerman - ERROR: {message}");
            }
        }

        #endregion
    }
}

