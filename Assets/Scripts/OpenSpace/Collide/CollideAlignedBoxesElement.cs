﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenSpace.Collide {
    public class CollideAlignedBoxesElement : ICollideGeometricElement {
        public class IndexedAlignedBox {
            public ushort minPoint;
            public ushort maxPoint;
            public Pointer off_material;

            public GameMaterial gameMaterial;
        }

        [JsonIgnore] public CollideMeshObject mesh;
        public Pointer offset;
		
        public Pointer off_boxes; // called IndexedSprites in the game code
        public ushort num_boxes;
        public IndexedAlignedBox[] boxes;
		
        private GameObject gao = null;
        public GameObject Gao {
            get {
                if (gao == null) {
                    gao = new GameObject("Collide Aligned Boxes @ " + offset);// Create object and read triangle data
                    gao.layer = LayerMask.NameToLayer("Collide");
                    CreateUnityMesh();
                }
                return gao;
            }
        }

        public CollideAlignedBoxesElement(Pointer offset, CollideMeshObject mesh) {
            this.mesh = mesh;
            this.offset = offset;
        }

        private void CreateUnityMesh() {
            for (uint i = 0; i < num_boxes; i++) {
                GameObject box_gao = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box_gao.layer = LayerMask.NameToLayer("Collide");
                box_gao.name = "Collide Aligned Boxes @ " + offset + " - " + i;
                box_gao.transform.SetParent(gao.transform);
                MeshFilter mf = box_gao.GetComponent<MeshFilter>();
                MeshRenderer mr = box_gao.GetComponent<MeshRenderer>();
				//MonoBehaviour.Destroy(box_gao.GetComponent<BoxCollider>());
				Vector3 center = Vector3.Lerp(mesh.vertices[boxes[i].minPoint], mesh.vertices[boxes[i].maxPoint], 0.5f);
                box_gao.transform.localPosition = center;
                box_gao.transform.localScale = mesh.vertices[boxes[i].maxPoint] - mesh.vertices[boxes[i].minPoint];

				mr.material = MapLoader.Loader.collideMaterial;
                if (boxes[i].gameMaterial != null && boxes[i].gameMaterial.collideMaterial != null) {
                    boxes[i].gameMaterial.collideMaterial.SetMaterial(mr);
                }
                if (mesh.type != CollideType.None) {
                    Color col = mr.material.color;
                    mr.material = MapLoader.Loader.collideTransparentMaterial;
                    mr.material.color = new Color(col.r, col.g, col.b, col.a * 0.7f);
                    switch (mesh.type) {
                        case CollideType.ZDD:
                            mr.material.SetTexture("_MainTex", Resources.Load<Texture2D>("Textures/zdd")); break;
                        case CollideType.ZDE:
                            mr.material.SetTexture("_MainTex", Resources.Load<Texture2D>("Textures/zde")); break;
                        case CollideType.ZDM:
                            mr.material.SetTexture("_MainTex", Resources.Load<Texture2D>("Textures/zdm")); break;
                        case CollideType.ZDR:
                            mr.material.SetTexture("_MainTex", Resources.Load<Texture2D>("Textures/zdr")); break;
                    }
                }
            }
        }

        public static CollideAlignedBoxesElement Read(Reader reader, Pointer offset, CollideMeshObject m) {
            MapLoader l = MapLoader.Loader;
            CollideAlignedBoxesElement s = new CollideAlignedBoxesElement(offset, m);
            if (Settings.s.engineVersion > Settings.EngineVersion.Montreal) {
                s.off_boxes = Pointer.Read(reader);
                s.num_boxes = reader.ReadUInt16();
                reader.ReadInt16(); // -1
            } else {
                s.num_boxes = (ushort)reader.ReadUInt32();
                s.off_boxes = Pointer.Read(reader);
            }

            if (s.off_boxes != null) {
                Pointer off_current = Pointer.Goto(ref reader, s.off_boxes);
                s.boxes = new IndexedAlignedBox[s.num_boxes];
                for (uint i = 0; i < s.num_boxes; i++) {
                    s.boxes[i] = new IndexedAlignedBox();
                    s.boxes[i].minPoint = reader.ReadUInt16();
                    s.boxes[i].maxPoint = reader.ReadUInt16();
                    s.boxes[i].off_material = Pointer.Read(reader);
                    s.boxes[i].gameMaterial = GameMaterial.FromOffsetOrRead(s.boxes[i].off_material, reader);
                }
                Pointer.Goto(ref reader, off_current);
            }
            return s;
        }

        // Call after clone
        public void Reset() {
            gao = null;
        }

        public ICollideGeometricElement Clone(CollideMeshObject mesh) {
            CollideAlignedBoxesElement sm = (CollideAlignedBoxesElement)MemberwiseClone();
            sm.mesh = mesh;
            sm.Reset();
            return sm;
        }
    }
}
