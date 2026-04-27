using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

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
		public List<CapabilityFeatureInfo> supportedFeatures = new List<CapabilityFeatureInfo>();
		public CapabilityUnityInfo unity = new CapabilityUnityInfo();
		public List<CapabilityConstraintInfo> constraints = new List<CapabilityConstraintInfo>();
		public CapabilityExportInfo exportInfo = new CapabilityExportInfo();
	}

	[Serializable]
	public class ItemCapabilitySet
	{
		public List<CapabilityFeatureInfo> supportedFeatures = new List<CapabilityFeatureInfo>();
		public CapabilityUnityInfo unity = new CapabilityUnityInfo();
		public List<CapabilityConstraintInfo> constraints = new List<CapabilityConstraintInfo>();
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
		public List<UnityCapabilityComponentInfo> components = new List<UnityCapabilityComponentInfo>();
		public List<UnityCapabilitySystemInfo> systems = new List<UnityCapabilitySystemInfo>();
		public List<UnityCapabilityGameObjectRoleInfo> gameObjectRoles = new List<UnityCapabilityGameObjectRoleInfo>();
		public List<UnityCapabilityBehaviorShapeInfo> behaviorShapes = new List<UnityCapabilityBehaviorShapeInfo>();
	}

	[Serializable]
	public class CapabilityTypeInfo
	{
		public string name = "";
		public string fullName = "";
		public string kind = "";
		public string @namespace = "";
		public string description = "";
		public bool exposed = true;
		public List<CapabilityTypeFieldInfo> fields = new List<CapabilityTypeFieldInfo>();
		public List<string> enumValues = new List<string>();
	}

	[Serializable]
	public class CapabilityTypeFieldInfo
	{
		public string name = "";
		public string type = "";
		public string description = "";
		public bool required;
	}

	[Serializable]
	public class CapabilityEventInfo
	{
		public string name = "";
		public string direction = "publishes";
		public string payloadType = "";
		public string declaringType = "";
		public string description = "";
		public bool allowedForCodegen = true;
		public string scope = "";
		public string authority = "";
		public List<string> tags = new List<string>();
	}

	[Serializable]
	public class CapabilityMethodInfo
	{
		public string name = "";
		public string declaringType = "";
		public string description = "";
		public List<CapabilityMethodParameterInfo> parameters = new List<CapabilityMethodParameterInfo>();
		public string returnType = "";
		public bool isStatic;
		public bool allowedForCodegen = true;
		public List<string> constraints = new List<string>();
		public List<string> tags = new List<string>();
	}

	[Serializable]
	public class CapabilityMethodParameterInfo
	{
		public string name = "";
		public string type = "";
		public string description = "";
		public bool required;
	}

	[Serializable]
	public class CapabilityParameterInfo
	{
		public string name = "";
		public string type = "";
		public bool required;
		public string @default = "";
		public float min;
		public float max;
		public List<string> enumValues = new List<string>();
		public string description = "";
		public bool moduleScoped;
		public string featureId = "";
		public List<string> tags = new List<string>();
	}

	[Serializable]
	public class CapabilityFeatureInfo
	{
		public string featureId = "";
		public string description = "";
		public bool codegenAllowed = true;
		public List<string> requiredDependencies = new List<string>();
		public List<string> incompatibleFeatures = new List<string>();
		public List<string> recommendedTemplates = new List<string>();
	}

	[Serializable]
	public class CapabilityConstraintInfo
	{
		public string code = "";
		public string description = "";
		public string severity = "warning";
		public string appliesToType = "";
		public string appliesToId = "";
	}

	[Serializable]
	public class UnityCapabilityComponentInfo
	{
		public string componentId = "";
		public string typeName = "";
		public string baseType = "MonoBehaviour";
		public string attachTarget = "self";
		public string description = "";
		public List<string> requiredComponents = new List<string>();
		public List<string> optionalComponents = new List<string>();
		public List<string> allowedFeatures = new List<string>();
		public List<CapabilityEventInfo> events = new List<CapabilityEventInfo>();
		public List<CapabilityMethodInfo> methods = new List<CapabilityMethodInfo>();
		public List<CapabilityParameterInfo> parameters = new List<CapabilityParameterInfo>();
		public List<string> tags = new List<string>();
		public bool codegenAllowed = true;
	}

	[Serializable]
	public class UnityCapabilitySystemInfo
	{
		public string systemId = "";
		public string displayName = "";
		public string description = "";
		public string role = "";
		public string primaryComponentId = "";
		public List<string> requiredModules = new List<string>();
		public List<string> eventIds = new List<string>();
		public List<string> methodIds = new List<string>();
		public List<string> featureIds = new List<string>();
		public List<string> tags = new List<string>();
	}

	[Serializable]
	public class UnityCapabilityGameObjectRoleInfo
	{
		public string roleId = "";
		public string displayName = "";
		public string description = "";
		public List<string> componentIds = new List<string>();
		public List<string> allowedFeatures = new List<string>();
		public List<string> requiredFeatures = new List<string>();
		public List<string> tags = new List<string>();
	}

	[Serializable]
	public class UnityCapabilityBehaviorShapeInfo
	{
		public string shapeId = "";
		public string displayName = "";
		public string description = "";
		public List<string> componentIds = new List<string>();
		public List<string> systemIds = new List<string>();
		public List<string> featureIds = new List<string>();
		public List<string> roleIds = new List<string>();
		public List<string> tags = new List<string>();
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

		if (string.IsNullOrWhiteSpace(manifest.module.version))
		{
			manifest.module.version = GetExporterVersion();
		}

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

		Dictionary<string, CapabilityTypeInfo> typeMap = new Dictionary<string, CapabilityTypeInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CapabilityMethodInfo> methodMap = new Dictionary<string, CapabilityMethodInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CapabilityEventInfo> eventMap = new Dictionary<string, CapabilityEventInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CapabilityParameterInfo> parameterMap = new Dictionary<string, CapabilityParameterInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CapabilityFeatureInfo> featureMap = new Dictionary<string, CapabilityFeatureInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CapabilityConstraintInfo> constraintMap = new Dictionary<string, CapabilityConstraintInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, UnityCapabilityComponentInfo> componentMap = new Dictionary<string, UnityCapabilityComponentInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, UnityCapabilitySystemInfo> systemMap = new Dictionary<string, UnityCapabilitySystemInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, UnityCapabilityGameObjectRoleInfo> roleMap = new Dictionary<string, UnityCapabilityGameObjectRoleInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, UnityCapabilityBehaviorShapeInfo> shapeMap = new Dictionary<string, UnityCapabilityBehaviorShapeInfo>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> namespaceRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		Type controllerType = ResolveTypeByName(controllerClass);
		if (controllerType != null)
		{
			AddTypeMetadata(controllerType, typeMap, assemblyNames, namespaceRoots);
			foreach (CapabilityMethodInfo method in BuildMethodInfos(controllerType))
			{
				AddMethod(methodMap, method);
			}

			foreach (CapabilityEventInfo eventInfo in BuildEventInfos(controllerType))
			{
				AddEvent(eventMap, eventInfo);
			}

			foreach (CapabilityParameterInfo parameter in BuildParameterInfos(controllerType, null))
			{
				AddParameter(parameterMap, parameter);
			}

			foreach (CapabilityConstraintInfo constraint in BuildConstraintInfos(controllerType))
			{
				AddConstraint(constraintMap, constraint);
			}
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

			if (itemCapabilities == null)
			{
				continue;
			}

			foreach (CapabilityFeatureInfo feature in itemCapabilities.supportedFeatures)
			{
				AddFeature(featureMap, feature);
			}

			foreach (CapabilityConstraintInfo constraint in itemCapabilities.constraints)
			{
				AddConstraint(constraintMap, constraint);
			}

			foreach (UnityCapabilityComponentInfo componentInfo in itemCapabilities.unity.components)
			{
				AddComponent(componentMap, componentInfo);

				Type componentType = ResolveTypeByName(componentInfo.typeName);
				if (componentType != null)
				{
					AddTypeMetadata(componentType, typeMap, assemblyNames, namespaceRoots);
				}

				foreach (CapabilityMethodInfo method in componentInfo.methods)
				{
					AddMethod(methodMap, method);
				}

				foreach (CapabilityEventInfo eventInfo in componentInfo.events)
				{
					AddEvent(eventMap, eventInfo);
				}

				foreach (CapabilityParameterInfo parameter in componentInfo.parameters)
				{
					AddParameter(parameterMap, parameter);
				}
			}

			foreach (UnityCapabilitySystemInfo systemInfo in itemCapabilities.unity.systems)
			{
				AddSystem(systemMap, systemInfo);
			}

			foreach (UnityCapabilityGameObjectRoleInfo roleInfo in itemCapabilities.unity.gameObjectRoles)
			{
				AddRole(roleMap, roleInfo);
			}

			foreach (UnityCapabilityBehaviorShapeInfo shapeInfo in itemCapabilities.unity.behaviorShapes)
			{
				AddBehaviorShape(shapeMap, shapeInfo);
			}
		}

		manifest.types = typeMap.Values.OrderBy(info => info.fullName).ToList();
		manifest.methods = methodMap.Values.OrderBy(info => info.declaringType).ThenBy(info => info.name).ToList();
		manifest.events = eventMap.Values.OrderBy(info => info.declaringType).ThenBy(info => info.name).ToList();
		manifest.parameters = parameterMap.Values.OrderBy(info => info.featureId).ThenBy(info => info.name).ToList();
		manifest.supportedFeatures = featureMap.Values.OrderBy(info => info.featureId).ToList();
		manifest.constraints = constraintMap.Values.OrderBy(info => info.code).ThenBy(info => info.description).ToList();
		manifest.unity.components = componentMap.Values.OrderBy(info => info.componentId).ToList();
		manifest.unity.systems = systemMap.Values.OrderBy(info => info.systemId).ToList();
		manifest.unity.gameObjectRoles = roleMap.Values.OrderBy(info => info.roleId).ToList();
		manifest.unity.behaviorShapes = shapeMap.Values.OrderBy(info => info.shapeId).ToList();
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

		Dictionary<string, CapabilityFeatureInfo> featureMap = new Dictionary<string, CapabilityFeatureInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CapabilityConstraintInfo> constraintMap = new Dictionary<string, CapabilityConstraintInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, UnityCapabilityComponentInfo> componentMap = new Dictionary<string, UnityCapabilityComponentInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, UnityCapabilitySystemInfo> systemMap = new Dictionary<string, UnityCapabilitySystemInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, UnityCapabilityGameObjectRoleInfo> roleMap = new Dictionary<string, UnityCapabilityGameObjectRoleInfo>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, UnityCapabilityBehaviorShapeInfo> shapeMap = new Dictionary<string, UnityCapabilityBehaviorShapeInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
		{
			Type componentType = component != null ? component.GetType() : null;
			if (componentType == null || !IsAssetBackedComponent(componentType, component))
			{
				continue;
			}

			UnityCapabilityComponentInfo componentInfo = BuildUnityComponentInfo(componentType, component);
			AddComponent(componentMap, componentInfo);

			foreach (string featureId in componentInfo.allowedFeatures)
			{
				AddFeature(featureMap, new CapabilityFeatureInfo
				{
					featureId = featureId,
					description = "Inferred from " + componentType.Name
				});
			}

			foreach (CapabilityConstraintInfo constraint in BuildConstraintInfos(componentType))
			{
				AddConstraint(constraintMap, constraint);
			}
		}

		InferPrefabRolesAndShapes(prefab, componentMap.Values.ToList(), featureMap, systemMap, roleMap, shapeMap);

		result.supportedFeatures = featureMap.Values.OrderBy(info => info.featureId).ToList();
		result.constraints = constraintMap.Values.OrderBy(info => info.code).ThenBy(info => info.description).ToList();
		result.unity.components = componentMap.Values.OrderBy(info => info.componentId).ToList();
		result.unity.systems = systemMap.Values.OrderBy(info => info.systemId).ToList();
		result.unity.gameObjectRoles = roleMap.Values.OrderBy(info => info.roleId).ToList();
		result.unity.behaviorShapes = shapeMap.Values.OrderBy(info => info.shapeId).ToList();
		return result;
	}

	private UnityCapabilityComponentInfo BuildUnityComponentInfo(Type componentType, Component instance)
	{
		UnityCapabilityComponentInfo componentInfo = new UnityCapabilityComponentInfo
		{
			componentId = componentType.FullName ?? componentType.Name,
			typeName = componentType.FullName ?? componentType.Name,
			baseType = GetBaseTypeLabel(componentType),
			attachTarget = "self",
			description = "Component inferred from Assets/ script",
			requiredComponents = BuildRequiredComponentNames(componentType),
			methods = BuildMethodInfos(componentType),
			events = BuildEventInfos(componentType),
			parameters = BuildParameterInfos(componentType, instance),
			tags = DistinctStrings(new[] { "component-first", "unity-exporter" }),
			codegenAllowed = true
		};

		HashSet<string> allowedFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"component:" + componentType.Name
		};

		foreach (MethodInfo method in componentType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
		{
			if (Array.IndexOf(UnityCallbackNames, method.Name) >= 0)
			{
				allowedFeatures.Add("callback:" + method.Name);
			}
		}

		foreach (FieldInfo field in GetSerializedFields(componentType))
		{
			allowedFeatures.Add("serialized-field:" + componentType.Name + "." + field.Name);
		}

		componentInfo.allowedFeatures = allowedFeatures.OrderBy(value => value).ToList();
		return componentInfo;
	}

	private void InferPrefabRolesAndShapes(
		GameObject prefab,
		List<UnityCapabilityComponentInfo> components,
		Dictionary<string, CapabilityFeatureInfo> featureMap,
		Dictionary<string, UnityCapabilitySystemInfo> systemMap,
		Dictionary<string, UnityCapabilityGameObjectRoleInfo> roleMap,
		Dictionary<string, UnityCapabilityBehaviorShapeInfo> shapeMap)
	{
		if (prefab == null)
		{
			return;
		}

		List<string> componentIds = components.Select(component => component.componentId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		List<string> featureIds = featureMap.Keys.OrderBy(value => value).ToList();

		if (prefab.GetComponentInChildren<Renderer>(true) != null)
		{
			AddRole(roleMap, new UnityCapabilityGameObjectRoleInfo
			{
				roleId = "renderable",
				displayName = "Renderable",
				description = "Prefab renders visible content",
				componentIds = new List<string>(componentIds),
				allowedFeatures = new List<string>(featureIds)
			});
		}

		if (prefab.GetComponentInChildren<Rigidbody>(true) != null || prefab.GetComponentInChildren<Rigidbody2D>(true) != null)
		{
			AddSystem(systemMap, new UnityCapabilitySystemInfo
			{
				systemId = "physics-body",
				displayName = "Physics Body",
				description = "Participates in Unity physics",
				role = "feature",
				featureIds = new List<string>(featureIds),
				primaryComponentId = componentIds.FirstOrDefault()
			});

			AddBehaviorShape(shapeMap, new UnityCapabilityBehaviorShapeInfo
			{
				shapeId = "physics-body",
				displayName = "Physics Body",
				description = "Rigid body driven gameplay object",
				componentIds = new List<string>(componentIds),
				systemIds = new List<string> { "physics-body" },
				featureIds = new List<string>(featureIds)
			});
		}

		if (prefab.GetComponentInChildren<Collider>(true) != null || prefab.GetComponentInChildren<Collider2D>(true) != null)
		{
			AddBehaviorShape(shapeMap, new UnityCapabilityBehaviorShapeInfo
			{
				shapeId = "trigger-collidable",
				displayName = "Trigger / Collidable",
				description = "Uses colliders for interaction or detection",
				componentIds = new List<string>(componentIds),
				featureIds = new List<string>(featureIds)
			});
		}

		if (prefab.GetComponentInChildren<Animator>(true) != null)
		{
			AddBehaviorShape(shapeMap, new UnityCapabilityBehaviorShapeInfo
			{
				shapeId = "animated-interactable",
				displayName = "Animated Interactable",
				description = "Uses Animator-driven presentation or interaction",
				componentIds = new List<string>(componentIds),
				featureIds = new List<string>(featureIds)
			});
		}
	}

	private List<CapabilityMethodInfo> BuildMethodInfos(Type type)
	{
		List<CapabilityMethodInfo> methods = new List<CapabilityMethodInfo>();
		foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
		{
			if (method.IsSpecialName)
			{
				continue;
			}

			methods.Add(new CapabilityMethodInfo
			{
				name = method.Name,
				declaringType = type.FullName ?? type.Name,
				description = "Reflected from " + type.Name,
				parameters = method.GetParameters()
					.Select(parameter => new CapabilityMethodParameterInfo
					{
						name = parameter.Name,
						type = GetFriendlyTypeName(parameter.ParameterType),
						description = "",
						required = !parameter.IsOptional
					})
					.ToList(),
				returnType = GetFriendlyTypeName(method.ReturnType),
				isStatic = method.IsStatic,
				allowedForCodegen = true,
				constraints = new List<string>(),
				tags = BuildMethodTags(method)
			});
		}

		return methods;
	}

	private List<CapabilityEventInfo> BuildEventInfos(Type type)
	{
		List<CapabilityEventInfo> events = new List<CapabilityEventInfo>();
		foreach (EventInfo eventInfo in type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
		{
			events.Add(new CapabilityEventInfo
			{
				name = eventInfo.Name,
				direction = "publishes",
				payloadType = eventInfo.EventHandlerType != null ? GetFriendlyTypeName(eventInfo.EventHandlerType) : "",
				declaringType = type.FullName ?? type.Name,
				description = "Reflected from " + type.Name,
				allowedForCodegen = true,
				scope = "",
				authority = "",
				tags = new List<string> { "reflection" }
			});
		}

		foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
		{
			if (Array.IndexOf(UnityCallbackNames, method.Name) < 0)
			{
				continue;
			}

			events.Add(new CapabilityEventInfo
			{
				name = method.Name,
				direction = "publishes",
				payloadType = "",
				declaringType = type.FullName ?? type.Name,
				description = "Unity callback",
				allowedForCodegen = true,
				scope = "unity-callback",
				authority = "",
				tags = new List<string> { "callback" }
			});
		}

		return events;
	}

	private List<CapabilityParameterInfo> BuildParameterInfos(Type type, Component instance)
	{
		List<CapabilityParameterInfo> parameters = new List<CapabilityParameterInfo>();
		foreach (FieldInfo field in GetSerializedFields(type))
		{
			parameters.Add(new CapabilityParameterInfo
			{
				name = field.Name,
				type = GetFriendlyTypeName(field.FieldType),
				required = false,
				@default = instance != null && field.GetValue(instance) != null ? field.GetValue(instance).ToString() : "",
				description = "Serialized field",
				moduleScoped = false,
				featureId = "serialized-field:" + type.Name + "." + field.Name,
				enumValues = field.FieldType.IsEnum ? Enum.GetNames(field.FieldType).ToList() : new List<string>(),
				tags = new List<string> { "serialized-field" }
			});
		}

		return parameters;
	}

	private List<CapabilityConstraintInfo> BuildConstraintInfos(Type type)
	{
		List<CapabilityConstraintInfo> constraints = new List<CapabilityConstraintInfo>();
		foreach (Type requiredType in GetRequireComponentTypes(type))
		{
			constraints.Add(new CapabilityConstraintInfo
			{
				code = "requires-component",
				description = type.Name + " requires " + requiredType.Name,
				severity = "warning",
				appliesToType = "type",
				appliesToId = type.FullName ?? type.Name
			});
		}

		return constraints;
	}

	private void AddTypeMetadata(Type type, Dictionary<string, CapabilityTypeInfo> typeMap, HashSet<string> assemblyNames, HashSet<string> namespaceRoots)
	{
		if (type == null || string.IsNullOrWhiteSpace(type.FullName) || !IsAssetsType(type))
		{
			return;
		}

		typeMap[type.FullName] = BuildTypeInfo(type);
		assemblyNames.Add(type.Assembly.GetName().Name);
		if (!string.IsNullOrWhiteSpace(type.Namespace))
		{
			namespaceRoots.Add(GetNamespaceRoot(type.Namespace));
		}
	}

	private CapabilityTypeInfo BuildTypeInfo(Type type)
	{
		CapabilityTypeInfo info = new CapabilityTypeInfo
		{
			name = type.Name,
			fullName = type.FullName ?? type.Name,
			kind = GetTypeKind(type),
			@namespace = type.Namespace ?? "",
			description = "User script type",
			exposed = true,
			fields = GetSerializedFields(type)
				.Select(field => new CapabilityTypeFieldInfo
				{
					name = field.Name,
					type = GetFriendlyTypeName(field.FieldType),
					description = "Serialized field",
					required = false
				})
				.ToList()
		};

		if (type.IsEnum)
		{
			info.enumValues = Enum.GetNames(type).ToList();
		}

		return info;
	}

	private void AddFeature(Dictionary<string, CapabilityFeatureInfo> map, CapabilityFeatureInfo feature)
	{
		if (feature == null || string.IsNullOrWhiteSpace(feature.featureId))
		{
			return;
		}

		string key = feature.featureId.Trim();
		if (!map.TryGetValue(key, out CapabilityFeatureInfo existing))
		{
			map[key] = CloneFeature(feature);
			return;
		}

		if (string.IsNullOrWhiteSpace(existing.description) && !string.IsNullOrWhiteSpace(feature.description))
		{
			existing.description = feature.description;
		}

		existing.codegenAllowed |= feature.codegenAllowed;
		existing.requiredDependencies = DistinctStrings(existing.requiredDependencies.Concat(feature.requiredDependencies ?? new List<string>()));
		existing.incompatibleFeatures = DistinctStrings(existing.incompatibleFeatures.Concat(feature.incompatibleFeatures ?? new List<string>()));
		existing.recommendedTemplates = DistinctStrings(existing.recommendedTemplates.Concat(feature.recommendedTemplates ?? new List<string>()));
	}

	private void AddConstraint(Dictionary<string, CapabilityConstraintInfo> map, CapabilityConstraintInfo constraint)
	{
		if (constraint == null || string.IsNullOrWhiteSpace(constraint.description))
		{
			return;
		}

		string key = (constraint.code ?? "") + "|" + constraint.description + "|" + (constraint.appliesToId ?? "");
		map[key] = CloneConstraint(constraint);
	}

	private void AddMethod(Dictionary<string, CapabilityMethodInfo> map, CapabilityMethodInfo method)
	{
		if (method == null || string.IsNullOrWhiteSpace(method.declaringType) || string.IsNullOrWhiteSpace(method.name))
		{
			return;
		}

		string key = method.declaringType + "." + method.name + "(" + string.Join(",", method.parameters.Select(parameter => parameter.type + ":" + parameter.name)) + ")";
		map[key] = CloneMethod(method);
	}

	private void AddEvent(Dictionary<string, CapabilityEventInfo> map, CapabilityEventInfo eventInfo)
	{
		if (eventInfo == null || string.IsNullOrWhiteSpace(eventInfo.declaringType) || string.IsNullOrWhiteSpace(eventInfo.name))
		{
			return;
		}

		string key = eventInfo.declaringType + "." + eventInfo.name;
		map[key] = CloneEvent(eventInfo);
	}

	private void AddParameter(Dictionary<string, CapabilityParameterInfo> map, CapabilityParameterInfo parameter)
	{
		if (parameter == null || string.IsNullOrWhiteSpace(parameter.name))
		{
			return;
		}

		string key = (parameter.featureId ?? "") + "|" + parameter.name;
		map[key] = CloneParameter(parameter);
	}

	private void AddComponent(Dictionary<string, UnityCapabilityComponentInfo> map, UnityCapabilityComponentInfo component)
	{
		if (component == null || string.IsNullOrWhiteSpace(component.componentId))
		{
			return;
		}

		if (!map.TryGetValue(component.componentId, out UnityCapabilityComponentInfo existing))
		{
			map[component.componentId] = CloneUnityComponent(component);
			return;
		}

		existing.requiredComponents = DistinctStrings(existing.requiredComponents.Concat(component.requiredComponents ?? new List<string>()));
		existing.optionalComponents = DistinctStrings(existing.optionalComponents.Concat(component.optionalComponents ?? new List<string>()));
		existing.allowedFeatures = DistinctStrings(existing.allowedFeatures.Concat(component.allowedFeatures ?? new List<string>()));
		existing.tags = DistinctStrings(existing.tags.Concat(component.tags ?? new List<string>()));
		MergeMethods(existing.methods, component.methods);
		MergeEvents(existing.events, component.events);
		MergeParameters(existing.parameters, component.parameters);
	}

	private void AddSystem(Dictionary<string, UnityCapabilitySystemInfo> map, UnityCapabilitySystemInfo systemInfo)
	{
		if (systemInfo == null || string.IsNullOrWhiteSpace(systemInfo.systemId))
		{
			return;
		}

		if (!map.TryGetValue(systemInfo.systemId, out UnityCapabilitySystemInfo existing))
		{
			map[systemInfo.systemId] = CloneSystem(systemInfo);
			return;
		}

		existing.featureIds = DistinctStrings(existing.featureIds.Concat(systemInfo.featureIds ?? new List<string>()));
		existing.methodIds = DistinctStrings(existing.methodIds.Concat(systemInfo.methodIds ?? new List<string>()));
		existing.eventIds = DistinctStrings(existing.eventIds.Concat(systemInfo.eventIds ?? new List<string>()));
		existing.tags = DistinctStrings(existing.tags.Concat(systemInfo.tags ?? new List<string>()));
	}

	private void AddRole(Dictionary<string, UnityCapabilityGameObjectRoleInfo> map, UnityCapabilityGameObjectRoleInfo roleInfo)
	{
		if (roleInfo == null || string.IsNullOrWhiteSpace(roleInfo.roleId))
		{
			return;
		}

		if (!map.TryGetValue(roleInfo.roleId, out UnityCapabilityGameObjectRoleInfo existing))
		{
			map[roleInfo.roleId] = CloneRole(roleInfo);
			return;
		}

		existing.componentIds = DistinctStrings(existing.componentIds.Concat(roleInfo.componentIds ?? new List<string>()));
		existing.allowedFeatures = DistinctStrings(existing.allowedFeatures.Concat(roleInfo.allowedFeatures ?? new List<string>()));
		existing.requiredFeatures = DistinctStrings(existing.requiredFeatures.Concat(roleInfo.requiredFeatures ?? new List<string>()));
		existing.tags = DistinctStrings(existing.tags.Concat(roleInfo.tags ?? new List<string>()));
	}

	private void AddBehaviorShape(Dictionary<string, UnityCapabilityBehaviorShapeInfo> map, UnityCapabilityBehaviorShapeInfo shapeInfo)
	{
		if (shapeInfo == null || string.IsNullOrWhiteSpace(shapeInfo.shapeId))
		{
			return;
		}

		if (!map.TryGetValue(shapeInfo.shapeId, out UnityCapabilityBehaviorShapeInfo existing))
		{
			map[shapeInfo.shapeId] = CloneShape(shapeInfo);
			return;
		}

		existing.componentIds = DistinctStrings(existing.componentIds.Concat(shapeInfo.componentIds ?? new List<string>()));
		existing.systemIds = DistinctStrings(existing.systemIds.Concat(shapeInfo.systemIds ?? new List<string>()));
		existing.featureIds = DistinctStrings(existing.featureIds.Concat(shapeInfo.featureIds ?? new List<string>()));
		existing.roleIds = DistinctStrings(existing.roleIds.Concat(shapeInfo.roleIds ?? new List<string>()));
		existing.tags = DistinctStrings(existing.tags.Concat(shapeInfo.tags ?? new List<string>()));
	}

	private void MergeMethods(List<CapabilityMethodInfo> target, List<CapabilityMethodInfo> source)
	{
		if (target == null || source == null)
		{
			return;
		}

		Dictionary<string, CapabilityMethodInfo> map = target.ToDictionary(
			method => method.declaringType + "." + method.name + "(" + string.Join(",", method.parameters.Select(parameter => parameter.type + ":" + parameter.name)) + ")",
			method => method,
			StringComparer.OrdinalIgnoreCase);

		foreach (CapabilityMethodInfo method in source)
		{
			string key = method.declaringType + "." + method.name + "(" + string.Join(",", method.parameters.Select(parameter => parameter.type + ":" + parameter.name)) + ")";
			map[key] = CloneMethod(method);
		}

		target.Clear();
		target.AddRange(map.Values.OrderBy(method => method.declaringType).ThenBy(method => method.name));
	}

	private void MergeEvents(List<CapabilityEventInfo> target, List<CapabilityEventInfo> source)
	{
		if (target == null || source == null)
		{
			return;
		}

		Dictionary<string, CapabilityEventInfo> map = target.ToDictionary(
			eventInfo => eventInfo.declaringType + "." + eventInfo.name,
			eventInfo => eventInfo,
			StringComparer.OrdinalIgnoreCase);

		foreach (CapabilityEventInfo eventInfo in source)
		{
			map[eventInfo.declaringType + "." + eventInfo.name] = CloneEvent(eventInfo);
		}

		target.Clear();
		target.AddRange(map.Values.OrderBy(eventInfo => eventInfo.declaringType).ThenBy(eventInfo => eventInfo.name));
	}

	private void MergeParameters(List<CapabilityParameterInfo> target, List<CapabilityParameterInfo> source)
	{
		if (target == null || source == null)
		{
			return;
		}

		Dictionary<string, CapabilityParameterInfo> map = target.ToDictionary(
			parameter => (parameter.featureId ?? "") + "|" + parameter.name,
			parameter => parameter,
			StringComparer.OrdinalIgnoreCase);

		foreach (CapabilityParameterInfo parameter in source)
		{
			map[(parameter.featureId ?? "") + "|" + parameter.name] = CloneParameter(parameter);
		}

		target.Clear();
		target.AddRange(map.Values.OrderBy(parameter => parameter.featureId).ThenBy(parameter => parameter.name));
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

	private static string GetFriendlyTypeName(Type type)
	{
		if (type == null)
		{
			return "";
		}

		if (type == typeof(void))
		{
			return "void";
		}

		if (type.IsGenericType)
		{
			string genericName = type.Name;
			int tickIndex = genericName.IndexOf('`');
			if (tickIndex >= 0)
			{
				genericName = genericName.Substring(0, tickIndex);
			}

			return genericName + "<" + string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName)) + ">";
		}

		return type.Name;
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

	private static string GetBaseTypeLabel(Type type)
	{
		if (typeof(MonoBehaviour).IsAssignableFrom(type))
		{
			return "MonoBehaviour";
		}

		if (typeof(ScriptableObject).IsAssignableFrom(type))
		{
			return "ScriptableObject";
		}

		return type.IsClass ? "PlainClass" : "Service";
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
		return IsAssetsType(type);
	}

	private static bool IsAssetsType(Type type)
	{
		if (type == null)
		{
			return false;
		}

		MonoScript script = FindMonoScriptForType(type);
		return script != null && IsAssetsPath(AssetDatabase.GetAssetPath(script));
	}

	private static bool IsAssetBackedComponent(Type type, Component instance)
	{
		if (type == null)
		{
			return false;
		}

		if (instance is MonoBehaviour monoBehaviour)
		{
			MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);
			return script != null && IsAssetsPath(AssetDatabase.GetAssetPath(script));
		}

		return IsAssetsType(type);
	}

	private static MonoScript FindMonoScriptForType(Type type)
	{
		if (type == null)
		{
			return null;
		}

		foreach (MonoScript script in Resources.FindObjectsOfTypeAll<MonoScript>())
		{
			if (script != null && script.GetClass() == type)
			{
				return script;
			}
		}

		return null;
	}

	private static bool IsAssetsPath(string assetPath)
	{
		return !string.IsNullOrWhiteSpace(assetPath) &&
			assetPath.Replace("\\", "/").StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
	}

	private static List<string> BuildRequiredComponentNames(Type type)
	{
		return GetRequireComponentTypes(type)
			.Select(requiredType => requiredType.Name)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(name => name)
			.ToList();
	}

	private static IEnumerable<Type> GetRequireComponentTypes(Type type)
	{
		foreach (RequireComponent requireComponent in type.GetCustomAttributes(typeof(RequireComponent), true))
		{
			foreach (Type requiredType in GetRequireComponentTypes(requireComponent))
			{
				if (requiredType != null)
				{
					yield return requiredType;
				}
			}
		}
	}

	private static IEnumerable<Type> GetRequireComponentTypes(RequireComponent requireComponent)
	{
		if (requireComponent == null)
		{
			yield break;
		}

		string[] fieldNames = { "m_Type0", "m_Type1", "m_Type2" };
		foreach (string fieldName in fieldNames)
		{
			FieldInfo field = typeof(RequireComponent).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (field == null)
			{
				continue;
			}

			Type requiredType = field.GetValue(requireComponent) as Type;
			if (requiredType != null)
			{
				yield return requiredType;
			}
		}
	}

	private static List<string> BuildMethodTags(MethodInfo method)
	{
		List<string> tags = new List<string> { "reflection" };
		if (Array.IndexOf(UnityCallbackNames, method.Name) >= 0)
		{
			tags.Add("callback");
		}

		return DistinctStrings(tags);
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
		capabilities.supportedFeatures = NormalizeFeatures(capabilities.supportedFeatures);
		capabilities.constraints = NormalizeConstraints(capabilities.constraints);
		capabilities.unity.components = NormalizeComponents(capabilities.unity.components);
		capabilities.unity.systems = NormalizeSystems(capabilities.unity.systems);
		capabilities.unity.gameObjectRoles = NormalizeRoles(capabilities.unity.gameObjectRoles);
		capabilities.unity.behaviorShapes = NormalizeShapes(capabilities.unity.behaviorShapes);
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
		capabilities.types = NormalizeTypes(capabilities.types);
		capabilities.supportedFeatures = NormalizeFeatures(capabilities.supportedFeatures);
		capabilities.constraints = NormalizeConstraints(capabilities.constraints);
		capabilities.unity.components = NormalizeComponents(capabilities.unity.components);
		capabilities.unity.systems = NormalizeSystems(capabilities.unity.systems);
		capabilities.unity.gameObjectRoles = NormalizeRoles(capabilities.unity.gameObjectRoles);
		capabilities.unity.behaviorShapes = NormalizeShapes(capabilities.unity.behaviorShapes);
		capabilities.module.assemblyNames = DistinctStrings(capabilities.module.assemblyNames);
		capabilities.module.namespaceRoots = DistinctStrings(capabilities.module.namespaceRoots);
		capabilities.module.dependencies = DistinctStrings(capabilities.module.dependencies);
		capabilities.module.tags = DistinctStrings(capabilities.module.tags);
	}

	private static List<CapabilityTypeInfo> NormalizeTypes(List<CapabilityTypeInfo> types)
	{
		Dictionary<string, CapabilityTypeInfo> map = new Dictionary<string, CapabilityTypeInfo>(StringComparer.OrdinalIgnoreCase);
		foreach (CapabilityTypeInfo type in types ?? new List<CapabilityTypeInfo>())
		{
			if (type == null || string.IsNullOrWhiteSpace(type.fullName))
			{
				continue;
			}

			Type resolvedType = ResolveStaticTypeByName(type.fullName);
			if (resolvedType != null && !IsAssetsType(resolvedType))
			{
				continue;
			}

			string key = type.fullName.Trim();
			CapabilityTypeInfo clone = JsonUtility.FromJson<CapabilityTypeInfo>(JsonUtility.ToJson(type)) ?? new CapabilityTypeInfo();
			clone.enumValues = DistinctStrings(clone.enumValues);
			map[key] = clone;
		}

		return map.Values.OrderBy(type => type.fullName).ToList();
	}

	private static List<CapabilityFeatureInfo> NormalizeFeatures(List<CapabilityFeatureInfo> features)
	{
		Dictionary<string, CapabilityFeatureInfo> map = new Dictionary<string, CapabilityFeatureInfo>(StringComparer.OrdinalIgnoreCase);
		foreach (CapabilityFeatureInfo feature in features ?? new List<CapabilityFeatureInfo>())
		{
			if (feature == null || string.IsNullOrWhiteSpace(feature.featureId))
			{
				continue;
			}

			string key = feature.featureId.Trim();
			map[key] = CloneFeature(feature);
			map[key].requiredDependencies = DistinctStrings(map[key].requiredDependencies);
			map[key].incompatibleFeatures = DistinctStrings(map[key].incompatibleFeatures);
			map[key].recommendedTemplates = DistinctStrings(map[key].recommendedTemplates);
		}

		return map.Values.OrderBy(feature => feature.featureId).ToList();
	}

	private static List<CapabilityConstraintInfo> NormalizeConstraints(List<CapabilityConstraintInfo> constraints)
	{
		Dictionary<string, CapabilityConstraintInfo> map = new Dictionary<string, CapabilityConstraintInfo>(StringComparer.OrdinalIgnoreCase);
		foreach (CapabilityConstraintInfo constraint in constraints ?? new List<CapabilityConstraintInfo>())
		{
			if (constraint == null || string.IsNullOrWhiteSpace(constraint.description))
			{
				continue;
			}

			string key = (constraint.code ?? "") + "|" + constraint.description + "|" + (constraint.appliesToId ?? "");
			map[key] = CloneConstraint(constraint);
		}

		return map.Values.OrderBy(constraint => constraint.code).ThenBy(constraint => constraint.description).ToList();
	}

	private static List<UnityCapabilityComponentInfo> NormalizeComponents(List<UnityCapabilityComponentInfo> components)
	{
		Dictionary<string, UnityCapabilityComponentInfo> map = new Dictionary<string, UnityCapabilityComponentInfo>(StringComparer.OrdinalIgnoreCase);
		foreach (UnityCapabilityComponentInfo component in components ?? new List<UnityCapabilityComponentInfo>())
		{
			if (component == null || string.IsNullOrWhiteSpace(component.componentId))
			{
				continue;
			}

			UnityCapabilityComponentInfo clone = CloneUnityComponent(component);
			clone.requiredComponents = DistinctStrings(clone.requiredComponents);
			clone.optionalComponents = DistinctStrings(clone.optionalComponents);
			clone.allowedFeatures = DistinctStrings(clone.allowedFeatures);
			clone.tags = DistinctStrings(clone.tags);
			map[clone.componentId] = clone;
		}

		return map.Values.OrderBy(component => component.componentId).ToList();
	}

	private static List<UnityCapabilitySystemInfo> NormalizeSystems(List<UnityCapabilitySystemInfo> systems)
	{
		Dictionary<string, UnityCapabilitySystemInfo> map = new Dictionary<string, UnityCapabilitySystemInfo>(StringComparer.OrdinalIgnoreCase);
		foreach (UnityCapabilitySystemInfo systemInfo in systems ?? new List<UnityCapabilitySystemInfo>())
		{
			if (systemInfo == null || string.IsNullOrWhiteSpace(systemInfo.systemId))
			{
				continue;
			}

			UnityCapabilitySystemInfo clone = CloneSystem(systemInfo);
			clone.requiredModules = DistinctStrings(clone.requiredModules);
			clone.eventIds = DistinctStrings(clone.eventIds);
			clone.methodIds = DistinctStrings(clone.methodIds);
			clone.featureIds = DistinctStrings(clone.featureIds);
			clone.tags = DistinctStrings(clone.tags);
			map[clone.systemId] = clone;
		}

		return map.Values.OrderBy(systemInfo => systemInfo.systemId).ToList();
	}

	private static List<UnityCapabilityGameObjectRoleInfo> NormalizeRoles(List<UnityCapabilityGameObjectRoleInfo> roles)
	{
		Dictionary<string, UnityCapabilityGameObjectRoleInfo> map = new Dictionary<string, UnityCapabilityGameObjectRoleInfo>(StringComparer.OrdinalIgnoreCase);
		foreach (UnityCapabilityGameObjectRoleInfo roleInfo in roles ?? new List<UnityCapabilityGameObjectRoleInfo>())
		{
			if (roleInfo == null || string.IsNullOrWhiteSpace(roleInfo.roleId))
			{
				continue;
			}

			UnityCapabilityGameObjectRoleInfo clone = CloneRole(roleInfo);
			clone.componentIds = DistinctStrings(clone.componentIds);
			clone.allowedFeatures = DistinctStrings(clone.allowedFeatures);
			clone.requiredFeatures = DistinctStrings(clone.requiredFeatures);
			clone.tags = DistinctStrings(clone.tags);
			map[clone.roleId] = clone;
		}

		return map.Values.OrderBy(roleInfo => roleInfo.roleId).ToList();
	}

	private static List<UnityCapabilityBehaviorShapeInfo> NormalizeShapes(List<UnityCapabilityBehaviorShapeInfo> shapes)
	{
		Dictionary<string, UnityCapabilityBehaviorShapeInfo> map = new Dictionary<string, UnityCapabilityBehaviorShapeInfo>(StringComparer.OrdinalIgnoreCase);
		foreach (UnityCapabilityBehaviorShapeInfo shapeInfo in shapes ?? new List<UnityCapabilityBehaviorShapeInfo>())
		{
			if (shapeInfo == null || string.IsNullOrWhiteSpace(shapeInfo.shapeId))
			{
				continue;
			}

			UnityCapabilityBehaviorShapeInfo clone = CloneShape(shapeInfo);
			clone.componentIds = DistinctStrings(clone.componentIds);
			clone.systemIds = DistinctStrings(clone.systemIds);
			clone.featureIds = DistinctStrings(clone.featureIds);
			clone.roleIds = DistinctStrings(clone.roleIds);
			clone.tags = DistinctStrings(clone.tags);
			map[clone.shapeId] = clone;
		}

		return map.Values.OrderBy(shapeInfo => shapeInfo.shapeId).ToList();
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

	private static CapabilityFeatureInfo CloneFeature(CapabilityFeatureInfo source)
	{
		return source == null ? new CapabilityFeatureInfo() : JsonUtility.FromJson<CapabilityFeatureInfo>(JsonUtility.ToJson(source)) ?? new CapabilityFeatureInfo();
	}

	private static CapabilityConstraintInfo CloneConstraint(CapabilityConstraintInfo source)
	{
		return source == null ? new CapabilityConstraintInfo() : JsonUtility.FromJson<CapabilityConstraintInfo>(JsonUtility.ToJson(source)) ?? new CapabilityConstraintInfo();
	}

	private static CapabilityMethodInfo CloneMethod(CapabilityMethodInfo source)
	{
		return source == null ? new CapabilityMethodInfo() : JsonUtility.FromJson<CapabilityMethodInfo>(JsonUtility.ToJson(source)) ?? new CapabilityMethodInfo();
	}

	private static CapabilityEventInfo CloneEvent(CapabilityEventInfo source)
	{
		return source == null ? new CapabilityEventInfo() : JsonUtility.FromJson<CapabilityEventInfo>(JsonUtility.ToJson(source)) ?? new CapabilityEventInfo();
	}

	private static CapabilityParameterInfo CloneParameter(CapabilityParameterInfo source)
	{
		return source == null ? new CapabilityParameterInfo() : JsonUtility.FromJson<CapabilityParameterInfo>(JsonUtility.ToJson(source)) ?? new CapabilityParameterInfo();
	}

	private static UnityCapabilityComponentInfo CloneUnityComponent(UnityCapabilityComponentInfo source)
	{
		return source == null ? new UnityCapabilityComponentInfo() : JsonUtility.FromJson<UnityCapabilityComponentInfo>(JsonUtility.ToJson(source)) ?? new UnityCapabilityComponentInfo();
	}

	private static UnityCapabilitySystemInfo CloneSystem(UnityCapabilitySystemInfo source)
	{
		return source == null ? new UnityCapabilitySystemInfo() : JsonUtility.FromJson<UnityCapabilitySystemInfo>(JsonUtility.ToJson(source)) ?? new UnityCapabilitySystemInfo();
	}

	private static UnityCapabilityGameObjectRoleInfo CloneRole(UnityCapabilityGameObjectRoleInfo source)
	{
		return source == null ? new UnityCapabilityGameObjectRoleInfo() : JsonUtility.FromJson<UnityCapabilityGameObjectRoleInfo>(JsonUtility.ToJson(source)) ?? new UnityCapabilityGameObjectRoleInfo();
	}

	private static UnityCapabilityBehaviorShapeInfo CloneShape(UnityCapabilityBehaviorShapeInfo source)
	{
		return source == null ? new UnityCapabilityBehaviorShapeInfo() : JsonUtility.FromJson<UnityCapabilityBehaviorShapeInfo>(JsonUtility.ToJson(source)) ?? new UnityCapabilityBehaviorShapeInfo();
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

	private static Type ResolveStaticTypeByName(string typeName)
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
