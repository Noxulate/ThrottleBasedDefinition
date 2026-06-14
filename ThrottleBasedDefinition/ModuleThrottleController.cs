 using System;
using UnityEngine;

namespace ThrottleBasedDefinition
{
    public class ModuleThrottleController : PartModule
    {
        // Fields
        ModuleEngines activeEngine;
        MultiModeEngine engineMulti;
        private double engineFmax, engineIspV, engineIspSL;
        private string engineTitle, engineType, engineTags;
        private string cachedEngineMode;

        [KSPField(isPersistant = true, groupName = "Engine", groupDisplayName = "Engine", guiName = "Engine Class", guiActive = true, guiActiveEditor = true)]
        public string mainClass;
        [KSPField(isPersistant = true, groupName = "Engine", groupDisplayName = "Engine", guiName = "Engine Sub-Class", guiActive = true, guiActiveEditor = true)]
        public string subClass;
        [KSPField(isPersistant = true)]
        public float minThrottle;
        [KSPField(isPersistant = true)]
        public float minThrust;
        [KSPField()]
        public bool IsHybridEngine = false; // Is it a hybrid airbreathing engine like the RAPIER?
        [KSPField(isPersistant = true)]
        public bool IsLanderEngine = false; // Is it a BOOSTER or SUSTAINER engine intended to land back at the launchpad? This must be manually assigned via a "TBD_[Mod Name].cfg" patch

        [KSPField(isPersistant = false, groupName = "Engine", groupDisplayName = "Engine", guiName = "Minimum Throttle", guiUnits = "%", guiFormat = "F0", guiActive = true, guiActiveEditor = true)]
        public float minThrottlePercent = 0f; // Displays minThrottle as a percentage in the PartActionWindow (PAW)
        [KSPField(groupName = "Engine", groupDisplayName = "Engine", guiName = "Current Throttle", guiUnits = "%", guiFormat = "F1", guiActive = true)]
        public float displayThrottle = 0f;

        // Properties
        //public ModuleEngines ActiveEngine => activeEngine;

        // Methods
        #region Active Game Interface
        public override void OnStart(StartState state) // Initiate active engine
        {
            base.OnStart(state);
            activeEngine = GetActiveEngine();

            if (activeEngine == null)
            {
                Debug.Log($"[TBD] GetActiveEngine returned null.");
                return;
            }
            if (engineMulti != null)
            {
                cachedEngineMode = engineMulti?.mode; // cache the mode on spawn
                Debug.Log($"[TBD] cached engine mode: {cachedEngineMode}");
            }

            else
            {
                CacheEngineInfo();

                if (mainClass == "UNCLASS")
                {
                    mainClass = EngineClassifier.AssignEngineMainClass(engineFmax, engineIspV, engineIspSL).ToString();
                    Debug.Log($"[TBD] AssignedEngineMainClass returned {mainClass} for {engineTitle}");
                }
                if (subClass == "UNCLASS")
                {
                    subClass = EngineClassifier.AssignEngineSubClass(engineTitle, engineType, engineTags, IsHybridEngine, IsLanderEngine).ToString();
                    Debug.Log($"[TBD] AssignedEngineSubClass returned {subClass} for {engineTitle}");
                }
                if (minThrottle == -1f)
                {
                    minThrottle = ((float)EngineClassifier.CalculateMinThrottle(mainClass, subClass, engineFmax, engineIspV, engineIspSL));
                    minThrottlePercent = minThrottle * 100f;
                    Debug.Log($"[TBD] CalculateMinThrottle returned {minThrottle} for {engineTitle}");
                }
                if (minThrottle != -1f)
                {
                    minThrottlePercent = minThrottle * 100f;
                }
            }
            Debug.Log($"[TBD] Engine: {activeEngine}, mainClass: {mainClass}, subClass: {subClass}, minThrottle: {minThrottle}");
        }

