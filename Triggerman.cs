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

        public delegate void OnArousalUpdateHandler(float percentToOrgasm);
        public event OnArousalUpdateHandler OnArousalUpdate;

        public delegate void OnOrgasmHandler();
        public event OnOrgasmHandler OnOrgasm;

        //cumulative arousal time in seconds
        float arousalTime = 0;
        //cumulative arousal time as percent to orgasm
        float percentToOrgasm = 0;
        //time of last penetration
        float timeLastPenetration = 0;
        //time of last foreplay
        float timeLastForeplay = 0;
        //arousal time required for orgasm
        JSONStorableFloat minArousalForOrgasm;
        //arousal time as percent to orgasm
        JSONStorableFloat percentToOrgasmFloat;

        JSONStorableString statusString;

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
            statusString = new JSONStorableString("", "");
            CreateTextField(statusString).height = 170f;

            minArousalForOrgasm = new JSONStorableFloat("Arousal time required for orgasm", 120.0f, 10.0f, 240.0f, false);
            RegisterFloat(minArousalForOrgasm);
            minArousalForOrgasm.storeType = JSONStorableParam.StoreType.Full;
            CreateSlider(minArousalForOrgasm);

            percentToOrgasmFloat = new JSONStorableFloat("Percent to orgasm", 0.0f, 0.0f, 1.0f, false);
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
                LogError("Must be loaded on a female Person atom");
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
            bool penetrating = false;
            bool foreplaying = false;

            if (IsLoading && !wasLoading)
            {
                wasLoading = true;
            }
            else if (!IsLoading && wasLoading)
            {
                wasLoading = false;
                ResetTouching();
            }

            //if penetrating and not still for more than a second (ie. hitting new colliders)
            if ((labiaTouching || vagTouching || deepVagTouching) && (CurrentTime - timeLastPenetration < 1.0f))
            {
                penetrating = true;

                //arousal up
                //NOTE: the more colliders you hit, the more arousal (ie. deeper = hotter)
                arousalTime += Time.deltaTime;
            }
            //if foreplaying and not still for more than a second
            else if ((lBreastTouching || rBreastTouching || lipTouching) && (CurrentTime - timeLastForeplay < 1.0f))
            {
                foreplaying = true;

                if (arousalTime < minArousalForOrgasm.val / 2.0f)
                {
                    //arousal up, but foreplay only counts up to 50%
                    arousalTime += Time.deltaTime;
                }
            }
            else if (arousalTime > 0)
            {
                //otherwise, arousal decays at 1/5 the rate it goes up
                arousalTime -= Time.deltaTime / 5.0f;
            }

            if (arousalTime >= minArousalForOrgasm.val)
            {
                //ORGASM (arousal time is past orgasm o'clock)

                if (orgasming)
                {
                    //if orgasm reached while orgasming (you absolute stud), begin orgasm again as soon as current one ends
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

            percentToOrgasm = arousalTime / minArousalForOrgasm.val;
            if (percentToOrgasm < 0) percentToOrgasm = 0;
            if (orgasming) percentToOrgasm = 1.0f;

            statusString.val = string.Format(
                "Cumulative arousal time: {0:F02} sec\n" +
                "Orgasm percent: {1:P}\n\n" +
                "Foreplaying: {2}\n" +
                "Penetrating: {3}",
                arousalTime,
                percentToOrgasm,
                foreplaying,
                penetrating
            );

            percentToOrgasmFloat.SetVal(percentToOrgasm);
        }

        public void FixedUpdate()
        {
            OnArousalUpdate(percentToOrgasm);
        }

        void StartOrgasm()
        {
            //set arousal to -33%; ie. you'll need 1/3 of min orgasm time to get the clock ticking again
            arousalTime = - minArousalForOrgasm.val / 3.0f;

            orgasming = true;

            LogMessage("Start orgasm");
        }

        void HandleOrgasm()
        {
            orgasming = false;
            timeLastPenetration = CurrentTime;

            OnOrgasm();

            LogMessage("End orgasm");
        }

        #region trigger callbacks

        void ObserveLipTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered" && !IsLoading)
            {
                if (!lipTouching && !orgasming)
                {
                    timeLastForeplay = CurrentTime;
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
            if (e.evtType == "Entered" && !IsLoading)
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
            if (e.evtType == "Entered" && !IsLoading)
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
            if (e.evtType == "Entered" && !IsLoading)
            {
                if (!lBreastTouching && !throatTouching && !mouthTouching && !orgasming)
                {
                    timeLastForeplay = CurrentTime;
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
            if (e.evtType == "Entered" && !IsLoading)
            {
                if (!rBreastTouching && !throatTouching && !mouthTouching && !orgasming)
                {
                    timeLastForeplay = CurrentTime;
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
            if (e.evtType == "Entered" && !IsLoading)
            {
                if (!labiaTouching && !orgasming)
                {
                    timeLastPenetration = CurrentTime;
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
            if (e.evtType == "Entered" && !IsLoading)
            {
                if (!vagTouching && !orgasming)
                {
                    timeLastPenetration = CurrentTime;
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
            if (e.evtType == "Entered" && !IsLoading)
            {
                if (!deepVagTouching && !orgasming)
                {
                    timeLastPenetration = CurrentTime;
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

        void LogError(string message)
        {
            if (logMessages)
            {
                SuperController.LogError($"Triggerman: {message}");
            }
        }

        bool IsLoading => SuperController.singleton.isLoading;
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

