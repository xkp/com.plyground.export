using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

public partial class ModuleExporter
{
	private const string CapabilityManifestVersion = "1.0.0";
	private const string CapabilityProducerName = "unity-module-exporter";
	private const string CapabilitySourceEnvironment = "unity-editor";
	private static readonly string[] UnityCallbackNames =
	{
		"Awake", "Start", "OnEnable", "OnDisable", "Update", "LateUpdate", "FixedUpdate",
		"OnCollisionEnter", "OnCollisionExit", "OnTriggerEnter", "OnTriggerExit",
		"OnMouseDown", "OnMouseUp", "OnAnimatorMove", "OnAudioFilterRead"
	};

	[Serializable]
	public class CapabilityManifest
	{
		public string manifestVersion = CapabilityManifestVersion;
		public CapabilityModuleInfo module = new CapabilityModuleInfo();
		public List<CapabilityTypeInfo> types = new List<CapabilityTypeInfo>();
		public List<CapabilityEventInfo> events = new List<CapabilityEventInfo>();
		public List<CapabilityMethodInfo> methods = new List<CapabilityMethodInfo>();
		public List<CapabilityParameterInfo> parameters = new List<CapabilityParameterInfo>();
		public List<string> supportedFeatures = new List<string>();
		public CapabilityUnityInfo unity = new CapabilityUnityInfo();
		public List<string> constraints = new List<string>();
		public CapabilityExportInfo exportInfo = new CapabilityExportInfo();
	}

	[Serializable]
	public class ItemCapabilitySet
	{
		public List<string> supportedFeatures = new List<string>();
		public CapabilityUnityInfo unity = new CapabilityUnityInfo();
		public List<string> constraints = new List<string>();
	}

	[Serializable]
	public class CapabilityModuleInfo
	{
		public string id = "";
		public string displayName = "";
		public string version = "1.0.0";
		public string category = "";
		public string description = "";
		public List<string> assemblyNames = new List<string>();
		public List<string> namespaceRoots = new List<string>();
		public List<string> dependencies = new List<string>();
		public List<string> tags = new List<string>();
		public CapabilityAvailability availability = new CapabilityAvailability();
		public bool codegenEnabled = true;
	}

	[Serializable]
	public class CapabilityAvailability
	{
		public bool local = true;
		public bool cloud;
	}

	[Serializable]
	public class CapabilityUnityInfo
	{
		public List<string> components = new List<string>();
		public List<string> systems = new List<string>();
		public List<string> gameObjectRoles = new List<string>();
		public List<string> behaviorShapes = new List<string>();
	}

	[Serializable]
	public class CapabilityTypeInfo
	{
		public string name = "";
		public string fullName = "";
		public string assemblyName = "";
		public string kind = "";
		public string description = "";
	}

	[Serializable]
	public class CapabilityEventInfo
	{
		public string name = "";
		public string declaringType = "";
		public string eventType = "";
		public string description = "";
		public string source = "";
	}

	[Serializable]
	public class CapabilityMethodInfo
	{
		public string name = "";
		public string declaringType = "";
		public string returnType = "";
		public string signature = "";
		public string description = "";
		public string source = "";
	}

	[Serializable]
	public class CapabilityParameterInfo
	{
		public string name = "";
		public string type = "";
		public string source = "";
		public string defaultValue = "";
		public string description = "";
		public bool required;
	}

	[Serializable]
	public class CapabilityExportInfo
	{
		public string exportedAt = "";
		public string sourceHash = "";
		public string producerName = CapabilityProducerName;
		public string producerVersion = "1.0.0";
		public string sourceEnvironment = CapabilitySourceEnvironment;
	}

