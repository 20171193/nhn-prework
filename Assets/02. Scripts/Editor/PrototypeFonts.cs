using TMPro;
using UnityEditor;
using UnityEngine;

// TMP Essential Resources 임포트 후 만들어진 기본 폰트 애셋을 찾아 반환한다.
public static class PrototypeFonts
{
    const string FontAssetPath = "Assets/TextMesh Pro/Fonts/LiberationSans SDF.asset";

    public static TMP_FontAsset GetDefaultFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font != null) return font;

        font = TMP_Settings.defaultFontAsset;
        if (font != null) return font;

        Debug.LogError($"TMP 폰트를 찾을 수 없습니다. '{FontAssetPath}' 또는 TMP Settings의 Default Font Asset을 확인하세요.");
        return null;
    }
}