        public void FixedUpdate() // Controller for minThrottle enforcement
        {
            if (!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel != vessel || activeEngine == null) return;
            if (engineMulti != null) CheckEngineMultiMode();
            if (activeEngine.isOperational)
            {
                float input = activeEngine.independentThrottle ? activeEngine.independentThrottlePercentage * 0.01f : FlightInputHandler.state.mainThrottle;
                if (input > 0f)
                {
                    activeEngine.minThrust = activeEngine.maxThrust * minThrottle;
                    activeEngine.throttleMin = minThrottle; // TESTING: seeing if throttleMin actually changes the "Throttle Bar" minimum value, API details are limited
                    displayThrottle = activeEngine.currentThrottle * 100f;
                    //Debug.Log($"[TBD] realIsp: {activeEngine.realIsp}");
                }
                else if (input == 0f)
                {
                    //Debug.Log("[TBD] Shutdown!");
                    activeEngine.minThrust = 0f;
                    activeEngine.throttleMin = 0f; // TESTING: seeing if throttleMin actually changes the "Throttle Bar" minimum value, API details are limited
                    displayThrottle = 0f;
                }
            }
        
            // Flow Separation for DEEP SPACE engines
        
            if (mainClass == "DEEPSPACE" && activeEngine.finalThrust > 0f)
            {
                FlowSeparation(); // Check if the DEEP SPACE engine is in a state of flow separation now
            }
            else return;
        }
        #endregion

        #region Functional Methods

        private ModuleEngines GetActiveEngine()
        {
            engineMulti = part.FindModuleImplementing<MultiModeEngine>();
            if (engineMulti != null)
            {
                Debug.Log($"[TBD] found multimodal: {engineMulti}");
                if (engineMulti.primaryEngineID == "AirBreathing")
                {
                    IsHybridEngine = true;
                }
                
                //string activeID = engineMulti.mode;
                foreach (var engine in part.FindModulesImplementing<ModuleEngines>())
                {
                    if (!IsAirBreathingEngine(engine) && !IsIonEngine(engine))
                    {
                        Debug.Log($"[TBD] found engine: {engine.engineID}");
                        return engine;
                    }
                }
                return null;
            }

            if (engineMulti == null)
            {
                foreach (var engine in part.FindModulesImplementing<ModuleEngines>())
                {
                    Debug.Log($"[TBD] found engine: {engine.engineID}");
                    return engine;
                }
            }
            return null;
        }

        private void CacheEngineInfo()
        {
            engineFmax = activeEngine.maxThrust;
            engineIspV = activeEngine.atmosphereCurve.Evaluate(0);
            engineIspSL = activeEngine.atmosphereCurve.Evaluate(1);
            engineTitle = activeEngine.part.partInfo.title;
            engineType = activeEngine.engineType.ToString();
            engineTags = activeEngine.part.partInfo.tags;
        }

        private void CheckEngineMultiMode()
        {
            if (engineMulti != null && engineMulti.mode != cachedEngineMode)
            {
                //activeEngine = GetActiveEngine();
                if (activeEngine != null) CacheEngineInfo();
                cachedEngineMode = engineMulti.mode;
                minThrottlePercent = minThrottle * 100f;
                displayThrottle = 0f;
            }
        }

        private bool IsAirBreathingEngine(ModuleEngines engine)
        {
            foreach (Propellant propellant in engine.propellants)
            {
                if (propellant.name == "IntakeAir") return true;
            }
            return false;
        }

        private bool IsIonEngine(ModuleEngines engine)
        {
                    
            if (engine.engineType == EngineType.Electric) return true;
            foreach (Propellant propellant in engine.propellants)
            {
                if (propellant.name == "ArgonGas" || propellant.name == "XenonGas" || propellant.name == "Lithium") return true;
            }
            return false;
        }
        /*    
                Flow separation occurs when the engine nozzle is overexpanded, causing pressure at the exit of the nozzle (p_e) to be less than the ambient pressure (p_a).
                This induces shocks, side loads, and thrust loss which causes the engine to fail catastrophically if not well managed.
                Simulation at this level of complexity is not possible in KSP, therefore ThrottleBasedDefinition uses the "overheating" of any DEEP SPACE class engine as a proxy for flow separation.
                The goal is to force the player to consider engine selection more intentionally and to use DEEP SPACE engines for their intended role in vacuum.
        */
        private void FlowSeparation()
        {
            double currentPressure = FlightGlobals.getStaticPressure(activeEngine.vessel.altitude, activeEngine.vessel.mainBody);
            double maxPressure = 0.01;

            if (currentPressure >= maxPressure)
            {
                double overPressure = currentPressure - maxPressure;
                double kiloWatts = 75000 * Math.Log10((overPressure / maxPressure)); // Minimum of 100,000 kW flux near 0.01 atm to a maximum ~200,000 kW flux at Kerbin Sea Level for vacuum engines
                part.AddThermalFlux(kiloWatts);
            }
        }
        #endregion
    }
}