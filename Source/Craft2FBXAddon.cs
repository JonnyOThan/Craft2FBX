﻿using KSP.UI.Screens;
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
		string modRootPath;

		void Start()
		{
			var iconTexture = GameDatabase.Instance.GetTexture("Craft2FBX/icon", false);
			toolbarButton = ApplicationLauncher.Instance.AddModApplication(ToolbarClick, ToolbarClick, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, iconTexture);
		}

		void OnDestroy()
		{
			ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
		}

		void GetCraftExportSettings(out Part rootPart, out string name)
		{
			if (HighLogic.LoadedSceneIsEditor)
			{
				rootPart = EditorLogic.RootPart;
				name = EditorLogic.fetch.ship.shipName;
			}
			else
			{
				rootPart = FlightGlobals.ActiveVessel.rootPart;
				name = FlightGlobals.ActiveVessel.vesselName;
			}
		}

		private void ToolbarClick()
		{
			using (var exporter = Autodesk.Fbx.Examples.Editor.FbxExporter06.Create())
			{
				GetCraftExportSettings(out Part rootPart, out string name);

				if (rootPart != null)
				{
					var modelsDirectory = Path.Combine(modRootPath, "Models");
					Directory.CreateDirectory(modelsDirectory);
					exporter.ExportAll(new[] { rootPart.gameObject }, Path.ChangeExtension(Path.Combine(modelsDirectory, name), "fbx"));
				}
			}
		}

	}
}

