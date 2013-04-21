using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using System.Linq;
using FeralTic.DX11.Resources;
using VVVV.DX11;
using VVVV.PluginInterfaces.V2;

using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

namespace VVVV.Utils.Message {

	public abstract class DynamicNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		public enum SelectEnum
		{
			All,
			First,
			Last
		}
		
		[Config ("Configuration", DefaultString="string Foo")]
		public IDiffSpread<string> FConfig;
		
		[Input ("Verbose", Visibility = PinVisibility.OnlyInspector, IsSingle = true, DefaultBoolean = true)]
		public ISpread<bool> FVerbose;
		
		[Import]
		protected ILogger Logger;
		
		[Import]
		protected IIOFactory IOFactory;
		
		protected Dictionary<string, IIOContainer> Pins = new Dictionary<string, IIOContainer>();
		protected Dictionary<string, string> Types = new Dictionary<string, string>();

		protected int Count = 2;
		
		public void OnImportsSatisfied() {
			FConfig.Changed += HandleConfigChange;
		}
		
		protected void HandleConfigChange(IDiffSpread<string> configSpread) {
			Count = 0;
			var invalidPins = Pins.Keys.ToList();
			
			var config = configSpread[0].Trim().Split(',');
			foreach (string pinConfig in config) {
				var pinData = pinConfig.Trim().Split(' ');
				
				try 
				{
					var type = pinData[0].ToLower();
					var name = pinData[1];
					
					var create = false;
					if (Pins.ContainsKey(name) && Pins[name] != null)
					{
						invalidPins.Remove(name);
						
						if (Types.ContainsKey(name))
						{
							if (Types[name] != type)
							{
								Pins[name].Dispose();
								Pins[name] = null;
								create = true;
							}
							
						} 
						else 
						{
							// key is in FPins, but no type defined. should never happen
							create = true;
						}
					} 
					else 
					{
						Pins.Add(name, null);
						create = true;
					}
					
					if (create) 
					{
						var ident = new MessageResolver().Identity;
						
						switch (type) 
						{
							case "double" : Pins[name] = CreatePin<double>(name); break;
							case "float" : Pins[name] = CreatePin<float>(name); break;
							case "int" : Pins[name] = CreatePin<int>(name); break;
							case "bool" : Pins[name] = CreatePin<bool>(name); break;
							case "vector2d" : Pins[name] = CreatePin<Vector2D>(name); break;
							case "vector3d" : Pins[name] = CreatePin<Vector3D>(name); break;
							case "vector4d" : Pins[name] = CreatePin<Vector3D>(name); break;
							case "string" : Pins[name] = CreatePin<string>(name); break;
							case "color" : Pins[name] = CreatePin<RGBAColor>(name); break;
							case "transform" : Pins[name] = CreatePin<Matrix4x4>(name); break;
							case "texture2d": Pins[name] = CreatePin<DX11Resource<DX11Texture2D>>(name); break;
							
							default:  Logger.Log(LogType.Debug, "Type "+type + " not supported!"); break;
						}

						Types.Add(name, type);
					}

					Count += 2;
				} 
				catch (Exception ex) 
				{
					Logger.Log(LogType.Debug, ex.ToString());
					
					Logger.Log(LogType.Debug, "Invalid Descriptor in Config Pin");
				}
			}

			foreach (var name in invalidPins) 
			{
				Pins[name].Dispose();
				Pins.Remove(name);
				Types.Remove(name);
			}
		}
		
		protected PluginInterfaces.V2.NonGeneric.ISpread GetISpreadData(IIOContainer pin, int index) {
			return (PluginInterfaces.V2.NonGeneric.ISpread) ((PluginInterfaces.V2.NonGeneric.ISpread)(pin.RawIOObject))[index];
		}
		
		protected ISpread<T> GetGenericISpreadData<T>(IIOContainer pin, int index) {
			return ((ISpread<ISpread<T>>)(pin.RawIOObject))[index];
		}
		
		protected PluginInterfaces.V2.NonGeneric.ISpread ToISpread(IIOContainer pin) {
			return (PluginInterfaces.V2.NonGeneric.ISpread)(pin.RawIOObject);
		}
		
		protected PluginInterfaces.V2.NonGeneric.IDiffSpread ToIDiffSpread(IIOContainer pin) {
			return (PluginInterfaces.V2.NonGeneric.IDiffSpread)(pin.RawIOObject);
		}
		protected ISpread<T> ToGenericISpread<T>(IIOContainer pin) {
			return (ISpread<T>)(pin.RawIOObject);
		}
		
		protected abstract IIOContainer<ISpread<ISpread<T>>> CreatePin<T>(string name);
		public abstract void Evaluate(int SpreadMax);	
	}

}