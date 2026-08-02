# Throttle Based Definition

Throttle Based Definition is a mod for Kerbal Space Program that adds semi-realistic features to bi-propellant rocket engine parts in the game. In vanilla KSP all engines (except Solid Rocket Boosters) can throttle from 0%-100%, this behavior is incredibly unrealistic as most engines in reality have a minimum "floor" beyond which the engine can no longer function. TBD makes bi-propellant rocket engines have a more realistic throttle range (ex. 61%-100%) among other features. 

The intent of Throttle Based Definition is to necessitate the player putting more thought into engine selection for a given mission and introduce more complexity to rocket design, while still being "fun". All without requiring bespoke patches for every other mod.

## Key Concepts

### Engine Classification
Engines fall into one of four main "classes"; BOOSTER, SUSTAINER, ORBITAL, or VACUUM.
   - **BOOSTER** class engines are the "sprinters" the "muscle" aka "big dumb boosters", if you need to lift something heavy from Kerbin's surface these engines should be your go-to.
   - **SUSTAINER** class engines are the "jack of all trades" engines, like the Space Shuttle's legendary RS-25. They are meant to fill the "gap" between surface and vacuum operations.
   - **ORBITAL** class engines are a unique one for KSP, some engines (like the LV-909 'Terrier') don't quite fall into any other class nicely. They are higher thrust, marginally lower efficency, vacuum or landing engines.
   - **VACUUM** class engines are the high efficiency "marathon runners" of the engine classes. VACUUM class engines excel at operations in... vacuum (I know, who would have guess?).
     Note: It is best that VACUUM engines only operate in deep space (~0 atm) environments, turn them on in atmosphere and you may have some "explosive" results from 'Flow Separation'.

Engines can also have a "sub-class" specialty;  EXOTIC, NUCLEAR, HYBRID, AEROSPIKE, LANDER, or MANEUVER.
   - **EXOTIC** sub-class engines are barely even recognizable as an engine, such as anti-matter engines, z-pinch, warp drives, etc.
   - **NUCLEAR** sub-class engines have incredibly high efficiency thanks to using hot radioactive rocks to heat up their fuel instead of 'combustion'.
   - **HYBRID** sub-class engines are engines that have two operating modes, typical one closed cycle rocket and one open cycle 'air-breathing' engine, such as the RAPIER.
   - **AEROSPIKE** sub-class engines are efficient across a wide range of pressures thanks to the surrounding atmosphere becoming their engine "nozzle'.
   - **LANDER** sub-class engines are typically BOOSTER or SUSTAINER engines that have the ability to deep-throttle so they might return to the launch pad.
   - **MANEUVER** sub-class engines are vernier or tiny engines used to precise maneuvers.

### Minimum Throttle Calculation
Based off of the engine's main-class and sub-class the respective engine's minimum throttle is calculated based off of interpolating the engine's mass flow rate (mdot) and the difference between their vacuum specific impulse and sea-level specific impulse (ΔIsp).
   - Using research done on the RS-25, F-1, and RL-10 engines, their real-life values were empirically interpolated to create a formula that matches reasonable minimum throttle to their KSP analogs:
      - **RS-25** -> **KS-25 'Vector'**, minThrottle = 61%
      - **F-1** -> **KE-1 'Mastodon'**, minThrottle = 100% (on or off)
      - **RL-10** -> **CL-10 'Hecate'** [CryoEngines], minThrottle = 10%
    From these values the following formula was made:

  <img width="466" height="41" alt="image" src="https://github.com/user-attachments/assets/ce6c0d03-6316-4773-a07b-7a3c683b4b33" />

  Where mdot is: 
  
  <img width="657" height="88" alt="image" src="https://github.com/user-attachments/assets/2534f948-a8b5-408d-a597-7f9d0290e505" />

### Flow Separation
For **VACUUM** class engines only:

Flow separation occurs when the engine nozzle is overexpanded, causing pressure at the exit of the nozzle (p_e) to be less than the ambient pressure (p_a). This induces shocks, side loads, and thrust loss which causes the engine to fail catastrophically if not well managed.
Simulation at this level of complexity is not possible in KSP, therefore ThrottleBasedDefinition uses the "overheating" of any VACUUM class engine when below ~0.01 atm of pressure as a proxy for flow separation.
The goal is to force the player to consider engine selection more intentionally and to use **VACUUM** engines only in vacuum environments.
