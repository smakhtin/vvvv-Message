#region usings
using System;
using System.ComponentModel.Composition;
using System.IO;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Utils.Collections;
using VVVV.Core.Logging;
using VVVV.Nodes;

using System.Collections.Generic;
using VVVV.PluginInterfaces.V2.Graph;


using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
#endregion usings

namespace VVVV.Nodes
{
	
	public class SAndH<T> : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", AutoValidate = false)]
		Pin<T> FInput;
		
		[Input("Set", IsBang = true, IsSingle = true)]
		ISpread<bool> FSet;
		
		[Input("Clone", IsToggle = true, IsSingle = true, Visibility = PinVisibility.OnlyInspector, DefaultBoolean = true)]
		ISpread<bool> FClone;
		
		[Output("Output", AutoFlush = false)]
		ISpread<T> FOutput;
		
		[Import]
		ILogger FLogger;
		
		protected DataContractResolver FResolver = null;
		
		
		#endregion fields & pins
		
		public void Evaluate(int SpreadMax)
		{
			if (FSet[0] == false) return;
			
			FInput.Sync();
			SpreadMax = FInput.SliceCount;
			FOutput.SliceCount = SpreadMax;
			
			bool clone = FClone[0] && (FInput[0]!=null) && (FInput[0] is ICloneable);
			for (int i=0;i<FInput.SliceCount;i++) {
								
				T output;
				if (clone) output = (T)((ICloneable)FInput[i]).Clone();
					else output = FInput[i];
				FOutput[i] = output;
			}
			
			FOutput.Flush();
		}
		
	}
	
	
}