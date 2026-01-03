using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
#if HAS_URP
using UnityEngine.Rendering.Universal;
using UnityEngine;
#endif

namespace UImGui.Renderer
{
#if HAS_URP
    public class RenderImGui : ScriptableRendererFeature
    {
        
        private class CommandBufferPass : ScriptableRenderPass
        {
            private readonly PassData _passData;

            private class PassData
            {
                public List<DrawCommand> Commands;
                
                public void Setup(List<DrawCommand> commands)
                {
                    Commands = commands;
                }
            }

            public CommandBufferPass()
            {
                _passData = new PassData();
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var commandBuffer = CommandBufferPool.Get("Dear ImGUI");
                
                DrawCommandUtils.BuildCommandBuffer(ref commandBuffer, UImGuiUtility.Context.DrawCommands);
                
                context.ExecuteCommandBuffer(commandBuffer);
                CommandBufferPool.Release(commandBuffer);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var commands = UImGuiUtility.Context.DrawCommands;
                
                using (var builder =
                       renderGraph.AddRasterRenderPass<PassData>("ImGui Render Pass", out var passData))
                {
                    DrawCommandUtils.PrepareForRenderGraph(builder, renderGraph, commands);
                    
                    passData.Setup(commands);
                    
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                    
                    builder.AllowPassCulling(commands.Count == 0);
                    
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        ExecuteImGuiPass(data, context);
                    });
                }
            }

            private void ExecuteImGuiPass(PassData data, RasterGraphContext context)
            {
                var cmd = context.cmd;
                
                DrawCommandUtils.BuildCommandBuffer(ref cmd, data.Commands);
            }
        }

        [Serializable]
        public class Settings
        {
            public bool drawInSceneView;
        }

        public Settings settings;
        public RenderPassEvent RenderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        private CommandBufferPass _commandBufferPass;

        public override void Create()
        {
            _commandBufferPass = new CommandBufferPass()
            {
                renderPassEvent = RenderPassEvent,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (ReferenceEquals(UImGuiUtility.Context, null)) return;
            if (ReferenceEquals(UImGuiUtility.Context.DrawCommands, null)) return;
            if (renderingData.cameraData.cameraType != CameraType.Game && (!settings.drawInSceneView ||
                                                                           renderingData.cameraData.cameraType !=
                                                                           CameraType.SceneView)) return;
            _commandBufferPass.renderPassEvent = RenderPassEvent;

            renderer.EnqueuePass(_commandBufferPass);
        }
    }
#else
	public class RenderImGui : UnityEngine.ScriptableObject
	{
		public CommandBuffer CommandBuffer;
	}
#endif
}