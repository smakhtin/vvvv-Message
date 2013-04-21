using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Xml;
using FeralTic.DX11.Resources;
using VVVV.DX11;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

namespace VVVV.Utils.Message {
	public class MessageResolver : DataContractResolver
	{
		
		public Dictionary<Type, string> Identity = new Dictionary<Type, string>();
		
		public MessageResolver() {
			Identity.Add(typeof(bool), "bool".ToLower());
			Identity.Add(typeof(int), "int".ToLower());
			Identity.Add(typeof(double), "double".ToLower());
			Identity.Add(typeof(float), "float".ToLower());
			Identity.Add(typeof(string), "string".ToLower());
			
			Identity.Add(typeof(RGBAColor), "Color".ToLower());
			Identity.Add(typeof(Matrix4x4), "Transform".ToLower());
			Identity.Add(typeof(Vector2D), "Vector2D".ToLower());
			Identity.Add(typeof(Vector3D), "Vector3D".ToLower());
			Identity.Add(typeof(Vector4D), "Vector4D".ToLower());
			Identity.Add(typeof(DX11Resource<DX11Texture2D>), "Texture2D".ToLower());

			Identity.Add(typeof(Message), "Message".ToLower());
		}
		
		public override bool TryResolveType(Type dataContractType, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
		{
			if (Identity.ContainsKey(dataContractType))
			{
				var dictionary = new XmlDictionary();
				typeName = dictionary.Add(Identity[dataContractType]);
				typeNamespace = dictionary.Add(dataContractType.FullName);
				return true;
			}
			
			// Defer to the known type resolver
			return knownTypeResolver.TryResolveType(dataContractType, declaredType, null, out typeName, out typeNamespace);
		}
		
		public override Type ResolveName(string typeName, string typeNamespace, Type type, DataContractResolver knownTypeResolver)
		{
			Type foundType = null;
			foreach (var t in Identity.Keys) {
				if (typeName.ToLower() == Identity[t] && typeNamespace == t.FullName) {
					foundType = t;
				}
			}

			if (foundType != null) {
				return foundType;
			}

			// Defer to the known type resolver
			return knownTypeResolver.ResolveName(typeName, typeNamespace, type, null);
		}
	}
	}