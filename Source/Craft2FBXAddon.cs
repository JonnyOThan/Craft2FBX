using KSP.UI.Screens;
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Craft2FBX
{
	[KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
	public class Craft2FBXAddon : MonoBehaviour
	{
		ApplicationLauncherButton toolbarButton;

		void Start()
		{
			var iconTexture = GameDatabase.Instance.GetTexture("Craft2FBX/icon", false);
			toolbarButton = ApplicationLauncher.Instance.AddModApplication(ToolbarClick, ToolbarClick, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, iconTexture);

			var dllDir = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "Craft2FBX", "PluginData");
			if (!SetDllDirectory(dllDir))
			{
				throw new Exception("SetDllDirectory failed");
			}
		}

		void OnDestroy()
		{
			ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
		}

		private void ToolbarClick()
		{
			using (var exporter = Autodesk.Fbx.Examples.Editor.FbxExporter06.Create())
			{
				Part craftRoot = HighLogic.LoadedSceneIsEditor ? EditorLogic.RootPart : FlightGlobals.ActiveVessel.rootPart;

				if (craftRoot != null)
				{
					exporter.ExportAll(new[] { craftRoot.gameObject }, Path.Combine(KSPUtil.ApplicationRootPath, "test.fbx"));
				}
			}
		}

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetDllDirectory(string lpPathName);

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
		private static extern IntPtr LoadLibrary(
			[MarshalAs(UnmanagedType.LPStr)] string lpFileName);

	}
}

