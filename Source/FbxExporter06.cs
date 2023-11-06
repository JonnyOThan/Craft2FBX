﻿//#define UNI_15935
// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.
//
// Licensed under the ##LICENSENAME##.
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using static Autodesk.Fbx.Examples.Editor.FbxExporter06;
using System;

namespace Autodesk.Fbx.Examples
{
	namespace Editor
	{
		public class FbxExporter06 : System.IDisposable
		{
			const string Title =
				"Example 06: exporting a static mesh with materials and textures";

			const string Subject =
				@"Example FbxExporter06 illustrates how to:

                    1) create and initialize an exporter
                    2) create a scene
                    3) create a node with transform data
                    4) add static mesh to a node
                    5) add texture UVs
                    6) add materials and textures
                    7) export the static mesh to a FBX file (FBX201400 compatible)
                            ";

			const string Keywords =
				"export mesh materials textures uvs";

			const string Comments =
				@"";

			const string MenuItemName = "File/Export FBX/6. Static mesh with materials and textures";

			const string FileBaseName = "example_static_mesh_with_materials_and_textures";

			/// <summary>
			/// Create instance of example
			/// </summary>
			public static FbxExporter06 Create() { return new FbxExporter06(); }

			/// <summary>
			/// Map Unity material name to FBX material object
			/// </summary>
			Dictionary<string, FbxSurfaceMaterial> MaterialMap = new Dictionary<string, FbxSurfaceMaterial>();

			/// <summary>
			/// Map texture filename name to FBX texture object
			/// </summary>
			Dictionary<string, FbxTexture> TextureMap = new Dictionary<string, FbxTexture>();

			Dictionary<Mesh, FbxMesh> MeshMap = new Dictionary<Mesh, FbxMesh>();

			/// <summary>
			/// Export an Unity Texture
			/// </summary>
			public void ExportTexture(Material unityMaterial, string unityPropName,
				FbxSurfaceMaterial fbxMaterial, string fbxPropName)
			{
				if (!unityMaterial) { return; }

				// Get the texture on this property, if any.
				if (!unityMaterial.HasProperty(unityPropName)) { return; }
				var unityTexture = unityMaterial.GetTexture(unityPropName);
				if (!unityTexture) { return; }

				// Find its filename
				var textureSourceFullPath = AssetDatabase.GetAssetPath(unityTexture);
				if (textureSourceFullPath == "") { return; }

				// get absolute filepath to texture
				textureSourceFullPath = Path.GetFullPath(textureSourceFullPath);

				if (Verbose)
					Debug.Log(string.Format("{2}.{1} setting texture path {0}", textureSourceFullPath, fbxPropName, fbxMaterial.GetName()));

				// Find the corresponding property on the fbx material.
				var fbxMaterialProperty = fbxMaterial.FindProperty(fbxPropName);
				if (fbxMaterialProperty == null || !fbxMaterialProperty.IsValid()) { Debug.Log("property not found"); return; }

				// Find or create an fbx texture and link it up to the fbx material.
				if (!TextureMap.ContainsKey(textureSourceFullPath))
				{
					var fbxTexture = FbxFileTexture.Create(fbxMaterial, unityTexture.name);
					fbxTexture.SetFileName(textureSourceFullPath);
					fbxTexture.SetTextureUse(unityPropName == "_BumpMap" ? FbxTexture.ETextureUse.eBumpNormalMap : FbxTexture.ETextureUse.eStandard);
					fbxTexture.SetAlphaSource(FbxTexture.EAlphaSource.eNone);
					fbxTexture.SetDefaultAlpha(1.0);
					fbxTexture.SetMappingType(FbxTexture.EMappingType.eUV);
					if (textureSourceFullPath.EndsWith(".dds", StringComparison.InvariantCultureIgnoreCase))
					{
						fbxTexture.SetScale(1, -1);
					}
					TextureMap.Add(textureSourceFullPath, fbxTexture);
				}
				TextureMap[textureSourceFullPath].ConnectDstProperty(fbxMaterialProperty);
			}

			/// <summary>
			/// Get the color of a material, or grey if we can't find it.
			/// </summary>
			public FbxDouble3 GetMaterialColor(Material unityMaterial, string unityPropName)
			{
				if (!unityMaterial) { return new FbxDouble3(0.5); }
				if (!unityMaterial.HasProperty(unityPropName)) { return new FbxDouble3(0.5); }
				var unityColor = unityMaterial.GetColor(unityPropName);
				return new FbxDouble3(unityColor.r, unityColor.g, unityColor.b);
			}

			/// <summary>
			/// Export (and map) a Unity PBS material to FBX classic material
			/// </summary>
			public FbxSurfaceMaterial ExportMaterial(Material unityMaterial, FbxScene fbxScene)
			{
				if (Verbose)
					Debug.Log(string.Format("exporting material {0}", unityMaterial.name));

				var materialName = unityMaterial ? unityMaterial.name : "DefaultMaterial";
				if (MaterialMap.ContainsKey(materialName))
				{
					return MaterialMap[materialName];
				}

				// We'll export either Phong or Lambert. Phong if it calls
				// itself specular, Lambert otherwise.
				var shader = unityMaterial ? unityMaterial.shader : null;
				bool specular = shader && shader.name.ToLower().Contains("specular");

				var fbxMaterial = specular
					? FbxSurfacePhong.Create(fbxScene, materialName)
					: FbxSurfaceLambert.Create(fbxScene, materialName);

				// Copy the flat colours over from Unity standard materials to FBX.
				fbxMaterial.Diffuse.Set(GetMaterialColor(unityMaterial, "_Color"));
				if (unityMaterial.HasProperty("_EmissiveColor"))
				{
					fbxMaterial.Emissive.Set(GetMaterialColor(unityMaterial, "_EmissiveColor"));
				}
				else
				{
					fbxMaterial.EmissiveFactor.Set(0);
				}
				fbxMaterial.Ambient.Set(new FbxDouble3());
				fbxMaterial.TransparencyFactor.Set(1);
				//fbxMaterial.BumpFactor.Set(unityMaterial ? unityMaterial.GetFloat("_BumpScale") : 0);
				if (specular)
				{
					(fbxMaterial as FbxSurfacePhong).Specular.Set(GetMaterialColor(unityMaterial, "_SpecColor"));
				}

				// Export the textures from Unity standard materials to FBX.
				ExportTexture(unityMaterial, "_MainTex", fbxMaterial, FbxSurfaceMaterial.sDiffuse);
				ExportTexture(unityMaterial, "_Emissive", fbxMaterial, "emissive");
				ExportTexture(unityMaterial, "_BumpMap", fbxMaterial, FbxSurfaceMaterial.sNormalMap);
				if (specular)
				{
					//ExportTexture(unityMaterial, "_SpecGlosMap", fbxMaterial, FbxSurfaceMaterial.sSpecular);
				}

				MaterialMap.Add(materialName, fbxMaterial);
				return fbxMaterial;
			}

			List<Vector3> vertexBuffer = new List<Vector3>();
			List<int> indexBuffer = new List<int>();
			List<Vector2> texCoordBuffer = new List<Vector2>();
			List<Vector3> normalBuffer = new List<Vector3>();

			public FbxMesh ExportMesh(Mesh mesh, FbxManager fbxManager)
			{
				if (MeshMap.TryGetValue(mesh, out var fbxMesh))
				{
					return fbxMesh;
				}

				NumMeshes++;
				NumTriangles += mesh.triangles.Length / 3;
				NumVertices += mesh.vertexCount;

				fbxMesh = FbxMesh.Create(fbxManager, mesh.name);

				// Create control points.
				int NumControlPoints = mesh.vertexCount;

				fbxMesh.InitControlPoints(NumControlPoints);

				mesh.GetVertices(vertexBuffer);
				mesh.GetTriangles(indexBuffer, 0);

				// copy control point data from Unity to FBX
				for (int v = 0; v < NumControlPoints; v++)
				{
					fbxMesh.SetControlPointAt(new FbxVector4(vertexBuffer[v].x, vertexBuffer[v].y, vertexBuffer[v].z), v);
				}

				// Export UVs & Normals
				{
					// Set the normals on Layer 0.
					FbxLayer fbxLayer = fbxMesh.GetLayer(0 /* default layer */);
					if (fbxLayer == null)
					{
						fbxMesh.CreateLayer();
						fbxLayer = fbxMesh.GetLayer(0 /* default layer */);
					}

					// export UVs
					using (var fbxLayerElement = FbxLayerElementUV.Create(fbxMesh, "UVSet"))
					{
						fbxLayerElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);

						mesh.GetUVs(0, texCoordBuffer);

						// set texture coordinates per vertex
						FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray();
						fbxElementArray.SetCount(texCoordBuffer.Count);

						for (int n = 0; n < texCoordBuffer.Count; n++)
						{
							fbxElementArray.SetAt(n, new FbxVector2(texCoordBuffer[n][0],
															  texCoordBuffer[n][1]));
						}

						fbxLayer.SetUVs(fbxLayerElement, FbxLayerElement.EType.eTextureDiffuse);
					}

					// export Normals
					using (var fbxLayerElement = FbxLayerElementNormal.Create(fbxMesh, "Normals"))
					{
						fbxLayerElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);

						mesh.GetNormals(normalBuffer);

						FbxLayerElementArray fbxNormalArray = fbxLayerElement.GetDirectArray();
						fbxNormalArray.SetCount(normalBuffer.Count);

						for (int i = 0; i < normalBuffer.Count; ++i)
						{
							Vector3 unityNormal = normalBuffer[i];
							fbxNormalArray.SetAt(i, new FbxVector4(unityNormal.x, unityNormal.y, unityNormal.z));
						}

						fbxLayer.SetNormals(fbxLayerElement);
					}
				}

				for (int f = 0; f < indexBuffer.Count / 3; f++)
				{
					fbxMesh.BeginPolygon();
					fbxMesh.AddPolygon(indexBuffer[3 * f]);
					fbxMesh.AddPolygon(indexBuffer[3 * f + 1]);
					fbxMesh.AddPolygon(indexBuffer[3 * f + 2]);
					fbxMesh.EndPolygon();
				}

				MeshMap[mesh] = fbxMesh;
				return fbxMesh;
			}

			/// <summary>
			/// Unconditionally export this mesh object to the file.
			/// We have decided; this mesh is definitely getting exported.
			/// </summary>
			public void ExportMesh(MeshInfo meshInfo, FbxNode fbxNode, FbxScene fbxScene)
			{
				if (!meshInfo.IsValid )
					return;

				var fbxMesh = ExportMesh(meshInfo.mesh, fbxScene.GetFbxManager());

				if (meshInfo.Material)
				{
					var fbxMaterial = ExportMaterial(meshInfo.Material, fbxScene);
					fbxNode.AddMaterial(fbxMaterial);
				}

				// set the fbxNode containing the mesh
				fbxNode.SetNodeAttribute(fbxMesh);
				fbxNode.SetShadingMode(FbxNode.EShadingMode.eWireFrame);
			}

			// get a fbxNode's global default position.
			protected void ExportTransform(UnityEngine.Transform unityTransform, FbxNode fbxNode)
			{
				// get local position of fbxNode (from Unity)
				UnityEngine.Vector3 unityTranslate = unityTransform.localPosition;
				UnityEngine.Vector3 unityRotate = unityTransform.localRotation.eulerAngles;
				UnityEngine.Vector3 unityScale = unityTransform.localScale;

				// transfer transform data from Unity to Fbx
				var fbxTranslate = new FbxDouble3(unityTranslate.x, unityTranslate.y, unityTranslate.z);
				var fbxRotate = new FbxDouble3(unityRotate.x, unityRotate.y, unityRotate.z);
				var fbxScale = new FbxDouble3(unityScale.x, unityScale.y, unityScale.z);

				// set the local position of fbxNode
				fbxNode.LclTranslation.Set(fbxTranslate);
				fbxNode.LclRotation.Set(fbxRotate);
				fbxNode.LclScaling.Set(fbxScale);

				return;
			}

			/// <summary>
			/// Unconditionally export components on this game object
			/// </summary>
			protected void ExportComponents(GameObject unityGo, FbxScene fbxScene, FbxNode fbxNodeParent)
			{
				// create an FbxNode and add it as a child of parent
				FbxNode fbxNode = FbxNode.Create(fbxScene, unityGo.name);
				NumNodes++;

				ExportTransform(unityGo.transform, fbxNode);
				ExportMesh(GetMeshInfo(unityGo), fbxNode, fbxScene);

				if (Verbose)
					Debug.Log(string.Format("exporting {0}", fbxNode.GetName()));

				fbxNodeParent.AddChild(fbxNode);

				// now  unityGo  through our children and recurse
				foreach (Transform childT in unityGo.transform)
				{
					ExportComponents(childT.gameObject, fbxScene, fbxNode);
				}

				return;
			}

			/// <summary>
			/// Export all the objects in the set.
			/// Return the number of objects in the set that we exported.
			/// </summary>
			public int ExportAll(IEnumerable<UnityEngine.Object> unityExportSet, string fileName)
			{
				Verbose = true;

				// Create the FBX manager
				using (var fbxManager = FbxManager.Create())
				{
					// Configure fbx IO settings.
					fbxManager.SetIOSettings(FbxIOSettings.Create(fbxManager, Globals.IOSROOT));

					// Export texture as embedded
					fbxManager.GetIOSettings().SetBoolProp(Globals.EXP_FBX_EMBEDDED, true);

					// Create the exporter
					var fbxExporter = FbxExporter.Create(fbxManager, "Exporter");

					// Initialize the exporter.
					int fileFormat = fbxManager.GetIOPluginRegistry().FindWriterIDByDescription("FBX binary (*.fbx)");
					bool status = fbxExporter.Initialize(fileName, fileFormat, fbxManager.GetIOSettings());
					// Check that initialization of the fbxExporter was successful
					if (status)
					{
						// Set compatibility to 2014
						fbxExporter.SetFileExportVersion("FBX201400");

						// Create a scene
						var fbxScene = FbxScene.Create(fbxManager, "Scene");

						// set up the scene info
						FbxDocumentInfo fbxSceneInfo = FbxDocumentInfo.Create(fbxManager, "SceneInfo");
						fbxSceneInfo.mTitle = Title;
						fbxSceneInfo.mSubject = Subject;
						fbxSceneInfo.mAuthor = "Unity Technologies";
						fbxSceneInfo.mRevision = "1.0";
						fbxSceneInfo.mKeywords = Keywords;
						fbxSceneInfo.mComment = Comments;
						fbxScene.SetSceneInfo(fbxSceneInfo);

						// Set up the axes (Y up, Z forward, X to the right) and units (meters)
						var fbxSettings = fbxScene.GetGlobalSettings();
						fbxSettings.SetSystemUnit(FbxSystemUnit.m);

						// The Unity axis system has Y up, Z forward, X to the right (left handed system with odd parity).
						fbxSettings.SetAxisSystem(new FbxAxisSystem(FbxAxisSystem.EUpVector.eYAxis, FbxAxisSystem.EFrontVector.eParityOdd, FbxAxisSystem.ECoordSystem.eLeftHanded));

						// export set of object
						FbxNode fbxRootNode = fbxScene.GetRootNode();
						foreach (var obj in unityExportSet)
						{
							var unityGo = GetGameObject(obj);

							if (unityGo)
							{
								this.ExportComponents(unityGo, fbxScene, fbxRootNode);
							}
						}

						// Export the scene to the file.
						status = fbxExporter.Export(fbxScene);

						if (!status)
						{
							Debug.LogError(fbxExporter.GetStatus().GetErrorString());
						}

						// cleanup
						fbxScene.Destroy();
						fbxExporter.Destroy();
					}

					return status == true ? NumNodes : 0;
				}
			}

