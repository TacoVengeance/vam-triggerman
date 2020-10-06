using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TacoVengeance
{
    /// <summary>
    /// Triggerman plugin
    /// By TacoVengeance
    /// Collision trigger based arousal event system, based on Geesp0t's EasyMoan
    /// Source: https://github.com/TacoVengeance/vam-triggerman
    /// </summary>
    public class TriggermanPlugin : MVRScript
    {
        readonly bool logMessages = false;

        //cumulative arousal time in seconds
        float vagTouchTime = 0;
        //cumulative arousal time in seconds as percent to orgasm
        float percentToOrgasm = 0;
        //time of last penetration
        float vagTouchLastTime = 0;
        //time of last foreplay
        float foreplayTouchLastTime = 0;

        //arousal time required for orgasm
        JSONStorableFloat stimulationToOrgasm;
        //arousal time required for orgasm as percent
        JSONStorableFloat percentToOrgasmFloat;
        JSONStorableString explanationString;

        Rigidbody lipTrigger;
        bool lipTouching = false;

        Rigidbody lBreastTrigger;
        bool lBreastTouching = false;

        Rigidbody rBreastTrigger;
        bool rBreastTouching = false;

        Rigidbody labiaTrigger;
        bool labiaTouching = false;

        Rigidbody vagTrigger;
        bool vagTouching = false;

        Rigidbody deepVagTrigger;
        bool deepVagTouching = false;

        Rigidbody mouthTrigger;
        bool mouthTouching = false;

        Rigidbody throatTrigger;
        bool throatTouching = false;

        bool orgasming = false;
        bool orgasmAgain = false;

        bool wasLoading = true;

        float CurrentTime => Time.timeSinceLevelLoad;

        public override void Init()
        {
            explanationString = new JSONStorableString("Orgasm percent: 0%", "");
            CreateTextField(explanationString).height = 50f;

            stimulationToOrgasm = new JSONStorableFloat("Shortest possible touch time till orgasm", 120.0f, 10.0f, 240.0f, false);
            RegisterFloat(stimulationToOrgasm);
            stimulationToOrgasm.storeType = JSONStorableParam.StoreType.Full;
            CreateSlider(stimulationToOrgasm);

            percentToOrgasmFloat = new JSONStorableFloat("Percent to orgasm float value", 0.0f, 0.0f, 1.0f, false);
            RegisterFloat(percentToOrgasmFloat);
            percentToOrgasmFloat.storeType = JSONStorableParam.StoreType.Full;
        }

        public void Start()
        {
            if (containingAtom.category == "People")
            {
                lipTrigger =     SetUpCollider("LipTrigger",        ObserveLipTrigger);
                mouthTrigger =   SetUpCollider("MouthTrigger",      ObserveMouthTrigger);
                throatTrigger =  SetUpCollider("ThroatTrigger",     ObserveThroatTrigger);
                lBreastTrigger = SetUpCollider("lNippleTrigger",    ObservelBreastTrigger);
                rBreastTrigger = SetUpCollider("rNippleTrigger",    ObserverBreastTrigger);
                labiaTrigger =   SetUpCollider("LabiaTrigger",      ObserveLabiaTrigger);
                vagTrigger =     SetUpCollider("VaginaTrigger",     ObserveVagTrigger);
                deepVagTrigger = SetUpCollider("DeepVaginaTrigger", ObserveDeepVagTrigger);
            }
            else
            {
                //FIXME: reference current class in this message
                SuperController.LogError("Must be loaded on a female Person atom");
            }

            ResetTouching();
        }

        Rigidbody SetUpCollider(string name, EventHandler<TriggerEventArgs> callback)
        {
            var rigidbody = containingAtom.rigidbodies.First(rb => rb.name == name);

            var collider = rigidbody.gameObject.GetComponentInChildren<TriggerCollide>();
            if (collider == null)
            {
                collider = rigidbody.gameObject.AddComponent<TriggerCollide>();
            }

            collider.OnCollide += callback;

            return rigidbody;
        }

        public void ResetTouching()
        {
            lipTouching = false;
            lBreastTouching = false;
            rBreastTouching = false;
            labiaTouching = false;
            vagTouching = false;
            deepVagTouching = false;
            mouthTouching = false;
            throatTouching = false;
        }

        public void Update()
        {
            if (SuperController.singleton.isLoading && !wasLoading)
            {
                wasLoading = true;
            }
            else if (!SuperController.singleton.isLoading && wasLoading)
            {
                wasLoading = false;
                ResetTouching();
            }

            //if penetrating and not still for more than a second
            if (CurrentTime - vagTouchLastTime < 1.0f && (labiaTouching || vagTouching || deepVagTouching))
            {
                //arousal up
                //NOTE: the more colliders you hit, the more arousal (ie. deeper = hotter)
                vagTouchTime += Time.deltaTime;
            }
            //if foreplaying and not still for more than a second
            else if (CurrentTime - foreplayTouchLastTime < 1.0f && (lBreastTouching || rBreastTouching || lipTouching))
            {
                if (vagTouchTime < stimulationToOrgasm.val / 2.0f)
                {
                    //arousal up, but foreplay only counts up to 50%
                    vagTouchTime += Time.deltaTime;
                }
            }
            else if (vagTouchTime > 0)
            {
                //otherwise, arousal decays at 1/5 the rate it goes up
                vagTouchTime -= Time.deltaTime / 5.0f;
            }

            if (vagTouchTime >= stimulationToOrgasm.val)
            {
                //ORGASM (arousal time is past orgasm o'clock)

                if (orgasming)
                {
                    //if we reached orgasm while orgasming (you absolute stud), begin orgasm again as soon as current one ends
                    orgasmAgain = true;
                }
                else
                {
                    StartOrgasm();
                }
            }

            if (orgasmAgain && !orgasming)
            {
                orgasmAgain = false;
                StartOrgasm();
            }

            if (orgasming)
            {
                HandleOrgasm();
            }

            percentToOrgasm = vagTouchTime / stimulationToOrgasm.val;
            if (percentToOrgasm < 0) percentToOrgasm = 0;
            if (orgasming) percentToOrgasm = 1.0f;

            explanationString.val = string.Format(
                "Cumulative arousal time: {1:F02} sec" +
                "\nOrgasm percent: {0:P}",
                percentToOrgasm,
                vagTouchTime
            );

            percentToOrgasmFloat.SetVal(percentToOrgasm);
        }

        void StartOrgasm()
        {
            //set arousal to minus 33%; ie. you'll need 30% of min orgasm time to get the clock ticking again

            vagTouchTime = - stimulationToOrgasm.val / 3.0f;
            orgasming = true;

            LogMessage("Start orgasm");
        }

        void HandleOrgasm()
        {
            orgasming = false;
            vagTouchLastTime = CurrentTime;

            LogMessage("End orgasm");
        }

        #region trigger callbacks

        void ObserveLipTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered" && !SuperController.singleton.isLoading)
            {
                if (!lipTouching && !orgasming)
                {
                    foreplayTouchLastTime = CurrentTime;
                    lipTouching = true;
                }
            }
            else
            {
                lipTouching = false;
            }
        }

        void ObserveMouthTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered" && !SuperController.singleton.isLoading)
            {
                if (!mouthTouching && !orgasming)
                {
                    mouthTouching = true;
                }
            }
            else
            {
                if (mouthTouching)
                {
                    mouthTouching = false;
                }
            }
        }

        void ObserveThroatTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered" && !SuperController.singleton.isLoading)
            {
                if (!throatTouching && !orgasming)
                {
                    throatTouching = true;
                }
            }
            else
            {
                if (throatTouching)
                {
                    throatTouching = false;
                }
            }
        }

        void ObservelBreastTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered" && !SuperController.singleton.isLoading)
            {
                if (!lBreastTouching && !throatTouching && !mouthTouching && !orgasming)
                {
                    foreplayTouchLastTime = CurrentTime;
                    lBreastTouching = true;
                }
            }
            else
            {
                lBreastTouching = false;
            }
        }

        void ObserverBreastTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered" && !SuperController.singleton.isLoading)
            {
                if (!rBreastTouching && !throatTouching && !mouthTouching && !orgasming)
                {
                    foreplayTouchLastTime = CurrentTime;
                    rBreastTouching = true;
                }
            }
            else
            {
                rBreastTouching = false;
            }
        }

        void ObserveLabiaTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered" && !SuperController.singleton.isLoading)
            {
                if (!labiaTouching && !orgasming)
                {
                    vagTouchLastTime = CurrentTime;
                    labiaTouching = true;
                }
            }
            else
            {
                labiaTouching = false;
            }
        }

        void ObserveVagTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered" && !SuperController.singleton.isLoading)
            {
                if (!vagTouching && !orgasming)
                {
                    vagTouchLastTime = CurrentTime;
                    vagTouching = true;
                }
            }
            else
            {
                vagTouching = false;
            }
        }

        void ObserveDeepVagTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered" && !SuperController.singleton.isLoading)
            {
                if (!deepVagTouching && !orgasming)
                {
                    vagTouchLastTime = CurrentTime;
                    deepVagTouching = true;
                }
            }
            else
            {
                deepVagTouching = false;
            }
        }

        #endregion

        void LogMessage(string message)
        {
            if (logMessages)
            {
                SuperController.LogMessage($"Triggerman: {message}");
            }
        }
    }

    #region VRAdultFun trigger helper

    public class TriggerEventArgs : EventArgs
    {
        public Collider collider { get; set; }
        public string evtType { get; set; }
    }

    public class TriggerCollide : MonoBehaviour
    {
        TriggerEventArgs lastEvent;

        public event EventHandler<TriggerEventArgs> OnCollide;

        void Awake()
        {
            lastEvent = new TriggerEventArgs
            {
                evtType = "none",
                collider = null
            };
        }

        private void OnTriggerEnter(Collider other)
        {
            //Don't collide with abdomen, or abdomen would trigger vag collider when it shouldn't
            //FIXME: debug this - looks like there are other rigidbodies which cause bogus collisions
            if (other.attachedRigidbody.name.StartsWith("AutoColliderFemaleAutoCollidersabdomen"))
            {
                return;
            }
            DoCollideEvent("Entered", other);
        }

        private void OnTriggerExit(Collider other)
        {
            DoCollideEvent("Exited", other);
        }

        /*
        private void OnTriggerStay(Collider other)
        {
            DoCollideEvent("Stay", other);
        }
        */

        private void DoCollideEvent(string evtType, Collider col)
        {
            if (string.Equals(evtType, lastEvent.evtType) && col.gameObject == lastEvent.collider.gameObject)
            {
                return;
            }
            else
            {
                TriggerEventArgs tempEvent = new TriggerEventArgs
                {
                    collider = col,
                    evtType = evtType
                };
                OnCollideEvent(tempEvent);
                lastEvent = tempEvent;
            }
        }

        protected virtual void OnCollideEvent(TriggerEventArgs e)
        {
            EventHandler<TriggerEventArgs> handler = OnCollide;
            handler?.Invoke(this, e);
        }
    }

    #endregion
}