	private void PrepareCapabilitiesForPersistence()
	{
		moduleCapabilities ??= new CapabilityManifest();
		if (!HasMeaningfulModuleCapabilities(moduleCapabilities))
		{
			moduleCapabilities = InferModuleCapabilities();
		}

		PopulateCapabilityModuleMetadata(moduleCapabilities);
		moduleCapabilities.manifestVersion = string.IsNullOrWhiteSpace(moduleCapabilities.manifestVersion)
			? CapabilityManifestVersion
			: moduleCapabilities.manifestVersion;
		moduleCapabilities.exportInfo ??= new CapabilityExportInfo();
		moduleCapabilities.exportInfo.exportedAt = DateTime.UtcNow.ToString("o");
		moduleCapabilities.exportInfo.producerName = CapabilityProducerName;
		moduleCapabilities.exportInfo.producerVersion = GetExporterVersion();
		moduleCapabilities.exportInfo.sourceEnvironment = CapabilitySourceEnvironment;
		moduleCapabilities.exportInfo.sourceHash = ComputeCapabilitySourceHash();

		foreach (ItemGroup group in itemGroups)
		{
			if (group?.items == null)
			{
				continue;
			}

			foreach (Item item in group.items)
			{
				if (!HasMeaningfulItemCapabilities(item.capabilities) && item.prefab != null)
				{
					item.capabilities = InferItemCapabilities(item);
				}
				else
				{
					item.capabilities ??= new ItemCapabilitySet();
				}

				NormalizeItemCapabilities(item.capabilities);
			}
		}

		NormalizeModuleCapabilities(moduleCapabilities);
	}

	private void PopulateCapabilityModuleMetadata(CapabilityManifest manifest)
	{
		manifest ??= new CapabilityManifest();
		manifest.module ??= new CapabilityModuleInfo();
		manifest.module.id = moduleId;
		manifest.module.displayName = moduleName;
		manifest.module.category = moduleType;
		manifest.module.description = description;
		manifest.module.dependencies = DistinctStrings(dependencies);

		if (manifest.module.tags == null || manifest.module.tags.Count == 0)
		{
			List<string> tags = new List<string>();
			if (!string.IsNullOrWhiteSpace(moduleType))
			{
				tags.Add(moduleType);
			}

			foreach (string category in itemGroups
				.Where(group => !string.IsNullOrWhiteSpace(group?.category))
				.Select(group => group.category))
			{
				tags.Add(category);
			}

			manifest.module.tags = DistinctStrings(tags);
		}
	}

	private CapabilityManifest InferModuleCapabilities()
	{
		CapabilityManifest manifest = new CapabilityManifest();
		PopulateCapabilityModuleMetadata(manifest);

		HashSet<string> featureSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> namespaceRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> components = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> systems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> shapes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> constraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CapabilityTypeInfo> typeMap = new Dictionary<string, CapabilityTypeInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CapabilityMethodInfo> methodMap = new Dictionary<string, CapabilityMethodInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CapabilityEventInfo> eventMap = new Dictionary<string, CapabilityEventInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CapabilityParameterInfo> parameterMap = new Dictionary<string, CapabilityParameterInfo>(StringComparer.OrdinalIgnoreCase);

		Type controllerType = ResolveTypeByName(controllerClass);
		if (controllerType != null)
		{
			AddTypeMetadata(controllerType, typeMap, assemblyNames, namespaceRoots);
			CollectMethodCapabilities(controllerType, methodMap, featureSet, shapes);
			CollectEventCapabilities(controllerType, eventMap, featureSet);
			CollectParameterCapabilities(controllerType, null, parameterMap, featureSet);
			CollectConstraintCapabilities(controllerType, constraints);
		}

		foreach (Item item in itemGroups.SelectMany(group => group.items))
		{
			if (item == null)
			{
				continue;
			}

			ItemCapabilitySet itemCapabilities = item.capabilities;
			if (!HasMeaningfulItemCapabilities(itemCapabilities) && item.prefab != null)
			{
				itemCapabilities = InferItemCapabilities(item);
				item.capabilities = itemCapabilities;
			}

			if (itemCapabilities != null)
			{
				UnionInto(featureSet, itemCapabilities.supportedFeatures);
				UnionInto(components, itemCapabilities.unity?.components);
				UnionInto(systems, itemCapabilities.unity?.systems);
				UnionInto(roles, itemCapabilities.unity?.gameObjectRoles);
				UnionInto(shapes, itemCapabilities.unity?.behaviorShapes);
				UnionInto(constraints, itemCapabilities.constraints);
			}

			if (item.prefab == null)
			{
				continue;
			}

			foreach (Component component in item.prefab.GetComponentsInChildren<Component>(true))
			{
				Type componentType = component != null ? component.GetType() : null;
				if (componentType == null)
				{
					continue;
				}

				AddTypeMetadata(componentType, typeMap, assemblyNames, namespaceRoots);
				if (IsUserDefinedType(componentType))
				{
					CollectMethodCapabilities(componentType, methodMap, featureSet, shapes);
					CollectEventCapabilities(componentType, eventMap, featureSet);
					CollectParameterCapabilities(componentType, component, parameterMap, featureSet);
					CollectConstraintCapabilities(componentType, constraints);
				}
			}
		}

		manifest.types = typeMap.Values.OrderBy(info => info.fullName).ToList();
		manifest.methods = methodMap.Values.OrderBy(info => info.declaringType).ThenBy(info => info.name).ToList();
		manifest.events = eventMap.Values.OrderBy(info => info.declaringType).ThenBy(info => info.name).ToList();
		manifest.parameters = parameterMap.Values.OrderBy(info => info.source).ThenBy(info => info.name).ToList();
		manifest.supportedFeatures = featureSet.OrderBy(value => value).ToList();
		manifest.unity.components = components.OrderBy(value => value).ToList();
		manifest.unity.systems = systems.OrderBy(value => value).ToList();
		manifest.unity.gameObjectRoles = roles.OrderBy(value => value).ToList();
		manifest.unity.behaviorShapes = shapes.OrderBy(value => value).ToList();
		manifest.constraints = constraints.OrderBy(value => value).ToList();
		manifest.module.assemblyNames = assemblyNames.OrderBy(value => value).ToList();
		manifest.module.namespaceRoots = namespaceRoots.OrderBy(value => value).ToList();
		manifest.exportInfo.producerVersion = GetExporterVersion();
		manifest.exportInfo.sourceEnvironment = CapabilitySourceEnvironment;
		return manifest;
	}

