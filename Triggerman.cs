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

        JSONStorableString explanationString;
        JSONStorableFloat stimulationToOrgasm;
        JSONStorableFloat percentToOrgasmFloat;

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

        float vagTouchTime = 0;
        float vagTouchLastTime = 0;
        float foreplayTouchLastTime = 0;

        bool orgasming = false;
        int orgasmStep = 0;
        float orgasmStartTime = 0;
        int lastOrgasmStep = 2;
        bool orgasmAgain = false;
        float percentToOrgasm = 0;

        bool wasLoading = true;

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

            //we entered recently, plus are touching
            if (Time.timeSinceLevelLoad - vagTouchLastTime < 1.0f && (labiaTouching || vagTouching || deepVagTouching))
            {
                vagTouchTime += Time.deltaTime;
            }
            else if (Time.timeSinceLevelLoad - foreplayTouchLastTime < 1.0f && (lBreastTouching || rBreastTouching || lipTouching))
            {
                //foreplay goes half way
                if (vagTouchTime < stimulationToOrgasm.val / 2.0f)
                {
                    vagTouchTime += Time.deltaTime;
                }
            }
            else if (vagTouchTime > 0)
            {
                vagTouchTime -= Time.deltaTime / 5.0f;
            }

            if (vagTouchTime >= stimulationToOrgasm.val)
            {
                //ORGASM
                if (orgasming)
                {
                    //as soon as we finish, start again
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

            explanationString.val = string.Format("Orgasm percent: {0:P}", percentToOrgasm);

            percentToOrgasmFloat.SetVal(percentToOrgasm);
        }

        void StartOrgasm()
        {
            vagTouchTime = - stimulationToOrgasm.val / 3.0f;
            orgasming = true;
            orgasmStartTime = Time.timeSinceLevelLoad;
            orgasmStep = 0;

            if (logMessages) SuperController.LogMessage("Start orgasm sequence");
        }

        void HandleOrgasm()
        {
            //give it a little time to finish each step before moving on
            float timeBetweenSteps = 0.5f;
            bool progressToNextStep = false;

            if (Time.timeSinceLevelLoad - orgasmStartTime > timeBetweenSteps)
            {
                progressToNextStep = true;
            }

            if (orgasmStep == 0 && progressToNextStep)
            {
                //if we choose a long orgasm clip, then just play it, and don't play the rest, 25% chance
                if (UnityEngine.Random.value < 0.25f)
                {
                    orgasmStep = lastOrgasmStep;
                    orgasmStartTime = Time.timeSinceLevelLoad;
                }
                else
                {
                    orgasmStep++;
                    orgasmStartTime = Time.timeSinceLevelLoad;
                }
            }
            else if (orgasmStep == 1 && progressToNextStep)
            {
                orgasmStep++;
                orgasmStartTime = Time.timeSinceLevelLoad;
            }
            else if (orgasmStep == lastOrgasmStep && progressToNextStep) //MAKE SURE THIS IS ONE AFTER THE PRIOR STEP
            {
                orgasming = false;
                orgasmStep = 0;
                vagTouchLastTime = Time.timeSinceLevelLoad;
            }
        }

        #region trigger callbacks

        void ObserveLipTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered" && !SuperController.singleton.isLoading)
            {
                if (!lipTouching && !orgasming)
                {
                    foreplayTouchLastTime = Time.timeSinceLevelLoad;
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
                    vagTouchLastTime = Time.timeSinceLevelLoad;
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
                    vagTouchLastTime = Time.timeSinceLevelLoad;
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
                    vagTouchLastTime = Time.timeSinceLevelLoad;
                    deepVagTouching = true;
                }
            }
            else
            {
                deepVagTouching = false;
            }
        }

        #endregion
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
            //Don't collide with abdomen, or abdomen triggers vag collider when it shouldn't
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

