using KSP.UI.Screens;
using KSP.UI.Screens.Flight;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThrottleBasedDefinition
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class MinThrottleMarker : MonoBehaviour
    {
        ThrottleGauge source;
        RotationalGauge gauge;
        List<ModuleThrottleController> throttleControllers;
        private Dictionary<ModuleThrottleController, RectTransform> markers = new Dictionary<ModuleThrottleController, RectTransform>();
        private bool initialized = false;

        private void LateUpdate()
        {
            if (!initialized)
            {
                TryInitialize();
                return;
            }

            foreach (var kvp in markers) // kvp = KeyValuePair
            {
                ModuleThrottleController controller = kvp.Key;
                RectTransform target = kvp.Value;

                bool ignited = controller.IsActiveEngineIgnited;
                target.gameObject.SetActive(ignited);
                if (ignited)
                {
                    UpdatePosition(controller.GetMinThrottle(), target);
                }
            }
        }

        private void TryInitialize()
        {
            if (FlightGlobals.ActiveVessel == null) return;

            source = FindObjectOfType<ThrottleGauge>();
            if (source == null || source.gauge == null) return;

            gauge = source.gauge;
            throttleControllers = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleThrottleController>();

            Debug.Log($"[TBD] found {throttleControllers.Count} controllers");

            if (throttleControllers.Count == 0) return; // if Vessel/Parts not fully loaded yet, retry next frame

            CreateMinThrottleMarker();
            initialized = true;
        }

        private void CreateMinThrottleMarker()
        {
            foreach (ModuleThrottleController controller in throttleControllers)
            {
                Color classColor = controller.GetClassColorRGB(controller.mainClass);
                Color faded = classColor;
                faded.a = 0.8f;
                CreateClone(controller, faded);
            }
        }
        private void CreateClone(ModuleThrottleController controller, Color color)
        {
            GameObject clone = Instantiate(gauge.pointer.gameObject, gauge.pointer.parent);
            Destroy(clone.GetComponentInChildren<RotationalGauge>());
            Destroy(clone.GetComponentInChildren<ThrottleGauge>());
            RectTransform rectTransform = clone.GetComponent<RectTransform>();
            Debug.Log($"[TBD] rectTransform.pivot={rectTransform.pivot}");
            Debug.Log($"[TBD] rectTransform.anchoredPosition{rectTransform.anchoredPosition}");
            Debug.Log($"[TBD] ((Transform)rectTransform.parent).localRotation={((Transform)rectTransform.parent).localRotation}");
            //rectTransform.anchoredPosition += new Vector2(-75f, 0f); // TESTING: offset of anchoredPosition (default (0f, 10f)) to make markers align with the gauge
            markers[controller] = rectTransform;
            foreach (var graphic in clone.GetComponentsInChildren<Graphic>())
            {
                graphic.color = color;
                graphic.rectTransform.localScale = new Vector3(1f, 1f, 1f);
                Debug.Log($"[TBD] graphic.color={graphic.color}");
            }
            Debug.Log($"[TBD] marker created for {controller.mainClass}, color={color}");
        }

        private void UpdatePosition(float minThrottle, RectTransform target)
        {
            if (!((Object)(object)target == null) && !((Object)(object)gauge == null))
            {
                if (gauge.logarithmic != 0f)
                {
                    minThrottle = ((minThrottle > 0f) ? Mathf.Max(0f, Mathf.Log(minThrottle, gauge.logarithmic)) : 0f);
                }
                float angleDeg = Mathf.Lerp(gauge.minRot, gauge.maxRot, Mathf.InverseLerp(gauge.minValue, gauge.maxValue, minThrottle));
                target.localRotation = Quaternion.AngleAxis(angleDeg, gauge.rotationAxis);
            }
        }
        private void OnEnable()
        {
            GameEvents.onVesselChange.Add(OnVesselChange);
        }
        private void OnDisable()
        {
            GameEvents.onVesselChange.Remove(OnVesselChange);
        }
        private void OnVesselChange(Vessel v)
        {
            foreach (var rectTransform in markers.Values)
                if (rectTransform != null) Object.Destroy(rectTransform.gameObject);
            markers.Clear();
            initialized = false;
        }
        private void OnDestroy()
        {
            foreach (var rectTransform in markers.Values)
                if (rectTransform != null) Object.Destroy(rectTransform.gameObject);
            markers.Clear();
        }
    }
}