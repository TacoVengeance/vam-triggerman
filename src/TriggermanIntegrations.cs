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

            IntegrateTo("MacGruber.Breathing", target => {
                LogMessage("enabling MacGruber's Life integration");

                triggerman.OnArousalUpdate += arousal => target.SetFloatParamValue("Intensity", arousal);
                triggerman.OnOrgasm += () => target.CallAction("QueueOrgasm");
            });

            IntegrateTo("EasyGaze", target => {
                LogMessage("enabling EasyGaze integration");

                triggerman.OnArousalUpdate += arousal =>
                {
                    target.SetFloatParamValue("Random head roll timer Maximum",    Modulate(6f,  3f, arousal));
                    target.SetFloatParamValue("Random roll Angle Maximum",         Modulate(2f, 25f, arousal));
                    target.SetFloatParamValue("Random head turn range Vertical",   Modulate(2f, 25f, arousal));
                    target.SetFloatParamValue("Random head turn range Horizontal", Modulate(2f, 25f, arousal));
                };
            });

            IntegrateTo("BreatheStandalone", target => {
                LogMessage("enabling BreatheStandalone integration");

                triggerman.OnArousalUpdate += arousal =>
                {
                    target.SetFloatParamValue("Breath Intensity", Modulate(0.2f,  1f, arousal));
                    target.SetFloatParamValue("Chest Movement",   Modulate( 15f, 30f, arousal));
                };
            });

            IntegrateTo("VAMMoan", target => {
                LogMessage("enabling VamMoan integration");

                triggerman.OnArousalUpdate += arousal =>
                {
                    if      (arousal < 0.01f)  target.CallAction("setVoiceBreathing");
                    else if (arousal < 0.10f)  target.CallAction("setVoiceIntensity0");
                    else if (arousal < 0.20f)  target.CallAction("setVoiceIntensity1");
                    else if (arousal < 0.35f)  target.CallAction("setVoiceIntensity2");
                    else if (arousal < 0.65f)  target.CallAction("setVoiceIntensity3");
                    else if (arousal < 0.85f)  target.CallAction("setVoiceIntensity4");

                    target.SetFloatParamValue("Kissing Speed", Modulate(1f, 0.25f, arousal));
                    target.SetFloatParamValue("Blowjob Speed", Modulate(1f, 0.75f, arousal));
                };

                triggerman.OnOrgasm += () => target.CallAction("setVoiceOrgasm");
            });
        }

        void IntegrateTo(string pluginNameSuffix, ActionOnPlugin action)
        {
            var plugin = SearchForLocalPluginBySuffix(pluginNameSuffix);

            if (plugin)
            {
                action(plugin);
            }
        }

        float Modulate(float startingValue, float finalValue, float arousal)
        {
            return (finalValue - startingValue) * arousal + startingValue;
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

