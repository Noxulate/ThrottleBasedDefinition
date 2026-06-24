using System;
using JetBrains.Annotations;
using UnityEngine;

namespace ThrottleBasedDefinition
{
    public class ModuleThrottleController : PartModule
    {
        // Fields
        ModuleEngines activeEngine;
        MultiModeEngine engineMulti;
        private float _engineMultFlow;
        private double _engineFmax, _engineIspV, _engineIspSL;
        private string _engineTitle, _engineType, _engineTags;
        private string cachedEngineMode, mainClassDesc, subClassDesc, classColor;

        [KSPField(isPersistant = true, groupName = "Engine", groupDisplayName = "Engine", guiName = "Engine Class", guiActive = true, guiActiveEditor = true)]
        public string mainClass;
        [KSPField(isPersistant = true, groupName = "Engine", groupDisplayName = "Engine", guiName = "Engine Sub-Class", guiActive = true, guiActiveEditor = true)]
        public string subClass;
        [KSPField(isPersistant = true)]
        public float minThrottle;

        [KSPField(isPersistant = true)]
        public bool IsHybridEngine = false; // Is it a hybrid airbreathing engine like the RAPIER?
        [KSPField(isPersistant = true)]
        public bool IsLanderEngine = false; // Is it a BOOSTER or SUSTAINER engine intended to land back at the launchpad? This must be manually assigned via a "TBD_[Mod Name].cfg" patch

        [KSPField(isPersistant = false, groupName = "Engine", groupDisplayName = "Engine", guiName = "Minimum Throttle", guiUnits = "%", guiFormat = "F0", guiActive = true, guiActiveEditor = true)]
        public float minThrottlePercent = 0f; // Displays minThrottle as a percentage in the PartActionWindow (PAW)
        [KSPField(groupName = "Engine", groupDisplayName = "Engine", guiName = "Current Throttle", guiUnits = "%", guiFormat = "F1", guiActive = true)]
        public float displayThrottle = 0f;


        // Methods
        #region Active Game Interface

