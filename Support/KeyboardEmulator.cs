using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WK.Libraries.HotkeyListenerNS;
using iRacingSDK.Support;
using WindowsInput;



//using GregsStack.InputSimulatorStandard;
//using GregsStack.InputSimulatorStandard.Native;
//using WindowsInput;


namespace iRacingReplayDirector.Support
{
    static class KeyboardEmulator
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        //static InputSimulator vKeyboard = new InputSimulator();

        static public void SendHotkey(Hotkey hotkey)
        {
            Win32VirtualKeyCodes modifier  = GetWin32KeyCode(hotkey.Modifiers);
            Win32VirtualKeyCodes key = GetWin32KeyCode(hotkey.KeyCode);

            SendKeyStrokes(key, modifier);
        }

        static public void SendHotkey(Win32VirtualKeyCodes key, Win32VirtualKeyCodes modifier)
        {
            SendKeyStrokes(key, modifier);
        }


        static private void SendKeyStrokes (Win32VirtualKeyCodes key= Win32VirtualKeyCodes.VK_NONAME, Win32VirtualKeyCodes modifier= Win32VirtualKeyCodes.VK_NONAME)
        {
            int keyPressDelay = 200;

            if (modifier != Win32VirtualKeyCodes.VK_NONAME)
            {
                keybd_event((Byte)modifier, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(keyPressDelay);
            }

            keybd_event((Byte)key, 0, 0, UIntPtr.Zero);
            System.Threading.Thread.Sleep(keyPressDelay);

            keybd_event((Byte)key, 0, 0x02, UIntPtr.Zero);
            System.Threading.Thread.Sleep(keyPressDelay);

            if (modifier != Win32VirtualKeyCodes.VK_NONAME)
            {
                keybd_event((Byte)modifier, 0, 0x02, UIntPtr.Zero); 
            }
                
            TraceDebug.WriteLine("keybd_event sent: modifier = {0} {1} | key = {2} {3}".F(modifier.ToString(), (Byte)modifier, key.ToString(), (Byte)key));
        }

        public static Win32VirtualKeyCodes GetWin32KeyCode(this Keys source)
        {
            switch (source.ToString())
            {
                case "Alt":     return Win32VirtualKeyCodes.VK_MENU;
                case "Control": return Win32VirtualKeyCodes.VK_CONTROL;
                case "Shift":   return Win32VirtualKeyCodes.VK_SHIFT;
                case "F1":      return Win32VirtualKeyCodes.VK_F1;
                case "F2":      return Win32VirtualKeyCodes.VK_F2;
                case "F3":      return Win32VirtualKeyCodes.VK_F3;
                case "F4":      return Win32VirtualKeyCodes.VK_F4;
                case "F5":      return Win32VirtualKeyCodes.VK_F5;
                case "F6":      return Win32VirtualKeyCodes.VK_F6;
                case "F7":      return Win32VirtualKeyCodes.VK_F7;
                case "F8":      return Win32VirtualKeyCodes.VK_F8;
                case "F9":      return Win32VirtualKeyCodes.VK_F9;
                case "F10":     return Win32VirtualKeyCodes.VK_F10;
                case "F11":     return Win32VirtualKeyCodes.VK_F11;
                case "F12":     return Win32VirtualKeyCodes.VK_F12;
            }
            return Win32VirtualKeyCodes.VK_NONAME;
        }

        public enum Win32VirtualKeyCodes
        {

            VK_LBUTTON = 0x01,      //Linke Maustaste
            VK_RBUTTON = 0x02,      //Schaltfläche mit der rechten Maustaste
            VK_CANCEL = 0x03,       //Steuerungsunterbrechungsverarbeitung
            VK_MBUTTON = 0x04,      //Mittlere Maustaste (Drei-Tasten-Maus)
                                    //VK_XBUTTON1 	0x05 	X1-Maustaste
                                    //VK_XBUTTON2 	0x06 	X2-Maustaste
                                    //- 	0x07 	Nicht definiert
                                    //VK_BACK 	0x08 	Rücktaste
            VK_TAB = 0x09, 	        //TABULATORTASTE
                                    //- 	0x0A-0B 	Reserviert
                                    //VK_CLEAR 	0x0C 	ENTF-TASTE
                                    //VK_RETURN 	0x0D 	EINGABETASTE
                                    //- 	0x0E-0F 	Nicht definiert
            VK_SHIFT = 0x10,        //Umschalttaste
            VK_CONTROL = 0x11,      //STRG-TASTE
            VK_MENU = 0x12,         //ALT-TASTE
                                    //VK_PAUSE 	0x13 	PAUSE-TASTE
                                    //VK_CAPITAL 	0x14 	FESTSTELLTASTE-TASTE
                                    //VK_KANA 	0x15 	IME-Kana-Modus
                                    //VK_HANGUEL 	0x15 	IME Hanguel-Modus (für Kompatibilität beibehalten; Verwendung VK_HANGUL)
                                    //VK_HANGUL 	0x15 	IME-Hangul-Modus
                                    //VK_IME_ON 	0x16 	IME On
                                    //VK_JUNJA 	0x17 	IME-Junja-Modus
                                    //VK_FINAL 	0x18 	IME-Final-Modus
                                    //VK_HANJA 	0x19 	IME-Hanja-Modus
                                    //VK_KANJI 	0x19 	IME-Kanji-Modus
                                    //VK_IME_OFF 	0x1A 	IME aus
                                    //VK_ESCAPE 	0x1B 	ESC-Taste
                                    //VK_CONVERT 	0x1C 	IME konvertieren
                                    //VK_NONCONVERT 	0x1D 	IME-Nicht-Konvertierung
                                    //VK_ACCEPT 	0x1E 	IME akzeptieren
                                    //VK_MODECHANGE 	0x1F 	IME-Modusänderungsanforderung
                                    //VK_SPACE 	0x20 	LEERTASTE
                                    //VK_PRIOR 	0x21 	BILD-NACH-OBEN-TASTE
                                    //VK_NEXT 	0x22 	BILD-NACH-UNTEN-TASTE
                                    //VK_END 	0x23 	END-TASTE
                                    //VK_HOME 	0x24 	HOME-TASTE
                                    //VK_LEFT 	0x25 	NACH-LINKS-TASTE
                                    //VK_UP 	0x26 	NACH-OBEN-TASTE
                                    //VK_RIGHT 	0x27 	NACH-RECHTS-TASTE
                                    //VK_DOWN 	0x28 	NACH-UNTEN-TASTE
                                    //VK_SELECT 	0x29 	SELECT-TASTE
                                    //VK_PRINT 	0x2A 	DRUCKTASTE
                                    //VK_EXECUTE 	0x2B 	EXECUTE-TASTE
                                    //VK_SNAPSHOT 	0x2C 	DRUCKBILDSCHIRMTASTE
                                    //VK_INSERT 	0x2D 	INS-TASTE
                                    //VK_DELETE 	0x2E 	ENTF-TASTE
                                    //VK_HELP 	0x2F 	HILFEtaste
                                    //	0x30 	0 Taste
                                    //	0x31 	1 Schlüssel
                                    //	0x32 	2 Taste
                                    //	0x33 	3 Taste
                                    //	0x34 	4 Taste
                                    //	0x35 	5 Taste
                                    //	0x36 	6 Taste
                                    //	0x37 	7 Taste
                                    //	0x38 	8 Taste
                                    //	0x39 	9 Taste
                                    //- 	0x3A-40 	Nicht definiert
                                    //	0x41 	Ein Schlüssel
                                    //	0x42 	B-Taste
                                    //	0x43 	C-Taste
                                    //	0x44 	D-Taste
                                    //	0x45 	E-Taste
                                    //	0x46 	F-TASTE
                                    //	0x47 	G-Taste
                                    //	0x48 	H-Taste
                                    //	0x49 	Ich schlüssel
                                    //	0x4A 	J-TASTE
                                    //	0x4B 	K-TASTE
                                    //	0x4C 	L-Taste
                                    //	0x4D 	M-Taste
                                    //	0x4E 	N-Taste
                                    //	0x4F 	O-TASTE
                                    //	0x50 	P-TASTE
                                    //	0x51 	Q-Taste
                                    //	0x52 	R-Taste
                                    //	0x53 	S-Taste
                                    //	0x54 	T-Taste
                                    //	0x55 	U-Taste
                                    //	0x56 	V-Taste
                                    //	0x57 	W-TASTE
                                    //	0x58 	X-TASTE
                                    //	0x59 	Y-Taste
                                    //	0x5A 	Z-TASTE
                                    //VK_LWIN 	0x5B 	Linke Windows Taste (Natürliche Tastatur)
                                    //VK_RWIN 	0x5C 	Rechte Windows Taste (Natürliche Tastatur)
                                    //VK_APPS 	0x5D 	Anwendungstaste (Natürliche Tastatur)
                                    //- 	0x5E 	Reserviert
                                    //VK_SLEEP 	0x5F 	Taste für Standbymodus
                                    //VK_NUMPAD0 	0x60 	Numerische Tastenkombination 0
                                    //VK_NUMPAD1 	0x61 	Numerische Tastenkombination 1
                                    //VK_NUMPAD2 	0x62 	Numerische Tastenkombination 2
                                    //VK_NUMPAD3 	0x63 	Numerische Tastenkombination 3
                                    //VK_NUMPAD4 	0x64 	Numerische Tastenkombination 4
                                    //VK_NUMPAD5 	0x65 	Numerische Tastenkombination 5
                                    //VK_NUMPAD6 	0x66 	Numerische Tastenkombination 6
                                    //VK_NUMPAD7 	0x67 	Numerische Tastenkombination 7
                                    //VK_NUMPAD8 	0x68 	Numerische Tastenkombination 8
                                    //VK_NUMPAD9 	0x69 	Numerische Tastenkombination 9
                                    //VK_MULTIPLY 	0x6A 	Multipliziertaste
                                    //VK_ADD 	0x6B 	Hinzufügen eines Schlüssels
                                    //VK_SEPARATOR 	0x6C 	Trenntaste
                                    //VK_SUBTRACT 	0x6D 	Subtrahieren des Schlüssels
                                    //VK_DECIMAL 	0x6E 	Dezimaltaste
                                    //VK_DIVIDE 	0x6F 	Schlüssel teilen
            VK_F1 = 0x70,       //F1-Taste
            VK_F2 = 0x71,       //F2-Taste
            VK_F3 = 0x72,       //F3-Taste
            VK_F4 = 0x73,       //F4-Taste
            VK_F5 = 0x74,       //F5-TASTE
            VK_F6 = 0x75,       //F6-Taste
            VK_F7 = 0x76,       //F7-Taste
            VK_F8 = 0x77,       //F8-Taste
            VK_F9 = 0x78,       //F9-Taste
            VK_F10 = 0x79,      //F10-Taste
            VK_F11 = 0x7A,      //F11-Taste
            VK_F12 = 0x7B,      //F12-Taste
            VK_F13 = 0x7C,      //F13-Taste
            VK_F14 = 0x7D,      //F14-Taste
            VK_F15 = 0x7E,      //F15-Taste
            VK_F16 = 0x7F,      //F16-Taste
            VK_F17 = 0x80,      //F17-Taste
            VK_F18 = 0x81,      //F18-Taste
            VK_F19 = 0x82,      //F19-Taste
            VK_F20 = 0x83,      //F20-Taste
            VK_F21 = 0x84,      //F21-Taste
            VK_F22 = 0x85,      //F22-Taste
            VK_F23 = 0x86,      //F23-Taste
            VK_F24 = 0x87,       //F24-Taste
                                //- 	0x88-8F 	Nicht zugewiesen
                                //VK_NUMLOCK 	0x90 	NUM-SPERRtaste
                                //VK_SCROLL 	0x91 	SCROLL-SPERRtaste
                                //	0x92-96 	OEM-spezifischer OEM
                                //- 	0x97-9F 	Nicht zugewiesen
                                //VK_LSHIFT 	0xA0 	Linke UMSCHALTTASTE
                                //VK_RSHIFT 	0xA1 	Rechte UMSCHALTTASTE
                                //VK_LCONTROL 	0xA2 	Linke STRG-Taste
                                //VK_RCONTROL 	0xA3 	Rechte STRG-Taste
                                //VK_LMENU 	0xA4 	Linke Menütaste
                                //VK_RMENU 	0xA5 	Rechte Menütaste
                                //VK_BROWSER_BACK 	0xA6 	Browserrücktaste
                                //VK_BROWSER_FORWARD 	0xA7 	Browser-Vorwärtstaste
                                //VK_BROWSER_REFRESH 	0xA8 	Browseraktualisierungsschlüssel
                                //VK_BROWSER_STOP 	0xA9 	Browserstopptaste
                                //VK_BROWSER_SEARCH 	0xAA 	Browser-Suchtaste
                                //VK_BROWSER_FAVORITES 	0xAB 	Browserfavoritentaste
                                //VK_BROWSER_HOME 	0xAC 	Browserstart- und Starttaste
                                //VK_VOLUME_MUTE 	0xAD 	Lautstärke stummschalten
                                //VK_VOLUME_DOWN 	0xAE 	LAUTSTÄRKE-NACH-UNTEN-TASTE
                                //VK_VOLUME_UP 	0xAF 	Lautstärke nach oben
                                //VK_MEDIA_NEXT_TRACK 	0xB0 	Nächster Nachverfolgungsschlüssel
                                //VK_MEDIA_PREV_TRACK 	0xB1 	Vorheriger Nachverfolgungsschlüssel
                                //VK_MEDIA_STOP 	0xB2 	Medientaste beenden
                                //VK_MEDIA_PLAY_PAUSE 	0xB3 	Medientaste wiedergeben/anhalten
                                //VK_LAUNCH_MAIL 	0xB4 	E-Mail-Taste starten
                                //VK_LAUNCH_MEDIA_SELECT 	0xB5 	Medientaste auswählen
                                //VK_LAUNCH_APP1 	0xB6 	Anwendung 1 starten
                                //VK_LAUNCH_APP2 	0xB7 	Anwendung 2 starten
                                //- 	0xB8-B9     Reserviert
                                //VK_OEM_1 	0xBA 	Wird für verschiedene Zeichen verwendet; sie kann je nach Tastatur variieren. Für die US-Standardtastatur wird die Taste ";:"
                                //VK_OEM_PLUS 	0xBB 	Für jedes Land/jede Region, den Schlüssel "+"
                                //VK_OEM_COMMA 	0xBC 	Für jedes Land/jede Region, den Schlüssel ","
                                //VK_OEM_MINUS 	0xBD 	Für jedes Land/jede Region ist der Schlüssel "-"
                                //VK_OEM_PERIOD 	0xBE 	Für jedes Land/jede Region ist der Schlüssel "."
                                //VK_OEM_2 	0xBF 	Wird für verschiedene Zeichen verwendet; sie kann je nach Tastatur variieren. Für die US-Standardtastatur ist das "/?" Schlüssel
                                //VK_OEM_3 	0xC0 	Wird für verschiedene Zeichen verwendet; sie kann je nach Tastatur variieren. Für die US-Standardtastatur wird die Taste "~"
                                //- 	0xC1-D7     Reserviert
                                //- 	0xD8-DA     Nicht zugewiesen
                                //VK_OEM_4 	0xDB 	Wird für verschiedene Zeichen verwendet; sie kann je nach Tastatur variieren. Für die US-Standardtastatur wird die Taste "[{"
                                //VK_OEM_5 	0xDC 	Wird für verschiedene Zeichen verwendet; sie kann je nach Tastatur variieren. Für die US-Standardtastatur wird die Taste "\|"
                                //VK_OEM_6 	0xDD 	Wird für verschiedene Zeichen verwendet; sie kann je nach Tastatur variieren. Für die US-Standardtastatur wird die Taste "]}"
                                //VK_OEM_7 	0xDE 	Wird für verschiedene Zeichen verwendet; sie kann je nach Tastatur variieren. Für die US-Standardtastatur wird die Taste "single-quote/double-quote"
                                //VK_OEM_8 	0xDF 	Wird für verschiedene Zeichen verwendet; sie kann je nach Tastatur variieren.
                                //- 	0xE0 	Reserviert
                                //	0xE1 	OEM-spezifisch
                                //VK_OEM_102 	0xE2 	Die <> Tasten auf der US-Standardtastatatur oder die \\| Taste auf der Nicht-US 102-Tastatur
                                //	0xE3-E4     OEM-spezifisch
                                //VK_PROCESSKEY 	0xE5 	IME PROCESS-Schlüssel
                                //	0xE6 	OEM-spezifisch
                                //VK_PACKET 	0xE7 	Wird verwendet, um Unicode-Zeichen wie Tastaturanschläge zu übergeben. Die VK_PACKET Taste ist das niedrige Wort eines 32-Bit-Werts für virtuelle Tasten, der für Nicht-Tastatureingabemethoden verwendet wird. Weitere Informationen finden Sie unter "Remarkierung" in KEYBDINPUT, SendInputWM_KEYDOWN, undWM_KEYUP
                                //- 	0xE8 	Nicht zugewiesen
                                //	0xE9-F5     OEM-spezifisch
                                //VK_ATTN 	0xF6 	Attn-Taste
                                //VK_CRSEL 	0xF7 	CrSel-Taste
                                //VK_EXSEL 	0xF8 	ExSel-Taste
                                //VK_EREOF 	0xF9 	EOF-Taste löschen
                                //VK_PLAY 	0xFA 	Wiedergabetaste
                                //VK_ZOOM 	0xFB 	Zoomtaste
        VK_NONAME = 0xFC 	    //Reserviert
                                //VK_PA1 	0xFD 	PA1-Taste
                                //VK_OEM_CLEAR 	0xFE 	Unverschlüsselter Schlüssel
        }


        /// <summary>
        /// tested and dismissed approaches to confert from "System.Windos.Input.Key" enum 
        /// to "Win32 virtual Keys" enum
        /// </summary>

        public static System.Windows.Input.Key WinformsToWPFKey(System.Windows.Forms.Keys inputKey)
        {
            // Put special case logic here if there's a key you need but doesn't map...  
            try
            {
                return (System.Windows.Input.Key)Enum.Parse(typeof(System.Windows.Input.Key), inputKey.ToString());
            }
            catch
            {
                // There wasn't a direct mapping...    
                return System.Windows.Input.Key.None;
            }
        }
        //public static TEnum ConvertEnum<TEnum>(this Enum source)
        public static TEnum ConvertEnum<TEnum>(this Enum source)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), source.ToString(), true);
            /*try 
            { 
                return (TEnum)Enum.Parse(typeof(TEnum), source.ToString(), true); 
            }
            catch (Exception ex)
            {
                
                switch(source.ToString()){
                    case "Alt":
                        //return (Win32VirtualKeyCodes.VK_MENU);
                        break;
                    default:
                        throw new InvalidEnumArgumentException(nameof(source), (int)source, typeof(source));
                }
            }*/


        }
        public class InputSender
        {
            #region Imports/Structs/Enums
            [StructLayout(LayoutKind.Sequential)]
            public struct KeyboardInput
            {
                public ushort wVk;
                public ushort wScan;
                public uint dwFlags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MouseInput
            {
                public int dx;
                public int dy;
                public uint mouseData;
                public uint dwFlags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct HardwareInput
            {
                public uint uMsg;
                public ushort wParamL;
                public ushort wParamH;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct InputUnion
            {
                [FieldOffset(0)] public MouseInput mi;
                [FieldOffset(0)] public KeyboardInput ki;
                [FieldOffset(0)] public HardwareInput hi;
            }

            public struct Input
            {
                public int type;
                public InputUnion u;
            }

            [Flags]
            public enum InputType
            {
                Mouse = 0,
                Keyboard = 1,
                Hardware = 2
            }

            [Flags]
            public enum KeyEventF
            {
                KeyDown = 0x0000,
                ExtendedKey = 0x0001,
                KeyUp = 0x0002,
                Unicode = 0x0004,
                Scancode = 0x0008
            }

            [Flags]
            public enum MouseEventF
            {
                Absolute = 0x8000,
                HWheel = 0x01000,
                Move = 0x0001,
                MoveNoCoalesce = 0x2000,
                LeftDown = 0x0002,
                LeftUp = 0x0004,
                RightDown = 0x0008,
                RightUp = 0x0010,
                MiddleDown = 0x0020,
                MiddleUp = 0x0040,
                VirtualDesk = 0x4000,
                Wheel = 0x0800,
                XDown = 0x0080,
                XUp = 0x0100
            }

            [DllImport("user32.dll", SetLastError = true)]
            private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

            [DllImport("user32.dll")]
            private static extern IntPtr GetMessageExtraInfo();

            [DllImport("user32.dll")]
            private static extern bool GetCursorPos(out POINT lpPoint);

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;
            }

            [DllImport("User32.dll")]
            private static extern bool SetCursorPos(int x, int y);
            #endregion

            //#region Wrapper Methods
            //public static POINT GetCursorPosition()
            //{
            //    GetCursorPos(out POINT point);
            //    return point;
            //}

            public static void SetCursorPosition(int x, int y)
            {
                SetCursorPos(x, y);
            }

            public static void SendKeyboardInput(KeyboardInput[] kbInputs)
            {
                Input[] inputs = new Input[kbInputs.Length];

                for (int i = 0; i < kbInputs.Length; i++)
                {
                    inputs[i] = new Input
                    {
                        type = (int)InputType.Keyboard,
                        u = new InputUnion
                        {
                            ki = kbInputs[i]
                        }
                    };
                }

                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
            }

            public static void ClickKey(ushort scanCode)
            {
                var inputs = new KeyboardInput[]
                {
                new KeyboardInput
                {
                    wScan = scanCode,
                    dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                    dwExtraInfo = GetMessageExtraInfo()
                },
                new KeyboardInput
                {
                    wScan = scanCode,
                    dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                    dwExtraInfo = GetMessageExtraInfo()
                }
                };
                SendKeyboardInput(inputs);
            }

            public static void SendMouseInput(MouseInput[] mInputs)
            {
                Input[] inputs = new Input[mInputs.Length];

                for (int i = 0; i < mInputs.Length; i++)
                {
                    inputs[i] = new Input
                    {
                        type = (int)InputType.Mouse,
                        u = new InputUnion
                        {
                            mi = mInputs[i]
                        }
                    };
                }

                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
            }
        }

        private static int GetModifiers(Keys key)
        {
            int modifiers = 0;
            if ((key & Keys.Alt) == Keys.Alt) modifiers |= 0x1;
            if ((key & Keys.Control) == Keys.Control) modifiers |= 0x2;
            if ((key & Keys.Shift) == Keys.Shift) modifiers |= 0x4;
            return modifiers;
        }

        private static int GetKeyCode(Keys key)
        {
            return (int)(key & ~Keys.Control & ~Keys.Shift & ~Keys.Alt);
        } //=> (int)(key & ~Keys.Control & ~Keys.Shift & ~Keys.Alt);

    }
}
