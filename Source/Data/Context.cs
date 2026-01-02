using System;
using System.Collections.Generic;
using UImGui.Renderer;
using UImGui.Texture;

namespace UImGui
{
	internal sealed class Context
	{
		public IntPtr ImGuiContext;
		public IntPtr ImNodesContext;
		public IntPtr ImPlotContext;
		public TextureManager TextureManager;
		public List<DrawCommand> DrawCommands;
	}
}