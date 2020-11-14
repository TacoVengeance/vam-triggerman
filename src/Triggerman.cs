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

        public delegate void ArousalHandler(float arousalRatio, bool kissing, bool sucking, bool orgasming);
        public event ArousalHandler OnArousalUpdate;

        public event Action OnKissingStart;
        public event Action OnKissingStop;
        bool kissing = false;

        public event Action OnSuckingStart;
        public event Action OnSuckingStop;
        bool sucking = false;

        public event Action OnOrgasmStart;
        public event Action OnOrgasmStop;
        bool orgasming = false;

        public event Action OnBreastTouch;
        bool breastsTouching = false;

        //when entering vagina
        public event Action OnPenetration;
        bool penetrating = false;

        //when hitting deep vagina
        public event Action OnPump;
        bool pumping = false;

        bool foreplaying = false;

        //cumulative arousal time in seconds
        float arousalTime = 0;
        //cumulative arousal time as ratio to orgasm (0 to 1)
        float arousalRatio = 0;
        //time of last penetration
        float timeLastPenetration = 0;
        //time of last foreplay
        float timeLastForeplay = 0;
        //time of last orgasm start
        float timeOrgasmStart = 0;
        //time in millisecs that the current orgasm will last
        float orgasmDuration = 0;
        //arousal time required for orgasm
        JSONStorableFloat minArousalForOrgasm;
        //arousal time as ratio to orgasm
        JSONStorableFloat ratioToOrgasmFloat;

        float timeLastUpdate = 0;
        bool wasLoading = true;

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

        Dictionary<TriggerCollide, EventHandler<TriggerEventArgs>> colliderCallbacks = new Dictionary<TriggerCollide, EventHandler<TriggerEventArgs>>();

        float CurrentTime => Time.timeSinceLevelLoad;

        public override void Init()
        {
            statusString = new JSONStorableString("", "");
            CreateTextField(statusString).height = 170f;

            minArousalForOrgasm = new JSONStorableFloat("Arousal time required for orgasm", 45f, 10f, 300f, false);
            RegisterFloat(minArousalForOrgasm);
            minArousalForOrgasm.storeType = JSONStorableParam.StoreType.Full;
            CreateSlider(minArousalForOrgasm);

            ratioToOrgasmFloat = new JSONStorableFloat("Ratio to orgasm", 0.0f, 0.0f, 1.0f, false);
            RegisterFloat(ratioToOrgasmFloat);
            ratioToOrgasmFloat.storeType = JSONStorableParam.StoreType.Full;
        }

        public void OnDestroy()
        {
            DisableColliderEvents();
            DisablePublicEvents();
        }

        public void OnDisable()
        {
            DisableColliderEvents();
        }

        public void OnEnable()
        {
            //ensure we don't end up registering them multiple times
            DisableColliderEvents(); 

            ReenableColliderEvents();
            ResetTouching();
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
            colliderCallbacks[collider] = callback;

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
            HandleSceneLoadState();

            kissing = lipTouching;
            sucking = mouthTouching || throatTouching;
            breastsTouching = lBreastTouching || rBreastTouching;
            penetrating = labiaTouching || vagTouching || deepVagTouching;
            foreplaying = lBreastTouching || rBreastTouching || lipTouching;
            pumping = deepVagTouching;

            //if penetrating and not still for more than a second (ie. hitting new colliders)
            if (penetrating && (CurrentTime - timeLastPenetration < 1.0f))
            {
                //arousal up
                arousalTime += Time.deltaTime;
            }
            //if foreplaying and not still for more than a second
            else if (foreplaying && (CurrentTime - timeLastForeplay < 1.0f))
            {
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
                StartOrgasm();
            }

            if (orgasming)
            {
                HandleOrgasm();
            }

            arousalRatio = arousalTime / minArousalForOrgasm.val;
            if (arousalRatio < 0) arousalRatio = 0;
            ratioToOrgasmFloat.SetVal(arousalRatio);

            statusString.val = string.Format(
                "Cumulative arousal time: {0:F02} sec\n" +
                "Orgasm progress: {1:P}\n\n" +
                "Foreplaying: {2}\n" +
                "Penetrating: {3}",
                arousalTime,
                arousalRatio,
                foreplaying,
                penetrating
            );

            //throttle updates for performance
            if (CurrentTime - timeLastUpdate > .5f)
            {
                timeLastUpdate = CurrentTime;

                OnArousalUpdate(arousalRatio, kissing, sucking, orgasming);
            }
        }

        void HandleSceneLoadState()
        {
            if (IsLoading && !wasLoading)
            {
                wasLoading = true;
            }
            else if (!IsLoading && wasLoading)
            {
                wasLoading = false;
                ResetTouching();
            }
        }

        void StartOrgasm()
        {
            arousalTime = 0f;
            orgasming = true;

            //orgasms last between 5 and 12secs
            orgasmDuration = UnityEngine.Random.Range(5f, 12f);
            timeOrgasmStart = CurrentTime;

            LogMessage(string.Format("Start orgasm of {0:F02} sec", orgasmDuration));
            OnOrgasmStart();
        }

        void HandleOrgasm()
        {
            if (CurrentTime - timeOrgasmStart >= orgasmDuration)
            {
                LogMessage("End orgasm");
                orgasming = false;
                OnOrgasmStop();
            }
        }

        #region trigger callbacks

        void ObserveLipTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered")
            {
                if (!lipTouching && !orgasming)
                {
                    timeLastForeplay = CurrentTime;
                    lipTouching = true;
                    OnKissingStart();
                }
            }
            else
            {
                if (lipTouching) OnKissingStop();
                lipTouching = false;
            }
        }

        void ObserveMouthTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered")
            {
                if (!mouthTouching && !orgasming)
                {
                    timeLastForeplay = CurrentTime;
                    mouthTouching = true;
                    OnSuckingStart();
                }
            }
            else
            {
                if (mouthTouching) OnSuckingStop();
                mouthTouching = false;
            }
        }

        void ObserveThroatTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered")
            {
                if (!throatTouching && !orgasming)
                {
                    timeLastForeplay = CurrentTime;
                    throatTouching = true;
                    OnSuckingStart();
                }
            }
            else
            {
                if (throatTouching) OnSuckingStop();
                throatTouching = false;
            }
        }

        void ObservelBreastTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered")
            {
                if (!lBreastTouching && !orgasming)
                {
                    timeLastForeplay = CurrentTime;
                    lBreastTouching = true;
                    OnBreastTouch();
                }
            }
            else
            {
                lBreastTouching = false;
            }
        }

        void ObserverBreastTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered")
            {
                if (!rBreastTouching && !orgasming)
                {
                    timeLastForeplay = CurrentTime;
                    rBreastTouching = true;
                    OnBreastTouch();
                }
            }
            else
            {
                rBreastTouching = false;
            }
        }

        void ObserveLabiaTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered")
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
            if (e.evtType == "Entered")
            {
                if (!vagTouching && !orgasming)
                {
                    timeLastPenetration = CurrentTime;
                    vagTouching = true;
                    OnPenetration();
                }
            }
            else
            {
                vagTouching = false;
            }
        }

        void ObserveDeepVagTrigger(object sender, TriggerEventArgs e)
        {
            if (e.evtType == "Entered")
            {
                if (!deepVagTouching && !orgasming)
                {
                    timeLastPenetration = CurrentTime;
                    deepVagTouching = true;
                    OnPump();
                }
            }
            else
            {
                deepVagTouching = false;
            }
        }

        #endregion

        #region helpers

        void DisablePublicEvents()
        {
            //cover your eyes, lest ye be blinded

            foreach (var d in OnArousalUpdate.GetInvocationList()) OnArousalUpdate -= (ArousalHandler) d;
            foreach (var d in OnKissingStart .GetInvocationList()) OnKissingStart  -= (Action) d;
            foreach (var d in OnKissingStop  .GetInvocationList()) OnKissingStop   -= (Action) d;
            foreach (var d in OnSuckingStart .GetInvocationList()) OnSuckingStart  -= (Action) d;
            foreach (var d in OnSuckingStop  .GetInvocationList()) OnSuckingStop   -= (Action) d;
            foreach (var d in OnOrgasmStart  .GetInvocationList()) OnOrgasmStart   -= (Action) d;
            foreach (var d in OnOrgasmStop   .GetInvocationList()) OnOrgasmStop    -= (Action) d;
            foreach (var d in OnBreastTouch  .GetInvocationList()) OnBreastTouch   -= (Action) d;
            foreach (var d in OnPenetration  .GetInvocationList()) OnPenetration   -= (Action) d;
            foreach (var d in OnPump         .GetInvocationList()) OnPump          -= (Action) d;
        }

        void DisableColliderEvents()
        {
            foreach (var entry in colliderCallbacks)
            {
                var collider = entry.Key;
                var callback = entry.Value;
                collider.OnCollide -= callback;
            }
        }

        void ReenableColliderEvents()
        {
            foreach (var entry in colliderCallbacks)
            {
                var collider = entry.Key;
                var callback = entry.Value;
                collider.OnCollide += callback;
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
                SuperController.LogError($"Triggerman - ERROR: {message}");
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

        bool ignoreIfLoading = true;
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

            if (ignoreIfLoading && SuperController.singleton.isLoading)
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

