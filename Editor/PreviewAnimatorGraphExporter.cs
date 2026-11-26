using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Plyground.Preview.Editor
{
    // Controller data belongs to the module export. The browser uses this
    // graph to run the same transitions and transpiled state behaviours as Unity.
    internal static class PreviewAnimatorGraphExporter
    {
        private static Dictionary<string, object> Map(params object[] pairs)
        {
            var result = new Dictionary<string, object>();
            for (int index = 0; index < pairs.Length; index += 2)
                result[(string)pairs[index]] = pairs[index + 1];
            return result;
        }

        private static object MotionRecord(Motion motion)
        {
            if (motion == null) return null;
            if (!(motion is BlendTree tree))
                return Map("kind", "clip", "name", motion.name,
                    "assetPath", AssetDatabase.GetAssetPath(motion),
                    "duration", motion is AnimationClip clip ? clip.length : 0f,
                    "loop", motion is AnimationClip loopingClip && loopingClip.isLooping);
            var children = new List<object>();
            foreach (var child in tree.children)
                children.Add(Map("motion", MotionRecord(child.motion), "threshold", child.threshold,
                    "position", Map("x", child.position.x, "y", child.position.y),
                    "timeScale", child.timeScale, "cycleOffset", child.cycleOffset,
                    "mirror", child.mirror));
            return Map("kind", "blendTree", "name", tree.name,
                "blendType", tree.blendType.ToString(), "parameter", tree.blendParameter,
                "parameterY", tree.blendParameterY, "children", children);
        }

        private static List<object> Transitions(AnimatorStateTransition[] transitions,
            Dictionary<AnimatorState, string> paths)
        {
            var result = new List<object>();
            foreach (var transition in transitions)
            {
                var conditions = new List<object>();
                foreach (var condition in transition.conditions)
                    conditions.Add(Map("parameter", condition.parameter,
                        "mode", condition.mode.ToString(), "threshold", condition.threshold));
                var destination = transition.destinationState != null
                    ? transition.destinationState.name : transition.destinationStateMachine != null
                        ? transition.destinationStateMachine.defaultState?.name : null;
                if (destination == null) continue;
                result.Add(Map("destination", destination,
                    "destinationPath", transition.destinationState != null
                        ? paths.GetValueOrDefault(transition.destinationState)
                        : transition.destinationStateMachine?.defaultState != null
                            ? paths.GetValueOrDefault(transition.destinationStateMachine.defaultState)
                            : null,
                    "duration", transition.duration,
                    "fixedDuration", transition.hasFixedDuration,
                    "hasExitTime", transition.hasExitTime,
                    "exitTime", transition.exitTime,
                    "offset", transition.offset,
                    "interruptionSource", transition.interruptionSource.ToString(),
                    "orderedInterruption", transition.orderedInterruption,
                    "canTransitionToSelf", transition.canTransitionToSelf,
                    "conditions", conditions));
            }
            return result;
        }

        private static List<object> Behaviours(AnimatorState state)
        {
            var result = new List<object>();
            foreach (var behaviour in state.behaviours)
            {
                if (behaviour == null) continue;
                var properties = new Dictionary<string, object>();
                foreach (var field in behaviour.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!field.IsPublic && !Attribute.IsDefined(field, typeof(SerializeField))) continue;
                    object value = field.GetValue(behaviour);
                    if (value == null || field.FieldType.IsPrimitive || field.FieldType == typeof(string))
                        properties[field.Name] = value;
                    else if (field.FieldType.IsEnum)
                        properties[field.Name] = Convert.ToInt32(value);
                }
                result.Add(Map("type", behaviour.GetType().FullName, "properties", properties));
            }
            return result;
        }

        private static void Paths(AnimatorStateMachine machine,
            Dictionary<AnimatorState, string> paths, string path)
        {
            foreach (var child in machine.states)
                paths[child.state] = path + "." + child.state.name;
            foreach (var child in machine.stateMachines)
                Paths(child.stateMachine, paths, path + "." + child.stateMachine.name);
        }

        private static void States(AnimatorStateMachine machine, List<object> output,
            string path, Dictionary<AnimatorState, string> paths)
        {
            foreach (var child in machine.states)
            {
                var state = child.state;
                var fullPath = path + "." + state.name;
                output.Add(Map("name", state.name, "speed", state.speed,
                    "iKOnFeet", state.iKOnFeet,
                    "fullPath", fullPath,
                    "shortNameHash", Animator.StringToHash(state.name),
                    "fullPathHash", Animator.StringToHash(fullPath),
                    "tag", state.tag, "tagHash", Animator.StringToHash(state.tag ?? ""),
                    "motion", MotionRecord(state.motion),
                    "transitions", Transitions(state.transitions, paths),
                    "behaviours", Behaviours(state)));
            }
            foreach (var child in machine.stateMachines)
                States(child.stateMachine, output, path + "." + child.stateMachine.name,
                    paths);
        }
        public static object Export(AnimatorController controller)
        {
            var parameters = new List<object>();
            foreach (var parameter in controller.parameters)
                parameters.Add(Map("name", parameter.name, "type", parameter.type.ToString(),
                    "defaultFloat", parameter.defaultFloat, "defaultInt", parameter.defaultInt,
                    "defaultBool", parameter.defaultBool));
            var layers = new List<object>();
            for (int index = 0; index < controller.layers.Length; index++)
            {
                var layer = controller.layers[index];
                var states = new List<object>();
                var paths = new Dictionary<AnimatorState, string>();
                Paths(layer.stateMachine, paths, layer.name);
                States(layer.stateMachine, states, layer.name, paths);
                var mask = new List<object>();
                if (layer.avatarMask != null)
                    foreach (AvatarMaskBodyPart part in Enum.GetValues(typeof(AvatarMaskBodyPart)))
                        if (layer.avatarMask.GetHumanoidBodyPartActive(part)) mask.Add(part.ToString());
                layers.Add(Map("name", layer.name, "index", index,
                    "defaultWeight", layer.defaultWeight, "humanoidMask", mask,
                    "defaultState", layer.stateMachine.defaultState?.name,
                    "defaultStatePath", layer.stateMachine.defaultState != null
                        ? paths.GetValueOrDefault(layer.stateMachine.defaultState) : null,
                    "states", states,
                    "anyStateTransitions", Transitions(layer.stateMachine.anyStateTransitions,
                        paths)));
            }
            return Map("name", controller.name, "parameters", parameters, "layers", layers);
        }
    }
}