#if UNITY_EDITOR
			//
			// Create a simple user interface (menu items)
			//
			/// <summary>
			/// create menu item in the File menu
			/// </summary>
			[MenuItem(MenuItemName, false)]
			public static void OnMenuItem()
			{
				OnExport();
			}

			/// <summary>
			// Validate the menu item defined by the function above.
			/// </summary>
			[MenuItem(MenuItemName, true)]
			public static bool OnValidateMenuItem()
			{
				// Return false if no transform is selected.
				return Selection.activeTransform != null;
			}
#endif

			//
			// export mesh info from Unity
			//
			///<summary>
			///Information about the mesh that is important for exporting.
			///</summary>
			public struct MeshInfo
			{
				/// <summary>
				/// The transform of the mesh.
				/// </summary>
				public Matrix4x4 xform;
				public Mesh mesh;

				/// <summary>
				/// The gameobject in the scene to which this mesh is attached.
				/// This can be null: don't rely on it existing!
				/// </summary>
				public GameObject unityObject;

				/// <summary>
				/// Return true if there's a valid mesh information
				/// </summary>
				/// <value>The vertex count.</value>
				public bool IsValid { get { return mesh != null; } }

				/// <summary>
				/// Gets the vertex count.
				/// </summary>
				/// <value>The vertex count.</value>
				public int VertexCount { get { return mesh.vertexCount; } }

				/// <summary>
				/// TODO: Gets the binormals for the vertices.
				/// </summary>
				/// <value>The normals.</value>
				private Vector3[] m_Binormals;
				public Vector3[] Binormals
				{
					get
					{
						/// NOTE: LINQ
						///    return mesh.normals.Zip (mesh.tangents, (first, second)
						///    => Math.cross (normal, tangent.xyz) * tangent.w
						if (m_Binormals.Length == 0)
						{
							m_Binormals = new Vector3[mesh.normals.Length];

							for (int i = 0; i < mesh.normals.Length; i++)
								m_Binormals[i] = Vector3.Cross(mesh.normals[i],
																 mesh.tangents[i])
														 * mesh.tangents[i].w;

						}
						return m_Binormals;
					}
				}

				/// <summary>
				/// The material used, if any; otherwise null.
				/// We don't support multiple materials on one gameobject.
				/// </summary>
				public Material Material
				{
					get
					{
						if (!unityObject) { return null; }
						var renderer = unityObject.GetComponent<Renderer>();
						if (!renderer) { return null; }
						// .material instantiates a new material, which is bad
						// most of the time.
						return renderer.sharedMaterial;
					}
				}

				/// <summary>
				/// Initializes a new instance of the <see cref="MeshInfo"/> struct.
				/// </summary>
				/// <param name="mesh">A mesh we want to export</param>
				public MeshInfo(Mesh mesh)
				{
					this.mesh = mesh;
					this.xform = Matrix4x4.identity;
					this.unityObject = null;
					this.m_Binormals = null;
				}

				/// <summary>
				/// Initializes a new instance of the <see cref="MeshInfo"/> struct.
				/// </summary>
				/// <param name="gameObject">The GameObject the mesh is attached to.</param>
				/// <param name="mesh">A mesh we want to export</param>
				public MeshInfo(GameObject gameObject, Mesh mesh)
				{
					this.mesh = mesh;
					this.xform = gameObject.transform.localToWorldMatrix;
					this.unityObject = gameObject;
					this.m_Binormals = null;
				}
			}

			/// <summary>
			/// Get the GameObject
			/// </summary>
			private GameObject GetGameObject(UnityEngine.Object obj)
			{
				if (obj is UnityEngine.Transform)
				{
					var xform = obj as UnityEngine.Transform;
					return xform.gameObject;
				}
				else if (obj is UnityEngine.GameObject)
				{
					return obj as UnityEngine.GameObject;
				}
				else if (obj is MonoBehaviour)
				{
					var mono = obj as MonoBehaviour;
					return mono.gameObject;
				}

				return null;
			}

			/// <summary>
			/// Get a mesh renderer's mesh.
			/// </summary>
			private MeshInfo GetMeshInfo(GameObject gameObject, bool requireRenderer = true)
			{
				// Two possibilities: it's a skinned mesh, or we have a mesh filter.
				Mesh mesh = null;

				if (gameObject.activeInHierarchy)
				{
					var meshFilter = gameObject.GetComponent<MeshFilter>();
					var meshRenderer = gameObject.GetComponent<MeshRenderer>();
					if (meshFilter && ((meshRenderer && meshRenderer.enabled || !requireRenderer)))
					{
						mesh = meshFilter.sharedMesh;
					}
					else
					{
						var renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
						if (renderer && renderer.enabled || !requireRenderer)
						{
							mesh = new Mesh();
							renderer.BakeMesh(mesh);

							mesh.GetVertices(vertexBuffer);

							Matrix4x4 xform = Matrix4x4.Scale(renderer.transform.lossyScale).inverse;

							for (int i = 0; i <  vertexBuffer.Count; i++)
							{
								vertexBuffer[i] = xform * vertexBuffer[i];
							}

							// todo: normals?
							mesh.SetVertices(vertexBuffer);
						}
					}
				}
				if (!mesh)
				{
					return new MeshInfo();
				}
				return new MeshInfo(gameObject, mesh);
			}

			/// <summary>
			/// Number of nodes exported including siblings and decendents
			/// </summary>
			public int NumNodes { private set; get; }

			/// <summary>
			/// Number of meshes exported
			/// </summary>
			public int NumMeshes { private set; get; }

			/// <summary>
			/// Number of triangles exported
			/// </summary>
			public int NumTriangles { private set; get; }

			/// <summary>
			/// Number of vertices
			/// </summary>
			public int NumVertices { private set; get; }

			/// <summary>
			/// Clean up this class on garbage collection
			/// </summary>
			public void Dispose() { }

			public bool Verbose { private set; get; }

			/// <summary>
			/// manage the selection of a filename
			/// </summary>
			static string LastFilePath { get; set; }
			const string Extension = "fbx";

			private static string MakeFileName(string basename = "test", string extension = "fbx")
			{
				return basename + "." + extension;
			}

