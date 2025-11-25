using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UserInput;

namespace UI.Icons
{
    public class InputIconManager : MonoBehaviour
    {
        private static InputIconDataSO s_inputIconData;
        private const string INPUT_ICON_DATA_PATH = "UI/Icons/InputIconData";

        private static bool _useDS4Icons = true;
        public static bool UseDS4Icons
        {
            get => _useDS4Icons;
            set
            {
                _useDS4Icons = value;
                s_inputIconData.SetUseDS4Icons(_useDS4Icons);
            }
        }

        public static event System.Action OnShouldUpdateSpriteIdentifiers;
        public static event System.Action OnSpriteAssetChanged;


        static InputIconManager()
        {
            try
            {
                s_inputIconData = Resources.Load(INPUT_ICON_DATA_PATH, typeof(InputIconDataSO)) as InputIconDataSO;
                if (s_inputIconData == null)
                    throw new System.Exception();

                s_inputIconData.SetUseDS4Icons(UseDS4Icons);
            }
            catch
            {
                Debug.LogError("Error when trying to load the InputIconDataSO from path: " + INPUT_ICON_DATA_PATH);
            }

            ClientInput.OnInputDeviceChanged += ClientInput_OnInputDeviceChanged;
        }
        private static void ClientInput_OnInputDeviceChanged() => OnShouldUpdateSpriteIdentifiers?.Invoke();


        /// <summary>
        ///     Retrieve the Sprite corresponding to the input options of the given InputAction.
        /// </summary>
        public static Sprite GetIconForAction(InputAction inputAction)
        {
            var bindings = inputAction.bindings;
            var bindingCount = bindings.Count;
            for (int i = 0; i < bindingCount; ++i)
            {
                if (bindings[i].groups.Contains(ClientInput.LastUsedDeviceName))
                {
                    // This binding is compatable with our active device. Use it to determine our Icon.
                    inputAction.GetBindingDisplayString(i, out string deviceLayoutName, out string controlPath);

                    if (s_inputSystemIdentifierToSpriteIdenfitierDictionary.TryGetValue(controlPath, out string spriteIdentifier))
                    {
                        return s_inputIconData.GetSprite(ClientInput.LastUsedDevice, spriteIdentifier);
                    }
                }
            }

            Debug.LogError($"Error: No Binding for the action '{inputAction.name}' for the last used device ({ClientInput.LastUsedDeviceName})");
            return null;
        }


        /// <summary>
        ///     Format the given text to include Sprite tags for the given InputAction and active Control Scheme.
        /// </summary>
        /// <remarks> Uses '{0}' as the formatting target.</remarks>
        /// <param name="unformattedText"> The unformatted text.</param>
        /// <param name="inputAction"> The InputAction we are retrieving the Sprite Tags for.</param>
        /// <returns> The text formatted with TMPro's sprite tags.</returns>
        public static string FormatTextForIconFromInputAction(string unformattedText, InputAction inputAction)
        {
            return string.Format(unformattedText, GetIconIdentifierForAction(inputAction));
        }
        /// <summary>
        ///     Get the TMPro Sprite Tag text for the given InputAction and active Control Scheme.
        /// </summary>
        /// <param name="inputAction"> The InputAction we are retrieving the Sprite Tags for.</param>
        /// <returns>The TMPro Sprite Tags for the given InputAction and active Control Scheme.</returns>
        public static string GetIconIdentifierForAction(InputAction inputAction)
        {
            var bindings = inputAction.bindings;
            int bindingCount = bindings.Count;
            string output = default(string);

            for(int i = 0; i < bindingCount; ++i)
            {
                if (bindings[i].groups.Contains(ClientInput.LastUsedDeviceName))
                {
                    // This binding is compatible with our active input device. Use it to determine our icon.
                    // Get our Control Path.
                    inputAction.GetBindingDisplayString(i, out string deviceLayoutName, out string controlPath);

                    // Try to get the Sprite Identifier for the control path.
                    if (s_inputSystemIdentifierToSpriteIdenfitierDictionary.TryGetValue(controlPath, out string spriteIdentifier))
                    {
                        // We found the identifier.
                        // Add our sprite tag with the formatted action name to the output.
                        output += $"<sprite name=\"{spriteIdentifier}\">";
                    }
                }
            }

            return output;
        }

        public static TMPro.TMP_SpriteAsset GetSpriteAsset() => s_inputIconData.GetSpriteAsset();


