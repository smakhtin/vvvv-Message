using System;
using System.IO;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Text;

using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;
using VVVV.Utils.Message;
using VVVV.Utils.Collections;

namespace VVVV.Nodes {
	[PluginInfo(Name = "Message", Category = "Join", Help = "Joins a Message from custom dynamic pins", Tags = "Dynamic, Bin, velcrome")]
	public class JoinMessageNode : DynamicNode {
		
		[Input("Send", IsSingle = true, IsToggle = true, DefaultBoolean = true)]
		ISpread<bool> FSet;
		
		[Input("Address", DefaultString = "Event")]
		ISpread<string> FAddress;
		
		[Output("Output", AutoFlush = false)]
		Pin<Message> FOutput;
		
		protected override IIOContainer<ISpread<ISpread<T>>> CreatePin<T>(string name) 
		{
			var attr = new InputAttribute(name)
				{
					BinVisibility = PinVisibility.Hidden,
					BinSize = 1,
					Order = Count,
					BinOrder = Count + 1,
					AutoValidate = false
				};

			return IOFactory.CreateIOContainer<ISpread<ISpread<T>>>(attr);
		}
		
		public override void Evaluate(int SpreadMax)
		{
			SpreadMax = 0;
			if (!FSet[0]) 
			{
				FOutput.SliceCount = 0;
				FOutput.Flush();
				
				return;
			}
			
			foreach (var name in Pins.Keys) 
			{
				var pin = ToISpread(Pins[name]);
				pin.Sync();
				SpreadMax = Math.Max(pin.SliceCount, SpreadMax);
			}
			
			FOutput.SliceCount = SpreadMax;
			for (var i = 0; i < SpreadMax; i++) 
			{
				var message = new Message {Address = FAddress[i]};

				foreach (var name in Pins.Keys) 
				{
					message.AssignFrom(name, GetISpreadData(Pins[name], i) );
				}

				FOutput[i] = message;
			}

			FOutput.Flush();
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "Message", Category = "Split", Help = "Splits a Message into custom dynamic pins", Tags = "Dynamic, Bin, velcrome")]
	#endregion PluginInfo
	public class SplitMessageNode : DynamicNode {
		[Input("Input")]
		IDiffSpread<Message> FInput;
		
		[Input("Match Rule", DefaultEnumEntry="All", IsSingle = true)]
		IDiffSpread<SelectEnum> FSelect;
		
		[Output("Address", AutoFlush = false)]
		ISpread<string> FAddress;
		
		[Output("Timestamp", AutoFlush = false)]
		ISpread<string> FTimeStamp;
		
		[Output("Configuration", AutoFlush = false)]
		ISpread<string> FConfigOut;
		
		protected override IIOContainer<ISpread<ISpread<T>>> CreatePin<T>(string name) {
			var attr = new OutputAttribute(name);
			attr.BinVisibility = PinVisibility.Hidden;
			attr.AutoFlush = false;
			
			attr.Order = Count;
			attr.BinOrder = Count+1;
			return IOFactory.CreateIOContainer<ISpread<ISpread<T>>>(attr);
		}
		
		public override void Evaluate(int SpreadMax)
		{
			SpreadMax = (FSelect[0] != SelectEnum.First)? FInput.SliceCount : 1;
			
			if (!FInput.IsChanged) {
				//				FLogger.Log(LogType.Debug, "skip split");
				return;
			}
			
			bool empty = FInput.SliceCount==0 || FInput[0] == null;
			foreach(string pinName in Pins.Keys) {
				if (empty) {
					ToISpread(Pins[pinName]).SliceCount = 0;
					FTimeStamp.SliceCount = 0;
					FAddress.SliceCount = 0;
					
				} else {
					if (FSelect[0] == SelectEnum.All) {
						ToISpread(Pins[pinName]).SliceCount = SpreadMax;
						FTimeStamp.SliceCount = SpreadMax;
						FAddress.SliceCount = SpreadMax;
					}
					else {
						ToISpread(Pins[pinName]).SliceCount = 1;
						FTimeStamp.SliceCount = 1;
						FAddress.SliceCount = 1;
					}
				}
			}
			if (empty) return;
			
			
			for (int i= (FSelect[0] == SelectEnum.Last)?SpreadMax-1:0;i<SpreadMax;i++) {
				Message message = FInput[i];
				FAddress[i] = message.Address;
				FTimeStamp[i] = message.TimeStamp.ToString();
				
				foreach (string name in Pins.Keys) {
					VVVV.PluginInterfaces.V2.NonGeneric.ISpread bin = GetISpreadData(Pins[name], i);
					try {
						//	FLogger.Log(LogType.Debug, message.ToString());
						
						BinList attrib = message[name];
						
						int count = attrib.Count;
						bin.SliceCount = count;
						for (int j = 0;j<count;j++)
						bin[j] = attrib[j];
						
					} catch (Exception err) {
						err.ToString(); // no warning
						bin.SliceCount = 0;
						if (FVerbose[0]) Logger.Log(LogType.Debug, "\"" + Types[name]+" " + name + "\" is not defined in Message.");
					}
				}
			}
			
			
			FAddress.Flush();
			FTimeStamp.Flush();
			foreach (string name in Pins.Keys) {
				ToISpread(Pins[name]).Flush();
			}
		}
		
