#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;

[InitializeOnLoad]
public static class FMODVersionDefinesSetter
{
    const string OldApiDefine = "FMOD_LEGACY_API";

    static FMODVersionDefinesSetter()
    {
        SetDefines();
    }

    private static void SetDefines()
    {
        // FMOD exposes its version as a hex uint: e.g. 0x00020204 = 2.02.04
        uint fmodVersion = FMOD.VERSION.number;

        // 2.02.x is anything from 0x00020200 to 0x000202FF
        bool isLegacy = fmodVersion < 0x00020300;

        var buildTarget = NamedBuildTarget.FromBuildTargetGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup);

        PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);
        var defineList = new System.Collections.Generic.List<string>(defines);

        bool hasDefine = defineList.Contains(OldApiDefine);

        if (isLegacy && !hasDefine)
        {
            defineList.Add(OldApiDefine);
            PlayerSettings.SetScriptingDefineSymbols(buildTarget, defineList.ToArray());
        }
        else if (!isLegacy && hasDefine)
        {
            defineList.Remove(OldApiDefine);
            PlayerSettings.SetScriptingDefineSymbols(buildTarget, defineList.ToArray());
        }
    }
}
#endif