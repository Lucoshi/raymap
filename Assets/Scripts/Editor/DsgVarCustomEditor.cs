﻿using UnityEngine;
using System.Collections;
using UnityEditor;
using OpenSpace.AI;
using System.Collections.Generic;
using OpenSpace;
using System;

[CustomEditor(typeof(DsgVarComponent))]
public class DsgVarCustomEditor : Editor {
    public Vector2 scrollPosition = new Vector2(0, 0);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DsgVarComponent c = (DsgVarComponent)target;

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Type/name");
        GUILayout.Label("Current value");
        GUILayout.Label("Initial value");

        GUILayout.EndHorizontal();

        foreach (DsgVarComponent.DsgVarEditableEntry entry in c.editableEntries) {

            GUILayout.BeginHorizontal();

            DrawDsgVarEntry(entry);

            GUILayout.EndHorizontal();
        }


        if (GUILayout.Button("Print initial dsgvar assignments")) {

            string printResult = "";

            foreach (DsgVarComponent.DsgVarEditableEntry dsgVarEntry in c.editableEntries) {

                printResult += DsgVarUtil.DsgVarEntryToCSharpAssignment(dsgVarEntry.entry) + Environment.NewLine;
            }

            MapLoader.Loader.print(printResult);
        }

        if (GUILayout.Button("Print dsgvar value offsets")) {

            string printResult = "";

            DsgMem dsgMem = c.dsgMem;
            foreach (DsgVarComponent.DsgVarEditableEntry dsgVarEntry in c.editableEntries) {
                Pointer offsetOfValue = (dsgMem.memBuffer + dsgVarEntry.entry.offsetInBuffer);
                printResult += dsgVarEntry.entry.NiceVariableName + " " + offsetOfValue +Environment.NewLine;
            }

            MapLoader.Loader.print(printResult);
        }