#if UNITY_EDITOR

			// use the SaveFile panel to allow user to enter a file name
			private static void OnExport()
			{
				// Now that we know we have stuff to export, get the user-desired path.
				var directory = string.IsNullOrEmpty(LastFilePath)
									  ? Application.dataPath
									  : System.IO.Path.GetDirectoryName(LastFilePath);

				var filename = string.IsNullOrEmpty(LastFilePath)
									 ? MakeFileName(basename: FileBaseName, extension: Extension)
									 : System.IO.Path.GetFileName(LastFilePath);

				var title = string.Format("Export FBX ({0})", FileBaseName);

				var filePath = EditorUtility.SaveFilePanel(title, directory, filename, "");

				if (string.IsNullOrEmpty(filePath))
				{
					return;
				}

				LastFilePath = filePath;

				using (var fbxExporter = Create())
				{
					// ensure output directory exists
					EnsureDirectory(filePath);

					if (fbxExporter.ExportAll(Selection.objects) > 0)
					{
						string message = string.Format("Successfully exported: {0}", filePath);
						UnityEngine.Debug.Log(message);
					}
				}
			}

			private static void EnsureDirectory(string path)
			{
				//check to make sure the path exists, and if it doesn't then
				//create all the missing directories.
				FileInfo fileInfo = new FileInfo(path);

				if (!fileInfo.Exists)
				{
					Directory.CreateDirectory(fileInfo.Directory.FullName);
				}
			}
#endif
		}
	}
}