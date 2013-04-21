using System;
using System.IO;

using System.Collections;
using System.Collections.Generic;

using System.Runtime.Serialization;

using System.Text;
using VVVV.Utils.OSC;
using VVVV.Utils.Collections;

namespace VVVV.Utils.Message{
	
	
	[KnownType(typeof(BinList))]
	[Serializable]
	public class Message : ISerializable, ICloneable 
	{
		
		// The inner dictionary.
		[DataMember]
		Dictionary<string, BinList> FDictionary = new Dictionary<string, BinList>();
		
		[DataMember]
		public DateTime TimeStamp 
		{
			get;
			set;
		}
		
		[DataMember]
		public string Address
		{
			get;
			set;
		}
		
		public Message() 
		{
			TimeStamp = DateTime.Now;
		}
		
		protected Message(SerializationInfo info, StreamingContext context)
		{
			// TODO: validate inputs before deserializing. See http://msdn.microsoft.com/en-us/library/ty01x675(VS.80).aspx
			foreach (SerializationEntry entry in info)
			{
				Add(entry.Name, entry.Value);
			}
		}
		
		// does not matter if you add a
		public void Add(string name, object val) 
		{
			if (val is BinList) FDictionary.Add(name, (BinList)val);
			else 
			{
				FDictionary.Add(name, new BinList());
				FDictionary[name].Add(val);
			}
		}
		
		public void AssignFrom(string name, IEnumerable list) {
			if (!FDictionary.ContainsKey(name)) FDictionary.Add(name, new BinList());
			else FDictionary[name].Clear();
			
			foreach (var obj in list) {
				FDictionary[name].Add(obj);
			}
		}
		
		public void AddFrom(string name, IEnumerable list) {
			if (!FDictionary.ContainsKey(name)) {
				FDictionary.Add(name, new BinList());
			}
			
			foreach (var obj in list) {
				FDictionary[name].Add(obj);
			}
		}
		
		public string GetConfig(Dictionary<Type, string> identities = null) {
			var sb = new StringBuilder();
			
			if (identities == null) identities = new MessageResolver().Identity;
			
			foreach (var name in FDictionary.Keys) {
				try 
				{
					var type = FDictionary[name][0].GetType();
					sb.Append(", " + identities[type]);
					sb.Append(" " + name);
				} 
				catch (Exception err) 
				{
					// type not defined
					err.ToString(); // no warning
				}
			}
			return sb.ToString().Substring(2);
		}
		
		public IEnumerable<string> GetDynamicMemberNames() 
		{
			return FDictionary.Keys;
		}
		
		public BinList this[string name]
		{
			get { return FDictionary[name]; }
			set { FDictionary[name] = value; }
		}
		
		public object Clone() {
			var message = new Message {Address = Address, TimeStamp = TimeStamp};

			foreach (var name in FDictionary.Keys) {
				var list = FDictionary[name];
				message.Add(name, list.Clone());
				
				// really deep cloning
				try 
				{
					for(var i = 0; i < list.Count; i++) 
					{
						list[i] = ((ICloneable)list[i]).Clone();
					}
				} 
				catch (Exception err) 
				{
					err.ToString(); // no warning
					// not cloneble. so keep it
				}
			}
			return message;
		}
		
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			foreach (var kvp in FDictionary)
			{
				info.AddValue(kvp.Key, kvp.Value);
			}
		}
		
		
		public override string ToString() 
		{
			var stringBuilder = new StringBuilder();
			
			stringBuilder.Append("Message " + Address + " (" + TimeStamp + ")\n");
			foreach (var name in FDictionary.Keys) 
			{
				
				stringBuilder.Append(" " + name + " \t: ");
				foreach(var o in FDictionary[name]) 
				{
					stringBuilder.Append(o + " ");
				}
				stringBuilder.AppendLine();
			}

			return stringBuilder.ToString();
		}
		
		public Stream ToOSC() 
		{
			var bundle = new OSCBundle(TimeStamp.ToFileTime());
			
			foreach (var name in FDictionary.Keys)  
			{
				var address = Address.Split('.');
				var oscAddress = "";

				foreach (var part in address) {
					if (part.Trim() != "") oscAddress += "/" + part;
				}
				
				var message = new OSCMessage(oscAddress+"/"+name);
				var binList = FDictionary[name];
				foreach (var obj in binList)
					message.Append(obj);
				bundle.Append(message);
			}

			return new MemoryStream(bundle.BinaryData); // packs implicitly
		}
		
		public static Message FromOSC(Stream stream) 
		{
			var ms = new MemoryStream();
			stream.CopyTo(ms);
			var bytes = ms.ToArray();
			var start = 0;
			var bundle = OSCBundle.Unpack(bytes, ref start, (int)stream.Length);
			
			var message = new Message();

			foreach (OSCMessage m in bundle.Values) 
			{
				var binList = new BinList();
				binList.AssignFrom(m.Values); // does not clone implicitly
				
				var address = m.Address.Split('/');
				var name = address[address.Length - 1];
				address[address.Length - 1] = "";
				var messageAddress = "";
				
				foreach (var part in address) 
				{
					if (part.Trim() != "") messageAddress += "." + part;
				}

				message.Address = messageAddress.Substring(1);
				message[name] = binList;
			}

			return message;
		}
	}
}