        GUILayout.EndVertical();
    }

    public void DrawDsgVarEntry(DsgVarComponent.DsgVarEditableEntry dsgVarEntry)
    {
        GUILayout.Label(dsgVarEntry.entry.type + "_" + dsgVarEntry.number);
        string stringVal = "";
        switch (dsgVarEntry.entry.type) {
            case DsgVarInfoEntry.DsgVarType.Boolean: dsgVarEntry.valueAsBool = EditorGUILayout.Toggle(dsgVarEntry.valueAsBool); break;
            case DsgVarInfoEntry.DsgVarType.Int: stringVal = GUILayout.TextField(dsgVarEntry.valueAsInt.ToString()); Int32.TryParse(stringVal, out dsgVarEntry.valueAsInt); break;
            case DsgVarInfoEntry.DsgVarType.UInt: stringVal = GUILayout.TextField(dsgVarEntry.valueAsUInt.ToString()); UInt32.TryParse(stringVal, out dsgVarEntry.valueAsUInt); break;
            case DsgVarInfoEntry.DsgVarType.Caps: stringVal = GUILayout.TextField(dsgVarEntry.valueAsUInt.ToString()); UInt32.TryParse(stringVal, out dsgVarEntry.valueAsUInt); break;
            case DsgVarInfoEntry.DsgVarType.Short: stringVal = GUILayout.TextField(dsgVarEntry.valueAsShort.ToString()); Int16.TryParse(stringVal, out dsgVarEntry.valueAsShort); break;
            case DsgVarInfoEntry.DsgVarType.UShort: stringVal = GUILayout.TextField(dsgVarEntry.valueAsUShort.ToString()); UInt16.TryParse(stringVal, out dsgVarEntry.valueAsUShort); break;
            case DsgVarInfoEntry.DsgVarType.Byte: stringVal = GUILayout.TextField(dsgVarEntry.valueAsSByte.ToString()); SByte.TryParse(stringVal, out dsgVarEntry.valueAsSByte); break;
            case DsgVarInfoEntry.DsgVarType.UByte: stringVal = GUILayout.TextField(dsgVarEntry.valueAsByte.ToString()); Byte.TryParse(stringVal, out dsgVarEntry.valueAsByte); break;
            case DsgVarInfoEntry.DsgVarType.Float: stringVal = GUILayout.TextField(dsgVarEntry.valueAsFloat.ToString()); Single.TryParse(stringVal, out dsgVarEntry.valueAsFloat); break;
            case DsgVarInfoEntry.DsgVarType.Text:
				stringVal = GUILayout.TextField(dsgVarEntry.valueAsUInt.ToString()); UInt32.TryParse(stringVal, out dsgVarEntry.valueAsUInt);
				GUILayout.Label(MapLoader.Loader.fontStruct.GetTextForHandleAndLanguageID((int)dsgVarEntry.valueAsUInt, 0)); break;
			case DsgVarInfoEntry.DsgVarType.Vector:
                float val_x = dsgVarEntry.valueAsVector.x;
                float val_y = dsgVarEntry.valueAsVector.y;
                float val_z = dsgVarEntry.valueAsVector.z;
                stringVal = GUILayout.TextField(dsgVarEntry.valueAsVector.x.ToString()); Single.TryParse(stringVal, out val_x);
                stringVal = GUILayout.TextField(dsgVarEntry.valueAsVector.y.ToString()); Single.TryParse(stringVal, out val_y);
                stringVal = GUILayout.TextField(dsgVarEntry.valueAsVector.z.ToString()); Single.TryParse(stringVal, out val_z);
                dsgVarEntry.valueAsVector = new Vector3(val_x, val_y, val_z);
                break;
            case DsgVarInfoEntry.DsgVarType.Perso:
                PersoBehaviour currentPersoBehaviour = dsgVarEntry.valueAsPersoGao != null ? dsgVarEntry.valueAsPersoGao.GetComponent<PersoBehaviour>() : null;
                PersoBehaviour selectedPersoBehaviour = ((PersoBehaviour)EditorGUILayout.ObjectField(currentPersoBehaviour, typeof(PersoBehaviour), true));

                if (selectedPersoBehaviour != null && selectedPersoBehaviour.gameObject != null) {
                    dsgVarEntry.valueAsPersoGao = selectedPersoBehaviour.gameObject;
                }
                break;
            case DsgVarInfoEntry.DsgVarType.SuperObject:
                GameObject currentGao = dsgVarEntry.valueAsSuperObjectGao != null ? dsgVarEntry.valueAsSuperObjectGao : null;
                GameObject selectedGao = ((GameObject)EditorGUILayout.ObjectField(currentGao, typeof(GameObject), true));

                if (selectedGao != null) {
                    dsgVarEntry.valueAsSuperObjectGao = selectedGao;
                }
                break;
            case DsgVarInfoEntry.DsgVarType.Waypoint:
                GameObject currentWaypointGao = dsgVarEntry.valueAsWaypointGao != null ? dsgVarEntry.valueAsWaypointGao : null;
                GameObject selectedWaypointGao = ((GameObject)EditorGUILayout.ObjectField(currentWaypointGao, typeof(GameObject), true));

                if (selectedWaypointGao != null) {
                    dsgVarEntry.valueAsWaypointGao = selectedWaypointGao;
                }
                break;
			case DsgVarInfoEntry.DsgVarType.TextArray:
				if (dsgVarEntry.entry.value.GetType().IsArray) {
					object[] array = (object[])dsgVarEntry.entry.value;

					GUILayout.BeginVertical();
					for (int i = 0; i < array.Length; i++) {

						if (array[i] != null) {
							GUILayout.TextField(array[i].ToString());
							GUILayout.Label(MapLoader.Loader.fontStruct.GetTextForHandleAndLanguageID((int)(uint)array[i], 0));
						}

					}
					GUILayout.EndVertical();
				}
				break;
			case DsgVarInfoEntry.DsgVarType.ActionArray:
            case DsgVarInfoEntry.DsgVarType.FloatArray:
            case DsgVarInfoEntry.DsgVarType.IntegerArray:
            case DsgVarInfoEntry.DsgVarType.PersoArray:
            case DsgVarInfoEntry.DsgVarType.SoundEventArray:
            case DsgVarInfoEntry.DsgVarType.SuperObjectArray:
            case DsgVarInfoEntry.DsgVarType.TextRefArray:
            case DsgVarInfoEntry.DsgVarType.VectorArray:
            case DsgVarInfoEntry.DsgVarType.WayPointArray:

                if (dsgVarEntry.entry.value.GetType().IsArray) {
                    object[] array = (object[])dsgVarEntry.entry.value;

                    GUILayout.BeginVertical();
                    for (int i = 0; i < array.Length; i++) {

                        if (array[i] != null) {
                            GUILayout.TextField(array[i].ToString());
                        }

                    }
                    GUILayout.EndVertical();
                }
                

                break;

        }

        if (dsgVarEntry.entry.initialValue != null) {
            GUILayout.Space(20);

            switch (dsgVarEntry.entry.type) {
                case DsgVarInfoEntry.DsgVarType.Boolean: dsgVarEntry.valueAsBool_initial = EditorGUILayout.Toggle(dsgVarEntry.valueAsBool_initial); break;
                case DsgVarInfoEntry.DsgVarType.Int: stringVal = GUILayout.TextField(dsgVarEntry.valueAsInt_initial.ToString()); Int32.TryParse(stringVal, out dsgVarEntry.valueAsInt_initial); break;
                case DsgVarInfoEntry.DsgVarType.UInt: stringVal = GUILayout.TextField(dsgVarEntry.valueAsUInt_initial.ToString()); UInt32.TryParse(stringVal, out dsgVarEntry.valueAsUInt_initial); break;
                case DsgVarInfoEntry.DsgVarType.Caps: stringVal = GUILayout.TextField(dsgVarEntry.valueAsUInt_initial.ToString()); UInt32.TryParse(stringVal, out dsgVarEntry.valueAsUInt_initial); break;
                case DsgVarInfoEntry.DsgVarType.Short: stringVal = GUILayout.TextField(dsgVarEntry.valueAsShort_initial.ToString()); Int16.TryParse(stringVal, out dsgVarEntry.valueAsShort_initial); break;
                case DsgVarInfoEntry.DsgVarType.UShort: stringVal = GUILayout.TextField(dsgVarEntry.valueAsUShort_initial.ToString()); UInt16.TryParse(stringVal, out dsgVarEntry.valueAsUShort_initial); break;
                case DsgVarInfoEntry.DsgVarType.Byte: stringVal = GUILayout.TextField(dsgVarEntry.valueAsSByte_initial.ToString()); SByte.TryParse(stringVal, out dsgVarEntry.valueAsSByte_initial); break;
                case DsgVarInfoEntry.DsgVarType.UByte: stringVal = GUILayout.TextField(dsgVarEntry.valueAsByte_initial.ToString()); Byte.TryParse(stringVal, out dsgVarEntry.valueAsByte_initial); break;
                case DsgVarInfoEntry.DsgVarType.Float: stringVal = GUILayout.TextField(dsgVarEntry.valueAsFloat_initial.ToString()); Single.TryParse(stringVal, out dsgVarEntry.valueAsFloat_initial); break;
                case DsgVarInfoEntry.DsgVarType.Text: stringVal = GUILayout.TextField(dsgVarEntry.valueAsUInt_initial.ToString()); UInt32.TryParse(stringVal, out dsgVarEntry.valueAsUInt); break;
                case DsgVarInfoEntry.DsgVarType.Vector:
                    float val_x = dsgVarEntry.valueAsVector_initial.x;
                    float val_y = dsgVarEntry.valueAsVector_initial.y;
                    float val_z = dsgVarEntry.valueAsVector_initial.z;
                    stringVal = GUILayout.TextField(dsgVarEntry.valueAsVector_initial.x.ToString()); Single.TryParse(stringVal, out val_x);
                    stringVal = GUILayout.TextField(dsgVarEntry.valueAsVector_initial.y.ToString()); Single.TryParse(stringVal, out val_y);
                    stringVal = GUILayout.TextField(dsgVarEntry.valueAsVector_initial.z.ToString()); Single.TryParse(stringVal, out val_z);
                    dsgVarEntry.valueAsVector_initial = new Vector3(val_x, val_y, val_z);
                    break;
                case DsgVarInfoEntry.DsgVarType.Perso:
                    PersoBehaviour currentPersoBehaviour = dsgVarEntry.valueAsPersoGao_initial != null ? dsgVarEntry.valueAsPersoGao_initial.GetComponent<PersoBehaviour>() : null;
                    PersoBehaviour selectedPersoBehaviour = ((PersoBehaviour)EditorGUILayout.ObjectField(currentPersoBehaviour, typeof(PersoBehaviour), true));

                    if (selectedPersoBehaviour != null && selectedPersoBehaviour.gameObject != null) {
                        dsgVarEntry.valueAsPersoGao_initial = selectedPersoBehaviour.gameObject;
                    }
                    break;
				case DsgVarInfoEntry.DsgVarType.Waypoint:
					GameObject currentWaypointGao = dsgVarEntry.valueAsWaypointGao_initial != null ? dsgVarEntry.valueAsWaypointGao_initial : null;
					GameObject selectedWaypointGao = ((GameObject)EditorGUILayout.ObjectField(currentWaypointGao, typeof(GameObject), true));

					if (selectedWaypointGao != null) {
						dsgVarEntry.valueAsWaypointGao_initial = selectedWaypointGao;
					}
					break;
				case DsgVarInfoEntry.DsgVarType.SuperObject:
                    GameObject currentGao = dsgVarEntry.valueAsSuperObjectGao_initial != null ? dsgVarEntry.valueAsSuperObjectGao_initial : null;
                    GameObject selectedGao = ((GameObject)EditorGUILayout.ObjectField(currentGao, typeof(GameObject), true));

                    if (selectedGao != null) {
                        dsgVarEntry.valueAsSuperObjectGao_initial = selectedGao;
                    }
                    break;

            }
        } else {
            GUILayout.FlexibleSpace();
        }
    }

}