using KSP.UI.Screens;
using System;
using ToolbarControl_NS;
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
		}

		void OnDestroy()
		{
			ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
		}

		private void ToolbarClick()
		{
			throw new NotImplementedException();
		}
	}
}
