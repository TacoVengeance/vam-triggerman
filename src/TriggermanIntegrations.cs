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

                triggerman.OnArousalUpdate += arousalPercent => target.SetFloatParamValue("Intensity", arousalPercent);
                triggerman.OnOrgasm += () => target.CallAction("QueueOrgasm");
            });

            IntegrateTo("EasyGaze", target => {
                LogMessage("enabling EasyGaze integration");

                triggerman.OnArousalUpdate += arousalPercent =>
                {
                    //6 - 3
                    target.SetFloatParamValue("Random head roll timer Maximum", -(3f * arousalPercent) + 6f);

                    //2 - 25
                    target.SetFloatParamValue("Random roll Angle Maximum", (23f * arousalPercent) + 2f);

                    //2 - 25
                    target.SetFloatParamValue("Random head turn range Vertical", (23f * arousalPercent) + 2f);

                    //2 - 25
                    target.SetFloatParamValue("Random head turn range Horizontal", (23f * arousalPercent) + 2f);
                };
            });

            IntegrateTo("BreatheStandalone", target => {
                LogMessage("enabling BreatheStandalone integration");

                triggerman.OnArousalUpdate += arousalPercent =>
                {
                    //0.2 - 1
                    target.SetFloatParamValue("Breath Intensity", (.8f * arousalPercent) + .2f);

                    //15 - 30
                    target.SetFloatParamValue("Chest Movement", (15f * arousalPercent) + 15f);
                };
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

