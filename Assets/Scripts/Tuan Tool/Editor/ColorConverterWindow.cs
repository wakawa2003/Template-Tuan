#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Công cụ chuyển đổi màu giữa Gamma (sRGB, như hex trong Photoshop) và Linear (color space Linear của Unity).
    /// Mở bằng menu Tools/Color Converter.
    /// - Hex (Gamma) -> Linear: nhập hex, nhận Color Linear và hex Linear.
    /// - Color (Gamma) -> Linear: nhập Color theo sRGB, nhận Color Linear và hex Linear.
    /// - Color (Linear) -> Gamma: nhập Color Linear, nhận Color Gamma và hex Gamma.
    /// Alpha được giữ nguyên khi chuyển đổi.
    /// </summary>
    public class ColorConverterWindow : EditorWindow
    {
        // Hex (Gamma) -> Linear
        string hexInput = "FFFFFF";
        Color hexLinearResult = Color.white;
        bool hexValid = true;

        // Color (Gamma) -> Linear
        Color gammaInput = Color.white;

        // Color (Linear) -> Gamma
        Color linearInput = Color.white;

        /// <summary>Khi bật, kết quả chuyển đổi mới được tự copy vào clipboard. Luôn bật khi mở cửa sổ.</summary>
        bool autoCopy;

        [MenuItem("Tools/Color Converter")]
        static void Open()
        {
            GetWindow<ColorConverterWindow>("Color Converter");
        }

        void OnEnable()
        {
            autoCopy = true;
        }

        void OnGUI()
        {
            autoCopy = EditorGUILayout.ToggleLeft(new GUIContent("Tự động copy kết quả vào clipboard", "Mỗi khi có kết quả mới, copy hex của không gian màu mới"), autoCopy);

            EditorGUILayout.Space(8);
            DrawHexToLinear();
            EditorGUILayout.Space(12);
            DrawGammaToLinear();
            EditorGUILayout.Space(12);
            DrawLinearToGamma();
        }

        /// <summary>
        /// Copy hex vào clipboard nếu đang bật tự động copy.
        /// </summary>
        void CopyIfEnabled(Color color)
        {
            if (autoCopy)
                EditorGUIUtility.systemCopyBuffer = ColorUtility.ToHtmlStringRGB(color);
        }

        /// <summary>
        /// Nhập hex theo gamma (sRGB), xem Color Linear tương ứng và hex Linear.
        /// </summary>
        void DrawHexToLinear()
        {
            EditorGUILayout.LabelField("Hex (Gamma) -> Linear", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            hexInput = EditorGUILayout.TextField(new GUIContent("Hex (Gamma)", "Mã hex RRGGBB hoặc RRGGBBAA, không cần dấu #"), hexInput);
            if (EditorGUI.EndChangeCheck() || GUILayout.Button("Convert"))
            {
                hexValid = TryParseHex(hexInput, out Color gamma);
                hexLinearResult = hexValid ? gamma.linear : Color.black;
                if (hexValid) CopyIfEnabled(hexLinearResult);
            }

            if (!hexValid)
            {
                EditorGUILayout.HelpBox("Hex không hợp lệ.", MessageType.Error);
                return;
            }

            EditorGUILayout.ColorField(new GUIContent("Linear", "Color đã chuyển sang Linear"), hexLinearResult);
            DrawHexRow("Hex Linear", hexLinearResult);
        }

        /// <summary>
        /// Nhập Color theo gamma (giá trị bạn thấy trên Photoshop/picker) và lấy Color Linear.
        /// </summary>
        void DrawGammaToLinear()
        {
            EditorGUILayout.LabelField("Color (Gamma) -> Linear", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            gammaInput = EditorGUILayout.ColorField(new GUIContent("Gamma", "Color theo sRGB (giá trị hiển thị)"), gammaInput);
            Color linear = gammaInput.linear;
            if (EditorGUI.EndChangeCheck()) CopyIfEnabled(linear);
            EditorGUILayout.ColorField(new GUIContent("Linear", "Color Linear dùng trong shader / material"), linear);
            DrawHexRow("Hex Linear", linear);
        }

        /// <summary>
        /// Nhập Color Linear (giá trị đang dùng trong shader / material) và lấy Color theo gamma.
        /// </summary>
        void DrawLinearToGamma()
        {
            EditorGUILayout.LabelField("Color (Linear) -> Gamma", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            linearInput = EditorGUILayout.ColorField(new GUIContent("Linear", "Color Linear dùng trong shader / material"), linearInput);
            Color gamma = linearInput.gamma;
            if (EditorGUI.EndChangeCheck()) CopyIfEnabled(gamma);
            EditorGUILayout.ColorField(new GUIContent("Gamma", "Color theo sRGB (giá trị hiển thị)"), gamma);
            DrawHexRow("Hex Gamma", gamma);
        }

        /// <summary>
        /// Hiển thị hex của color kèm nút copy vào clipboard.
        /// </summary>
        static void DrawHexRow(string label, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            string hex = ColorUtility.ToHtmlStringRGB(color);
            EditorGUILayout.SelectableLabel($"{label}: {hex}", GUILayout.Height(18));
            if (GUILayout.Button("Copy", GUILayout.Width(50)))
                EditorGUIUtility.systemCopyBuffer = hex;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Parse hex (có hoặc không có #) thành Color theo gamma. Trả về false nếu sai định dạng.
        /// </summary>
        static bool TryParseHex(string hex, out Color color)
        {
            return ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out color);
        }
    }
}
#endif
