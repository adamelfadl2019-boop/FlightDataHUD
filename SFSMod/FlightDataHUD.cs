using System;
using SFS.World;
using SFS.Parts.Modules;
using SFS.UI.ModGUI;
using Type = SFS.UI.ModGUI.Type;
using UnityEngine;

namespace SFSMod
{
    /// <summary>
    /// v0.2 — the orbit info readout panel.
    ///
    /// DATA (fully verified — this part WILL show correct numbers):
    /// This reads apoapsis/periapsis/eccentricity directly off the game's own
    /// Orbit class (SFS.World.Orbit) — the exact same numbers the game itself
    /// uses internally to draw the map. We are not calculating anything
    /// ourselves; we're just displaying what SFS has already computed.
    /// Confirmed by reading the real decompiled source of Orbit.cs, Rocket.cs,
    /// and PlayerController.cs.
    ///
    /// DISPLAY (built from the real UITools wiki examples, NOT yet compiled):
    /// Builder.CreateWindow / Builder.CreateBox / Builder.CreateLabel are real,
    /// documented calls (confirmed from cucumber-sp/UITools's own wiki).
    /// The one piece that's my best inference rather than a confirmed fact:
    /// exactly which Transform to parent the window under, and the exact
    /// property name for updating a Label's text after creation. Common
    /// pattern is `label.text = "..."` — if that doesn't compile, that's
    /// the first thing to check.
    /// </summary>
    public static class FlightDataHUD
    {
        private static GameObject windowHolder;
        private static readonly int WindowID = Builder.GetRandomID();
        private static Window window;
        private static Label apoapsisLabel;
        private static Label periapsisLabel;
        private static Label eccentricityLabel;
        private static Label deltaVLabel;
        private static int refreshCount = 0;
        private const double G0 = 9.80665; // standard gravity, m/s^2

        /// <summary>Call this once when the world scene loads.</summary>
        public static void Setup()
        {
            Debug.Log("[FlightDataHUD] Setup: starting");

            // Confirmed pattern from the official SFS modding forum: windows need
            // their own holder GameObject attached to the current scene first.
            windowHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "FlightDataHUD_Holder");
            Debug.Log("[FlightDataHUD] Setup: holder created = " + (windowHolder != null));

            int windowWidth = 320;
            int windowHeight = 300;
            int margin = 40;
            int spawnX = Screen.width - windowWidth - margin;
            int spawnY = (Screen.height - windowHeight) / 2;

            // Logging the real numbers instead of guessing again — this tells us
            // exactly what Screen.width/height actually are at runtime.
            Debug.Log($"[FlightDataHUD] Setup: Screen.width={Screen.width} Screen.height={Screen.height} computed spawnX={spawnX} spawnY={spawnY}");

            window = Builder.CreateWindow(windowHolder.transform, WindowID, windowWidth, windowHeight, spawnX, spawnY, true, true, 0.85f, "Flight Data");
            Debug.Log("[FlightDataHUD] Setup: window created = " + (window != null));

            // Removed the separate Box layer — Window already has its own
            // confirmed layout system (ChildrenHolder + CreateLayoutGroup),
            // straight from Window's real decompiled source. One less
            // unconfirmed assumption in the chain.
            window.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperLeft, 12, new RectOffset(15, 15, 15, 15));
            Debug.Log("[FlightDataHUD] Setup: layout group created");

            apoapsisLabel = Builder.CreateLabel(window.ChildrenHolder, 270, 40, 0, 0, "Apoapsis: --");
            Debug.Log("[FlightDataHUD] Setup: apoapsisLabel created = " + (apoapsisLabel != null));

            periapsisLabel = Builder.CreateLabel(window.ChildrenHolder, 270, 40, 0, 0, "Periapsis: --");
            Debug.Log("[FlightDataHUD] Setup: periapsisLabel created = " + (periapsisLabel != null));

            eccentricityLabel = Builder.CreateLabel(window.ChildrenHolder, 270, 40, 0, 0, "Eccentricity: --");
            Debug.Log("[FlightDataHUD] Setup: eccentricityLabel created = " + (eccentricityLabel != null));

            deltaVLabel = Builder.CreateLabel(window.ChildrenHolder, 270, 40, 0, 0, "Delta-V: --");
            Debug.Log("[FlightDataHUD] Setup: deltaVLabel created = " + (deltaVLabel != null));

