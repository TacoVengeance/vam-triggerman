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
                return;
            }

            IntegrateTo("MacGruber.Breathing", plugin => {
                LogMessage("enabling MacGruber's Life integration");

                triggerman.OnArousalUpdate += arousal =>
                {
                    plugin.SetFloatParamValue("Intensity", Modulate(0.45f, 1f, arousal));
                };
                triggerman.OnOrgasm += arousal => plugin.CallAction("QueueOrgasm");

                plugin.SetFloatParamValue("Rhythm Randomness", 0.02f);
                plugin.SetFloatParamValue("Rhythm Damping", 0.05f);
                plugin.SetFloatParamValue("Audio Variance", 1f);
            });

            IntegrateTo("EasyGaze", plugin => {
                LogMessage("enabling EasyGaze integration");

                triggerman.OnArousalUpdate += arousal =>
                {
                    plugin.SetFloatParamValue("Random head roll timer Maximum",    Modulate(6f,  3f, arousal));
                    plugin.SetFloatParamValue("Random roll Angle Maximum",         Modulate(2f, 25f, arousal));
                    plugin.SetFloatParamValue("Random head turn range Vertical",   Modulate(2f, 25f, arousal));
                    plugin.SetFloatParamValue("Random head turn range Horizontal", Modulate(2f, 25f, arousal));
                };
            });

            IntegrateTo("BreatheStandalone", plugin => {
                LogMessage("enabling BreatheStandalone integration");

                triggerman.OnArousalUpdate += arousal =>
                {
                    plugin.SetFloatParamValue("Breath Intensity", Modulate(0.2f,  1f, arousal));
                    plugin.SetFloatParamValue("Chest Movement",   Modulate( 15f, 30f, arousal));
                };
            });

            IntegrateTo("VAMMoan", plugin => {
                LogMessage("enabling VamMoan integration");

                //FIXME: doesn't work, goes straight to breathing
                triggerman.OnOrgasm += arousal => plugin.CallAction("setVoiceOrgasm");

                triggerman.OnArousalUpdate += arousal =>
                {
                    if      (arousal < 0.01f)  plugin.CallAction("setVoiceBreathing");
                    else if (arousal < 0.10f)  plugin.CallAction("setVoiceIntensity0");
                    else if (arousal < 0.20f)  plugin.CallAction("setVoiceIntensity1");
                    else if (arousal < 0.35f)  plugin.CallAction("setVoiceIntensity2");
                    else if (arousal < 0.65f)  plugin.CallAction("setVoiceIntensity3");
                    else if (arousal < 0.85f)  plugin.CallAction("setVoiceIntensity4");

                    plugin.SetFloatParamValue("Kissing Speed", Modulate(1f, 0.25f, arousal));
                    plugin.SetFloatParamValue("Blowjob Speed", Modulate(1f, 0.75f, arousal));
                };
            });
        }

        void IntegrateTo(string pluginNameSuffix, ActionOnPlugin action)
        {
            var plugin = SearchForLocalPluginBySuffix(pluginNameSuffix);

            if (plugin && plugin.enabled)
            {
                action(plugin);
            }
        }

        //returns a value between starting and final, based on arousal ratio
        float Modulate(float startingValue, float finalValue, float arousalRatio)
        {
            return (finalValue - startingValue) * arousalRatio + startingValue;
        }

        JSONStorable SearchForLocalPluginBySuffix(string pluginNameSuffix)
        {
            foreach (var sid in containingAtom.GetStorableIDs().Where(id => id.StartsWith("plugin#")))
            {
                if (sid.EndsWith(pluginNameSuffix))
                {
                    return containingAtom.GetStorableByID(sid);
                }
            }

            return null;
        }

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
                SuperController.LogError($"Triggerman: {message}");
            }
        }

        #endregion
    }
}

