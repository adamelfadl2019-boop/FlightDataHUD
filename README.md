Flight Data HUD
A code mod for Spaceflight Simulator that restores the numeric orbit readout removed in version 1.4, and adds a live delta-v calculator — both in one clean panel while you fly.
What it shows
Apoapsis / Periapsis — altitude in meters, restoring the detailed numeric display the game used to have
Eccentricity
Delta-V — total remaining delta-v for your current rocket, calculated live from your actual engines, fuel, and mass (not an estimate — real per-engine thrust/Isp weighted across your whole vehicle)
The window is draggable and stays out of your way — move it wherever you want.
Installation
Make sure you're on a version of SFS with the built-in mod loader (1.5.8.5+)
Download UITools.dll from cucumber-sp/UITools — this mod depends on it
Download FlightDataHUD.dll from this repo's Releases page
In-game, click Open Mods Folder from the main menu
Drop both `.dll` files directly into that Mods folder (not in a subfolder)
Launch the game — both mods should show as loaded in the mod list
Known limitations (v0.3)
Delta-v is calculated for the whole rocket right now, not per-stage. A future version may add per-stage breakdown, but that requires digging into SFS's staging system separately — this version gives you one accurate total number, not a stage-by-stage plan.
Credits
Built on the 105-Code/sfs-mod mod template
Uses UITools by cucumber-sp for the in-game UI
