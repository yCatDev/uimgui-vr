using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace UImGui.Renderer
{
    public enum DrawCommandType
    {
        Invalid,
        SetViewport,
        SetViewProjectionMatrices,
        BeginSample,
        EndSample,
        SetGlobalTexture,
        SetGlobalInt,
        SetGlobalVector,
        EnableScissorRect,
        DisableScissorRect,
        DrawMesh,
        DrawRenderer,
        DrawProcedural,
        ClearDepth,
        EnableKeyword,
        DisableKeyword
    }

    public struct DrawCommand
    {
        public DrawCommandType type;
        public Vector4 vectorData;
        public Matrix4x4 matrixA;
        public Matrix4x4 matrixB;
        public string stringData;
        public int propertyId;
        public UnityEngine.Texture textureData;
        public Mesh meshData;
        public Material materialData;
        public int intData;
        public GraphicsBuffer indexBuffer;
        public ComputeBuffer argumentsBuffer;
        public UnityEngine.Renderer renderer;
#if UNITY_6000_0_OR_NEWER
        public TextureHandle renderGraphTextureHandle;
#endif
        public LocalKeyword keyword;
    }

    public static class DrawCommandUtils
    {
#if UNITY_6000_0_OR_NEWER
        public static void BuildCommandBuffer(ref RasterCommandBuffer commandBuffer, List<DrawCommand> drawCommands)
        {
            for (int i = 0; i < drawCommands.Count; i++)
            {
                var command = drawCommands[i];
                switch (command.type)
                {
                    case DrawCommandType.SetViewport:
                        commandBuffer.SetViewport(new Rect(command.vectorData.x, command.vectorData.y,
                            command.vectorData.z, command.vectorData.w));
                        break;
                    case DrawCommandType.SetViewProjectionMatrices:
                        commandBuffer.SetViewProjectionMatrices(command.matrixA, command.matrixB);
                        break;
                    case DrawCommandType.BeginSample:
                        commandBuffer.BeginSample(command.stringData);
                        break;
                    case DrawCommandType.EndSample:
                        commandBuffer.EndSample(command.stringData);
                        break;
                    case DrawCommandType.SetGlobalTexture:
                        commandBuffer.SetGlobalTexture(command.propertyId, command.renderGraphTextureHandle);
                        break;
                    case DrawCommandType.SetGlobalInt:
                        commandBuffer.SetGlobalInt(command.propertyId, command.intData);
                        break;
                    case DrawCommandType.SetGlobalVector:
                        commandBuffer.SetGlobalVector(command.propertyId, command.vectorData);
                        break;
                    case DrawCommandType.EnableScissorRect:
                        commandBuffer.EnableScissorRect(new Rect(command.vectorData.x, command.vectorData.y,
                            command.vectorData.z, command.vectorData.w));
                        break;
                    case DrawCommandType.DisableScissorRect:
                        commandBuffer.DisableScissorRect();
                        break;
                    case DrawCommandType.DrawMesh:
                        commandBuffer.DrawMesh(command.meshData, command.matrixA, command.materialData, command.intData,
                            0);
                        break;
                    case DrawCommandType.DrawRenderer:
                        commandBuffer.DrawRenderer(command.renderer, command.materialData, command.intData, 0);
                        break;
                    case DrawCommandType.DrawProcedural:
                        commandBuffer.DrawProceduralIndirect(command.indexBuffer, command.matrixA, command.materialData,
                            -1, MeshTopology.Triangles, command.argumentsBuffer, command.intData);
                        break;
                    case DrawCommandType.ClearDepth:
                        commandBuffer.ClearRenderTarget(true, false, Color.clear);
                        break;
                    case DrawCommandType.EnableKeyword:
                        commandBuffer.EnableKeyword(command.materialData, command.keyword);
                        break;
                    case DrawCommandType.DisableKeyword:
                        commandBuffer.DisableKeyword(command.materialData, command.keyword);
                        break;
                }
            }
        }
#endif

        public static void BuildCommandBuffer(ref CommandBuffer commandBuffer, List<DrawCommand> drawCommands)
        {
            for (int i = 0; i < drawCommands.Count; i++)
            {
                var command = drawCommands[i];
                switch (command.type)
                {
                    case DrawCommandType.SetViewport:
                        commandBuffer.SetViewport(new Rect(command.vectorData.x, command.vectorData.y,
                            command.vectorData.z, command.vectorData.w));
                        break;
                    case DrawCommandType.SetViewProjectionMatrices:
                        commandBuffer.SetViewProjectionMatrices(command.matrixA, command.matrixB);
                        break;
                    case DrawCommandType.BeginSample:
                        commandBuffer.BeginSample(command.stringData);
                        break;
                    case DrawCommandType.EndSample:
                        commandBuffer.EndSample(command.stringData);
                        break;
                    case DrawCommandType.SetGlobalTexture:
                        commandBuffer.SetGlobalTexture(command.propertyId, command.textureData);
                        break;
                    case DrawCommandType.SetGlobalInt:
                        commandBuffer.SetGlobalInt(command.propertyId, command.intData);
                        break;
                    case DrawCommandType.SetGlobalVector:
                        commandBuffer.SetGlobalVector(command.propertyId, command.vectorData);
                        break;
                    case DrawCommandType.EnableScissorRect:
                        commandBuffer.EnableScissorRect(new Rect(command.vectorData.x, command.vectorData.y,
                            command.vectorData.z, command.vectorData.w));
                        break;
                    case DrawCommandType.DisableScissorRect:
                        commandBuffer.DisableScissorRect();
                        break;
                    case DrawCommandType.DrawMesh:
                        commandBuffer.DrawMesh(command.meshData, command.matrixA, command.materialData, command.intData,
                            0);
                        break;
                    case DrawCommandType.DrawRenderer:
                        commandBuffer.DrawRenderer(command.renderer, command.materialData, command.intData, 0);
                        break;
                    case DrawCommandType.DrawProcedural:
                        commandBuffer.DrawProceduralIndirect(command.indexBuffer, command.matrixA, command.materialData,
                            -1, MeshTopology.Triangles, command.argumentsBuffer, command.intData);
                        break;
                    case DrawCommandType.ClearDepth:
                        commandBuffer.ClearRenderTarget(true, false, Color.clear);
                        break;
                    case DrawCommandType.EnableKeyword:
                        commandBuffer.EnableKeyword(command.materialData, command.keyword);
                        break;
                    case DrawCommandType.DisableKeyword:
                        commandBuffer.DisableKeyword(command.materialData, command.keyword);
                        break;
                }
            }
        }

#if UNITY_6000_0_OR_NEWER
        public static void PrepareForRenderGraph(IRasterRenderGraphBuilder builder, RenderGraph renderGraph,
            List<DrawCommand> commands)
        {
            for (var i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                if (command.type == DrawCommandType.SetGlobalTexture)
                {
                    var handle = renderGraph.ImportTexture(RTHandles.Alloc(command.textureData));
                    builder.UseTexture(handle, AccessFlags.Read);
                    command.renderGraphTextureHandle = handle;
                }

                commands[i] = command;
            }
        }
#endif
    }
}