		#region PluginInfo
		[PluginInfo(Name = "SetMessage", Category = "Message", Help = "Adds or edits a Message Property", Tags = "Dynamic, Bin, velcrome")]
		#endregion PluginInfo
		public class SetMessageNode : DynamicNode {
			[Input("Input")]
			IDiffSpread<Message> FInput;
			
			[Output("Output", AutoFlush=false)]
			Pin<Message> FOutput;
			
			protected override IIOContainer<ISpread<ISpread<T>>> CreatePin<T>(string name) {
				var attr = new InputAttribute(name);
				attr.BinVisibility = PinVisibility.Hidden;
				attr.BinSize = 1;
				attr.Order = Count;
				attr.BinOrder = Count+1;
				return IOFactory.CreateIOContainer<IDiffSpread<ISpread<T>>>(attr);
			}
			
			public override void Evaluate(int SpreadMax) {
				SpreadMax = FInput.SliceCount;
				bool isChanged = false;
				foreach (string name in Pins.Keys) {
					var pin = ToIDiffSpread(Pins[name]);
					if (pin.IsChanged) isChanged = true;
				}
				
				if (!isChanged && !FInput.IsChanged) {
					//				FLogger.Log(LogType.Debug, "skip set");
					return;
				}
				
				
				if (FInput.SliceCount==0 || FInput[0] == null) {
					FOutput.SliceCount = 0;
					return;
				}
				for (int i=0;i<SpreadMax;i++) {
					Message message = FInput[i];
					foreach (string name in Pins.Keys) {
						var pin = GetISpreadData(Pins[name],i);
						message.AssignFrom(name, pin);
					}
				}
				FOutput.AssignFrom(FInput);
				FOutput.Flush();
			}
		}
		
		#region PluginInfo
		[PluginInfo(Name = "AsOSC", Category = "Message", Help = "Outputs OSC Bundle Strings", Tags = "Dynamic, OSC, velcrome")]
		#endregion PluginInfo
		public class MessageMessageAsOscNode : IPluginEvaluate {
			[Input("Input")]
			IDiffSpread<Message> FInput;
			
			[Output("OSC", AutoFlush = false)]
			ISpread<Stream> FOutput;
			
			public void Evaluate(int SpreadMax) {
				if (FInput.SliceCount <= 0 || FInput[0] == null) SpreadMax = 0;
				else SpreadMax = FInput.SliceCount;
				
				if (!FInput.IsChanged) return;
				FOutput.SliceCount = SpreadMax;
				
				for (int i=0;i<SpreadMax;i++) {
					FOutput[i] = FInput[i].ToOSC();
				}
				FOutput.Flush();
			}
		}
		
		
		#region PluginInfo
		[PluginInfo(Name = "AsMessage", Category = "Message", Help = "Converts OSC Bundles into Messages ", Tags = "Dynamic, OSC, velcrome")]
		#endregion PluginInfo
		public class MessageOscAsMessageNode : IPluginEvaluate {
			[Input("OSC")]
			IDiffSpread<Stream> FInput;
			
			[Output("Output", AutoFlush = false)]
			ISpread<Message> FOutput;
			
			public void Evaluate(int SpreadMax) {
				
				if (!FInput.IsChanged) return;
				
				if (FInput.SliceCount <= 0 || FInput[0] == null) SpreadMax = 0;
				else SpreadMax = FInput.SliceCount;
				
				FOutput.SliceCount = SpreadMax;
				
				for (int i=0;i<SpreadMax;i++) {
					Message message = Message.FromOSC(FInput[i]);
					FOutput[i] = message;
				}
				FOutput.Flush();
			}
		}
		
		#region PluginInfo
		[PluginInfo(Name = "Info", Category = "Message", Help = "Help to Debug Messages", Tags = "Dynamic, velcrome")]
		#endregion PluginInfo
		public class MessageInfoNode : IPluginEvaluate {
			[Input("Input")]
			IDiffSpread<Message> FInput;
			
			[Input("Print Message", IsBang = true)]
			IDiffSpread<bool> FPrint;
			
			[Output("Address", AutoFlush = false)]
			ISpread<string> FAddress;
			
			[Output("Timestamp", AutoFlush = false)]
			ISpread<string> FTimeStamp;
			
			[Output("Output", AutoFlush = false)]
			ISpread<string> FOutput;
			
			[Output("Configuration", AutoFlush = false)]
			ISpread<string> FConfigOut;
			
			[Import()]
			protected ILogger FLogger;
			