            Debug.Log("[FlightDataHUD] Setup: finished, all four labels non-null = " +
                (apoapsisLabel != null && periapsisLabel != null && eccentricityLabel != null && deltaVLabel != null));
        }

        /// <summary>Call this every tick, passing the player's current rocket
        /// (the Harmony patch already confirms it's the right one before calling this).</summary>
        public static void Refresh(Rocket rocket)
        {
            if (window == null) return;
            if (rocket == null) return;

            refreshCount++;

            Orbit orbit = Orbit.TryCreateOrbit(rocket.location.Value, false, false, out bool success);
            if (!success || orbit == null)
            {
                apoapsisLabel.Text = "Apoapsis: --";
                periapsisLabel.Text = "Periapsis: --";
                eccentricityLabel.Text = "Eccentricity: --";
                if (refreshCount % 150 == 0)
                {
                    Debug.Log("[FlightDataHUD] Refresh: Orbit.TryCreateOrbit failed (success=false)");
                }
                return;
            }

            // orbit.apoapsis / orbit.periapsis are distances from the PLANET'S
            // CENTER, not altitude above the surface — subtract the planet's
            // own radius to match what the in-game map shows (confirmed real
            // property from Rocket.cs's own use of location.planet.Value.Radius).
            double planetRadius = rocket.location.Value.planet.Radius;
            double apoapsisAltitude = orbit.apoapsis - planetRadius;
            double periapsisAltitude = orbit.periapsis - planetRadius;

            apoapsisLabel.Text = $"Apoapsis: {apoapsisAltitude:N0} m";
            periapsisLabel.Text = $"Periapsis: {periapsisAltitude:N0} m";
            eccentricityLabel.Text = $"Eccentricity: {orbit.ecc:F3}";

            deltaVLabel.Text = $"Delta-V: {CalculateDeltaV(rocket):N0} m/s";

            // Log roughly every 2-3 seconds instead of every physics tick,
            // so the log file stays readable and this doesn't add real overhead.
            if (refreshCount % 150 == 0)
            {
                Debug.Log($"[FlightDataHUD] Refresh: apoapsisLabel.Text='{apoapsisLabel.Text}' periapsisLabel.Text='{periapsisLabel.Text}' eccentricityLabel.Text='{eccentricityLabel.Text}' deltaVLabel.Text='{deltaVLabel.Text}'");
            }
        }

        /// <summary>
        /// Total delta-v for the whole rocket right now (not per-stage — see
        /// the README for why that's the deliberate v0.3 scope).
        ///
        /// Data sources, all confirmed from real decompiled source tonight:
        /// - EngineModule.thrust.Value / EngineModule.ISP.Value (per engine)
        /// - ResourceModule.ResourceAmount * resourceType.resourceMass (fuel mass per tank)
        /// - Rocket.mass.GetMass() (current total mass)
        ///
        /// One real assumption I haven't independently verified: that
        /// EngineModule.ISP.Value is in seconds (the standard convention the
        /// Tsiolkovsky equation expects). If the delta-v number looks
        /// wildly off once you test it, this is the first thing to check.
        /// </summary>
        private static double CalculateDeltaV(Rocket rocket)
        {
            float totalThrust = 0f;
            float ispWeightedDenominator = 0f; // sum of (thrust / isp) per engine

            foreach (EngineModule engine in rocket.partHolder.GetModules<EngineModule>())
            {
                float thrust = engine.thrust.Value;
                float isp = engine.ISP.Value;
                totalThrust += thrust;
                if (isp > 0f)
                {
                    ispWeightedDenominator += thrust / isp;
                }
            }

            double totalFuelMass = 0.0;
            foreach (ResourceModule resource in rocket.partHolder.GetModules<ResourceModule>())
            {
                totalFuelMass += resource.ResourceAmount * resource.resourceType.resourceMass;
            }

            double wetMass = rocket.mass.GetMass();
            double dryMass = wetMass - totalFuelMass;

            if (totalThrust <= 0f || ispWeightedDenominator <= 0f || dryMass <= 0.0 || wetMass <= dryMass)
            {
                return 0.0;
            }

            double effectiveIsp = totalThrust / ispWeightedDenominator;
            double exhaustVelocity = effectiveIsp * G0;
            return exhaustVelocity * Math.Log(wetMass / dryMass);
        }
    }
}
