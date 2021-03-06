﻿using OpenSpace.Loader;
using System.Linq;
using UnityEngine;

namespace OpenSpace.ROM {
	public class State : ROMStruct {
		// Size: 20
		public Reference<AnimationReference> anim; // 0
		public ushort ref_32or31; // 2
		public Reference<StateTransitionArray> transitions;
		public Reference<State> state6; //6
		public Reference<State> state8; //8
		public ushort len_transitions;
		public ushort type_32or31; // 12
		public ushort word14;
		public ushort speed;
		public byte byte18;
		public byte byte19;

		protected override void ReadInternal(Reader reader) {
			anim = new Reference<AnimationReference>(reader, true);
			ref_32or31 = reader.ReadUInt16();
			transitions = new Reference<StateTransitionArray>(reader);
			state6 = new Reference<State>(reader, true);
			state8 = new Reference<State>(reader, true);
			len_transitions = reader.ReadUInt16();
			type_32or31 = reader.ReadUInt16();
			word14 = reader.ReadUInt16();
			speed = reader.ReadUInt16();
			byte18 = reader.ReadByte();
			byte19 = reader.ReadByte();

			transitions.Resolve(reader, t => t.length = len_transitions);
		}
	}
}
