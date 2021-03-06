﻿#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste {
    class patch_OuiFileNaming : OuiFileNaming {

        private int index;
        
        public bool UseKeyboardInput {
            get {
                var settings = Mod.Core.CoreModule.Instance._Settings as Mod.Core.CoreModuleSettings;
                return settings?.UseKeyboardForTextInput ?? false;
            }
        }

        public void OnTextInput(char c) {
            if (!UseKeyboardInput) {
                return;
            }

            if (c == (char) 13) {
                // Enter - confirm.
                Finish();

            } else if (c == (char) 8) {
                // Backspace - trim.
                if (Name.Length > 0) {
                    Name = Name.Substring(0, Name.Length - 1);
                    Audio.Play(SFX.ui_main_rename_entry_backspace);
                } else {
                    Audio.Play(SFX.ui_main_button_invalid);
                }

            } else if (c == (char) 127) {
                // Delete - currenly not handled.

            } else if (c == ' ') {
                // Space - append.
                if (Name.Length < 12 && Name.Length > 0) {
                    Audio.Play(SFX.ui_main_rename_entry_space);
                    Name += c;
                } else {
                    Audio.Play(SFX.ui_main_button_invalid);
                }

            } else if (!char.IsControl(c)) {
                // Any other character - append.
                if (Name.Length < 12 && ActiveFont.FontSize.Characters.ContainsKey(c)) {
                    Audio.Play(SFX.ui_main_rename_entry_char);
                    Name += c;
                } else {
                    Audio.Play(SFX.ui_main_button_invalid);
                }
            }
        }

        public extern IEnumerator orig_Enter(Oui from);
        public override IEnumerator Enter(Oui from) {
            Engine.Commands.Enabled = false;
            if (!Everest.Flags.IsDisabled)
                TextInput.OnInput += OnTextInput;
            return orig_Enter(from);
        }

        public extern IEnumerator orig_Leave(Oui next);
        public override IEnumerator Leave(Oui next) {
            Engine.Commands.Enabled = true;
            if (!Everest.Flags.IsDisabled)
                TextInput.OnInput -= OnTextInput;
            return orig_Leave(next);
        }

        [MonoModIgnore]
        public extern void ResetDefaultName();

        public extern void orig_Update();
        public override void Update() {
            bool wasFocused = Focused;
            if (!Everest.Flags.IsDisabled) {
                // Only "focus" if we're not using the keyboard for input
                Focused = wasFocused && !UseKeyboardInput;

                // If we aren't focused to kill controller input, still allow the player to Ctrl+S to choose a new name.
                if (Selected && wasFocused && !Focused
                    && !string.IsNullOrWhiteSpace(Name) && MInput.Keyboard.Check(Keys.LeftControl) && MInput.Keyboard.Pressed(Keys.S)) {

                    ResetDefaultName();
                }
            }

            orig_Update();

            if (!Everest.Flags.IsDisabled && wasFocused && !Focused) {
                if (Input.ESC)
                    Cancel();
            }

            Focused = wasFocused;
        }

        [MonoModIgnore]
        private extern void Cancel();

        [MonoModIgnore]
        private extern void Finish();

        [PatchOuiFileNamingRendering]
        public extern void orig_Render();
        public override void Render() {
            int prevIndex = index;
            // Only "focus" if we're not using the keyboard for input
            if (!Everest.Flags.IsDisabled && UseKeyboardInput)
                index = -1;

            orig_Render();

            index = prevIndex;
        }

        private extern void orig_DrawOptionText(string text, Vector2 at, Vector2 justify, Vector2 scale, bool selected, bool disabled = false);
        private void DrawOptionText(string text, Vector2 at, Vector2 justify, Vector2 scale, bool selected, bool disabled = false) {
            // Only draw "interactively" if not using the keyboard for input
            if (!Everest.Flags.IsDisabled && UseKeyboardInput) {
                selected = false;
                disabled = true;
            }
            orig_DrawOptionText(text, at, justify, scale, selected, disabled);
        }

        private bool _shouldDisplaySwitchAlphabetPrompt() {
            return Japanese && !UseKeyboardInput;
        }
    }
}
