# BiLasers
A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that Adds bisexual lasers<br>

This mod provides configuration for Start/End colors, Smoothing amount, and If the mod is enabled.<br>
If you modify the config, all lasers affected will use the new values instantly.<br>

It also provides variables in the `User` Variable Space, for real-time modification.<br><br>
Write to `User/Laser_L_Start` to drive the start color of the left laser
- You must create a DynamicValueVariable<colorX> to write the laser color.

Read `User/InteractionLaser_R_End` to get the real color of the right laser end, rather than the modified one.

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place [BiLasers.dll](https://github.com/l79627550-dot/BiLasers/releases/latest/download/BiLasers.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Resonite logs.

Conflicts with LaserRecolorJank (and probably other laser-based mods)
