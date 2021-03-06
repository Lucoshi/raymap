﻿using OpenSpace.Loader;
using System;
using System.Linq;
using UnityEngine;

namespace OpenSpace.ROM {
	public class GeometricObject : ROMStruct {
		public float scaleFactor;
		public float factor_1;
		public Reference<CompressedVector3Array> verticesCollide;
		public Reference<CompressedVector3Array> verticesVisual;
		public Reference<CompressedVector3Array> normals;
		public Reference<GeometricElementListCollide> elementsCollide;
		public Reference<GeometricElementListVisual> elementsVisual;
		public ushort num_verticesCollide;
		public ushort num_verticesVisual;
		public ushort num_elementsCollide;
		public ushort num_elementsVisual;
		public ushort unk0;
		public ushort unk1;
		public ushort hasVertexColors;
		public ushort lookAtMode;

		public float ScaleFactor {
			get {
				if (scaleFactor != 0f) return scaleFactor;
				return 1f;
			}
		}

		protected override void ReadInternal(Reader reader) {
			scaleFactor = reader.ReadSingle();
			factor_1 = reader.ReadSingle();
			verticesCollide = new Reference<CompressedVector3Array>(reader);
			if (Settings.s.platform == Settings.Platform._3DS) {
				verticesVisual = new Reference<CompressedVector3Array>(reader);
				normals = new Reference<CompressedVector3Array>(reader);
			}
			elementsCollide = new Reference<GeometricElementListCollide>(reader);
			elementsVisual = new Reference<GeometricElementListVisual>(reader);
			num_verticesCollide = reader.ReadUInt16();
			if (Settings.s.platform == Settings.Platform._3DS) {
				num_verticesVisual = reader.ReadUInt16();
			}
			num_elementsCollide = reader.ReadUInt16();
			num_elementsVisual = reader.ReadUInt16();
			unk0 = reader.ReadUInt16();
			unk1 = reader.ReadUInt16();
			hasVertexColors = reader.ReadUInt16();
			lookAtMode = reader.ReadUInt16();

			//MapLoader.Loader.print("Vertices: " + num_vectors_1 + " or " + string.Format("{0:X4}", num_vectors_1));
			if (Settings.s.platform != Settings.Platform._3DS) {
				num_verticesVisual = num_verticesCollide;
				/*verticesCollide = normals;
				verticesVisual = normals;*/
			}

			verticesCollide?.Resolve(reader, v => { v.length = num_verticesCollide; });
			verticesVisual?.Resolve(reader, v => { v.length = num_verticesVisual; });
			normals?.Resolve(reader, v => { v.length = num_verticesVisual; });
			elementsCollide.Resolve(reader, v => { v.length = num_elementsCollide; });
			elementsVisual.Resolve(reader, v => { v.length = num_elementsVisual; });
		}

		public GameObject GetGameObject(Type type) {
			GameObject gao = new GameObject("GeometricObject @ " + Offset);
			if (type == Type.Visual) {
				gao.layer = LayerMask.NameToLayer("Visual");
			} else {
				gao.layer = LayerMask.NameToLayer("Collide");
			}
			gao.name = gao.name + " - S0:" + scaleFactor
										+ " - S1:" + factor_1
										+ " - U0:" + string.Format("{0:X4}", unk0)
										+ " - U1:" + string.Format("{0:X4}", unk1)
										+ " - U2:" + string.Format("{0:X4}", hasVertexColors)
										+ " - U3:" + string.Format("{0:X4}", lookAtMode);
			MapLoader l = MapLoader.Loader;
			if (type == Type.Visual) {
				if (elementsVisual.Value != null) {
					//gao.transform.position = new Vector3(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f));
					foreach (GeometricElementListVisual.GeometricElementListEntry entry in elementsVisual.Value.elements) {
						/*if (entry.element.Value == null) {
							l.print("Visual element null: " + entry.element.type);
						}*/
						if (entry.element.Value != null) {
							GameObject child = null;
							if (entry.element.Value is GeometricElementTriangles) {
								GeometricElementTriangles el = entry.element.Value as GeometricElementTriangles;
								child = el.GetGameObject(type, this);
							} else if (entry.element.Value is GeometricElementSprites) {
								GeometricElementSprites el = entry.element.Value as GeometricElementSprites;
								child = el.GetGameObject(type, this);
							}
							if (child != null) {
								child.transform.SetParent(gao.transform);
								child.transform.localPosition = Vector3.zero;
							}
						}
					}
				}
				if (lookAtMode != 0) {
					BillboardBehaviour billboard = gao.AddComponent<BillboardBehaviour>();
					billboard.mode = (BillboardBehaviour.LookAtMode)lookAtMode;
				}
			} else {
				if (elementsCollide.Value != null) {
					foreach (GeometricElementListCollide.GeometricElementListEntry entry in elementsCollide.Value.elements) {
						if (entry.element.Value == null) {
							l.print("Collide element null: " + entry.element.type);
						}
						if (entry.element.Value != null) {
							GameObject child = null;
							if (entry.element.Value is GeometricElementTrianglesCollide) {
								GeometricElementTrianglesCollide el = entry.element.Value as GeometricElementTrianglesCollide;
								child = el.GetGameObject(type, this);
							}
							if (child != null) {
								child.transform.SetParent(gao.transform);
								child.transform.localPosition = Vector3.zero;
							}
						}
					}
				}
			}
			return gao;
		}

		public void MorphVertices(GameObject gao, GeometricObject go, float lerp) {
			for (int i = 0; i < num_elementsVisual; i++) {
				ROMStruct entry1 = elementsVisual.Value.elements[i].element.Value;
				ROMStruct entry2 = go.elementsVisual.Value.elements[i].element.Value;
				if (entry1 != null && entry2 != null && entry1 is GeometricElementTriangles && entry2 is GeometricElementTriangles) {
					GeometricElementTriangles tris1 = entry1 as GeometricElementTriangles;
					GeometricElementTriangles tris2 = entry2 as GeometricElementTriangles;
					MeshFilter[] mfs = gao.GetComponentsInChildren<MeshFilter>();
					MeshFilter mf = mfs.FirstOrDefault(m => m.name == "ElementTriangles @ " + tris1.Offset);
					if (mf != null) {
						tris1.MorphVertices(mf.sharedMesh, tris2, this, go, lerp);
					}
				}
			}
		}

		public void ResetMorph(GameObject gao) {
			for (int i = 0; i < num_elementsVisual; i++) {
				ROMStruct entry1 = elementsVisual.Value.elements[i].element.Value;
				if (entry1 != null && entry1 is GeometricElementTriangles ) {
					GeometricElementTriangles tris1 = entry1 as GeometricElementTriangles;
					MeshFilter[] mfs = gao.GetComponentsInChildren<MeshFilter>();
					MeshFilter mf = mfs.FirstOrDefault(m => m.name == "ElementTriangles @ " + tris1.Offset);
					if (mf != null) {
						tris1.ResetMorph(mf.sharedMesh, this);
						//mf.mesh.RecalculateNormals();
					}
				}
			}
		}

		public enum Type {
			Visual,
			Collide
		}
    }
}
