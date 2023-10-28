using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEditor
{
	internal class AssetDatabase
	{
		public static string GetAssetPath(Texture texture)
		{
			var textureInfo = GameDatabase.Instance.GetTextureInfo(texture.name);
			return textureInfo != null ? textureInfo.file.fullPath : string.Empty;
		}
	}

}
