using System;
using UImGui.Platform;
using UImGui.Renderer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UImGui
{
    public class SimpleImGUIActivator : MonoBehaviour
    {
        [SerializeField] private UImGui uImGui;
        [SerializeField] private InputActionProperty vrActivationButton;
        [SerializeField] private InputActionProperty editorActivationButton;
        [SerializeField] private float holdDuration;
        private float _time;

        private void Start()
        {
            vrActivationButton.action.Enable();
#if UNITY_EDITOR
            editorActivationButton.action.Enable();
#endif
        }

        private void LateUpdate()
        {
            var isVRButtonPressed = vrActivationButton.action.IsPressed();
            var isEditorButtonPressed = editorActivationButton.action.IsInProgress();

            var needToOpen = isVRButtonPressed
#if UNITY_EDITOR
                             || isEditorButtonPressed
#endif
                ;
            
            if (needToOpen)
            {
                if (_time - Time.timeSinceLevelLoad < 0)
                {
                    if (!uImGui.enabled)
                    {
                        uImGui.RendererType = isVRButtonPressed ? RenderType.VRMesh : RenderType.Mesh;
                        uImGui.PlatformType = isVRButtonPressed ? InputType.VRInput : InputType.InputSystem;
                    }


                    uImGui.enabled = !uImGui.enabled;
                    _time = Time.time + holdDuration + 1f;
                }
            }
            else
            {
                _time = Time.timeSinceLevelLoad + holdDuration;
            }
        }
    }
}