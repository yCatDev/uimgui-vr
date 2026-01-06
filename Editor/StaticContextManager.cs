using UnityEditor;

namespace UImGui.Editor
{
    public class StaticContextManager
    {
        [InitializeOnEnterPlayMode]
        private static void ResetStatic()
        {
            UImGuiUtility.ResetStaticContext();
        }
    }
}