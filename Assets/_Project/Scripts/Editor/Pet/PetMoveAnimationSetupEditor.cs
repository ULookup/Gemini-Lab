#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.EditorTools.Pet
{
    /// <summary>
    /// Builds pet move / idle / selected interact clips and wires them into the controller.
    /// </summary>
    public static class PetMoveAnimationSetupEditor
    {
        private const string MoveSpriteFolder = "Assets/_Project/Art/Sprites/Pet/Frames/Move";
        private const string MoveFrontFolder = MoveSpriteFolder + "/正面";
        private const string MoveBackFolder = MoveSpriteFolder + "/背面";
        private const string MoveSideFolder = MoveSpriteFolder + "/侧面";

        private const string IdleSpriteFolder = "Assets/_Project/Art/Sprites/Pet/Frames/Idle";
        private const string IdleFrontFolder = IdleSpriteFolder + "/正面";
        private const string IdleBackFolder = IdleSpriteFolder + "/背面";
        private const string IdleSideFolder = IdleSpriteFolder + "/侧面";

        private const string InteractRootFolder = "Assets/_Project/Art/Sprites/Pet/Frames/Interact";
        private const string InteractReadFolder = InteractRootFolder + "/self/read";
        private const string InteractBesideDoorFolder = InteractRootFolder + "/self/beside door";
        private const string SleepFolder = InteractRootFolder + "/self/sleep";

        private const string AnimationFolder = "Assets/_Project/Animations/Pet";
        private const string FrontClipPath = AnimationFolder + "/Pet_Angel_Move_Front.anim";
        private const string BackClipPath = AnimationFolder + "/Pet_Angel_Move_Back.anim";
        private const string SideClipPath = AnimationFolder + "/Pet_Angel_Move_Side.anim";
        private const string IdleFrontClipPath = AnimationFolder + "/Pet_Angel_Idle_Front.anim";
        private const string IdleBackClipPath = AnimationFolder + "/Pet_Angel_Idle_Back.anim";
        private const string IdleSideClipPath = AnimationFolder + "/Pet_Angel_Idle_Side.anim";
        private const string InteractReadClipPath = AnimationFolder + "/Pet_Angel_Interact_Read.anim";
        private const string InteractBesideDoorClipPath = AnimationFolder + "/Pet_Angel_Interact_BesideDoor.anim";
        private const string SleepClipPath = AnimationFolder + "/Pet_Angel_Sleep.anim";
        private const string ControllerPath = AnimationFolder + "/Pet_Angel.controller";

        private const float DefaultFps = 12f;
        private const int InteractEdgeHoldFrames = 10;
        private const int IdleEdgeHoldFrames = 4;

        [MenuItem("Tools/GeminiLab/Pet/Setup Move Animations")]
        public static void SetupMoveAnimations()
        {
            try
            {
                EnsureFolder(AnimationFolder);

                List<Sprite> moveFrontSprites = LoadMoveSprites(MoveFrontFolder, "Pet_Angel_Move_Front_");
                List<Sprite> moveBackSprites = LoadMoveSprites(MoveBackFolder, "Pet_Angel_Move_Back_");
                List<Sprite> moveSideSprites = LoadMoveSprites(MoveSideFolder, "Pet_Angel_Move_Side_");

                List<Sprite> idleFrontSprites = LoadSpritesFromFolder(IdleFrontFolder);
                List<Sprite> idleBackSprites = LoadSpritesFromFolder(IdleBackFolder);
                List<Sprite> idleSideSprites = LoadSpritesFromFolder(IdleSideFolder);

                List<Sprite> interactReadSprites = LoadSpritesFromFolder(InteractReadFolder);
                List<Sprite> interactBesideDoorSprites = LoadSpritesFromFolder(InteractBesideDoorFolder);
                List<Sprite> sleepSprites = LoadSpritesFromFolder(SleepFolder);

                if (moveFrontSprites.Count == 0 || moveBackSprites.Count == 0 || moveSideSprites.Count == 0)
                {
                    Debug.LogError($"[PetAnimSetup] Missing move sequence frames. Front={moveFrontSprites.Count}, Back={moveBackSprites.Count}, Side={moveSideSprites.Count}");
                    return;
                }

                AnimationClip moveFrontClip = CreateOrUpdateSpriteClip(FrontClipPath, moveFrontSprites, DefaultFps);
                AnimationClip moveBackClip = CreateOrUpdateSpriteClip(BackClipPath, moveBackSprites, DefaultFps);
                AnimationClip moveSideClip = CreateOrUpdateSpriteClip(SideClipPath, moveSideSprites, DefaultFps);

                AnimationClip? idleFrontClip = idleFrontSprites.Count > 0
                    ? CreateOrUpdateSpriteClipWithHeldEdges(IdleFrontClipPath, idleFrontSprites, DefaultFps, IdleEdgeHoldFrames, IdleEdgeHoldFrames)
                    : null;
                AnimationClip? idleBackClip = idleBackSprites.Count > 0
                    ? CreateOrUpdateSpriteClipWithHeldEdges(IdleBackClipPath, idleBackSprites, DefaultFps, IdleEdgeHoldFrames, IdleEdgeHoldFrames)
                    : null;
                AnimationClip? idleSideClip = idleSideSprites.Count > 0
                    ? CreateOrUpdateSpriteClipWithHeldEdges(IdleSideClipPath, idleSideSprites, DefaultFps, IdleEdgeHoldFrames, IdleEdgeHoldFrames)
                    : null;

                AnimationClip? interactReadClip = interactReadSprites.Count > 0
                    ? CreateOrUpdateSpriteClipWithHeldEdges(InteractReadClipPath, interactReadSprites, DefaultFps, InteractEdgeHoldFrames, InteractEdgeHoldFrames)
                    : null;
                AnimationClip? interactBesideDoorClip = interactBesideDoorSprites.Count > 0
                    ? CreateOrUpdateSpriteClipWithHeldEdges(InteractBesideDoorClipPath, interactBesideDoorSprites, DefaultFps, InteractEdgeHoldFrames, InteractEdgeHoldFrames)
                    : null;
                AnimationClip? sleepClip = sleepSprites.Count > 0
                    ? CreateOrUpdateSpriteClip(SleepClipPath, sleepSprites, DefaultFps)
                    : null;

                AnimatorController controller = CreateOrUpdateController(
                    ControllerPath,
                    moveFrontClip,
                    moveBackClip,
                    moveSideClip,
                    idleFrontClip,
                    idleBackClip,
                    idleSideClip,
                    interactReadClip,
                    interactBesideDoorClip,
                    sleepClip);
                int assigned = BindControllerToPetControllers(controller);
                EditorSceneManager.MarkAllScenesDirty();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                int idleClipCount = (idleFrontClip is null ? 0 : 1) + (idleBackClip is null ? 0 : 1) + (idleSideClip is null ? 0 : 1);
                int interactClipCount = (interactReadClip is null ? 0 : 1) + (interactBesideDoorClip is null ? 0 : 1) + (sleepClip is null ? 0 : 1);
                Debug.Log($"[PetAnimSetup] Completed. Move clips updated: 3, idle clips updated: {idleClipCount}, interact/sleep clips updated: {interactClipCount}, controller: {controller.name}, animators assigned/updated: {assigned}.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static int BindControllerToPetControllers(AnimatorController controller)
        {
            int assigned = 0;
            PetController[] pets = UnityEngine.Object.FindObjectsByType<PetController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < pets.Length; i++)
            {
                PetController pet = pets[i];
                if (pet == null)
                {
                    continue;
                }

                bool hasAnimator = pet.TryGetComponent(out Animator animator);
                if (!hasAnimator || animator == null)
                {
                    animator = pet.gameObject.AddComponent<Animator>();
                }

                SerializedObject petSerialized = new(pet);
                SerializedProperty? controllerProperty = petSerialized.FindProperty("_movementController");
                if (controllerProperty is not null && controllerProperty.objectReferenceValue != controller)
                {
                    controllerProperty.objectReferenceValue = controller;
                    petSerialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(pet);
                }

                if (animator.runtimeAnimatorController != controller)
                {
                    animator.runtimeAnimatorController = controller;
                    EditorUtility.SetDirty(animator);
                    assigned++;
                }
            }

            return assigned;
        }

        private static AnimatorController CreateOrUpdateController(
            string path,
            AnimationClip moveFront,
            AnimationClip moveBack,
            AnimationClip moveSide,
            AnimationClip? idleFront,
            AnimationClip? idleBack,
            AnimationClip? idleSide,
            AnimationClip? interactRead,
            AnimationClip? interactBesideDoor,
            AnimationClip? sleep)
        {
            AnimatorController? controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller is null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveDir", AnimatorControllerParameterType.Int);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState moveFrontState = GetOrCreateState(sm, "Move_Front", moveFront);
            _ = GetOrCreateState(sm, "Move_Back", moveBack);
            _ = GetOrCreateState(sm, "Move_Side", moveSide);

            if (idleFront is not null)
            {
                _ = GetOrCreateState(sm, "Idle_Front", idleFront);
            }

            if (idleBack is not null)
            {
                _ = GetOrCreateState(sm, "Idle_Back", idleBack);
            }

            if (idleSide is not null)
            {
                _ = GetOrCreateState(sm, "Idle_Side", idleSide);
            }

            if (interactRead is not null)
            {
                _ = GetOrCreateState(sm, "Interact_Read", interactRead);
            }

            if (interactBesideDoor is not null)
            {
                _ = GetOrCreateState(sm, "Interact_BesideDoor", interactBesideDoor);
            }

            if (sleep is not null)
            {
                _ = GetOrCreateState(sm, "Sleep", sleep);
            }

            sm.defaultState = moveFrontState;

            ClearAnyStateTransitions(sm);
            AddDirectionTransition(sm, moveFrontState, dir: 0);
            AddDirectionTransition(sm, GetOrCreateState(sm, "Move_Back", moveBack), dir: 1);
            AddDirectionTransition(sm, GetOrCreateState(sm, "Move_Side", moveSide), dir: 2);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddDirectionTransition(AnimatorStateMachine sm, AnimatorState destination, int dir)
        {
            AnimatorStateTransition transition = sm.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            transition.AddCondition(AnimatorConditionMode.Equals, dir, "MoveDir");
        }

        private static void ClearAnyStateTransitions(AnimatorStateMachine sm)
        {
            AnimatorStateTransition[] transitions = sm.anyStateTransitions.ToArray();
            for (int i = 0; i < transitions.Length; i++)
            {
                sm.RemoveAnyStateTransition(transitions[i]);
            }
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string stateName, Motion motion)
        {
            ChildAnimatorState[] states = sm.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state.name != stateName)
                {
                    continue;
                }

                states[i].state.motion = motion;
                states[i].state.writeDefaultValues = true;
                return states[i].state;
            }

            AnimatorState state = sm.AddState(stateName);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                {
                    return;
                }
            }

            controller.AddParameter(name, type);
        }

        private static AnimationClip CreateOrUpdateSpriteClip(string assetPath, IReadOnlyList<Sprite> sprites, float fps)
        {
            AnimationClip? clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip is null)
            {
                clip = new AnimationClip
                {
                    frameRate = fps,
                    name = Path.GetFileNameWithoutExtension(assetPath)
                };
                AssetDatabase.CreateAsset(clip, assetPath);
            }

            clip.frameRate = fps;

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
                string.Empty,
                typeof(SpriteRenderer),
                "m_Sprite");

            ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
            {
                frames[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationClip CreateOrUpdateSpriteClipWithHeldEdges(
            string assetPath,
            IReadOnlyList<Sprite> sprites,
            float fps,
            int leadingHoldFrames,
            int trailingHoldFrames)
        {
            AnimationClip? clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip is null)
            {
                clip = new AnimationClip
                {
                    frameRate = fps,
                    name = Path.GetFileNameWithoutExtension(assetPath)
                };
                AssetDatabase.CreateAsset(clip, assetPath);
            }

            clip.frameRate = fps;

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
                string.Empty,
                typeof(SpriteRenderer),
                "m_Sprite");

            ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Count];
            float currentTime = 0f;
            for (int i = 0; i < sprites.Count; i++)
            {
                frames[i] = new ObjectReferenceKeyframe
                {
                    time = currentTime,
                    value = sprites[i]
                };

                if (i == 0)
                {
                    currentTime += leadingHoldFrames / fps;
                }
                else if (i < sprites.Count - 1)
                {
                    currentTime += 1f / fps;
                }
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.stopTime = currentTime + (trailingHoldFrames / fps);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static List<Sprite> LoadSpritesByPrefix(string prefix)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Sprite {prefix}", new[] { MoveSpriteFolder });
            Regex suffixRegex = new(@"_(\d+)$", RegexOptions.Compiled);

            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
                .Where(sprite => sprite is not null && sprite.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(sprite => ExtractOrder(sprite!.name, suffixRegex))
                .Cast<Sprite>()
                .ToList();
        }

        private static List<Sprite> LoadMoveSprites(string folderPath, string legacyPrefix)
        {
            List<Sprite> folderSprites = LoadSpritesFromFolder(folderPath);
            if (folderSprites.Count > 0)
            {
                return folderSprites;
            }

            return LoadSpritesByPrefix(legacyPrefix);
        }

        private static List<Sprite> LoadSpritesFromFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return new List<Sprite>();
            }

            return AssetDatabase.FindAssets("t:Sprite", new[] { folderPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
                .Where(sprite => sprite is not null)
                .OrderBy(sprite => ExtractTrailingNumber(sprite!.name))
                .Cast<Sprite>()
                .ToList();
        }

        private static int ExtractOrder(string spriteName, Regex suffixRegex)
        {
            Match match = suffixRegex.Match(spriteName);
            if (!match.Success)
            {
                return int.MaxValue;
            }

            return int.TryParse(match.Groups[1].Value, out int order) ? order : int.MaxValue;
        }

        private static int ExtractTrailingNumber(string spriteName)
        {
            Match match = Regex.Match(spriteName, @"(\d+)$");
            if (!match.Success)
            {
                return int.MaxValue;
            }

            return int.TryParse(match.Groups[1].Value, out int order) ? order : int.MaxValue;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
