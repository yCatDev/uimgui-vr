using ImGuiNET;
using System;
using System.Collections.Generic;
using UImGui.Assets;
using UImGui.VR;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace UImGui.Platform
{
    /// <summary>
    /// Platform bindings for ImGui in Unity for VR based on Input System`s setup 
    /// </summary>
    internal sealed class VRPlatform : PlatformBase
    {

        public VRPlatform(CursorShapesAsset cursorShapes, IniSettingsAsset iniSettings)
            : base(cursorShapes, iniSettings)
        {
        }

        private static void UpdateMouse(ImGuiIOPtr io, VirtualXRInput virtualXRInput,
            WorldSpaceTransformer worldSpaceTransformer)
        {
            var mouseScreenPosition = worldSpaceTransformer.GetCursorPosition(virtualXRInput);
            io.MousePos = Utils.ScreenToImGui(mouseScreenPosition);

            var mouseScroll = virtualXRInput.Scroll.ReadValue<Vector2>();
            io.MouseWheel = mouseScroll.y;
            io.MouseWheelH = mouseScroll.x;

            io.MouseDown[0] = virtualXRInput.PressButton.IsPressed();
            io.MouseDown[1] = virtualXRInput.SecondaryPressButton.IsPressed();
            io.MouseDown[2] = false; // TODO: Middle scroll button, maybe we need this...
        }

        private static void UpdateXRControllers(ImGuiIOPtr io, VirtualXRInput virtualXRInput)
        {
            io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;

            io.AddKeyAnalogEvent(ImGuiKey.GamepadFaceDown, virtualXRInput.PrimaryButton.IsPressed(),
                virtualXRInput.PrimaryButton.ReadValue<float>()); 
            io.AddKeyAnalogEvent(ImGuiKey.GamepadFaceRight, virtualXRInput.SecondaryButton.IsPressed(),
                virtualXRInput.SecondaryButton.ReadValue<float>());

            var thumbstickVector = virtualXRInput.Thumbstick.ReadValue<Vector2>();
            io.AddKeyAnalogEvent(ImGuiKey.GamepadDpadUp, thumbstickVector.y > 0.5f, thumbstickVector.y);
            io.AddKeyAnalogEvent(ImGuiKey.GamepadDpadDown, thumbstickVector.y < -0.5f, -thumbstickVector.y);
            io.AddKeyAnalogEvent(ImGuiKey.GamepadDpadLeft, thumbstickVector.x < -0.5f, -thumbstickVector.x);
            io.AddKeyAnalogEvent(ImGuiKey.GamepadDpadRight, thumbstickVector.x > 0.5f, thumbstickVector.x);
        }
        
        

        #region Overrides of PlatformBase

        public override bool Initialize(ImGuiIOPtr io, UIOConfig config, string platformName)
        {
           
            base.Initialize(io, config, platformName);

            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

            unsafe
            {
                PlatformCallbacks.SetClipboardFunctions(PlatformCallbacks.GetClipboardTextCallback,
                    PlatformCallbacks.SetClipboardTextCallback);
            }

            return true;
        }
        

        public override void PrepareFrame(ImGuiIOPtr io, Rect displayRect)
        {
            base.PrepareFrame(io, displayRect);
            
            UpdateMouse(io, UImGuiUtility.VRContext.VirtualXRInput, UImGuiUtility.VRContext.WorldSpaceTransformer);
            //UpdateCursor(io, ImGui.GetMouseCursor());
            UpdateXRControllers(io, UImGuiUtility.VRContext.VirtualXRInput);
        }

        #endregion
    }
}