        private static Dictionary<string, string> s_inputSystemIdentifierToSpriteIdenfitierDictionary = new Dictionary<string, string>()
        {
            #region Keyboard

            {"0", "0_Key"},
            {"1", "1_Key"},
            {"2", "2_Key"},
            {"3", "3_Key"},
            {"4", "4_Key"},
            {"5", "5_Key"},
            {"6", "6_Key"},
            {"7", "7_Key"},
            {"8", "8_Key"},
            {"9", "9_Key"},

            {"a", "A_Key"},
            {"b", "B_Key"},
            {"c", "C_Key"},
            {"d", "D_Key"},
            {"e", "E_Key"},
            {"f", "F_Key"},
            {"g", "G_Key"},
            {"h", "H_Key"},
            {"i", "I_Key"},
            {"j", "J_Key"},
            {"k", "K_Key"},
            {"l", "L_Key"},
            {"m", "M_Key"},
            {"n", "N_Key"},
            {"o", "O_Key"},
            {"p", "P_Key"},
            {"q", "Q_Key"},
            {"r", "R_Key"},
            {"s", "S_Key"},
            {"t", "T_Key"},
            {"u", "U_Key"},
            {"v", "V_Key"},
            {"w", "W_Key"},
            {"x", "X_Key"},
            {"y", "Y_Key"},
            {"z", "Z_Key"},

            {"numpadMultiply", "Asterisk_Key"},
            {"leftBracket", "Bracket_Left_Key"},
            {"rightBracket", "Bracket_Right_Key"},
            //{"", "Mark_Left_Key"},
            //{"", "Mark_Right_Key"},
            {"minus", "Minus_Key"},
            {"plus", "Plus_Key"},
            //{"", "Question_Key"},
            //{"", "Quote_Key"},
            {"semicolon", "Semicolon_Key"},
            {"slash", "Forwardslash_Key"},
            {"OEM1", "Backslash_Key"},
            {"backquote", "Tilde_Key"},

            {"upArrow", "Arrow_Up_Key"},
            {"downArrow", "Arrow_Down_Key"},
            {"leftArrow", "Arrow_Left_Key"},
            {"rightArrow", "Arrow_Right_Key"},

            {"alt", "Alt_Key"},
            {"backspace", "Backspace_Key"},
            {"capsLock", "Caps_Lock_Key"},
            {"ctrl", "Ctrl_Key"}, {"leftCtrl", "Ctrl_Key"}, {"rightCtrl", "Ctrl_Key"},
            {"delete", "Delete_Key"},
            {"end", "End_Key"},
            {"enter", "Return_Key"},
            {"escape", "Escape_Key"},
            {"home", "Home_Key"},
            {"insert", "Insert_Key"},
            {"numLock", "Num_Lock_Key"},
            {"pageDown", "Page_Down_Key"},
            {"pageUp", "Page_Up_Key"},
            {"printScreen", "Print_Screen_Key"},
            {"shift", "Shift_Key"}, {"leftShift", "Shift_Key"},
            {"rightShift", "Shift_Alt_Key"},
            {"space", "Space_Key"},
            {"tab", "Tab_Key"},

            {"f1", "F1_Key"},
            {"f2", "F2_Key"},
            {"f3", "F3_Key"},
            {"f4", "F4_Key"},
            {"f5", "F5_Key"},
            {"f6", "F6_Key"},
            {"f7", "F7_Key"},
            {"f8", "F8_Key"},
            {"f9", "F9_Key"},
            {"f10", "F10_Key"},
            {"f11", "F11_Key"},
            {"f12", "F12_Key"},

            #endregion


            #region Mouse

            {"leftButton", "Mouse_Left"},
            {"rightButton", "Mouse_Right"},
            {"middleButton", "Mouse_Middle"},
            {"delta", "Mouse_Simple"},

            #endregion


            #region Gamepad

            {"buttonNorth", "Button_North"},
            {"buttonSouth", "Button_South"},
            {"buttonWest", "Button_West"},
            {"buttonEast", "Button_East"},

            {"dpad", "Dpad"},
            {"dpad/up", "Dpad_Up"},
            {"dpad/down", "Dpad_Down"},
            {"dpad/left", "Dpad_Left"},
            {"dpad/right", "Dpad_Right"},

            {"leftShoulder", "Left_Shoulder"},
            {"leftTrigger", "Left_Trigger"},
            {"rightShoulder", "Right_Shoulder"},
            {"rightTrigger", "Right_Trigger"},

            {"leftStick", "Left_Stick"},
            {"leftStickPress", "Left_Stick_Press"},
            {"rightStick", "Right_Stick"},
            {"rightStickPress", "Right_Stick_Press"},

            {"start", "Start"},
            {"select", "Select"},

            #endregion
        };
    }
}