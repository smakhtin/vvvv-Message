using System;
using System.Runtime.Serialization;
using System.Collections;

namespace VVVV.Utils.Collections
{
	[Serializable]
	public class BinList : ArrayList, ISerializable
	{
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			for (var i = 0; i<this.Count; i++)
			{
				info.AddValue(i.ToString(), this[i]);
			}
		}
		
		public void AssignFrom(IEnumerable source) {
			
			foreach (var obj in source) {
				Add(obj);
			}
			
		}
		
		public new BinList Clone() {
			var clone = new BinList();
			clone.AssignFrom(this);
			
			return clone;
		}
	}
}
