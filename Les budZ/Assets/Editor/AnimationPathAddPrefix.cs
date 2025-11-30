using UnityEngine;
using UnityEditor;

public static class AnimationPathAddPrefix
{
    [MenuItem("Tools/Animations/Ajouter prefix au chemin")]
    private static void AddPrefixToSelectedClips()
    {
        const string prefix = "PlayerModel/"; // adapte le nom

        foreach (Object obj in Selection.objects)
        {
            AnimationClip clip = obj as AnimationClip;
            if (clip == null)
                continue;

            Undo.RecordObject(clip, "Add path prefix");

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);

                var newBinding = binding;
                newBinding.path = prefix + binding.path;

                AnimationUtility.SetEditorCurve(clip, binding, null);
                AnimationUtility.SetEditorCurve(clip, newBinding, curve);
            }

            var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in objBindings)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);

                var newBinding = binding;
                newBinding.path = prefix + binding.path;

                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                AnimationUtility.SetObjectReferenceCurve(clip, newBinding, keys);
            }
        }

        Debug.Log("Prefix ajouté aux clips sélectionnés.");
    }
}