        public override string GetInfo()
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING)
            {
                // HACK: Thanks JonnyOThan for the GetInfo hook trick: "compiling module infos is one of the last things that happens when compiling the prefab."
                activeEngine = GetActiveEngine();
                CacheEngineInfo();
                SetThrottleController();
                activeEngine.minThrust = minThrottle * activeEngine.maxThrust;
                mainClassDesc = mainClass switch
                {
                    "BOOSTER" => "Booster engines are best for atmospheric launch.<br>Highest TWRs.<br>Low efficiency.",
                    "SUSTAINER" => "Jack of all trades, master of some.<br>Medium TWRs.<br>Medium efficiency.",
                    "ORBITAL" => "Good in all low pressure situations.<br>Low-Medium TWRs.<br>Better efficiency.",
                    "DEEPSPACE" => "Awesome in vacuum only, it will destroy itself if fired in atmosphere!<br>Low TWRs.<br>Incredible efficiency.",
                    _ => "Whelp, we don't know apparently. Report this error.",
                };

                classColor = mainClass switch
                {
                    "BOOSTER" => "#D85A30",
                    "SUSTAINER" => "#BA7517",
                    "ORBITAL" => "#1D9E75",
                    "DEEPSPACE" => "#776bfa", 
                    _ => "#FFFFFF",
                };
                string info = "<b>Class: <color=" + classColor + $">{ mainClass}</color></b><br><br>{mainClassDesc}<br><br><b>Minimum Throttle:</b> {minThrottlePercent}%  of  Max Thrust.";

                subClassDesc = subClass switch
                {
                    "EXOTIC" => "Is this even really an 'engine' anymore..?",
                    "NUCLEAR" => "Harnessing the power of atoms and E=mc^2 to produce thrust. Caution, might be slightly radioactive.",
                    "HYBRID" => "Air-breathing rocket engine! Great choice for SSTOs.",
                    "AEROSPIKE" => "Spikey rockets. Uses the ambient air-pressure to form the 'nozzle', resulting in higher efficiency at all altitudes.",
                    "LANDER" => "This engine is designed to land reusable rockets back where they came from, hopefully without Rapid Unscheduled Disassembly.",
                    "MANEUVER" => "This engine is meant to help steer or make minor attitude adjustments.",
                    _ => "",
                };
                if (subClass != "NONE") info += $"<br><br><b>Sub-Class:</b> {subClass}<br><br>{subClassDesc}";
                return info;
            }
            else return base.GetInfo();
        }

        public string GetModuleTitle()
        {
            return "Engine Classification";
        }
        public override string GetModuleDisplayName()
        {
            return "Engine Classification"; // TODO: Localization... #autoLOC_012345
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            activeEngine = GetActiveEngine();
            if (activeEngine == null)
            {
                Debug.LogError($"[TBD] GetActiveEngine returned null.");
                return;
            }
            if (engineMulti != null)
            {
                cachedEngineMode = engineMulti?.mode; // cache the mode on spawn
                Debug.Log($"[TBD] cached engine mode: {cachedEngineMode}");
            }
        }

        public void FixedUpdate() // Controller for minThrottle enforcement
        {
            if (!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel != vessel || activeEngine == null) return;
            if (engineMulti != null) CheckEngineMultiMode();
            if (activeEngine.EngineIgnited)
            {
                float input = activeEngine.independentThrottle ? activeEngine.independentThrottlePercentage * 0.01f : vessel.ctrlState.mainThrottle;

                /*if (activeEngine.EngineIgnited && input >= minThrottle)
                {
                    activeEngine.UnFlameout(true);
                }

                if (activeEngine.isOperational)
                {
                    if (input >= minThrottle)
                    {
                        activeEngine.minThrust = activeEngine.maxThrust * minThrottle;
                        activeEngine.throttleMin = minThrottle;
                        displayThrottle = activeEngine.currentThrottle * 100f;
                    }
                    else if (input < minThrottle)
                    {
                        activeEngine.Flameout("Below minimum throttle!");
                        activeEngine.minThrust = 0f;
                        activeEngine.throttleMin = 0f;
                        displayThrottle = 0f;
                    }
                }*/

                if (input >= minThrottle)
                {
                    activeEngine.UnFlameout(true);
                    activeEngine.minThrust = activeEngine.maxThrust * minThrottle;
                    activeEngine.multFlow = 1f;
                    activeEngine.minFuelFlow = minThrottle * activeEngine.maxFuelFlow;
                    displayThrottle = activeEngine.currentThrottle * 100f;
                    //activeEngine.ActivatePowerFX();
                    //activeEngine.flameoutBar = 0f;
                    //activeEngine.UpdateThrottle();
                    //activeEngine.CalculateThrust();
                    //activeEngine.flameout = false;
                    //activeEngine.maxThrust = (float)_engineFmax;
                    //activeEngine.maxFuelFlow = _engineMaxFuelFlow;
                    //activeEngine.throttleMin = minThrottle;
                    //Debug.Log($"[TBD] realIsp: {activeEngine.realIsp}");
                }
                else if (input < minThrottle)
                {
                    activeEngine.Flameout("Below minimum throttle!");
                    activeEngine.minThrust = 0f;
                    activeEngine.multFlow = 0f;
                    activeEngine.minFuelFlow = 0f;
                    displayThrottle = 0f;
                    //activeEngine.flameoutBar = float.MaxValue;
                    //activeEngine.maxThrust = 0f;
                    //activeEngine.requestedMassFlow = 0f;
                    //activeEngine.finalThrust = 0f;
                    //activeEngine.flameout = true;
                    //activeEngine.DeactivatePowerFX();
                    //activeEngine.currentThrottle = 0f;
                    //activeEngine.maxFuelFlow = 0f;
                    //activeEngine.throttleMin = 0f; // TESTING: seeing if throttleMin actually changes the "Throttle Bar" minimum value, API details are limited
                }
                //}

                // Flow Separation for DEEP SPACE engines

                if (mainClass == "DEEPSPACE" && activeEngine.finalThrust > 0f)
                {
                    FlowSeparation(); // Check if the DEEP SPACE engine is in a state of flow separation now
                }
                else return;
            }
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
                foreach (ModuleEngines engine in part.FindModulesImplementing<ModuleEngines>())
                {
                    if (!IsAirBreathingEngine(engine))
                    {
                        Debug.Log($"[TBD] found engine: {engine.engineID}");
                        Debug.Log($"[TBD] MultFlow: {_engineMultFlow}");
                        return engine;
                    }
                }
                return null;
            }

            if (engineMulti == null)
            {
                foreach (ModuleEngines engine in part.FindModulesImplementing<ModuleEngines>())
                {
                    if (!IsAirBreathingEngine(engine))
                    {
                        Debug.Log($"[TBD] found engine: {engine.engineID}");
                        Debug.Log($"[TBD] MultFlow: {_engineMultFlow}");
                        return engine;
                    }
                }
            }
            return null;
        }

        private void CacheEngineInfo()
        {
            _engineFmax = activeEngine.maxThrust;
            _engineIspV = activeEngine.atmosphereCurve.Evaluate(0);
            _engineIspSL = activeEngine.atmosphereCurve.Evaluate(1);
            _engineMultFlow = activeEngine.multFlow;
            
            _engineTitle = activeEngine.part.partInfo.title;
            _engineType = activeEngine.engineType.ToString();
            _engineTags = activeEngine.part.partInfo.tags;
        }

        public void SetThrottleController()
        {
            if (mainClass == "UNCLASS")
            {
                mainClass = EngineClassifier.AssignEngineMainClass(_engineFmax, _engineIspV, _engineIspSL).ToString();
                Debug.Log($"[TBD] AssignedEngineMainClass returned {mainClass} for {_engineTitle}");
            }
            if (subClass == "UNCLASS")
            {
                subClass = EngineClassifier.AssignEngineSubClass(_engineTitle, _engineType, _engineTags, IsHybridEngine, IsLanderEngine).ToString();
                Debug.Log($"[TBD] AssignedEngineSubClass returned {subClass} for {_engineTitle}");
            }
            if (minThrottle == -1f)
            {
                minThrottle = ((float)EngineClassifier.CalculateMinThrottle(mainClass, subClass, _engineFmax, _engineIspV, _engineIspSL));
                minThrottlePercent = minThrottle * 100f;
                Debug.Log($"[TBD] CalculateMinThrottle returned {minThrottle} for {_engineTitle}");
            }
            if (minThrottle != -1f)
            {
                minThrottlePercent = minThrottle * 100f;
                activeEngine.minThrust = activeEngine.maxThrust * minThrottle;
            }
        }

        private void CheckEngineMultiMode()
        {
            if (engineMulti != null && engineMulti.mode != cachedEngineMode)
            {
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