# Retro Space Shooter Art Pack

Generated pixel-art assets for the three-level vertical shooter specification.

## Contents

- `Backgrounds`: Deep Space, Desert Canyon, and River Valley at 480x640.
- `Enemies`: Small, Medium, and Big enemies with four idle-flight frames each.
- `Bosses`: Space boss with six idle frames and six attack-charge frames.
- `Player/Engine`: Eight looping engine-flame frames.
- `Projectiles`: Eight player and enemy projectile variants.
- `PowerUps`: Weapon, shield, speed, and extra-life icons.
- `Explosions`: Eight-frame small explosion, big explosion, and shield pulse.

## Unity import and animations

`Assets/Editor/RetroAssetImporter.cs` configures the generated PNG files as:

- Sprite (2D and UI)
- Point filtering
- Uncompressed
- No mipmaps
- Transparent sprites with clamped edges
- Repeating backgrounds

It also creates these animation clips and Animator Controllers:

- EnemySmall_Idle
- EnemyMedium_Idle
- EnemyBig_Idle
- Boss_Idle
- Boss_Attack
- Explosion_Small
- Explosion_Big
- Shield_Pulse
- Player_Engine

The importer runs automatically after Unity recompiles. It can also be triggered
manually from:

`Tools > Retro Space Shooter > Rebuild Generated Assets`