	private ItemCapabilitySet InferItemCapabilities(Item item)
	{
		return item?.prefab == null ? new ItemCapabilitySet() : InferItemCapabilities(item.prefab);
	}

	private ItemCapabilitySet InferItemCapabilities(GameObject prefab)
	{
		ItemCapabilitySet result = new ItemCapabilitySet();
		if (prefab == null)
		{
			return result;
		}

		HashSet<string> featureSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> components = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> systems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> shapes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> constraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		Component[] prefabComponents = prefab.GetComponentsInChildren<Component>(true);
		foreach (Component component in prefabComponents)
		{
			Type componentType = component != null ? component.GetType() : null;
			if (componentType == null)
			{
				continue;
			}

			components.Add(componentType.Name);
			featureSet.Add("component:" + componentType.Name);
			MapBuiltInCapabilities(componentType, systems, roles, shapes, featureSet);

			if (!IsUserDefinedType(componentType))
			{
				continue;
			}

			foreach (MethodInfo method in componentType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (Array.IndexOf(UnityCallbackNames, method.Name) >= 0)
				{
					shapes.Add("callback:" + method.Name);
					featureSet.Add("callback:" + method.Name);
				}
			}

			foreach (FieldInfo field in GetSerializedFields(componentType))
			{
				featureSet.Add("serialized-field:" + componentType.Name + "." + field.Name);
			}

			CollectConstraintCapabilities(componentType, constraints);
		}

		if (prefab.GetComponentInChildren<Renderer>(true) != null)
		{
			roles.Add("renderable");
			featureSet.Add("renderable");
		}

		if (prefab.GetComponentInChildren<Transform>(true) != null)
		{
			roles.Add("hierarchical");
		}

		result.supportedFeatures = featureSet.OrderBy(value => value).ToList();
		result.unity.components = components.OrderBy(value => value).ToList();
		result.unity.systems = systems.OrderBy(value => value).ToList();
		result.unity.gameObjectRoles = roles.OrderBy(value => value).ToList();
		result.unity.behaviorShapes = shapes.OrderBy(value => value).ToList();
		result.constraints = constraints.OrderBy(value => value).ToList();
		return result;
	}

	private void CollectMethodCapabilities(Type type, Dictionary<string, CapabilityMethodInfo> methodMap, HashSet<string> featureSet, HashSet<string> shapes)
	{
		foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
		{
			if (method.IsSpecialName)
			{
				continue;
			}

			string key = type.FullName + "." + method.Name + "(" + string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName)) + ")";
			methodMap[key] = new CapabilityMethodInfo
			{
				name = method.Name,
				declaringType = type.FullName,
				returnType = method.ReturnType.Name,
				signature = BuildMethodSignature(method),
				description = "Reflected from " + type.Name,
				source = "reflection"
			};
			featureSet.Add("method:" + method.Name);

