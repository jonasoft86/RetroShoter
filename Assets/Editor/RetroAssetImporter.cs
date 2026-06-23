using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class RetroAssetImporter
{
    private const string ArtRoot = "Assets/Art";
    private const string AnimationRoot = "Assets/Animations/RetroShooter";
    private const string SessionKey = "RetroSpaceShooter.AssetsGenerated";

    private readonly struct ClipDefinition
    {
        public ClipDefinition(
            string name,
            string folder,
            string prefix,
            int frameCount,
            float frameRate,
            bool loop)
        {
            Name = name;
            Folder = folder;
            Prefix = prefix;
            FrameCount = frameCount;
            FrameRate = frameRate;
            Loop = loop;
        }

        public string Name { get; }
        public string Folder { get; }
        public string Prefix { get; }
        public int FrameCount { get; }
        public float FrameRate { get; }
        public bool Loop { get; }
    }

    private static readonly ClipDefinition[] Clips =
    {
        new("EnemySmall_Idle", "Enemies", "enemy_small", 4, 8f, true),
        new("EnemyMedium_Idle", "Enemies", "enemy_medium", 4, 8f, true),
        new("EnemyBig_Idle", "Enemies", "enemy_big", 4, 7f, true),
        new("Boss_Idle", "Bosses", "boss_idle", 6, 6f, true),
        new("Boss_Attack", "Bosses", "boss_attack", 6, 10f, false),
        new("Explosion_Small", "Explosions", "explosion_small", 8, 14f, false),
        new("Explosion_Big", "Explosions", "explosion_big", 8, 12f, false),
        new("Shield_Pulse", "Explosions", "shield_pulse", 8, 10f, true),
        new("Player_Engine", "Player/Engine", "player_engine", 8, 12f, true),
    };

    static RetroAssetImporter()
    {
        EditorApplication.delayCall += BuildOncePerSession;
    }

    [MenuItem("Tools/Retro Space Shooter/Rebuild Generated Assets")]
    public static void RebuildFromMenu()
    {
        SessionState.EraseBool(SessionKey);
        BuildOncePerSession();
    }

    private static void BuildOncePerSession()
    {
        if (SessionState.GetBool(SessionKey, false) || EditorApplication.isCompiling)
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        ConfigureTextures();
        Directory.CreateDirectory(AnimationRoot);

        Dictionary<string, AnimationClip> generatedClips = Clips.ToDictionary(
            definition => definition.Name,
            CreateOrUpdateClip);

        CreateSimpleController("EnemySmall", generatedClips["EnemySmall_Idle"]);
        CreateSimpleController("EnemyMedium", generatedClips["EnemyMedium_Idle"]);
        CreateSimpleController("EnemyBig", generatedClips["EnemyBig_Idle"]);
        CreateSimpleController("ExplosionSmall", generatedClips["Explosion_Small"]);
        CreateSimpleController("ExplosionBig", generatedClips["Explosion_Big"]);
        CreateSimpleController("ShieldPulse", generatedClips["Shield_Pulse"]);
        CreateSimpleController("PlayerEngine", generatedClips["Player_Engine"]);
        CreateBossController(generatedClips["Boss_Idle"], generatedClips["Boss_Attack"]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Retro Space Shooter: generated {Clips.Length} animation clips and 8 controllers.");
    }

    private static void ConfigureTextures()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });

        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                continue;
            }

            int pixelsPerUnit = path.Contains("/Backgrounds/", StringComparison.Ordinal)
                ? 64
                : path.Contains("/Bosses/", StringComparison.Ordinal)
                    ? 96
                    : 64;

            bool needsImport =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.filterMode != FilterMode.Point ||
                importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.spritePixelsPerUnit != pixelsPerUnit ||
                importer.spritePivot != new Vector2(0.5f, 0.5f) ||
                !importer.alphaIsTransparency;

            if (!needsImport)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.wrapMode = path.Contains("/Backgrounds/", StringComparison.Ordinal)
                ? TextureWrapMode.Repeat
                : TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }

    private static AnimationClip CreateOrUpdateClip(ClipDefinition definition)
    {
        string clipPath = $"{AnimationRoot}/{definition.Name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }
        else
        {
            clip.ClearCurves();
        }

        clip.name = definition.Name;
        clip.frameRate = definition.FrameRate;

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[definition.FrameCount];
        for (int index = 0; index < definition.FrameCount; index++)
        {
            string spritePath =
                $"{ArtRoot}/{definition.Folder}/{definition.Prefix}_{index:00}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (sprite == null)
            {
                throw new InvalidOperationException($"Missing sprite: {spritePath}");
            }

            keyframes[index] = new ObjectReferenceKeyframe
            {
                time = index / definition.FrameRate,
                value = sprite,
            };
        }

        EditorCurveBinding binding = new()
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite",
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        SerializedObject serializedClip = new(clip);
        SerializedProperty loopTime =
            serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopTime != null)
        {
            loopTime.boolValue = definition.Loop;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void CreateSimpleController(string name, AnimationClip clip)
    {
        string path = $"{AnimationRoot}/{name}.controller";
        AnimatorController controller = LoadOrCreateController(path);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState state = stateMachine.AddState(clip.name);
        state.motion = clip;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
    }

    private static void CreateBossController(AnimationClip idle, AnimationClip attack)
    {
        string path = $"{AnimationRoot}/Boss.controller";
        AnimatorController controller = LoadOrCreateController(path);

        if (!controller.parameters.Any(parameter => parameter.name == "Attack"))
        {
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idle;
        AnimatorState attackState = stateMachine.AddState("Attack");
        attackState.motion = attack;
        stateMachine.defaultState = idleState;

        AnimatorStateTransition toAttack = idleState.AddTransition(attackState);
        toAttack.hasExitTime = false;
        toAttack.duration = 0f;
        toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition toIdle = attackState.AddTransition(idleState);
        toIdle.hasExitTime = true;
        toIdle.exitTime = 1f;
        toIdle.duration = 0f;
        EditorUtility.SetDirty(controller);
    }

    private static AnimatorController LoadOrCreateController(string path)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        return controller != null
            ? controller
            : AnimatorController.CreateAnimatorControllerAtPath(path);
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            stateMachine.RemoveState(childState.state);
        }

        foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
        {
            stateMachine.RemoveStateMachine(childStateMachine.stateMachine);
        }

        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
        {
            stateMachine.RemoveAnyStateTransition(transition);
        }
    }
}
