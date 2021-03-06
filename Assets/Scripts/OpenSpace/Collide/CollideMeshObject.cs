﻿using Newtonsoft.Json;
using OpenSpace.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenSpace.Collide {
    /// <summary>
    /// Mesh data (both static and dynamic)
    /// </summary>
    public class CollideMeshObject {
        public PhysicalObject po;
        public Pointer offset;
        public CollideType type;

        public GameObject gao = null;
		
        public Pointer off_modelstart;
        public ushort num_vertices;
        public ushort num_subblocks;
        public Pointer off_vertices;
        public Pointer off_normals = null;
        public Pointer off_subblock_types;
        public Pointer off_subblocks;

        public Vector3[] vertices = null;
        public Vector3[] normals = null;
        public ushort[] subblock_types = null;
        public ICollideGeometricElement[] subblocks = null;

        public CollideMeshObject(Pointer offset, CollideType type = CollideType.None) {
            this.offset = offset;
            this.type = type;
        }

        public void SetVisualsActive(bool active) {
			if (gao == null) return;
            Renderer[] renderers = gao.GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (Renderer ren in renderers) {
                ren.enabled = active;
            }
            /*if (subblocks != null) {
                foreach (ICollideGeometricElement subblock in subblocks) {
                    GameObject child = subblock.Gao;
                    if (child != null) {
                        Renderer mainRen = child.GetComponent<Renderer>();
                    }
                    //subblock.Gao
                }
            }*/
        }

        public static CollideMeshObject Read(Reader reader, Pointer offset, CollideType type = CollideType.None) {
            MapLoader l = MapLoader.Loader;
			//l.print("CollideMesh " + offset);
            CollideMeshObject m = new CollideMeshObject(offset, type);
            //l.print("Mesh obj: " + offset);
            if (Settings.s.engineVersion == Settings.EngineVersion.R3 || Settings.s.game == Settings.Game.R2Revolution) {
                m.num_vertices = reader.ReadUInt16();
                m.num_subblocks = reader.ReadUInt16();
				if (Settings.s.engineVersion == Settings.EngineVersion.R3 && Settings.s.game != Settings.Game.LargoWinch) {
					reader.ReadUInt32();
				}
            }
            if (Settings.s.engineVersion <= Settings.EngineVersion.Montreal) m.num_vertices = (ushort)reader.ReadUInt32();
            m.off_vertices = Pointer.Read(reader);
            if (Settings.s.engineVersion < Settings.EngineVersion.R3 && Settings.s.game != Settings.Game.R2Revolution) {
                m.off_normals = Pointer.Read(reader);
                Pointer.Read(reader);
                reader.ReadInt32();
            }
            if (Settings.s.engineVersion <= Settings.EngineVersion.Montreal) m.num_subblocks = (ushort)reader.ReadUInt32();
            m.off_subblock_types = Pointer.Read(reader);
            m.off_subblocks = Pointer.Read(reader);
			if (Settings.s.game != Settings.Game.R2Revolution && Settings.s.game != Settings.Game.LargoWinch) {
				Pointer.Read(reader);
				if (Settings.s.engineVersion == Settings.EngineVersion.R2) {
					reader.ReadInt32();
					reader.ReadInt32();
					reader.ReadInt32();
					reader.ReadInt32();
					m.num_vertices = reader.ReadUInt16();
					m.num_subblocks = reader.ReadUInt16();
				}
				if (Settings.s.engineVersion <= Settings.EngineVersion.Montreal) {
					reader.ReadInt32();
					reader.ReadInt32();
				}
			}
            reader.ReadInt32();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            if (Settings.s.engineVersion < Settings.EngineVersion.R3) reader.ReadUInt32();
            
            // Vertices
            Pointer off_current = Pointer.Goto(ref reader, m.off_vertices);
            m.vertices = new Vector3[m.num_vertices];
            for (int i = 0; i < m.num_vertices; i++) {
                float x = reader.ReadSingle();
                float z = reader.ReadSingle();
                float y = reader.ReadSingle();
                m.vertices[i] = new Vector3(x, y, z);
            }

            // Normals
            if (m.off_normals != null) {
                off_current = Pointer.Goto(ref reader, m.off_normals);
                m.normals = new Vector3[m.num_vertices];
                for (int i = 0; i < m.num_vertices; i++) {
                    float x = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    m.normals[i] = new Vector3(x, y, z);
                }
            }
            // Read subblock types & initialize arrays
            Pointer.Goto(ref reader, m.off_subblock_types);
            m.subblock_types = new ushort[m.num_subblocks];
            m.subblocks = new ICollideGeometricElement[m.num_subblocks];
            for (uint i = 0; i < m.num_subblocks; i++) {
                m.subblock_types[i] = reader.ReadUInt16();
            }
            m.gao = new GameObject("Collide Set "+ (type != CollideType.None ? type + " " : "") +"@ " + offset);
            m.gao.tag = "Collide";
            m.gao.layer = LayerMask.NameToLayer("Collide");
            for (uint i = 0; i < m.num_subblocks; i++) {
                Pointer.Goto(ref reader, m.off_subblocks + (i * 4));
                Pointer block_offset = Pointer.Read(reader);
                Pointer.Goto(ref reader, block_offset);
                switch (m.subblock_types[i]) {
                    /*1 = indexedtriangles
                    2 = facemap
                    3 = sprite
                    4 = TMesh
                    5 = points
                    6 = lines
                    7 = spheres
                    8 = alignedboxes
                    9 = cones
                    13 = deformationsetinfo*/
                    case 1: // Collide submesh
                        m.subblocks[i] = CollideMeshElement.Read(reader, block_offset, m);
                        //material_i++;
                        break;
                    case 7:
                        m.subblocks[i] = CollideSpheresElement.Read(reader, block_offset, m);
                        break;
                    case 8:
                        m.subblocks[i] = CollideAlignedBoxesElement.Read(reader, block_offset, m);
                        break;
                    default:
                        m.subblocks[i] = null;
                        l.print("Unknown collide geometric element type " + m.subblock_types[i] + " at offset " + block_offset + " (Object: " + offset + ")");
                        break;
                }
            }

            for (uint i = 0; i < m.num_subblocks; i++) {
                if (m.subblocks[i] != null) {
                    GameObject child = m.subblocks[i].Gao;
                    child.transform.SetParent(m.gao.transform);
                    child.transform.localPosition = Vector3.zero;
                    /*if (m.subblocks[i] is CollideMeshElement) {
                        GameObject child = ((CollideMeshElement)m.subblocks[i]).Gao;
                        child.transform.SetParent(m.gao.transform);
                        child.transform.localPosition = Vector3.zero;
                    } else if (m.subblocks[i] is CollideSpheresElement) {
                        GameObject child = ((CollideSpheresElement)m.subblocks[i]).Gao;
                        child.transform.SetParent(m.gao.transform);
                        child.transform.localPosition = Vector3.zero;
                    } else if (m.subblocks[i] is CollideAlignedBoxesElement) {
                        GameObject child = ((CollideAlignedBoxesElement)m.subblocks[i]).Gao;
                        child.transform.SetParent(m.gao.transform);
                        child.transform.localPosition = Vector3.zero;
                    }*/
                }
            }
            m.SetVisualsActive(false); // Invisible by default
            //m.gao.SetActive(false); // Invisible by default
            return m;
        }

        public CollideMeshObject Clone() {
            CollideMeshObject m = (CollideMeshObject)MemberwiseClone();
            m.gao = new GameObject("Collide Set @ " + offset);
            m.gao.tag = "Collide";
            m.subblocks = new ICollideGeometricElement[num_subblocks];
            for (uint i = 0; i < m.num_subblocks; i++) {
                if (subblocks[i] != null) {
                    m.subblocks[i] = subblocks[i].Clone(m);
                }
            }
            for (uint i = 0; i < m.num_subblocks; i++) {
                if (m.subblocks[i] != null) {
                    GameObject child = m.subblocks[i].Gao;
                    child.transform.SetParent(m.gao.transform);
                    child.transform.localPosition = Vector3.zero;
                    /*if (m.subblocks[i] is CollideMeshElement) {
                        GameObject child = ((CollideMeshElement)m.subblocks[i]).Gao;
                        child.transform.SetParent(m.gao.transform);
                        child.transform.localPosition = Vector3.zero;
                    } else if (m.subblocks[i] is CollideSpheresElement) {
                        GameObject child = ((CollideSpheresElement)m.subblocks[i]).Gao;
                        child.transform.SetParent(m.gao.transform);
                        child.transform.localPosition = Vector3.zero;
                    } else if (m.subblocks[i] is CollideAlignedBoxesElement) {
                        GameObject child = ((CollideAlignedBoxesElement)m.subblocks[i]).Gao;
                        child.transform.SetParent(m.gao.transform);
                        child.transform.localPosition = Vector3.zero;
                    }*/
                }
            }
            m.SetVisualsActive(false); // Invisible by default
            //m.gao.SetActive(false); // Invisible by default
            return m;
        }
    }
}