			if (Array.IndexOf(UnityCallbackNames, method.Name) >= 0)
			{
				shapes.Add("callback:" + method.Name);
			}
		}
	}

	private void CollectEventCapabilities(Type type, Dictionary<string, CapabilityEventInfo> eventMap, HashSet<string> featureSet)
	{
		foreach (EventInfo eventInfo in type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
		{
			string key = type.FullName + "." + eventInfo.Name;
			eventMap[key] = new CapabilityEventInfo
			{
				name = eventInfo.Name,
				declaringType = type.FullName,
				eventType = eventInfo.EventHandlerType != null ? eventInfo.EventHandlerType.Name : "",
				description = "Reflected from " + type.Name,
				source = "reflection"
			};
			featureSet.Add("event:" + eventInfo.Name);
		}
	}

	private void CollectParameterCapabilities(Type type, Component instance, Dictionary<string, CapabilityParameterInfo> parameterMap, HashSet<string> featureSet)
	{
		foreach (FieldInfo field in GetSerializedFields(type))
		{
			string key = type.FullName + "." + field.Name;
			string defaultValue = "";
			if (instance != null)
			{
				object value = field.GetValue(instance);
				defaultValue = value != null ? value.ToString() : "";
			}

			parameterMap[key] = new CapabilityParameterInfo
			{
				name = field.Name,
				type = TranslateType(field.FieldType),
				source = type.FullName,
				defaultValue = defaultValue,
				description = "Serialized field",
				required = false
			};
			featureSet.Add("parameter:" + field.Name);
		}
	}

	private void CollectConstraintCapabilities(Type type, HashSet<string> constraints)
	{
		foreach (RequireComponent requireComponent in type.GetCustomAttributes(typeof(RequireComponent), true))
		{
			if (requireComponent.m_Type0 != null)
			{
				constraints.Add("requires-component:" + requireComponent.m_Type0.Name);
			}

			if (requireComponent.m_Type1 != null)
			{
				constraints.Add("requires-component:" + requireComponent.m_Type1.Name);
			}

			if (requireComponent.m_Type2 != null)
			{
				constraints.Add("requires-component:" + requireComponent.m_Type2.Name);
			}
		}
	}

	private void AddTypeMetadata(Type type, Dictionary<string, CapabilityTypeInfo> typeMap, HashSet<string> assemblyNames, HashSet<string> namespaceRoots)
	{
		if (type == null || string.IsNullOrWhiteSpace(type.FullName))
		{
			return;
		}

		typeMap[type.FullName] = new CapabilityTypeInfo
		{
			name = type.Name,
			fullName = type.FullName,
			assemblyName = type.Assembly.GetName().Name,
			kind = GetTypeKind(type),
			description = IsUserDefinedType(type) ? "User script type" : "Unity or package type"
		};

		assemblyNames.Add(type.Assembly.GetName().Name);
		if (!string.IsNullOrWhiteSpace(type.Namespace))
		{
			namespaceRoots.Add(GetNamespaceRoot(type.Namespace));
		}
	}

	private void MapBuiltInCapabilities(Type componentType, HashSet<string> systems, HashSet<string> roles, HashSet<string> shapes, HashSet<string> features)
	{
		string fullName = componentType.FullName ?? componentType.Name;
		switch (fullName)
		{
			case "UnityEngine.Rigidbody":
			case "UnityEngine.Rigidbody2D":
				systems.Add("physics");
				shapes.Add("physics-driven");
				features.Add("physics");
				break;
			case "UnityEngine.Collider":
			case "UnityEngine.BoxCollider":
			case "UnityEngine.CapsuleCollider":
			case "UnityEngine.MeshCollider":
			case "UnityEngine.SphereCollider":
			case "UnityEngine.Collider2D":
			case "UnityEngine.BoxCollider2D":
				systems.Add("physics");
				roles.Add("collidable");
				features.Add("collision");
				break;
			case "UnityEngine.Animator":
				systems.Add("animation");
				shapes.Add("animation-driven");
				features.Add("animation");
				break;
			case "UnityEngine.AudioSource":
				systems.Add("audio");
				roles.Add("audio-emitter");
				features.Add("audio-playback");
				break;
			case "UnityEngine.UI.Button":
				systems.Add("ui");
				roles.Add("interactive-ui");
				shapes.Add("ui-interaction");
				features.Add("button");
				break;
			case "TMPro.TMP_Text":
			case "TMPro.TextMeshPro":
			case "TMPro.TextMeshProUGUI":
				systems.Add("ui");
				roles.Add("text-display");
				features.Add("text");
				break;
		}

		if (typeof(Renderer).IsAssignableFrom(componentType))
		{
			roles.Add("renderable");
		}

		if (typeof(Canvas).IsAssignableFrom(componentType))
		{
			systems.Add("ui");
			roles.Add("canvas-root");
		}
	}

	private static IEnumerable<FieldInfo> GetSerializedFields(Type type)
	{
		return type
			.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(field =>
				!field.IsStatic &&
				(field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Length > 0) &&
				IsCapabilitySupportedFieldType(field.FieldType));
	}

	private static bool IsCapabilitySupportedFieldType(Type type)
	{
		return type.IsPrimitive ||
			type == typeof(string) ||
			type.IsEnum ||
			type == typeof(Vector2) ||
			type == typeof(Vector3) ||
			type == typeof(Vector4) ||
			type == typeof(Color);
	}

	private static string BuildMethodSignature(MethodInfo method)
	{
		StringBuilder builder = new StringBuilder();
		builder.Append(method.Name);
		builder.Append("(");
		builder.Append(string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name + " " + parameter.Name)));
		builder.Append(")");
		return builder.ToString();
	}

	private static string GetTypeKind(Type type)
	{
		if (typeof(Component).IsAssignableFrom(type))
		{
			return "component";
		}

		if (type.IsEnum)
		{
			return "enum";
		}

		if (type.IsInterface)
		{
			return "interface";
		}

		return type.IsClass ? "class" : "value";
	}

	private static string GetNamespaceRoot(string ns)
	{
		if (string.IsNullOrWhiteSpace(ns))
		{
			return "";
		}

		string[] parts = ns.Split('.');
		return parts.Length > 1 ? parts[0] + "." + parts[1] : parts[0];
	}

	private static bool IsUserDefinedType(Type type)
	{
		if (type == null)
		{
			return false;
		}

		string ns = type.Namespace ?? "";
		return !ns.StartsWith("UnityEngine", StringComparison.Ordinal) &&
			!ns.StartsWith("UnityEditor", StringComparison.Ordinal) &&
			!ns.StartsWith("TMPro", StringComparison.Ordinal) &&
			!type.Assembly.GetName().Name.StartsWith("Unity", StringComparison.Ordinal);
	}

	private static void UnionInto(HashSet<string> target, IEnumerable<string> values)
	{
		if (target == null || values == null)
		{
			return;
		}

		foreach (string value in values)
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				target.Add(value.Trim());
			}
		}
	}

	private static List<string> DistinctStrings(IEnumerable<string> values)
	{
		return values == null
			? new List<string>()
			: values.Where(value => !string.IsNullOrWhiteSpace(value))
				.Select(value => value.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(value => value)
				.ToList();
	}

	private static void NormalizeItemCapabilities(ItemCapabilitySet capabilities)
	{
		if (capabilities == null)
		{
			return;
		}

		capabilities.unity ??= new CapabilityUnityInfo();
		capabilities.supportedFeatures = DistinctStrings(capabilities.supportedFeatures);
		capabilities.constraints = DistinctStrings(capabilities.constraints);
		capabilities.unity.components = DistinctStrings(capabilities.unity.components);
		capabilities.unity.systems = DistinctStrings(capabilities.unity.systems);
		capabilities.unity.gameObjectRoles = DistinctStrings(capabilities.unity.gameObjectRoles);
		capabilities.unity.behaviorShapes = DistinctStrings(capabilities.unity.behaviorShapes);
	}

	private static void NormalizeModuleCapabilities(CapabilityManifest capabilities)
	{
		if (capabilities == null)
		{
			return;
		}

		capabilities.module ??= new CapabilityModuleInfo();
		capabilities.unity ??= new CapabilityUnityInfo();
		capabilities.exportInfo ??= new CapabilityExportInfo();
		capabilities.supportedFeatures = DistinctStrings(capabilities.supportedFeatures);
		capabilities.constraints = DistinctStrings(capabilities.constraints);
		capabilities.unity.components = DistinctStrings(capabilities.unity.components);
		capabilities.unity.systems = DistinctStrings(capabilities.unity.systems);
		capabilities.unity.gameObjectRoles = DistinctStrings(capabilities.unity.gameObjectRoles);
		capabilities.unity.behaviorShapes = DistinctStrings(capabilities.unity.behaviorShapes);
		capabilities.module.assemblyNames = DistinctStrings(capabilities.module.assemblyNames);
		capabilities.module.namespaceRoots = DistinctStrings(capabilities.module.namespaceRoots);
		capabilities.module.dependencies = DistinctStrings(capabilities.module.dependencies);
		capabilities.module.tags = DistinctStrings(capabilities.module.tags);
	}

	private static CapabilityManifest CloneModuleCapabilities(CapabilityManifest source)
	{
		if (source == null)
		{
			return new CapabilityManifest();
		}

		CapabilityManifest clone = JsonUtility.FromJson<CapabilityManifest>(JsonUtility.ToJson(source));
		return clone ?? new CapabilityManifest();
	}

	private static ItemCapabilitySet CloneItemCapabilities(ItemCapabilitySet source)
	{
		if (source == null)
		{
			return new ItemCapabilitySet();
		}

		ItemCapabilitySet clone = JsonUtility.FromJson<ItemCapabilitySet>(JsonUtility.ToJson(source));
		return clone ?? new ItemCapabilitySet();
	}

	private static bool HasMeaningfulItemCapabilities(ItemCapabilitySet capabilities)
	{
		return capabilities != null &&
			((capabilities.supportedFeatures != null && capabilities.supportedFeatures.Count > 0) ||
			(capabilities.constraints != null && capabilities.constraints.Count > 0) ||
			(capabilities.unity != null &&
				((capabilities.unity.components != null && capabilities.unity.components.Count > 0) ||
				(capabilities.unity.systems != null && capabilities.unity.systems.Count > 0) ||
				(capabilities.unity.gameObjectRoles != null && capabilities.unity.gameObjectRoles.Count > 0) ||
				(capabilities.unity.behaviorShapes != null && capabilities.unity.behaviorShapes.Count > 0))));
	}

	private static bool HasMeaningfulModuleCapabilities(CapabilityManifest capabilities)
	{
		return capabilities != null &&
			(HasMeaningfulItemCapabilities(new ItemCapabilitySet
			{
				supportedFeatures = capabilities.supportedFeatures,
				unity = capabilities.unity,
				constraints = capabilities.constraints
			}) ||
			(capabilities.types != null && capabilities.types.Count > 0) ||
			(capabilities.events != null && capabilities.events.Count > 0) ||
			(capabilities.methods != null && capabilities.methods.Count > 0) ||
			(capabilities.parameters != null && capabilities.parameters.Count > 0));
	}

	private string ComputeCapabilitySourceHash()
	{
		CapabilityManifest snapshot = CloneModuleCapabilities(moduleCapabilities);
		snapshot.exportInfo = new CapabilityExportInfo
		{
			producerName = CapabilityProducerName,
			producerVersion = GetExporterVersion(),
			sourceEnvironment = CapabilitySourceEnvironment
		};
		return ComputeFNV1aHash(JsonUtility.ToJson(snapshot)).ToString("X8");
	}

	private string GetExporterVersion()
	{
		try
		{
			PackageInfo info = PackageInfo.FindForAssembly(GetType().Assembly);
			if (info != null && !string.IsNullOrWhiteSpace(info.version))
			{
				return info.version;
			}
		}
		catch
		{
		}

		try
		{
			string packageJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "package.json");
			if (File.Exists(packageJsonPath))
			{
				string json = File.ReadAllText(packageJsonPath);
				const string marker = "\"version\":";
				int markerIndex = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
				if (markerIndex >= 0)
				{
					int firstQuote = json.IndexOf('"', markerIndex + marker.Length);
					int secondQuote = firstQuote >= 0 ? json.IndexOf('"', firstQuote + 1) : -1;
					if (firstQuote >= 0 && secondQuote > firstQuote)
					{
						return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
					}
				}
			}
		}
		catch
		{
		}

		return "1.0.0";
	}

	private Type ResolveTypeByName(string typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
		{
			return null;
		}

		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			Type resolved = assembly.GetType(typeName, false);
			if (resolved != null)
			{
				return resolved;
			}

			try
			{
				resolved = assembly.GetTypes().FirstOrDefault(type =>
					string.Equals(type.Name, typeName, StringComparison.Ordinal) ||
					string.Equals(type.FullName, typeName, StringComparison.Ordinal));
				if (resolved != null)
				{
					return resolved;
				}
			}
			catch (ReflectionTypeLoadException)
			{
			}
		}

		return null;
	}
}