			public void Evaluate(int SpreadMax) {
				if (FInput.SliceCount <= 0 || FInput[0] == null) SpreadMax = 0;
				else SpreadMax = FInput.SliceCount;
				
				if (!FInput.IsChanged) return;
				
				FOutput.SliceCount = SpreadMax;
				FTimeStamp.SliceCount = SpreadMax;
				FAddress.SliceCount = SpreadMax;
				FConfigOut.SliceCount = SpreadMax;
				
				Dictionary<Type, string> identities = new MessageResolver().Identity;
				
				for (int i=0;i<SpreadMax;i++) {
					Message m = FInput[i];
					FOutput[i]= m.ToString();
					FAddress[i] = m.Address;
					FTimeStamp[i] = m.TimeStamp.ToString();
					StringBuilder config = new StringBuilder();
					FConfigOut[i] = FInput[i].GetConfig(identities);
					
					if (FPrint[i]) {
						StringBuilder sb = new StringBuilder();
						sb.Append("== Message "+i+" ==") ;
						sb.AppendLine();
						
						sb.AppendLine(FInput[i].ToString()) ;
						sb.Append("====\n");
						FLogger.Log(LogType.Debug, sb.ToString());
					}
				}
				FAddress.Flush();
				FTimeStamp.Flush();
				FOutput.Flush();
				FConfigOut.Flush();
			}
		}
		
		
		#region PluginInfo
		[PluginInfo(Name = "Sift", Category = "Message", Help = "Filter Messages", Tags = "Dynamic, velcrome")]
		#endregion PluginInfo
		public class MessageSiftNode : IPluginEvaluate {
			[Input("Input")]
			IDiffSpread<Message> FInput;
			
			[Input("Filter", DefaultString = "*")]
			IDiffSpread<string> FFilter;
			
			[Output("Output", AutoFlush = false)]
			ISpread<Message> FOutput;
			
			[Output("NotFound", AutoFlush = false)]
			ISpread<Message> FNotFound;
			
			[Import()]
			protected ILogger FLogger;
			
			public void Evaluate(int SpreadMax) {
				if (!FInput.IsChanged) return;
				
				SpreadMax = FInput.SliceCount;
				
				FOutput.SliceCount = 0;
				bool[] found = new bool[SpreadMax];
				for (int i=0;i<SpreadMax;i++) found[i] = false;
				
				for (int i=0;i<FFilter.SliceCount;i++) {
					string[] filter = FFilter[i].Split('.');
					
					for (int j=0;j<SpreadMax;j++) {
						if (!found[j]) {
							string[] message = FInput[j].Address.Split('.');
							
							if (message.Length < filter.Length) continue;
							
							bool match = true;
							for (int k=0;k<filter.Length;k++) {
								if ((filter[k].Trim() != message[k].Trim()) && (filter[k].Trim() != "*")) match = false;
							}
							found[j] = match;
						}
					}
				}
				
				for (int i=0;i<SpreadMax;i++) {
					if (found[i]) FOutput.Add(FInput[i]);
					else FNotFound.Add(FInput[i]);
				}
				FOutput.Flush();
				FNotFound.Flush();
			}
		}
		[PluginInfo(Name = "Cons", Category = "Message", Help = "Concatenates all Messages", Tags = "velcrome")]
		public class MessageConsNode : Cons<Message>
		{}
		
		
		[PluginInfo(Name = "Buffer", Category = "Message", Help = "Buffers all Messages", Tags = "velcrome")]
		public class MessageBufferNode : BufferNode<Message>
		{}
		
		
		[PluginInfo(Name = "Queue", Category = "Message", Help = "Queues all Messages", Tags = "velcrome")]
		public class MessageQueueNode : QueueNode<Message>
		{}
		
		
		[PluginInfo(Name = "RingBuffer", Category = "Message", Help = "Ringbuffers all Messages", Tags = "velcrome")]
		public class MessageRingBufferNode : RingBufferNode<Message>
		{}
		
		[PluginInfo(Name = "Frame", Category = "Message", Help = "Pushes the Message into the Delayer", Tags = "FrameDelay", AutoEvaluate=true, Ignore = true)]
		public class MessageFrameNode: Frame<Message>
		{}
		
		[PluginInfo(Name = "Delayer", Category = "Message", Help = "Relays any pushed Messages", Tags = "FrameDelay", Ignore = true)]
		public class MessageDelayerNode : Delayer<Message>
		{}
		
		[PluginInfo(Name = "Serialize", Category = "Message", Help = "Makes binary from Messages", Tags = "Raw")]
		public class MessageSerializeNode: Serialize<Message>
		{
			
			public MessageSerializeNode() : base() {
				FResolver = new MessageResolver();
			}
		}
		
		[PluginInfo(Name = "DeSerialize", Category = "Message", Help = "Creates Messages from binary", Tags = "Raw")]
		public class MessageDeSerializeNode : DeSerialize<Message>
		{
			public MessageDeSerializeNode() : base() {
				FResolver = new MessageResolver();
			}
		}
		
		[PluginInfo(Name = "S+H", Category = "Message", Help = "Save a Message", Tags = "Dynamic, velcrome")]
		public class MessageSAndHNode : SAndH<Message>
		{}
		
		[PluginInfo(Name = "GetSlice", Category = "Message", Help = "GetSlice Messages", Tags = "Dynamic, velcrome")]
		public class MessageGetSliceNode : GetSlice<Message>
		{}
		
		
		[PluginInfo(Name = "Select", Category = "Message", Help = "Select Messages", Tags = "Dynamic, velcrome")]
		public class MessageSelectNode : Select<Message>
		{}
		
	}
	
}