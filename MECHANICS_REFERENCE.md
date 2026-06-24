# RetroShooter — Referencia de Mecánicas, Clases y Lógica

## Índice
1. [Arquitectura general](#1-arquitectura-general)
2. [Estado del juego](#2-estado-del-juego)
3. [Sistema de eventos](#3-sistema-de-eventos)
4. [Datos persistentes entre escenas](#4-datos-persistentes-entre-escenas)
5. [Jugador](#5-jugador)
6. [Armas y proyectiles](#6-armas-y-proyectiles)
7. [Enemigos — Stats y movimiento](#7-enemigos--stats-y-movimiento)
8. [Oleadas y niveles](#8-oleadas-y-niveles)
9. [Boss](#9-boss)
10. [Power-ups](#10-power-ups)
11. [Managers auxiliares](#11-managers-auxiliares)
12. [UI](#12-ui)
13. [Flujos de juego clave](#13-flujos-de-juego-clave)
14. [Patrones arquitectónicos](#14-patrones-arquitectónicos)
15. [ScriptableObjects de datos](#15-scriptableobjects-de-datos)

---

## 1. Arquitectura general

```
BootLoader → Boot scene
           → Menu scene (MenuController)
           → Level scene (Milestone1Game inicializa todo)
           → Win / GameOver scene (EndScreenController)
```

`Milestone1Game` es el punto de entrada de cada nivel:
1. Configura cámara (orthographic, size 5, posición 0,0,-10)
2. Crea `GameObject "Game Systems"` con todos los managers
3. Genera fondo scrolleante de 2 capas (parallax 0.75×)
4. Spawnea player y HUD

Todos los managers son **singletons** accesibles por `Instance`.

---

## 2. Estado del juego

**Clase:** `GameManager` · **Archivo:** `Managers/GameManager.cs`

```
Boot → Playing ─┬─ Paused   (TogglePause → Time.timeScale = 0)
                ├─ GameOver  (PlayerHealth muere con 0 vidas)
                └─ Victory   (Boss derrotado → CompleteLevel)
```

| Método | Qué hace |
|--------|----------|
| `SetState(GameState)` | Cambia estado y lanza `GameEvents.StateChanged` |
| `LoseLife(PlayerHealth)` | Resta vida → respawn o GameOver |
| `AddLife()` | Suma vida (llamado por ExtraLife power-up) |
| `CompleteLevel()` | State = Victory → carga escena Win |
| `TogglePause()` | Alterna timeScale 0 / 1 |
| `Restart()` | Recarga escena actual |

---

## 3. Sistema de eventos

**Clase:** `GameEvents` · **Archivo:** `Core/CombatTypes.cs`

Bus de eventos estático. Cualquier sistema se suscribe sin referencia directa al emisor.

| Evento | Parámetros | Quién lo emite |
|--------|-----------|----------------|
| `ScoreChanged` | `int score` | ScoreManager |
| `HighScoreChanged` | `int score` | ScoreManager |
| `MultiplierChanged` | `int mult` | ScoreManager |
| `LivesChanged` | `int lives` | GameManager |
| `HealthChanged` | `int current, int max` | PlayerHealth |
| `StateChanged` | `GameState` | GameManager |
| `BossHealthChanged` | `string name, int cur, int max` | BossController |
| `BossVisibilityChanged` | `bool visible` | BossController |
| `PowerUpCollected` | `PowerUpType, float duration` | PowerUp |

---

## 4. Datos persistentes entre escenas

**Clase:** `RunSession` · **Archivo:** `Core/CombatTypes.cs`

Clase estática que sobrevive cambios de escena.

```csharp
static int  Lives;
static int  Score;
static int  Multiplier;
static void Reset();   // llamado al iniciar nueva partida
```

`PlayerPrefs` guarda solo el **HighScore** (clave `"HighScore"`).

---

## 5. Jugador

### PlayerController — `Player/PlayerController.cs`

Movimiento con soporte multi-plataforma.

| Input | Plataforma |
|-------|-----------|
| WASD / Flechas | Teclado |
| Stick izquierdo | Gamepad |
| Primer toque | Joystick virtual en móvil |

- Movimiento limitado al viewport (padding 6% en todos los bordes)
- `MoveSpeed` = 5 por defecto; modificable por SpeedBoost
- `FireTouchActive` = true cuando hay un segundo toque activo (disparo automático en móvil)

**MobileJoystick** se instancia en runtime (`MobileJoystick.Create()`). Genera canvas overlay con un knob circular azul de 52px que sigue el dedo.

### PlayerHealth — `Player/PlayerHealth.cs`

Implementa `IDamageable`.

| Propiedad | Descripción |
|-----------|-------------|
| `CurrentHealth` | Solo lectura |
| `MaxHealth` | Solo lectura |
| `IsAlive` | `currentHealth > 0` |

| Método | Descripción |
|--------|-------------|
| `TakeDamage(int)` | Reduce salud + invencibilidad temporal (1.5s) + blink sprite |
| `AddHealth(int)` | Restaura salud (cap en MaxHealth) |
| `Respawn(Vector3)` | Salud completa en posición dada |
| `ActivateShield(float)` | Invencibilidad temporal sin blink |

**Efectos al recibir daño:**
- Screen shake (0.12s)
- Audio `PlayDamage()`
- `ScoreManager.ResetMultiplier()`
- `GameEvents.HealthChanged`

---

## 6. Armas y proyectiles

### PlayerWeapon — `Player/PlayerWeapon.cs`

| Nivel | Disparo |
|-------|---------|
| 1 | 1 proyectil central |
| 2 | 1 central + 2 angulados |
| 3 | 1 central + 2 angulados (daño mayor) |

- `Upgrade()` incrementa nivel (máx 3) — llamado por WeaponUpgrade power-up
- `fireInterval` = 0.2s por defecto
- Input: Espacio / botón Sur gamepad / segundo toque en móvil

### Projectile — `Projectiles/Projectile.cs`

Creado con `Projectile.Create(owner, damage, speed, direction, position)`.

- `owner` = `Player` o `Enemy`
- Colisión por trigger: llama `TakeDamage()` en el target adecuado
- Se destruye si `|y| > 7` o `|x| > 7`

---

## 7. Enemigos — Stats y movimiento

### EnemyData (ScriptableObject)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `maxHealth` | int | Puntos de vida |
| `speed` | float | Velocidad base |
| `scoreValue` | int | Puntos al morir |
| `contactDamage` | int | Daño por colisión con jugador |
| `fireRate` | float | 0 = no dispara |
| `dropChance` | float 0–1 | Probabilidad de soltar power-up |
| `projectilePrefab` | Projectile | Proyectil que usa EnemyShooter |

### Enemy — `Enemies/Enemy.cs`

Implementa `IDamageable`. Movimiento controlado por `Update()` con 8 patrones excluyentes asignados en runtime por `WaveManager`.

#### Patrones de movimiento

| Patrón | Método | Descripción |
|--------|--------|-------------|
| **Recto** | _(default)_ | Baja en línea recta |
| **Zigzag** | `SetZigzag(amplitude, frequency, phase)` | PingPong izq→der desde X de spawn. Amplitud 1–1.5 u. Fase desfasada por enemigo |
| **Sweep** | `SetSweep(horizontalSpeed)` | Deriva constante horizontal. Dirección ± aleatoria por oleada |
| **Homing** | `SetHoming(strength)` | Corrige X hacia el jugador gradualmente (`strength` = velocidad de corrección) |
| **Bounce** | `SetBounce(speed)` | Rebota en los bordes (5%–95% viewport) |
| **Dive** | `SetDive(thresholdY, speed)` | Al llegar a Y umbral, se lanza en línea recta hacia la posición actual del jugador |
| **Stop & Shoot** | `SetStopAndShoot(thresholdY, duration)` | Se detiene en Y umbral durante `duration` segundos. EnemyShooter sigue disparando |
| **Circle** | `SetCircle(center, angularSpeed, orbits)` | Orbita en sentido horario alrededor del centro de pantalla N vueltas, luego sale hacia abajo |
| **Linger** | `SetLinger(thresholdY, duration, lateralSpeed)` | Se detiene en Y umbral, se mueve lateralmente rebotando entre bordes, luego continúa |

**Prioridad de evaluación en Update():**
```
Pausa (Stop/Linger activo) → Dive → Circle → Recto + Zigzag/Sweep/Homing/Bounce
```

**Al morir:**
- `ScoreManager.AddScore(scoreValue)`
- `PowerUpManager.TryDrop(position, dropChance)`
- `VFXManager.SpawnSmallExplosion(position)`
- `AudioManager.PlayExplosion()`

### EnemyShooter — `Enemies/EnemyShooter.cs`

Componente opcional adjunto al Enemy. Dispara independientemente del movimiento.
- Solo dispara si `GameManager.State == Playing`
- Apunta al jugador si existe, sino dispara directo hacia abajo
- `fireInterval` = 2s por defecto

---

## 8. Oleadas y niveles

### WaveData (ScriptableObject)

Configuración de una oleada. Campos principales:

```
enemyPrefab        → qué enemigo spawner
enemyCount         → cuántos (mín 1)
spawnInterval      → segundos entre spawns
delayBeforeNextWave → espera tras destruir todos
horizontalPattern  → AnimationCurve que define posición X de cada enemy (0–1 normalizado)
```

**Secciones de patrón** (cada patrón tiene `*Chance` 0–1 + parámetros):

| Header | Campos clave |
|--------|-------------|
| Zigzag | `zigzagChance`, `zigzagAmplitudeMin/Max`, `zigzagFrequency` |
| Sweep | `sweepChance`, `sweepSpeed` |
| Homing | `homingChance`, `homingStrength` |
| Bounce | `bounceChance`, `bounceSpeed` |
| Dive | `diveChance`, `diveThresholdY`, `diveSpeed` |
| Stop and Shoot | `stopShootChance`, `stopThresholdY`, `stopDuration` |
| Circle / Orbit | `circleChance`, `circleAngularSpeed`, `circleOrbits` |
| Linger | `lingerChance`, `lingerThresholdY`, `lingerDuration`, `lingerLateralSpeed` |

**Selección de patrón por oleada:** Un único `Random.value` se compara contra rangos acumulativos. La probabilidad restante `(1 − suma de chances)` resulta en movimiento recto. Los patrones no se mezclan dentro de la misma oleada.

### LevelData (ScriptableObject)

```
levelName     → nombre para HUD
background    → Sprite de fondo
waves[]       → array ordenado de WaveData
bossPrefab    → BossController a instanciar al terminar oleadas
music         → AudioClip (o null para música procedural)
musicTheme    → int 1–3 para variante musical procedural
nextSceneName → escena a cargar tras victoria
```

### WaveManager — `Managers/WaveManager.cs`

Flujo de ejecución:
```
for each wave:
    SpawnWave() → instancia enemigos con intervalo
    WaitUntil(no quedan Enemy en escena)
    WaitForSeconds(delayBeforeNextWave)

if !loopWaves → Instantiate(bossPrefab)
```

`loopWaves = true` en escenas de sandbox/tutorial (las oleadas se repiten indefinidamente).

**`horizontalPattern` evaluation:**
```csharp
float progress = index / (float)(enemyCount - 1);      // 0 a 1
float normalizedX = curve.Evaluate(progress);           // 0 a 1
worldX = Lerp(0.12, 0.88, normalizedX);                // 12%–88% pantalla
```

---

## 9. Boss

### BossController — `Bosses/BossController.cs`

Implementa `IDamageable`.

**Patrones de ataque** (`BossAttackPattern` enum):

| Patrón | Descripción |
|--------|-------------|
| `Spread` | Abanico de proyectiles en cono de ±38° |
| `AimedBurst` | Apuntado al jugador + dispersión ±5° por proyectil |
| `Spiral` | Rota el patrón 24° por ataque acumulativamente |

**Ciclo de ataque:**
1. Espera 1s al spawnear
2. Llama `FirePattern()` cada `attackInterval` (default 2s)
3. Trigger `"Attack"` en Animator sincroniza animación

**Al recibir daño:**
- Flash de color 0.08s (`BossData.hitFlashColor`)
- Screen shake (0.12s, strength 0.08)
- `GameEvents.BossHealthChanged` → actualiza barra de vida en HUD

**Al morir:**
- Screen shake (0.7s, strength 0.3)
- `VFXManager.SpawnBigExplosion()`
- `AudioManager.PlayBossDeath()`
- `ScoreManager.AddScore(scoreValue)` (default 3000)
- `GameManager.CompleteLevel()`
- `GameEvents.BossVisibilityChanged(false)` → música vuelve a normal

### BossData (ScriptableObject)

```
bossName, maxHealth, phaseCount, scoreValue
attackInterval, attackPattern, projectileCount, projectileSpeed
hitFlashColor
```

---

## 10. Power-ups

### Tipos (`PowerUpType` enum)

| Tipo | Efecto | Método invocado |
|------|--------|-----------------|
| `WeaponUpgrade` | Sube nivel de arma (máx 3) | `PlayerWeapon.Upgrade()` |
| `Shield` | Invencibilidad temporal | `PlayerHealth.ActivateShield(duration)` |
| `SpeedBoost` | Velocidad × 1.5 durante N segundos | `PowerUpManager.ApplySpeedBoost(controller, duration)` |
| `ExtraLife` | +1 vida | `GameManager.AddLife()` |

**Comportamiento:** Cae hacia abajo a 1.4 u/s. Se destruye si sale de pantalla. Colisión por trigger con PlayerHealth.

**Drop:** `PowerUpManager.TryDrop(position, probability)` → `Random.value < probability` para decidir si spawnea. Prefabs configurados en `Milestone1Game`.

---

## 11. Managers auxiliares

### ScoreManager — `Managers/ScoreManager.cs`

```
AddScore(base) → Score += base × Multiplier
               → Multiplier++ (máx 4)
               → GameEvents.ScoreChanged, MultiplierChanged

ResetMultiplier() → Multiplier = 1
                  → GameEvents.MultiplierChanged
```

HighScore persiste en `PlayerPrefs["HighScore"]`.

### AudioManager — `Managers/AudioManager.cs`

Todos los sonidos son **procedurales** (generados en código, sin archivos de audio).

| Método | Sonido |
|--------|--------|
| `PlayMusic(int theme)` | 160 BPM, 4 barras. Kick/snare/hi-hat + bass + arp + melodía. 3 variantes temáticas |
| `PlayShot()` | Tono 760 Hz, 0.07s |
| `PlayExplosion()` | Ruido blanco, 0.18s |
| `PlayDamage()` | Tono bajo 130 Hz, 0.2s |
| `PlayPowerUp()` | Barrido 420–920 Hz, 0.25s |
| `PlayBossHit()` | Tono 220 Hz, 0.08s |
| `PlayBossDeath()` | Ruido con decay, 0.7s |

Boss usa música separada (200 BPM, más intensa). Transición suave 0.4s al aparecer/morir el boss vía `BossVisibilityChanged`.

### VFXManager — `Managers/VFXManager.cs`

```
SpawnSmallExplosion(pos) → Instantiate, Destroy después de 0.75s
SpawnBigExplosion(pos)   → Instantiate, Destroy después de 1s
```

### ScreenShake — `Core/ScreenShake.cs`

```csharp
Shake(float duration, float strength)
// Offset aleatorio via Random.insideUnitCircle
// Un nuevo Shake cancela el anterior
```

### PowerUpManager — `Managers/PowerUpManager.cs`

```
TryDrop(position, probability) → Random check → Instantiate(randomPowerUpPrefab)
ApplySpeedBoost(controller, duration) → speed × 1.5 → coroutine restora al terminar
```

---

## 12. UI

### UIManager — `UI/UIManager.cs`

Escucha todos los `GameEvents` y actualiza el HUD en tiempo real.

| Elemento | Fuente |
|----------|--------|
| Score / HighScore | `GameEvents.ScoreChanged / HighScoreChanged` |
| Vidas (número + ♥) | `GameEvents.LivesChanged` |
| Barra de salud | `GameEvents.HealthChanged` (verde > 50%, amarillo > 25%, rojo < 25%) |
| Multiplicador x1–x4 | `GameEvents.MultiplierChanged` |
| Barra de vida del boss | `GameEvents.BossHealthChanged` (oculta cuando no hay boss) |
| Timer de power-up | `GameEvents.PowerUpCollected` → coroutine con fill animado |

**Restart en GameOver:** R / toque / click llama `GameManager.Restart()`.

### MenuController / EndScreenController

Generan UI en runtime si no hay prefab asignado. Incluyen:
- Título con pulso sinusoidal (cyan), campo de estrellas animado (80 partículas)
- High score desde `PlayerPrefs`
- Botones con targets touch 320×70
- Atajos: Enter / R (retry) / ESC (menú)

---

## 13. Flujos de juego clave

### Matar un enemigo
```
Projectile.OnTriggerEnter2D
→ Enemy.TakeDamage(damage)
→ currentHealth -= damage
→ if ≤ 0: Enemy.Die(awardScore: true)
   → ScoreManager.AddScore(scoreValue)   [Multiplier se incrementa]
   → PowerUpManager.TryDrop(pos, chance)
   → VFXManager.SpawnSmallExplosion(pos)
   → AudioManager.PlayExplosion()
   → Destroy(gameObject)
```

### Jugador recibe daño
```
EnemyProjectile / Enemy.OnTriggerEnter2D
→ PlayerHealth.TakeDamage(amount)
→ currentHealth -= amount
→ ScoreManager.ResetMultiplier()
→ ScreenShake.Shake(0.12s)
→ AudioManager.PlayDamage()
→ VFXManager.SpawnSmallExplosion(pos)
→ GameEvents.HealthChanged
→ if currentHealth ≤ 0:
   → GameManager.LoseLife(this)
      → lives--
      → if lives > 0: PlayerHealth.Respawn(spawnPos)
      → else: GameManager.SetState(GameOver)
```

### Boss derrotado
```
PlayerProjectile → BossController.TakeDamage
→ currentHealth -= damage
→ GameEvents.BossHealthChanged (actualiza barra HUD)
→ if currentHealth ≤ 0:
   → ScoreManager.AddScore(3000)
   → VFXManager.SpawnBigExplosion
   → ScreenShake.Shake(0.7s, 0.3)
   → AudioManager.PlayBossDeath()
   → GameEvents.BossVisibilityChanged(false)  → música vuelve a normal
   → GameManager.CompleteLevel()
      → SetState(Victory)
      → LoadScene("Win")
```

---

## 14. Patrones arquitectónicos

| Patrón | Dónde se usa |
|--------|-------------|
| **Singleton** | GameManager, ScoreManager, AudioManager, VFXManager, PowerUpManager, WaveManager, ScreenShake |
| **Event bus estático** | `GameEvents` — desacoplamiento total entre sistemas |
| **Factory** | `Projectile.Create()`, `MobileJoystick.Create()` |
| **Interface** | `IDamageable` implementado por Enemy, BossController, PlayerHealth |
| **ScriptableObject como datos** | EnemyData, BossData, WaveData, LevelData, PowerUpData |
| **Coroutines para estado temporal** | Invencibilidad, SpeedBoost, Stop/Linger routine, Boss attack loop |
| **Procedural audio/UI** | AudioManager genera sonidos; MenuController genera UI en runtime |
| **Data-driven waves** | WaveData.horizontalPattern (AnimationCurve) define formaciones sin código |

---

## 15. ScriptableObjects de datos

| Asset | Script | Ubicación |
|-------|--------|-----------|
| EnemySmall/Medium/Big | `EnemyData` | `Settings/Data/` |
| BossDeepSpace/Desert/River/Mothership | `BossData` | `Settings/Data/` |
| Wave01_Scouts … M3_Wave04_FinalStorm | `WaveData` | `Settings/Data/` |
| TB_Wave01 … TB_Wave06 | `WaveData` | `Settings/Data/` |
| Level01_DeepSpace … Level03_RiverValley | `LevelData` | `Settings/Data/` |
| PowerUpShield/Speed/Weapon/Life | `PowerUpData` | `Settings/Data/` |

---

*Generado automáticamente a partir del código fuente. Actualizar al añadir nuevas mecánicas.*
