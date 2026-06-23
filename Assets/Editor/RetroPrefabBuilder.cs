using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RetroPrefabBuilder
{
    private const string SessionKey = "RetroSpaceShooter.PrefabsBuilt.v5";
    private const string DataRoot = "Assets/Settings/Data";

    static RetroPrefabBuilder()
    {
        EditorApplication.delayCall += BuildOnce;
    }

    [MenuItem("Tools/Retro Space Shooter/Rebuild All Prefabs")]
    public static void BuildFromMenu()
    {
        SessionState.EraseBool(SessionKey);
        BuildOnce();
    }

    private static void BuildOnce()
    {
        if (SessionState.GetBool(SessionKey, false) ||
            EditorApplication.isCompiling ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        EnsureFolders();

        Dictionary<string, Projectile> projectiles = BuildProjectiles();
        Dictionary<PowerUpType, PowerUpData> powerUpData = BuildPowerUpData();
        Dictionary<string, EnemyData> enemyData = BuildEnemyData(projectiles);
        Dictionary<string, BossData> bossData = BuildBossData();

        BuildPlayerPrefab();
        BuildEnemyPrefabs(enemyData);
        BuildBossPrefabs(bossData, projectiles["BossSpread"]);
        BuildPowerUpPrefabs(powerUpData);
        BuildVfxPrefabs();
        BuildHudPrefab();
        BuildMilestone1Data();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Retro Space Shooter: generated player, enemies, boss, projectiles, power-ups, VFX and UI prefabs.");
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            DataRoot,
            "Assets/Prefabs/Player",
            "Assets/Prefabs/Enemies",
            "Assets/Prefabs/Bosses",
            "Assets/Prefabs/Projectiles",
            "Assets/Prefabs/PowerUps",
            "Assets/Prefabs/VFX",
            "Assets/Prefabs/UI",
        };

        foreach (string folder in folders)
        {
            Directory.CreateDirectory(folder);
        }
    }

    private static Dictionary<string, Projectile> BuildProjectiles()
    {
        var definitions = new[]
        {
            new ProjectileDefinition("PlayerBolt", "player_bolt", ProjectileOwner.Player, Vector2.up, 9f, 1),
            new ProjectileDefinition("PlayerLaser", "player_laser", ProjectileOwner.Player, Vector2.up, 12f, 3),
            new ProjectileDefinition("EnemyOrb", "enemy_orb", ProjectileOwner.Enemy, Vector2.down, 5f, 1),
            new ProjectileDefinition("EnemyFireball", "enemy_fireball", ProjectileOwner.Enemy, Vector2.down, 4f, 2),
            new ProjectileDefinition("EnemyPlasma", "enemy_plasma", ProjectileOwner.Enemy, Vector2.down, 6f, 1),
            new ProjectileDefinition("EnemyMissile", "enemy_missile", ProjectileOwner.Enemy, Vector2.down, 5f, 2),
            new ProjectileDefinition("BossSpread", "boss_spread_bullet", ProjectileOwner.Enemy, Vector2.down, 4.5f, 1),
        };

        Dictionary<string, Projectile> result = new();
        foreach (ProjectileDefinition definition in definitions)
        {
            GameObject instance = new(definition.Name);
            SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite($"Assets/Art/Projectiles/{definition.SpriteName}.png");
            renderer.sortingOrder = 10;

            Rigidbody2D body = instance.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D collider = instance.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.35f, 0.8f);

            Projectile projectile = instance.AddComponent<Projectile>();
            SetField(projectile, "owner", definition.Owner);
            SetField(projectile, "damage", definition.Damage);
            SetField(projectile, "speed", definition.Speed);
            SetField(projectile, "direction", definition.Direction);

            string path = $"Assets/Prefabs/Projectiles/{definition.Name}.prefab";
            GameObject prefab = SavePrefab(instance, path);
            result[definition.Name] = prefab.GetComponent<Projectile>();
        }
        return result;
    }

    private static Dictionary<string, EnemyData> BuildEnemyData(
        IReadOnlyDictionary<string, Projectile> projectiles)
    {
        Dictionary<string, EnemyData> result = new();
        result["Small"] = CreateOrUpdateAsset<EnemyData>(
            $"{DataRoot}/EnemySmall.asset",
            data =>
            {
                data.enemyName = "Small Enemy";
                data.maxHealth = 1;
                data.speed = 2.6f;
                data.scoreValue = 100;
                data.contactDamage = 1;
                data.fireRate = 0f;
                data.dropChance = 0.05f;
                data.projectilePrefab = null;
            });
        result["Medium"] = CreateOrUpdateAsset<EnemyData>(
            $"{DataRoot}/EnemyMedium.asset",
            data =>
            {
                data.enemyName = "Medium Enemy";
                data.maxHealth = 3;
                data.speed = 1.8f;
                data.scoreValue = 250;
                data.contactDamage = 1;
                data.fireRate = 2.2f;
                data.dropChance = 0.12f;
                data.projectilePrefab = projectiles["EnemyOrb"];
            });
        result["Big"] = CreateOrUpdateAsset<EnemyData>(
            $"{DataRoot}/EnemyBig.asset",
            data =>
            {
                data.enemyName = "Big Enemy";
                data.maxHealth = 8;
                data.speed = 1.1f;
                data.scoreValue = 500;
                data.contactDamage = 2;
                data.fireRate = 1.6f;
                data.dropChance = 0.25f;
                data.projectilePrefab = projectiles["EnemyFireball"];
            });
        return result;
    }

    private static Dictionary<PowerUpType, PowerUpData> BuildPowerUpData()
    {
        var definitions = new[]
        {
            new PowerUpDefinition(PowerUpType.WeaponUpgrade, "powerup_weapon", 0f, 1f),
            new PowerUpDefinition(PowerUpType.Shield, "powerup_shield", 6f, 1f),
            new PowerUpDefinition(PowerUpType.SpeedBoost, "powerup_speed", 6f, 1.5f),
            new PowerUpDefinition(PowerUpType.ExtraLife, "powerup_extra_life", 0f, 1f),
        };

        Dictionary<PowerUpType, PowerUpData> result = new();
        foreach (PowerUpDefinition definition in definitions)
        {
            PowerUpData data = CreateOrUpdateAsset<PowerUpData>(
                $"{DataRoot}/PowerUp{definition.Type}.asset",
                asset =>
                {
                    asset.type = definition.Type;
                    asset.duration = definition.Duration;
                    asset.value = definition.Value;
                    asset.sprite = LoadSprite($"Assets/Art/PowerUps/{definition.SpriteName}.png");
                });
            result[definition.Type] = data;
        }
        return result;
    }

    private static Dictionary<string, BossData> BuildBossData()
    {
        Dictionary<string, BossData> result = new();
        result["DeepSpace"] = CreateBossData(
            "BossDeepSpace", "VOID MOTHERSHIP", 100, 2f,
            BossAttackPattern.Spread, 7, 4.5f, new Color(0.3f, 1f, 1f));
        result["Desert"] = CreateBossData(
            "BossDesert", "SOLAR DREADNOUGHT", 150, 1.6f,
            BossAttackPattern.AimedBurst, 5, 5.2f, new Color(1f, 0.65f, 0.15f));
        result["River"] = CreateBossData(
            "BossRiver", "GROTTO CORE", 220, 1.2f,
            BossAttackPattern.Spiral, 10, 5.8f, new Color(0.5f, 1f, 0.45f));
        return result;
    }

    private static BossData CreateBossData(
        string assetName, string bossName, int health, float interval,
        BossAttackPattern pattern, int count, float speed, Color flash) =>
        CreateOrUpdateAsset<BossData>(
            $"{DataRoot}/{assetName}.asset",
            data =>
            {
                data.bossName = bossName;
                data.maxHealth = health;
                data.phaseCount = 3;
                data.scoreValue = 3000;
                data.attackInterval = interval;
                data.attackPattern = pattern;
                data.projectileCount = count;
                data.projectileSpeed = speed;
                data.hitFlashColor = flash;
            });

    private static void BuildPlayerPrefab()
    {
        GameObject player = new("Player");
        player.transform.localScale = Vector3.one * 0.65f;
        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite("Assets/Resources/Sprites/player-retro-spaceship.png");
        renderer.sortingOrder = 6;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.1f, 1.5f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerHealth>();
        PlayerWeapon weapon = player.AddComponent<PlayerWeapon>();
        SetField(weapon, "projectileSprite", LoadSprite("Assets/Art/Projectiles/player_bolt.png"));

        GameObject engine = new("Engine");
        engine.transform.SetParent(player.transform, false);
        engine.transform.localPosition = new Vector3(0f, -1.45f, 0.05f);
        engine.transform.localScale = Vector3.one * 0.55f;
        SpriteRenderer engineRenderer = engine.AddComponent<SpriteRenderer>();
        engineRenderer.sprite = LoadSprite("Assets/Art/Player/Engine/player_engine_00.png");
        engineRenderer.sortingOrder = 5;
        engine.AddComponent<Animator>().runtimeAnimatorController =
            LoadAsset<RuntimeAnimatorController>("Assets/Animations/RetroShooter/PlayerEngine.controller");

        GameObject particlesObject = new("Engine Particles");
        particlesObject.transform.SetParent(player.transform, false);
        particlesObject.transform.localPosition = new Vector3(0f, -1.15f, 0.1f);
        ParticleSystem particles = particlesObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = 0.35f;
        main.startSpeed = 1.8f;
        main.startSize = 0.12f;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.cyan, new Color(1f, 0.35f, 0.05f));
        main.maxParticles = 40;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 24f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.rotation = new Vector3(180f, 0f, 0f);
        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingOrder = 4;

        SavePrefab(player, "Assets/Prefabs/Player/Player.prefab");
    }

    private static void BuildEnemyPrefabs(IReadOnlyDictionary<string, EnemyData> data)
    {
        BuildEnemy("Small", "enemy_small_00", "EnemySmall.controller", data["Small"], false, null);
        BuildEnemy("Medium", "enemy_medium_00", "EnemyMedium.controller", data["Medium"], true, "enemy_orb");
        BuildEnemy("Big", "enemy_big_00", "EnemyBig.controller", data["Big"], true, "enemy_fireball");
    }

    private static void BuildEnemy(
        string name,
        string spriteName,
        string controllerName,
        EnemyData data,
        bool shoots,
        string projectileSpriteName)
    {
        GameObject enemy = new($"Enemy{name}");
        enemy.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite($"Assets/Art/Enemies/{spriteName}.png");
        renderer.sortingOrder = 5;
        enemy.AddComponent<Animator>().runtimeAnimatorController =
            LoadAsset<RuntimeAnimatorController>($"Assets/Animations/RetroShooter/{controllerName}");

        Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = name == "Big" ? new Vector2(1.8f, 1.5f) : new Vector2(1.1f, 1.2f);

        Enemy enemyComponent = enemy.AddComponent<Enemy>();
        SetField(enemyComponent, "data", data);

        if (shoots)
        {
            EnemyShooter shooter = enemy.AddComponent<EnemyShooter>();
            SetField(shooter, "projectileSprite",
                LoadSprite($"Assets/Art/Projectiles/{projectileSpriteName}.png"));
            SetField(shooter, "fireInterval", data.fireRate);
        }

        SavePrefab(enemy, $"Assets/Prefabs/Enemies/Enemy{name}.prefab");
    }

    private static void BuildBossPrefabs(
        IReadOnlyDictionary<string, BossData> data, Projectile projectile)
    {
        BuildBossPrefab("BossDeepSpace", data["DeepSpace"], projectile, Color.white);
        BuildBossPrefab("BossDesert", data["Desert"], projectile, new Color(1f, 0.55f, 0.35f));
        BuildBossPrefab("BossRiver", data["River"], projectile, new Color(0.55f, 1f, 0.7f));
    }

    private static void BuildBossPrefab(
        string prefabName, BossData data, Projectile projectile, Color tint)
    {
        GameObject boss = new(prefabName);
        SpriteRenderer renderer = boss.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite("Assets/Art/Bosses/boss_idle_00.png");
        renderer.color = tint;
        renderer.sortingOrder = 5;
        Animator animator = boss.AddComponent<Animator>();
        animator.runtimeAnimatorController =
            LoadAsset<RuntimeAnimatorController>("Assets/Animations/RetroShooter/Boss.controller");

        Rigidbody2D body = boss.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        BoxCollider2D collider = boss.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2.6f, 2.4f);

        BossController controller = boss.AddComponent<BossController>();
        SetField(controller, "data", data);
        SetField(controller, "projectileSprite", projectile.GetComponent<SpriteRenderer>().sprite);
        SetField(controller, "animator", animator);
        SavePrefab(boss, $"Assets/Prefabs/Bosses/{prefabName}.prefab");
    }

    private static void BuildPowerUpPrefabs(IReadOnlyDictionary<PowerUpType, PowerUpData> data)
    {
        foreach (KeyValuePair<PowerUpType, PowerUpData> entry in data)
        {
            GameObject item = new($"PowerUp{entry.Key}");
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = entry.Value.sprite;
            renderer.sortingOrder = 8;
            Rigidbody2D body = item.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            CircleCollider2D collider = item.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            item.AddComponent<PowerUp>().Initialize(entry.Value);
            SavePrefab(item, $"Assets/Prefabs/PowerUps/PowerUp{entry.Key}.prefab");
        }
    }

    private static void BuildVfxPrefabs()
    {
        BuildVfx("ExplosionSmall", "explosion_small_00", "ExplosionSmall.controller", 20);
        BuildVfx("ExplosionBig", "explosion_big_00", "ExplosionBig.controller", 20);
        BuildVfx("ShieldPulse", "shield_pulse_00", "ShieldPulse.controller", 7);
    }

    private static void BuildVfx(string name, string spriteName, string controllerName, int sortingOrder)
    {
        GameObject effect = new(name);
        SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite($"Assets/Art/Explosions/{spriteName}.png");
        renderer.sortingOrder = sortingOrder;
        effect.AddComponent<Animator>().runtimeAnimatorController =
            LoadAsset<RuntimeAnimatorController>($"Assets/Animations/RetroShooter/{controllerName}");
        SavePrefab(effect, $"Assets/Prefabs/VFX/{name}.prefab");
    }

    private static void BuildHudPrefab()
    {
        GameObject canvasObject = new("HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(480f, 640f);
        canvasObject.AddComponent<GraphicRaycaster>();

        Text score = CreateText(canvas.transform, "Score", "SCORE  000000",
            Vector2.up, new Vector2(16f, -16f), TextAnchor.UpperLeft, 22);
        Text lives = CreateText(canvas.transform, "Lives", "LIVES  3",
            Vector2.one, new Vector2(-16f, -16f), TextAnchor.UpperRight, 22);
        Text health = CreateText(canvas.transform, "Health", "HP  3/3",
            Vector2.up, new Vector2(16f, -48f), TextAnchor.UpperLeft, 18);
        Text highScore = CreateText(canvas.transform, "High Score", "HIGH  000000",
            Vector2.one, new Vector2(-16f, -48f), TextAnchor.UpperRight, 18);
        Text multiplier = CreateText(canvas.transform, "Multiplier", "x1",
            new Vector2(0.5f, 1f), new Vector2(0f, -18f), TextAnchor.UpperCenter, 22);
        GameObject bossRoot = new("Boss Bar");
        bossRoot.transform.SetParent(canvas.transform, false);
        RectTransform bossRect = bossRoot.AddComponent<RectTransform>();
        bossRect.anchorMin = bossRect.anchorMax = new Vector2(0.5f, 1f);
        bossRect.pivot = new Vector2(0.5f, 1f);
        bossRect.anchoredPosition = new Vector2(0f, -58f);
        bossRect.sizeDelta = new Vector2(320f, 48f);
        Text bossName = CreateText(bossRoot.transform, "Boss Name", "BOSS",
            new Vector2(0.5f, 1f), Vector2.zero, TextAnchor.UpperCenter, 16);
        bossName.rectTransform.sizeDelta = new Vector2(320f, 22f);
        GameObject sliderObject = new("Boss Health");
        sliderObject.transform.SetParent(bossRoot.transform, false);
        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.sizeDelta = new Vector2(300f, 18f);
        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(0.1f, 0.08f, 0.12f, 0.95f);
        Slider bossSlider = sliderObject.AddComponent<Slider>();
        GameObject fill = new("Fill");
        fill.transform.SetParent(sliderObject.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(1f, 0.18f, 0.12f);
        bossSlider.fillRect = fillRect;
        bossSlider.targetGraphic = fillImage;
        bossSlider.direction = Slider.Direction.LeftToRight;
        bossRoot.SetActive(false);
        Text state = CreateStateText(canvas.transform);
        state.gameObject.SetActive(false);
        canvasObject.AddComponent<UIManager>().Initialize(
            score, lives, health, highScore, multiplier, state,
            bossRoot, bossSlider, bossName);
        SavePrefab(canvasObject, "Assets/Prefabs/UI/HUD.prefab");
    }

    private static void BuildMilestone1Data()
    {
        Enemy smallEnemy = LoadAsset<GameObject>(
            "Assets/Prefabs/Enemies/EnemySmall.prefab").GetComponent<Enemy>();

        WaveData wave1 = CreateOrUpdateAsset<WaveData>(
            $"{DataRoot}/Wave01_Scouts.asset",
            wave =>
            {
                wave.enemyPrefab = smallEnemy;
                wave.enemyCount = 6;
                wave.spawnInterval = 1.15f;
                wave.delayBeforeNextWave = 1.5f;
                wave.horizontalPattern = new AnimationCurve(
                    new Keyframe(0f, 0.15f),
                    new Keyframe(0.5f, 0.85f),
                    new Keyframe(1f, 0.25f));
            });

        WaveData wave2 = CreateOrUpdateAsset<WaveData>(
            $"{DataRoot}/Wave02_Crossfire.asset",
            wave =>
            {
                wave.enemyPrefab = smallEnemy;
                wave.enemyCount = 8;
                wave.spawnInterval = 0.9f;
                wave.delayBeforeNextWave = 1.5f;
                wave.horizontalPattern = new AnimationCurve(
                    new Keyframe(0f, 0.1f),
                    new Keyframe(0.25f, 0.9f),
                    new Keyframe(0.5f, 0.1f),
                    new Keyframe(0.75f, 0.9f),
                    new Keyframe(1f, 0.5f));
            });

        WaveData wave3 = CreateOrUpdateAsset<WaveData>(
            $"{DataRoot}/Wave03_Assault.asset",
            wave =>
            {
                wave.enemyPrefab = smallEnemy;
                wave.enemyCount = 10;
                wave.spawnInterval = 0.7f;
                wave.delayBeforeNextWave = 2f;
                wave.horizontalPattern = AnimationCurve.Linear(0f, 0.1f, 1f, 0.9f);
            });

        CreateOrUpdateAsset<LevelData>(
            $"{DataRoot}/Level01_DeepSpace.asset",
            level =>
            {
                level.levelName = "Level 1: Deep Space";
                level.background = LoadSprite(
                    "Assets/Art/Backgrounds/level01_deep_space.png");
                level.waves = new[] { wave1, wave2, wave3 };
                level.bossPrefab = LoadAsset<GameObject>(
                    "Assets/Prefabs/Bosses/BossDeepSpace.prefab").GetComponent<BossController>();
                level.music = null;
                level.musicTheme = 0;
                level.nextSceneName = "Level02_DesertCanyon";
            });

        BuildMilestone2Data();
    }

    private static void BuildMilestone2Data()
    {
        Enemy small = LoadAsset<GameObject>(
            "Assets/Prefabs/Enemies/EnemySmall.prefab").GetComponent<Enemy>();
        Enemy medium = LoadAsset<GameObject>(
            "Assets/Prefabs/Enemies/EnemyMedium.prefab").GetComponent<Enemy>();
        Enemy big = LoadAsset<GameObject>(
            "Assets/Prefabs/Enemies/EnemyBig.prefab").GetComponent<Enemy>();

        WaveData wave1 = CreateWave(
            "M2_Wave01_ScoutFormation", small, 8, 0.9f, 1.5f,
            new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 0.9f)));
        WaveData wave2 = CreateWave(
            "M2_Wave02_Gunships", medium, 5, 1.3f, 2f,
            new AnimationCurve(
                new Keyframe(0f, 0.2f), new Keyframe(0.5f, 0.8f), new Keyframe(1f, 0.2f)));
        WaveData wave3 = CreateWave(
            "M2_Wave03_HeavyEscort", big, 3, 2f, 1.5f,
            AnimationCurve.Linear(0f, 0.2f, 1f, 0.8f));
        WaveData wave4 = CreateWave(
            "M2_Wave04_ScoutRush", small, 12, 0.55f, 1.5f,
            new AnimationCurve(
                new Keyframe(0f, 0.5f), new Keyframe(0.25f, 0.1f),
                new Keyframe(0.5f, 0.9f), new Keyframe(0.75f, 0.1f),
                new Keyframe(1f, 0.9f)));
        WaveData wave5 = CreateWave(
            "M2_Wave05_FinalGuard", medium, 8, 0.85f, 2f,
            AnimationCurve.Linear(0f, 0.85f, 1f, 0.15f));

        CreateOrUpdateAsset<LevelData>(
            $"{DataRoot}/Level02_DesertCanyon.asset",
            level =>
            {
                level.levelName = "Milestone 2: Desert Canyon";
                level.background = LoadSprite(
                    "Assets/Art/Backgrounds/level02_desert_canyon.png");
                level.waves = new[] { wave1, wave2, wave3, wave4, wave5 };
                level.bossPrefab = LoadAsset<GameObject>(
                    "Assets/Prefabs/Bosses/BossDesert.prefab").GetComponent<BossController>();
                level.music = null;
                level.musicTheme = 1;
                level.nextSceneName = "Level03_RiverValley";
            });

        BuildMilestone3Data(small, medium, big);
    }

    private static void BuildMilestone3Data(Enemy small, Enemy medium, Enemy big)
    {
        WaveData wave1 = CreateWave("M3_Wave01_Flyers", small, 12, 0.5f, 1f,
            AnimationCurve.EaseInOut(0f, 0.15f, 1f, 0.85f));
        WaveData wave2 = CreateWave("M3_Wave02_Eyes", medium, 9, 0.75f, 1.2f,
            new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.5f, 0.1f), new Keyframe(1f, 0.9f)));
        WaveData wave3 = CreateWave("M3_Wave03_Mechs", big, 5, 1.35f, 1.5f,
            AnimationCurve.Linear(0f, 0.85f, 1f, 0.15f));
        WaveData wave4 = CreateWave("M3_Wave04_FinalStorm", medium, 14, 0.45f, 2f,
            new AnimationCurve(
                new Keyframe(0f, 0.1f), new Keyframe(0.33f, 0.9f),
                new Keyframe(0.66f, 0.1f), new Keyframe(1f, 0.9f)));

        CreateOrUpdateAsset<LevelData>(
            $"{DataRoot}/Level03_RiverValley.asset",
            level =>
            {
                level.levelName = "Level 3: River Valley";
                level.background = LoadSprite("Assets/Art/Backgrounds/level03_river_valley.png");
                level.waves = new[] { wave1, wave2, wave3, wave4 };
                level.bossPrefab = LoadAsset<GameObject>(
                    "Assets/Prefabs/Bosses/BossRiver.prefab").GetComponent<BossController>();
                level.music = null;
                level.musicTheme = 2;
                level.nextSceneName = "Win";
            });
    }

    private static WaveData CreateWave(
        string name, Enemy prefab, int count, float interval,
        float delay, AnimationCurve pattern) =>
        CreateOrUpdateAsset<WaveData>(
            $"{DataRoot}/{name}.asset",
            wave =>
            {
                wave.enemyPrefab = prefab;
                wave.enemyCount = count;
                wave.spawnInterval = interval;
                wave.delayBeforeNextWave = delay;
                wave.horizontalPattern = pattern;
            });

    private static Text CreateText(
        Transform parent, string name, string content, Vector2 anchor,
        Vector2 position, TextAnchor alignment, int fontSize)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(220f, 45f);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = content;
        return text;
    }

    private static Text CreateStateText(Transform parent)
    {
        GameObject textObject = new("Game State");
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 42;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.35f, 0.2f);
        return text;
    }

    private static T CreateOrUpdateAsset<T>(string path, Action<T> configure)
        where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        configure(asset);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static GameObject SavePrefab(GameObject instance, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
        UnityEngine.Object.DestroyImmediate(instance);
        return prefab;
    }

    private static Sprite LoadSprite(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    private static T LoadAsset<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(path);

    private static void SetField(UnityEngine.Object target, string name, object value)
    {
        SerializedObject serialized = new(target);
        SerializedProperty property = serialized.FindProperty(name);
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                property.intValue = Convert.ToInt32(value);
                break;
            case SerializedPropertyType.Float:
                property.floatValue = Convert.ToSingle(value);
                break;
            case SerializedPropertyType.Enum:
                property.enumValueIndex = Convert.ToInt32(value);
                break;
            case SerializedPropertyType.Vector2:
                property.vector2Value = (Vector2)value;
                break;
            case SerializedPropertyType.ObjectReference:
                property.objectReferenceValue = value as UnityEngine.Object;
                break;
            default:
                throw new InvalidOperationException($"Unsupported property {name}: {property.propertyType}");
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct ProjectileDefinition
    {
        public ProjectileDefinition(
            string name, string spriteName, ProjectileOwner owner,
            Vector2 direction, float speed, int damage)
        {
            Name = name;
            SpriteName = spriteName;
            Owner = owner;
            Direction = direction;
            Speed = speed;
            Damage = damage;
        }

        public string Name { get; }
        public string SpriteName { get; }
        public ProjectileOwner Owner { get; }
        public Vector2 Direction { get; }
        public float Speed { get; }
        public int Damage { get; }
    }

    private readonly struct PowerUpDefinition
    {
        public PowerUpDefinition(
            PowerUpType type, string spriteName, float duration, float value)
        {
            Type = type;
            SpriteName = spriteName;
            Duration = duration;
            Value = value;
        }

        public PowerUpType Type { get; }
        public string SpriteName { get; }
        public float Duration { get; }
        public float Value { get; }
    }
}
