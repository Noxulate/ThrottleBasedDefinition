using System;

namespace ThrottleBasedDefinition
{
    internal static class EngineClassifier
    {

        public const double g0 = 9.80665; // gravitational constant of Earth & Kerbin (m/s^2)
        public const double k = 0.0205; // variation constant for mass flow rate (empirical)
        public const double a = 0.638; // scaling exponent for mass flow rate (empirical)

        public static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
        public enum EngineMainClass { UNCLASS, BOOSTER, SUSTAINER, ORBITAL, DEEPSPACE, UNKNOWN }
        public enum EngineSubClass { UNCLASS, EXOTIC, NUCLEAR, HYBRID, AEROSPIKE, LANDER, MANEUVER, NONE }

        public static EngineMainClass AssignEngineMainClass(double Fmax, double IspV, double IspSL)
        {
            double dIsp = IspV - IspSL;
            double mdot = (Fmax * 1000) / (IspV * g0);

            if (mdot > 0)
            {
                if (mdot >= 300 && dIsp < 150) return EngineMainClass.BOOSTER;
                else if (mdot < 300 && dIsp < 125) return EngineMainClass.SUSTAINER;
                else if (mdot < 300 && dIsp >= 125 && dIsp < 325) return EngineMainClass.ORBITAL;
                else if (mdot < 300 && dIsp >= 325) return EngineMainClass.DEEPSPACE;
                else return EngineMainClass.UNKNOWN;
            }
            return EngineMainClass.UNKNOWN;
        }
        public static EngineSubClass AssignEngineSubClass(string engineTitle, string engineType, string engineTags, bool IsHybridEngine, bool IsLanderEngine)
        {
            if (engineTags.Contains("exotic") || engineTags.Contains("antimatter")) return EngineSubClass.EXOTIC;
            else if (engineType == EngineType.Nuclear.ToString() || engineTags.Contains("nuclear") || engineTags.Contains("fission") || engineTags.Contains("fusion")) return EngineSubClass.NUCLEAR;
            else if (IsHybridEngine) return EngineSubClass.HYBRID;
            else if (engineTags.Contains("aerospike") || engineTitle.Contains("Aerospike")) return EngineSubClass.AEROSPIKE;
            else if (IsLanderEngine) return EngineSubClass.LANDER;
            else if (engineTags.Contains("vernier") || engineTags.Contains("maneuver") || engineTitle.Contains("Vernier") || engineTitle.Contains("vernier")) return EngineSubClass.MANEUVER;
            else return EngineSubClass.NONE;
        }
        public static float CalculateMinThrottle(string mainClass, string subClass, double Fmax, double IspV, double IspSL)
        {
            double mdot = (Fmax * 1000) / (IspV * g0);
            double minThrottle = k * Math.Pow(mdot, a);

            // Handle clamping subclasses first since they have unique properties for minThrottle
            switch (subClass)
            {
                case "EXOTIC": return (float)Math.Round(Clamp(minThrottle, 0.0, 0.0), 2);
                case "NUCLEAR": return (float)Math.Round(Clamp(minThrottle, 0.0, 0.10), 2);
                case "HYBRID": return (float)Math.Round(Clamp(minThrottle, 0.1, 0.4), 2);
                case "AEROSPIKE": return (float)Math.Round(Clamp(minThrottle, 0.1, 0.4), 2);
                case "LANDER": return (float)Math.Round(Clamp(minThrottle, 0.0, 0.4), 2);
                case "ELECTRIC": return (float)Math.Round(Clamp(minThrottle, 0.0, 0.0), 2);
                case "MANEUVER": return (float)Math.Round(Clamp(minThrottle, 0.0, 0.0), 2);
            }

            // If subclass is "NONE" then mainClass will determine minThrottle
            switch (mainClass)
            {
                case "BOOSTER": return (float)Math.Round(Clamp(minThrottle, 0.75, 1.0), 2);
                case "SUSTAINER": return (float)Math.Round(Clamp(minThrottle, 0.25, 0.75), 2);
                case "ORBITAL": return (float)Math.Round(Clamp(minThrottle, 0.10, 0.25), 2);
                case "DEEPSPACE": return (float)Math.Round(Clamp(minThrottle, 0.0, 0.15), 2);
                default: return (float)Math.Round(Clamp(minThrottle, 0.0, 1.0), 2);
            }
        }
    }
}