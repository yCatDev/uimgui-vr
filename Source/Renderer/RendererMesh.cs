#if UNITY_2020_1_OR_NEWER
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UImGui.Assets;
using UImGui.Texture;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UImGui.Renderer
{
	internal sealed class RendererMesh : IRenderer
	{
		// Skip all checks and validation when updating the mesh.
		private const MeshUpdateFlags NoMeshChecks = MeshUpdateFlags.DontNotifyMeshUsers |
			MeshUpdateFlags.DontRecalculateBounds |
			MeshUpdateFlags.DontResetBoneBounds |
			MeshUpdateFlags.DontValidateIndices;

		// Color sent with TexCoord1 semantics because otherwise Color attribute would be reordered to come before UVs.
		private static readonly VertexAttributeDescriptor[] _vertexAttributes = new[]
		{
			new VertexAttributeDescriptor(VertexAttribute.Position , VertexAttributeFormat.Float32, 2), // Position.
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2), // UV.
			new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UInt32 , 1), // Color.
        };

		private Material _material;
		private Mesh _mesh;

		private readonly Shader _shader;
		private readonly int _textureID;
		private readonly TextureManager _textureManager;
		
		private int _prevSubMeshCount = 1;  // number of sub meshes used previously
		private readonly LocalKeyword _clipRectKeyword;

		public RendererMesh(ShaderResourcesAsset resources, TextureManager texManager)
		{
			_shader = resources.Shader.Mesh;
			_textureManager = texManager;
			_textureID = Shader.PropertyToID(resources.PropertyNames.Texture);
			_clipRectKeyword = new LocalKeyword(resources.Shader.Mesh, resources.PropertyNames.ClipRectKeyword);
		}

		public void Initialize(ImGuiIOPtr io)
		{
			io.SetBackendRendererName("Unity Mesh");
			// Supports ImDrawCmd::VtxOffset to output large meshes while still using 16-bits indices.
			io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

			_material = new Material(_shader)
			{
				hideFlags = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset
			};

			_mesh = new Mesh
			{
				name = "DearImGui Mesh"
			};
			_mesh.MarkDynamic();
		}

		public void Shutdown(ImGuiIOPtr io)
		{
			io.SetBackendRendererName(null);

			if (_mesh != null)
			{
				Object.Destroy(_mesh);
				_mesh = null;
			}

			if (_material != null)
			{
				Object.Destroy(_material);
				_material = null;
			}
		}

		public void RenderDrawLists(List<DrawCommand> commands, ImDrawDataPtr drawData)
		{
			Vector2 fbOSize = drawData.DisplaySize * drawData.FramebufferScale;

			// Avoid rendering when minimized.
			if (fbOSize.x <= 0f || fbOSize.y <= 0f || drawData.TotalVtxCount == 0) return;

			Constants.UpdateMeshMarker.Begin();
			UpdateMesh(drawData);
			Constants.UpdateMeshMarker.End();

			commands.Add(new DrawCommand()
			{
				type = DrawCommandType.BeginSample,
				stringData = Constants.ExecuteDrawCommandsMarker
			});
			//commandBuffer.BeginSample(Constants.ExecuteDrawCommandsMarker);
			Constants.CreateDrawCommandsMarker.Begin();

			CreateDrawCommands(commands, drawData, fbOSize);

			Constants.CreateDrawCommandsMarker.End();
			commands.Add(new DrawCommand()
			{
				type = DrawCommandType.EndSample,
				stringData = Constants.ExecuteDrawCommandsMarker
			});
			//commandBuffer.EndSample(Constants.ExecuteDrawCommandsMarker);
		}

		private void UpdateMesh(ImDrawDataPtr drawData)
		{
			// Number of submeshes is the same as the nr of ImDrawCmd.
			int subMeshCount = 0;
			for (int n = 0, nMax = drawData.CmdListsCount; n < nMax; ++n)
			{
				subMeshCount += drawData.CmdLists[n].CmdBuffer.Size;
			}

			if (_prevSubMeshCount != subMeshCount)
			{
				// Occasionally crashes when changing subMeshCount without clearing first.
				_mesh.Clear(true);
				_mesh.subMeshCount = _prevSubMeshCount = subMeshCount;
			}
			_mesh.SetVertexBufferParams(drawData.TotalVtxCount, _vertexAttributes);
			_mesh.SetIndexBufferParams(drawData.TotalIdxCount, IndexFormat.UInt16);

			//  Upload data into mesh.
			int vtxOf = 0;
			int idxOf = 0;
			List<SubMeshDescriptor> descriptors = new List<SubMeshDescriptor>();

			for (int n = 0, nMax = drawData.CmdListsCount; n < nMax; ++n)
			{
				ImDrawListPtr drawList = drawData.CmdLists[n];

				unsafe
				{
					// TODO: Convert NativeArray to C# array or list (remove collections).
					NativeArray<ImDrawVert> vtxArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ImDrawVert>(
						(void*)drawList.VtxBuffer.Data, drawList.VtxBuffer.Size, Allocator.None);
					NativeArray<ushort> idxArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ushort>(
						(void*)drawList.IdxBuffer.Data, drawList.IdxBuffer.Size, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
					NativeArrayUnsafeUtility
						.SetAtomicSafetyHandle(ref vtxArray, AtomicSafetyHandle.GetTempMemoryHandle());
					NativeArrayUnsafeUtility
						.SetAtomicSafetyHandle(ref idxArray, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
					// Upload vertex/index data.
					_mesh.SetVertexBufferData(vtxArray, 0, vtxOf, vtxArray.Length, 0, NoMeshChecks);
					_mesh.SetIndexBufferData(idxArray, 0, idxOf, idxArray.Length, NoMeshChecks);

					// Define subMeshes.
					for (int i = 0, iMax = drawList.CmdBuffer.Size; i < iMax; ++i)
					{
						ImDrawCmdPtr cmd = drawList.CmdBuffer[i];
						SubMeshDescriptor descriptor = new SubMeshDescriptor
						{
							topology = MeshTopology.Triangles,
							indexStart = idxOf + (int)cmd.IdxOffset,
							indexCount = (int)cmd.ElemCount,
							baseVertex = vtxOf + (int)cmd.VtxOffset,
						};
						descriptors.Add(descriptor);
					}

					vtxOf += vtxArray.Length;
					idxOf += idxArray.Length;
				}
			}

			_mesh.SetSubMeshes(descriptors, NoMeshChecks);
			_mesh.UploadMeshData(false);
		}

		private void CreateDrawCommands(List<DrawCommand> commands, ImDrawDataPtr drawData, Vector2 fbSize)
		{
			IntPtr prevTextureId = IntPtr.Zero;
			Vector4 clipOffset = new Vector4(drawData.DisplayPos.x, drawData.DisplayPos.y,
				drawData.DisplayPos.x, drawData.DisplayPos.y);
			Vector4 clipScale = new Vector4(drawData.FramebufferScale.x, drawData.FramebufferScale.y,
				drawData.FramebufferScale.x, drawData.FramebufferScale.y);

			commands.Add(new DrawCommand()
			{
				type = DrawCommandType.SetViewport,
				vectorData = new Vector4(0f, 0f, fbSize.x, fbSize.y)
			});
			commands.Add(new DrawCommand()
			{
				type = DrawCommandType.SetViewProjectionMatrices,
				matrixA = Matrix4x4.Translate(new Vector3(0.5f / fbSize.x, 0.5f / fbSize.y, 0f)), // Small adjustment to improve text.
				matrixB = Matrix4x4.Ortho(0f, fbSize.x, fbSize.y, 0f, 0f, 1f)
			});
			//commandBuffer.SetViewport(new Rect(0f, 0f, fbSize.x, fbSize.y));
			/*commandBuffer.SetViewProjectionMatrices(
				Matrix4x4.Translate(new Vector3(0.5f / fbSize.x, 0.5f / fbSize.y, 0f)), // Small adjustment to improve text.
				Matrix4x4.Ortho(0f, fbSize.x, fbSize.y, 0f, 0f, 1f));*/
			

			int subOf = 0;
			for (int n = 0, nMax = drawData.CmdListsCount; n < nMax; ++n)
			{
				ImDrawListPtr drawList = drawData.CmdLists[n];
				for (int i = 0, iMax = drawList.CmdBuffer.Size; i < iMax; ++i, ++subOf)
				{
					ImDrawCmdPtr drawCmd = drawList.CmdBuffer[i];
					if (drawCmd.UserCallback != IntPtr.Zero)
					{
						UserDrawCallback userDrawCallback = Marshal.GetDelegateForFunctionPointer<UserDrawCallback>(drawCmd.UserCallback);
						userDrawCallback(drawList, drawCmd);
					}
					else
					{
						// Project scissor rectangle into framebuffer space and skip if fully outside.
						Vector4 clipSize = drawCmd.ClipRect - clipOffset;
						Vector4 clip = Vector4.Scale(clipSize, clipScale);

						if (clip.x >= fbSize.x || clip.y >= fbSize.y || clip.z < 0f || clip.w < 0f) continue;

						if (prevTextureId != drawCmd.TextureId)
						{
							prevTextureId = drawCmd.TextureId;

							// TODO: Implement ImDrawCmdPtr.GetTexID().
							bool hasTexture = _textureManager.TryGetTexture(prevTextureId, out UnityEngine.Texture texture);
							Assert.IsTrue(hasTexture, $"Texture {prevTextureId} does not exist. Try to use UImGuiUtility.GetTextureID().");

							commands.Add(new DrawCommand()
							{
								type = DrawCommandType.SetGlobalTexture,
								propertyId = _textureID,
								textureData = texture
							});
						}
						commands.Add(new DrawCommand()
						{
							type = DrawCommandType.DisableKeyword,
							materialData = _material,
							keyword = _clipRectKeyword
						});

						commands.Add(new DrawCommand()
						{
							 type = DrawCommandType.EnableScissorRect,
							 vectorData = new Vector4(clip.x, fbSize.y - clip.w, clip.z - clip.x, clip.w - clip.y)
						});
						commands.Add(new DrawCommand()
						{
							type = DrawCommandType.DrawMesh,
							meshData = _mesh,
							matrixA = Matrix4x4.identity,
							materialData = _material,
							intData = subOf,
						});
						//commandBuffer.EnableScissorRect(new Rect(clip.x, fbSize.y - clip.w, clip.z - clip.x, clip.w - clip.y)); // Invert y.
						//commandBuffer.DrawMesh(_mesh, Matrix4x4.identity, _material, subOf, -1, _materialProperties);
					}
				}
			}
			
			commands.Add(new DrawCommand()
			{
				type = DrawCommandType.DisableScissorRect
			});
			//commandBuffer.DisableScissorRect();
		}
	}
}
#endif