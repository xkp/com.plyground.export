using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
	private static readonly Dictionary<string, CachedSourceScriptInfo> SourceScriptCache = new Dictionary<string, CachedSourceScriptInfo>(StringComparer.OrdinalIgnoreCase);
	private static Assembly RoslynAssembly;
	private static Assembly RoslynCSharpAssembly;
	private static bool RoslynLoadAttempted;
	private bool capabilityDebugLogging = true;

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

	private class CachedSourceScriptInfo
	{
		public DateTime lastWriteUtc;
		public SourceScriptInfo info;
	}

	private class SourceScriptInfo
	{
		public string assetPath = "";
		public string namespaceName = "";
		public string className = "";
		public string fullName = "";
		public string baseTypeName = "";
		public string summary = "";
		public bool isComponent;
		public List<SourceMethodInfo> methods = new List<SourceMethodInfo>();
		public List<SourceFieldInfo> fields = new List<SourceFieldInfo>();
		public List<SourceEventInfo> events = new List<SourceEventInfo>();
	}

	private class SourceMethodInfo
	{
		public string name = "";
		public string returnType = "";
		public List<CapabilityMethodParameterInfo> parameters = new List<CapabilityMethodParameterInfo>();
		public string summary = "";
		public bool isStatic;
	}

	private class SourceFieldInfo
	{
		public string name = "";
		public string type = "";
		public string summary = "";
		public bool required;
		public bool serialized;
	}

	private class SourceEventInfo
	{
		public string name = "";
		public string payloadType = "";
		public string summary = "";
	}

	private void PrepareCapabilitiesForPersistence()
	{
		moduleCapabilities ??= new CapabilityManifest();

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
		List<SourceScriptInfo> sourceScripts = GetAllLocalSourceScripts();
		HashSet<string> relevantNamespaceRoots = GetRelevantNamespaceRoots(sourceScripts);
		LogCapabilityDebug("Source discovery: scripts={0}, componentScripts={1}, namespaceRoots=[{2}]",
			sourceScripts.Count,
			sourceScripts.Count(sourceInfo => sourceInfo != null && sourceInfo.isComponent),
			string.Join(", ", relevantNamespaceRoots.OrderBy(value => value).ToArray()));

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

		List<SourceScriptInfo> relevantSourceComponents = GetRelevantSourceComponents(sourceScripts, relevantNamespaceRoots);
		LogCapabilityDebug("Relevant source components kept={0}. Sample=[{1}]",
			relevantSourceComponents.Count,
			string.Join(", ", relevantSourceComponents.Take(12).Select(sourceInfo => sourceInfo.fullName).ToArray()));
		foreach (SourceScriptInfo sourceInfo in relevantSourceComponents)
		{
			Type resolvedType = ResolveTypeByName(sourceInfo.fullName);
			UnityCapabilityComponentInfo componentInfo = resolvedType != null
				? BuildUnityComponentInfo(resolvedType, null, sourceInfo)
				: BuildUnityComponentInfo(sourceInfo);
			AddComponent(componentMap, componentInfo);
			AddFeature(featureMap, new CapabilityFeatureInfo
			{
				featureId = "component:" + sourceInfo.className,
				description = string.IsNullOrWhiteSpace(sourceInfo.summary) ? "Declared in source" : sourceInfo.summary
			});

			if (resolvedType != null)
			{
				AddTypeMetadata(resolvedType, typeMap, assemblyNames, namespaceRoots);
				foreach (CapabilityMethodInfo method in BuildMethodInfos(resolvedType, sourceInfo))
				{
					AddMethod(methodMap, method);
				}

				foreach (CapabilityEventInfo eventInfo in BuildEventInfos(resolvedType, sourceInfo))
				{
					AddEvent(eventMap, eventInfo);
				}

				foreach (CapabilityParameterInfo parameter in BuildParameterInfos(resolvedType, null, sourceInfo))
				{
					AddParameter(parameterMap, parameter);
				}
			}
			else
			{
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

				typeMap[sourceInfo.fullName] = BuildTypeInfo(sourceInfo);
				if (!string.IsNullOrWhiteSpace(sourceInfo.namespaceName))
				{
					namespaceRoots.Add(GetNamespaceRoot(sourceInfo.namespaceName));
				}
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

		Dictionary<string, UnityCapabilityComponentInfo> componentMap = new Dictionary<string, UnityCapabilityComponentInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
		{
			Type componentType = component != null ? component.GetType() : null;
			if (componentType == null || !IsAssetBackedComponent(componentType, component))
			{
				continue;
			}

			SourceScriptInfo sourceInfo = GetSourceScriptInfoForType(componentType);
			UnityCapabilityComponentInfo componentInfo = BuildUnityComponentInfo(componentType, component, sourceInfo);
			AddComponent(componentMap, componentInfo);
		}
		result.unity.components = componentMap.Values.OrderBy(info => info.componentId).ToList();
		return result;
	}

	private UnityCapabilityComponentInfo BuildUnityComponentInfo(Type componentType, Component instance)
	{
		return BuildUnityComponentInfo(componentType, instance, GetSourceScriptInfoForType(componentType));
	}

	private UnityCapabilityComponentInfo BuildUnityComponentInfo(Type componentType, Component instance, SourceScriptInfo sourceInfo)
	{
		UnityCapabilityComponentInfo componentInfo = new UnityCapabilityComponentInfo
		{
			componentId = componentType.FullName ?? componentType.Name,
			typeName = componentType.FullName ?? componentType.Name,
			baseType = GetBaseTypeLabel(componentType),
			attachTarget = "self",
			description = sourceInfo != null && !string.IsNullOrWhiteSpace(sourceInfo.summary) ? sourceInfo.summary : "Component inferred from Assets/ script",
			requiredComponents = BuildRequiredComponentNames(componentType),
			methods = BuildMethodInfos(componentType, sourceInfo),
			events = BuildEventInfos(componentType, sourceInfo),
			parameters = BuildParameterInfos(componentType, instance, sourceInfo),
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
			if (IsEventFieldType(field.FieldType))
			{
				continue;
			}

			allowedFeatures.Add("serialized-field:" + componentType.Name + "." + field.Name);
		}

		componentInfo.allowedFeatures = allowedFeatures.OrderBy(value => value).ToList();
		return componentInfo;
	}

	private UnityCapabilityComponentInfo BuildUnityComponentInfo(SourceScriptInfo sourceInfo)
	{
		UnityCapabilityComponentInfo componentInfo = new UnityCapabilityComponentInfo
		{
			componentId = sourceInfo.fullName,
			typeName = sourceInfo.fullName,
			baseType = GetBaseTypeLabel(sourceInfo.baseTypeName),
			attachTarget = "self",
			description = sourceInfo.summary,
			requiredComponents = new List<string>(),
			methods = BuildMethodInfos(sourceInfo),
			events = BuildEventInfos(sourceInfo),
			parameters = BuildParameterInfos(sourceInfo),
			tags = DistinctStrings(new[] { "component-first", "unity-exporter", "source-derived" }),
			codegenAllowed = true,
			allowedFeatures = BuildAllowedFeatures(sourceInfo)
		};

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
		return BuildMethodInfos(type, GetSourceScriptInfoForType(type));
	}

	private List<CapabilityMethodInfo> BuildMethodInfos(Type type, SourceScriptInfo sourceInfo)
	{
		List<CapabilityMethodInfo> methods = new List<CapabilityMethodInfo>();
		Dictionary<string, SourceMethodInfo> sourceMethodMap = BuildSourceLookup(
			sourceInfo != null ? sourceInfo.methods : null,
			method => method.name);
		foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
		{
			if (method.IsSpecialName)
			{
				continue;
			}

			sourceMethodMap.TryGetValue(method.Name, out SourceMethodInfo sourceMethod);

			methods.Add(new CapabilityMethodInfo
			{
				name = method.Name,
				declaringType = type.FullName ?? type.Name,
				description = sourceMethod != null && !string.IsNullOrWhiteSpace(sourceMethod.summary) ? sourceMethod.summary : "Reflected from " + type.Name,
				parameters = BuildMethodParameters(method, sourceMethod),
				returnType = GetFriendlyTypeName(method.ReturnType),
				isStatic = method.IsStatic,
				allowedForCodegen = true,
				constraints = new List<string>(),
				tags = BuildMethodTags(method)
			});
		}

		return methods;
	}

	private List<CapabilityMethodInfo> BuildMethodInfos(SourceScriptInfo sourceInfo)
	{
		return sourceInfo.methods
			.Select(method => new CapabilityMethodInfo
			{
				name = method.name,
				declaringType = sourceInfo.fullName,
				description = method.summary,
				parameters = method.parameters != null ? CloneMethodParameterList(method.parameters) : new List<CapabilityMethodParameterInfo>(),
				returnType = string.IsNullOrWhiteSpace(method.returnType) ? "void" : method.returnType,
				isStatic = method.isStatic,
				allowedForCodegen = true,
				constraints = new List<string>(),
				tags = new List<string> { "source" }
			})
			.ToList();
	}

	private List<CapabilityEventInfo> BuildEventInfos(Type type)
	{
		return BuildEventInfos(type, GetSourceScriptInfoForType(type));
	}

	private List<CapabilityEventInfo> BuildEventInfos(Type type, SourceScriptInfo sourceInfo)
	{
		List<CapabilityEventInfo> events = new List<CapabilityEventInfo>();
		Dictionary<string, SourceEventInfo> sourceEventMap = BuildSourceLookup(
			sourceInfo != null ? sourceInfo.events : null,
			eventInfo => eventInfo.name);
		foreach (EventInfo eventInfo in type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
		{
			sourceEventMap.TryGetValue(eventInfo.Name, out SourceEventInfo sourceEvent);
			events.Add(new CapabilityEventInfo
			{
				name = eventInfo.Name,
				direction = "publishes",
				payloadType = eventInfo.EventHandlerType != null ? GetFriendlyTypeName(eventInfo.EventHandlerType) : "",
				declaringType = type.FullName ?? type.Name,
				description = sourceEvent != null && !string.IsNullOrWhiteSpace(sourceEvent.summary) ? sourceEvent.summary : "Reflected from " + type.Name,
				allowedForCodegen = true,
				scope = "",
				authority = "",
				tags = new List<string> { "reflection" }
			});
		}

		foreach (FieldInfo field in GetSerializedFields(type))
		{
			if (!IsEventFieldType(field.FieldType))
			{
				continue;
			}

			sourceEventMap.TryGetValue(field.Name, out SourceEventInfo sourceEvent);
			events.Add(new CapabilityEventInfo
			{
				name = field.Name,
				direction = "publishes",
				payloadType = GetFriendlyTypeName(field.FieldType),
				declaringType = type.FullName ?? type.Name,
				description = sourceEvent != null && !string.IsNullOrWhiteSpace(sourceEvent.summary) ? sourceEvent.summary : "Event field",
				allowedForCodegen = true,
				scope = "",
				authority = "",
				tags = new List<string> { "field-event", "reflection" }
			});
		}

		return events;
	}

	private List<CapabilityEventInfo> BuildEventInfos(SourceScriptInfo sourceInfo)
	{
		List<CapabilityEventInfo> events = sourceInfo.events
			.Where(eventInfo => !IsUnityLifecycleEventName(eventInfo.name))
			.Select(eventInfo => new CapabilityEventInfo
			{
				name = eventInfo.name,
				direction = "publishes",
				payloadType = eventInfo.payloadType,
				declaringType = sourceInfo.fullName,
				description = eventInfo.summary,
				allowedForCodegen = true,
				scope = "",
				authority = "",
				tags = new List<string> { "source" }
			})
			.ToList();

		foreach (SourceFieldInfo field in sourceInfo.fields.Where(IsSourceEventField))
		{
			events.Add(new CapabilityEventInfo
			{
				name = field.name,
				direction = "publishes",
				payloadType = field.type,
				declaringType = sourceInfo.fullName,
				description = field.summary,
				allowedForCodegen = true,
				scope = "",
				authority = "",
				tags = new List<string> { "field-event", "source" }
			});
		}

		return events;
	}

	private List<CapabilityParameterInfo> BuildParameterInfos(Type type, Component instance)
	{
		return BuildParameterInfos(type, instance, GetSourceScriptInfoForType(type));
	}

	private List<CapabilityParameterInfo> BuildParameterInfos(Type type, Component instance, SourceScriptInfo sourceInfo)
	{
		List<CapabilityParameterInfo> parameters = new List<CapabilityParameterInfo>();
		Dictionary<string, SourceFieldInfo> sourceFieldMap = BuildSourceLookup(
			sourceInfo != null ? sourceInfo.fields : null,
			field => field.name);
		foreach (FieldInfo field in GetSerializedFields(type))
		{
			if (IsEventFieldType(field.FieldType))
			{
				continue;
			}

			sourceFieldMap.TryGetValue(field.Name, out SourceFieldInfo sourceField);
			parameters.Add(new CapabilityParameterInfo
			{
				name = field.Name,
				type = GetFriendlyTypeName(field.FieldType),
				required = false,
				@default = instance != null && field.GetValue(instance) != null ? field.GetValue(instance).ToString() : "",
				description = sourceField != null && !string.IsNullOrWhiteSpace(sourceField.summary) ? sourceField.summary : "Serialized field",
				moduleScoped = false,
				featureId = "serialized-field:" + type.Name + "." + field.Name,
				enumValues = field.FieldType.IsEnum ? Enum.GetNames(field.FieldType).ToList() : new List<string>(),
				tags = new List<string> { "serialized-field" }
			});
		}

		return parameters;
	}

	private List<CapabilityParameterInfo> BuildParameterInfos(SourceScriptInfo sourceInfo)
	{
		return sourceInfo.fields
			.Where(field => field.serialized && !IsSourceEventField(field))
			.Select(field => new CapabilityParameterInfo
			{
				name = field.name,
				type = field.type,
				required = field.required,
				@default = "",
				description = field.summary,
				moduleScoped = false,
				featureId = "serialized-field:" + sourceInfo.className + "." + field.name,
				enumValues = new List<string>(),
				tags = new List<string> { "source" }
			})
			.ToList();
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
		SourceScriptInfo sourceInfo = GetSourceScriptInfoForType(type);
		CapabilityTypeInfo info = new CapabilityTypeInfo
		{
			name = type.Name,
			fullName = type.FullName ?? type.Name,
			kind = GetTypeKind(type),
			@namespace = type.Namespace ?? "",
			description = sourceInfo != null && !string.IsNullOrWhiteSpace(sourceInfo.summary) ? sourceInfo.summary : "User script type",
			exposed = true,
			fields = GetSerializedFields(type)
				.Select(field => new CapabilityTypeFieldInfo
				{
					name = field.Name,
					type = GetFriendlyTypeName(field.FieldType),
					description = sourceInfo != null ? GetSourceFieldSummary(sourceInfo, field.Name) : "Serialized field",
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

	private CapabilityTypeInfo BuildTypeInfo(SourceScriptInfo sourceInfo)
	{
		return new CapabilityTypeInfo
		{
			name = sourceInfo.className,
			fullName = sourceInfo.fullName,
			kind = "component",
			@namespace = sourceInfo.namespaceName,
			description = sourceInfo.summary,
			exposed = true,
			fields = sourceInfo.fields
				.Where(field => field.serialized)
				.Select(field => new CapabilityTypeFieldInfo
				{
					name = field.name,
					type = field.type,
					description = field.summary,
					required = field.required
				})
				.ToList(),
			enumValues = new List<string>()
		};
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

	private SourceScriptInfo GetSourceScriptInfoForType(Type type)
	{
		MonoScript script = FindMonoScriptForType(type);
		if (script == null)
		{
			return null;
		}

		string assetPath = AssetDatabase.GetAssetPath(script);
		return ParseSourceScript(assetPath, type);
	}

	private List<SourceScriptInfo> GetAllLocalSourceScripts()
	{
		List<SourceScriptInfo> scripts = new List<SourceScriptInfo>();
		List<string> assetPaths = capabilitySourceScriptPaths == null
			? new List<string>()
			: capabilitySourceScriptPaths
				.Where(path => !string.IsNullOrWhiteSpace(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

		if (assetPaths.Count == 0)
		{
			LogCapabilityDebug("No explicit script selection. Source-driven component discovery skipped.");
			return scripts;
		}

		LogCapabilityDebug("Using explicit script selection. Selected scripts={0}.", assetPaths.Count);

		foreach (string assetPath in assetPaths)
		{
			MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
			Type scriptType = script != null ? script.GetClass() : null;
			SourceScriptInfo sourceInfo = ParseSourceScript(assetPath, scriptType);
			if (sourceInfo != null && sourceInfo.isComponent)
			{
				scripts.Add(sourceInfo);
			}
		}

		LogCapabilityDebug("Parsed local source scripts: kept {0} component candidates. Sample=[{1}]",
			scripts.Count,
			string.Join(", ", scripts.Take(12).Select(sourceInfo => sourceInfo.fullName).ToArray()));

		return scripts;
	}

	private SourceScriptInfo ParseSourceScript(string assetPath, Type scriptType)
	{
		if (string.IsNullOrWhiteSpace(assetPath))
		{
			return null;
		}

		string fullPath = Path.GetFullPath(assetPath);
		if (!File.Exists(fullPath))
		{
			return null;
		}

		DateTime lastWriteUtc = File.GetLastWriteTimeUtc(fullPath);
		if (SourceScriptCache.TryGetValue(fullPath, out CachedSourceScriptInfo cached) && cached.lastWriteUtc == lastWriteUtc)
		{
			return cached.info;
		}

		SourceScriptInfo info = ParseSourceText(File.ReadAllLines(fullPath), assetPath, scriptType);
		SourceScriptCache[fullPath] = new CachedSourceScriptInfo
		{
			lastWriteUtc = lastWriteUtc,
			info = info
		};
		return info;
	}

	private SourceScriptInfo ParseSourceText(string[] lines, string assetPath, Type scriptType)
	{
		SourceScriptInfo roslynInfo = TryParseSourceWithRoslyn(lines, assetPath, scriptType);
		if (roslynInfo != null)
		{
			return roslynInfo;
		}

		SourceScriptInfo info = new SourceScriptInfo
		{
			assetPath = assetPath,
			namespaceName = scriptType != null ? scriptType.Namespace ?? "" : "",
			className = scriptType != null ? scriptType.Name : "",
			fullName = scriptType != null ? scriptType.FullName ?? "" : "",
			baseTypeName = scriptType != null && scriptType.BaseType != null ? scriptType.BaseType.Name : "",
			isComponent = scriptType != null && typeof(Component).IsAssignableFrom(scriptType)
		};

		List<string> commentBuffer = new List<string>();
		for (int i = 0; i < lines.Length; i++)
		{
			string trimmed = lines[i].Trim();
			if (trimmed.StartsWith("///", StringComparison.Ordinal))
			{
				commentBuffer.Add(trimmed.Substring(3).Trim().Trim('<', '>', '/', ' '));
				continue;
			}

			if (trimmed.StartsWith("//", StringComparison.Ordinal))
			{
				commentBuffer.Add(trimmed.Substring(2).Trim());
				continue;
			}

			if (string.IsNullOrWhiteSpace(trimmed))
			{
				continue;
			}

			if (trimmed.StartsWith("namespace ", StringComparison.Ordinal))
			{
				info.namespaceName = trimmed.Substring("namespace ".Length).Trim().Trim('{', ' ');
				if (string.IsNullOrWhiteSpace(info.fullName) && !string.IsNullOrWhiteSpace(info.className))
				{
					info.fullName = info.namespaceName + "." + info.className;
				}
				commentBuffer.Clear();
				continue;
			}

			if (trimmed.Contains(" class "))
			{
				info.summary = JoinCommentBuffer(commentBuffer);
				string classLine = trimmed.Replace("{", " ");
				int classIndex = classLine.IndexOf(" class ", StringComparison.Ordinal);
				string afterClass = classLine.Substring(classIndex + " class ".Length).Trim();
				string[] classParts = afterClass.Split(new[] { ':', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (classParts.Length > 0)
				{
					info.className = classParts[0];
				}

				int colonIndex = afterClass.IndexOf(':');
				if (colonIndex >= 0)
				{
					string basePart = afterClass.Substring(colonIndex + 1).Trim();
					string[] bases = basePart.Split(new[] { ',', ' ', '\t', '{' }, StringSplitOptions.RemoveEmptyEntries);
					if (bases.Length > 0)
					{
						info.baseTypeName = bases[0];
					}
				}

				if (string.IsNullOrWhiteSpace(info.fullName))
				{
					info.fullName = string.IsNullOrWhiteSpace(info.namespaceName) ? info.className : info.namespaceName + "." + info.className;
				}
				commentBuffer.Clear();
				continue;
			}

			if (trimmed.Contains(" event ") && trimmed.EndsWith(";"))
			{
				info.events.Add(ParseSourceEvent(trimmed, JoinCommentBuffer(commentBuffer)));
				commentBuffer.Clear();
				continue;
			}

			if (LooksLikeMethodSignature(trimmed))
			{
				info.methods.Add(ParseSourceMethod(trimmed, JoinCommentBuffer(commentBuffer)));
				commentBuffer.Clear();
				continue;
			}

			if (LooksLikeFieldDeclaration(trimmed))
			{
				info.fields.Add(ParseSourceField(trimmed, JoinCommentBuffer(commentBuffer)));
				commentBuffer.Clear();
				continue;
			}

			commentBuffer.Clear();
		}

		if (!info.isComponent && string.Equals(info.baseTypeName, "MonoBehaviour", StringComparison.Ordinal))
		{
			info.isComponent = true;
		}

		return info;
	}

	private SourceScriptInfo TryParseSourceWithRoslyn(string[] lines, string assetPath, Type scriptType)
	{
		if (!TryEnsureRoslynAssemblies())
		{
			return null;
		}

		try
		{
			Type syntaxTreeType = RoslynCSharpAssembly.GetType("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree");
			if (syntaxTreeType == null)
			{
				return null;
			}

			string sourceText = string.Join("\n", lines);
			MethodInfo parseTextMethod = syntaxTreeType
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(method => method.Name == "ParseText" && method.GetParameters().Length >= 1 && method.GetParameters()[0].ParameterType == typeof(string));
			if (parseTextMethod == null)
			{
				return null;
			}

			object syntaxTree = parseTextMethod.Invoke(null, new object[] { sourceText, null, assetPath, System.Text.Encoding.UTF8, null });
			if (syntaxTree == null)
			{
				return null;
			}

			MethodInfo getRootMethod = syntaxTree.GetType().GetMethod("GetRoot", Type.EmptyTypes);
			object root = getRootMethod != null ? getRootMethod.Invoke(syntaxTree, null) : null;
			if (root == null)
			{
				return null;
			}

			SourceScriptInfo info = new SourceScriptInfo
			{
				assetPath = assetPath,
				namespaceName = scriptType != null ? scriptType.Namespace ?? "" : "",
				className = scriptType != null ? scriptType.Name : "",
				fullName = scriptType != null ? scriptType.FullName ?? "" : "",
				baseTypeName = scriptType != null && scriptType.BaseType != null ? scriptType.BaseType.Name : "",
				isComponent = scriptType != null && typeof(Component).IsAssignableFrom(scriptType)
			};

			PopulateRoslynSourceInfo(root, info, scriptType);
			return info;
		}
		catch
		{
			return null;
		}
	}

	private void PopulateRoslynSourceInfo(object root, SourceScriptInfo info, Type scriptType)
	{
		IEnumerable<object> descendants = EnumerateRoslynNodes(root);
		object targetClass = null;
		string targetFullName = scriptType != null ? scriptType.FullName ?? "" : "";

		foreach (object node in descendants)
		{
			string nodeTypeName = node.GetType().Name;
			if (nodeTypeName == "FileScopedNamespaceDeclarationSyntax" || nodeTypeName == "NamespaceDeclarationSyntax")
			{
				if (string.IsNullOrWhiteSpace(info.namespaceName))
				{
					info.namespaceName = GetRoslynPropertyString(node, "Name");
				}
				continue;
			}

			if (nodeTypeName != "ClassDeclarationSyntax")
			{
				continue;
			}

			string className = GetRoslynPropertyString(node, "Identifier");
			string namespaceName = FindRoslynNamespace(node);
			string fullName = string.IsNullOrWhiteSpace(namespaceName) ? className : namespaceName + "." + className;
			if (!string.IsNullOrWhiteSpace(targetFullName))
			{
				if (!string.Equals(fullName, targetFullName, StringComparison.Ordinal))
				{
					continue;
				}
			}
			else if (!string.IsNullOrWhiteSpace(info.className) && !string.Equals(className, info.className, StringComparison.Ordinal))
			{
				continue;
			}

			targetClass = node;
			info.className = className;
			info.namespaceName = namespaceName;
			info.fullName = fullName;
			info.summary = GetRoslynLeadingComment(node);
			info.baseTypeName = GetRoslynBaseTypeName(node);
			info.isComponent = info.isComponent || string.Equals(info.baseTypeName, "MonoBehaviour", StringComparison.Ordinal) || string.Equals(info.baseTypeName, "ScriptableObject", StringComparison.Ordinal);
			break;
		}

		if (targetClass == null)
		{
			return;
		}

		object members = GetRoslynPropertyValue(targetClass, "Members");
		foreach (object member in EnumerateRoslynList(members))
		{
			string memberTypeName = member.GetType().Name;
			if (memberTypeName == "MethodDeclarationSyntax")
			{
				info.methods.Add(ParseRoslynMethod(member));
			}
			else if (memberTypeName == "FieldDeclarationSyntax")
			{
				info.fields.AddRange(ParseRoslynFields(member));
			}
			else if (memberTypeName == "EventFieldDeclarationSyntax")
			{
				info.events.AddRange(ParseRoslynEventFields(member));
			}
			else if (memberTypeName == "EventDeclarationSyntax")
			{
				SourceEventInfo eventInfo = ParseRoslynEvent(member);
				if (eventInfo != null)
				{
					info.events.Add(eventInfo);
				}
			}
		}
	}

	private SourceMethodInfo ParseRoslynMethod(object methodNode)
	{
		SourceMethodInfo method = new SourceMethodInfo
		{
			name = GetRoslynPropertyString(methodNode, "Identifier"),
			returnType = GetRoslynPropertyString(methodNode, "ReturnType"),
			summary = GetRoslynLeadingComment(methodNode),
			isStatic = RoslynHasModifier(methodNode, "static")
		};

		object parameterList = GetRoslynPropertyValue(methodNode, "ParameterList");
		object parameters = GetRoslynPropertyValue(parameterList, "Parameters");
		foreach (object parameterNode in EnumerateRoslynList(parameters))
		{
			method.parameters.Add(new CapabilityMethodParameterInfo
			{
				name = GetRoslynPropertyString(parameterNode, "Identifier"),
				type = GetRoslynPropertyString(parameterNode, "Type"),
				description = "",
				required = GetRoslynPropertyValue(parameterNode, "Default") == null
			});
		}

		return method;
	}

	private List<SourceFieldInfo> ParseRoslynFields(object fieldNode)
	{
		List<SourceFieldInfo> fields = new List<SourceFieldInfo>();
		string typeName = GetRoslynPropertyString(GetRoslynPropertyValue(fieldNode, "Declaration"), "Type");
		string summary = GetRoslynLeadingComment(fieldNode);
		bool serialized = RoslynHasModifier(fieldNode, "public") || RoslynHasAttribute(fieldNode, "SerializeField");
		object variables = GetRoslynPropertyValue(GetRoslynPropertyValue(fieldNode, "Declaration"), "Variables");
		foreach (object variableNode in EnumerateRoslynList(variables))
		{
			fields.Add(new SourceFieldInfo
			{
				name = GetRoslynPropertyString(variableNode, "Identifier"),
				type = typeName,
				summary = summary,
				required = false,
				serialized = serialized
			});
		}

		return fields;
	}

	private List<SourceEventInfo> ParseRoslynEventFields(object eventNode)
	{
		List<SourceEventInfo> events = new List<SourceEventInfo>();
		string payloadType = GetRoslynPropertyString(GetRoslynPropertyValue(eventNode, "Declaration"), "Type");
		string summary = GetRoslynLeadingComment(eventNode);
		object variables = GetRoslynPropertyValue(GetRoslynPropertyValue(eventNode, "Declaration"), "Variables");
		foreach (object variableNode in EnumerateRoslynList(variables))
		{
			events.Add(new SourceEventInfo
			{
				name = GetRoslynPropertyString(variableNode, "Identifier"),
				payloadType = payloadType,
				summary = summary
			});
		}

		return events;
	}

	private SourceEventInfo ParseRoslynEvent(object eventNode)
	{
		return new SourceEventInfo
		{
			name = GetRoslynPropertyString(eventNode, "Identifier"),
			payloadType = GetRoslynPropertyString(eventNode, "Type"),
			summary = GetRoslynLeadingComment(eventNode)
		};
	}

	private bool TryEnsureRoslynAssemblies()
	{
		if (RoslynLoadAttempted)
		{
			return RoslynAssembly != null && RoslynCSharpAssembly != null;
		}

		RoslynLoadAttempted = true;
		RoslynAssembly = FindOrLoadAssembly("Microsoft.CodeAnalysis");
		RoslynCSharpAssembly = FindOrLoadAssembly("Microsoft.CodeAnalysis.CSharp");
		return RoslynAssembly != null && RoslynCSharpAssembly != null;
	}

	private static Assembly FindOrLoadAssembly(string assemblyName)
	{
		Assembly loaded = AppDomain.CurrentDomain.GetAssemblies()
			.FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));
		if (loaded != null)
		{
			return loaded;
		}

		try
		{
			return Assembly.Load(assemblyName);
		}
		catch
		{
			return null;
		}
	}

	private IEnumerable<object> EnumerateRoslynNodes(object root)
	{
		if (root == null)
		{
			yield break;
		}

		yield return root;
		MethodInfo descendantsMethod = root.GetType().GetMethod("DescendantNodes", Type.EmptyTypes);
		if (descendantsMethod == null)
		{
			yield break;
		}

		foreach (object node in EnumerateRoslynList(descendantsMethod.Invoke(root, null)))
		{
			yield return node;
		}
	}

	private IEnumerable<object> EnumerateRoslynList(object enumerable)
	{
		if (!(enumerable is System.Collections.IEnumerable sequence))
		{
			yield break;
		}

		foreach (object item in sequence)
		{
			yield return item;
		}
	}

	private object GetRoslynPropertyValue(object target, string propertyName)
	{
		return target == null ? null : target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target, null);
	}

	private string GetRoslynPropertyString(object target, string propertyName)
	{
		object value = GetRoslynPropertyValue(target, propertyName);
		return value != null ? value.ToString() : "";
	}

	private string FindRoslynNamespace(object node)
	{
		object current = node;
		while (current != null)
		{
			string currentTypeName = current.GetType().Name;
			if (currentTypeName == "NamespaceDeclarationSyntax" || currentTypeName == "FileScopedNamespaceDeclarationSyntax")
			{
				return GetRoslynPropertyString(current, "Name");
			}

			current = GetRoslynPropertyValue(current, "Parent");
		}

		return "";
	}

	private string GetRoslynLeadingComment(object node)
	{
		if (node == null)
		{
			return "";
		}

		MethodInfo leadingTriviaMethod = node.GetType().GetMethod("GetLeadingTrivia", Type.EmptyTypes);
		object triviaList = leadingTriviaMethod != null ? leadingTriviaMethod.Invoke(node, null) : null;
		if (triviaList == null)
		{
			return "";
		}

		MethodInfo toFullStringMethod = triviaList.GetType().GetMethod("ToFullString", Type.EmptyTypes);
		string raw = toFullStringMethod != null ? toFullStringMethod.Invoke(triviaList, null) as string : triviaList.ToString();
		return CleanCommentText(raw);
	}

	private string GetRoslynBaseTypeName(object classNode)
	{
		object baseList = GetRoslynPropertyValue(classNode, "BaseList");
		object types = GetRoslynPropertyValue(baseList, "Types");
		object firstBase = EnumerateRoslynList(types).FirstOrDefault();
		if (firstBase == null)
		{
			return "";
		}

		string typeName = GetRoslynPropertyString(firstBase, "Type");
		int lastDot = typeName.LastIndexOf('.');
		return lastDot >= 0 ? typeName.Substring(lastDot + 1) : typeName;
	}

	private bool RoslynHasModifier(object node, string modifierText)
	{
		object modifiers = GetRoslynPropertyValue(node, "Modifiers");
		return EnumerateRoslynList(modifiers).Any(modifier =>
			string.Equals(modifier.ToString(), modifierText, StringComparison.OrdinalIgnoreCase));
	}

	private bool RoslynHasAttribute(object node, string attributeName)
	{
		object attributeLists = GetRoslynPropertyValue(node, "AttributeLists");
		foreach (object attributeList in EnumerateRoslynList(attributeLists))
		{
			object attributes = GetRoslynPropertyValue(attributeList, "Attributes");
			foreach (object attribute in EnumerateRoslynList(attributes))
			{
				string name = GetRoslynPropertyString(attribute, "Name");
				if (string.Equals(name, attributeName, StringComparison.Ordinal) || string.Equals(name, attributeName + "Attribute", StringComparison.Ordinal))
				{
					return true;
				}
			}
		}

		return false;
	}

	private string CleanCommentText(string rawComment)
	{
		if (string.IsNullOrWhiteSpace(rawComment))
		{
			return "";
		}

		StringBuilder builder = new StringBuilder();
		foreach (string rawLine in rawComment.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string line = rawLine.Trim();
			if (line.StartsWith("///", StringComparison.Ordinal))
			{
				line = line.Substring(3).Trim();
			}
			else if (line.StartsWith("//", StringComparison.Ordinal))
			{
				line = line.Substring(2).Trim();
			}

			line = line.Replace("<summary>", "").Replace("</summary>", "").Trim();
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			if (builder.Length > 0)
			{
				builder.Append(' ');
			}

			builder.Append(line);
		}

		return builder.ToString().Trim();
	}

	private SourceMethodInfo ParseSourceMethod(string line, string summary)
	{
		SourceMethodInfo method = new SourceMethodInfo
		{
			summary = summary
		};

		int parenIndex = line.IndexOf('(');
		int closeParenIndex = line.LastIndexOf(')');
		string beforeParen = parenIndex > 0 ? line.Substring(0, parenIndex).Trim() : line;
		string parameterBlock = parenIndex >= 0 && closeParenIndex > parenIndex ? line.Substring(parenIndex + 1, closeParenIndex - parenIndex - 1) : "";
		string[] beforeParts = beforeParen.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		if (beforeParts.Length >= 2)
		{
			method.name = beforeParts[beforeParts.Length - 1];
			method.returnType = beforeParts[beforeParts.Length - 2];
			method.isStatic = beforeParts.Any(part => string.Equals(part, "static", StringComparison.Ordinal));
		}

		foreach (string parameter in parameterBlock.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string cleaned = parameter.Trim();
			if (string.IsNullOrWhiteSpace(cleaned))
			{
				continue;
			}

			string[] parameterParts = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parameterParts.Length >= 2)
			{
				method.parameters.Add(new CapabilityMethodParameterInfo
				{
					name = parameterParts[parameterParts.Length - 1],
					type = parameterParts[parameterParts.Length - 2],
					description = "",
					required = !cleaned.Contains("=")
				});
			}
		}

		return method;
	}

	private SourceFieldInfo ParseSourceField(string line, string summary)
	{
		SourceFieldInfo field = new SourceFieldInfo
		{
			summary = summary,
			serialized = line.Contains("public ") || line.Contains("[SerializeField]")
		};

		string cleaned = line.Replace(";", "").Replace("=", " = ");
		string[] parts = cleaned.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length >= 2)
		{
			field.name = parts[parts.Length - 1] == "=" && parts.Length >= 3 ? parts[parts.Length - 2] : parts[parts.Length - 1];
			int nameIndex = Array.IndexOf(parts, field.name);
			if (nameIndex > 0)
			{
				field.type = parts[nameIndex - 1];
			}
		}

		return field;
	}

	private SourceEventInfo ParseSourceEvent(string line, string summary)
	{
		SourceEventInfo eventInfo = new SourceEventInfo
		{
			summary = summary
		};

		string cleaned = line.Replace(";", "");
		string[] parts = cleaned.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		int eventIndex = Array.IndexOf(parts, "event");
		if (eventIndex >= 0 && parts.Length > eventIndex + 2)
		{
			eventInfo.payloadType = parts[eventIndex + 1];
			eventInfo.name = parts[eventIndex + 2];
		}

		return eventInfo;
	}

	private bool LooksLikeMethodSignature(string line)
	{
		if (!line.Contains("(") || !line.Contains(")") || line.StartsWith("if ", StringComparison.Ordinal) || line.StartsWith("for ", StringComparison.Ordinal) || line.StartsWith("while ", StringComparison.Ordinal) || line.StartsWith("switch ", StringComparison.Ordinal))
		{
			return false;
		}

		return line.Contains("public ") || line.Contains("private ") || line.Contains("protected ") || line.Contains("internal ");
	}

	private bool LooksLikeFieldDeclaration(string line)
	{
		if (!line.EndsWith(";") || line.Contains("(") || line.IndexOf(" event ", StringComparison.Ordinal) >= 0)
		{
			return false;
		}

		return line.Contains("public ") || line.Contains("[SerializeField]") || line.Contains("private ") || line.Contains("protected ");
	}

	private bool IsUnityLifecycleEventName(string name)
	{
		return !string.IsNullOrWhiteSpace(name) && Array.IndexOf(UnityCallbackNames, name) >= 0;
	}

	private bool IsEventFieldType(Type fieldType)
	{
		if (fieldType == null)
		{
			return false;
		}

		if (typeof(Delegate).IsAssignableFrom(fieldType))
		{
			return true;
		}

		Type current = fieldType;
		while (current != null)
		{
			string fullName = current.FullName ?? current.Name;
			if (string.Equals(fullName, "UnityEngine.Events.UnityEventBase", StringComparison.Ordinal) ||
				string.Equals(fullName, "UnityEngine.Events.UnityEvent", StringComparison.Ordinal) ||
				string.Equals(fullName, "UnityEngine.Events.UnityAction", StringComparison.Ordinal))
			{
				return true;
			}

			current = current.BaseType;
		}

		return false;
	}

	private bool IsSourceEventField(SourceFieldInfo field)
	{
		if (field == null || string.IsNullOrWhiteSpace(field.type))
		{
			return false;
		}

		string typeName = field.type.Trim();
		return typeName.IndexOf("UnityAction", StringComparison.Ordinal) >= 0 ||
			typeName.IndexOf("UnityEvent", StringComparison.Ordinal) >= 0 ||
			typeName.IndexOf("System.Action", StringComparison.Ordinal) >= 0 ||
			string.Equals(typeName, "Action", StringComparison.Ordinal) ||
			typeName.IndexOf("Action<", StringComparison.Ordinal) >= 0 ||
			typeName.IndexOf("Func<", StringComparison.Ordinal) >= 0;
	}

	private string JoinCommentBuffer(List<string> commentBuffer)
	{
		if (commentBuffer == null || commentBuffer.Count == 0)
		{
			return "";
		}

		string joined = string.Join(" ", commentBuffer.Where(line => !string.IsNullOrWhiteSpace(line)));
		commentBuffer.Clear();
		return joined.Replace("summary", "").Trim();
	}

	private List<SourceScriptInfo> GetRelevantSourceComponents(List<SourceScriptInfo> sourceScripts, HashSet<string> relevantNamespaceRoots)
	{
		List<SourceScriptInfo> filtered = sourceScripts
			.Where(sourceInfo => sourceInfo != null && sourceInfo.isComponent)
			.Where(sourceInfo =>
				string.IsNullOrWhiteSpace(sourceInfo.namespaceName) ||
				!IsUnityRelatedNamespace(sourceInfo.namespaceName))
			.OrderBy(sourceInfo => sourceInfo.fullName)
			.ToList();

		List<SourceScriptInfo> excluded = sourceScripts
			.Where(sourceInfo => sourceInfo != null && sourceInfo.isComponent)
			.Except(filtered)
			.OrderBy(sourceInfo => sourceInfo.fullName)
			.Take(20)
			.ToList();

		LogCapabilityDebug("Filtering source components by namespace roots kept={0}, excluded={1}. Excluded sample=[{2}]",
			filtered.Count,
			sourceScripts.Count(sourceInfo => sourceInfo != null && sourceInfo.isComponent) - filtered.Count,
			string.Join(", ", excluded.Select(sourceInfo => sourceInfo.fullName).ToArray()));

		return filtered.Count > 0 ? filtered : sourceScripts.Where(sourceInfo => sourceInfo != null && sourceInfo.isComponent).OrderBy(sourceInfo => sourceInfo.fullName).ToList();
	}

	private HashSet<string> GetRelevantNamespaceRoots(List<SourceScriptInfo> sourceScripts)
	{
		HashSet<string> roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (SourceScriptInfo sourceInfo in sourceScripts)
		{
			if (!string.IsNullOrWhiteSpace(sourceInfo.namespaceName) && !IsUnityRelatedNamespace(sourceInfo.namespaceName))
			{
				roots.Add(GetNamespaceRoot(sourceInfo.namespaceName));
			}
		}

		LogCapabilityDebug("Computed namespace roots from controller/prefabs: [{0}]",
			string.Join(", ", roots.OrderBy(value => value).ToArray()));

		return roots;
	}

	private bool IsUnityRelatedNamespace(string namespaceName)
	{
		if (string.IsNullOrWhiteSpace(namespaceName))
		{
			return false;
		}

		return namespaceName.StartsWith("Unity", StringComparison.Ordinal) ||
			namespaceName.StartsWith("UnityEngine", StringComparison.Ordinal) ||
			namespaceName.StartsWith("UnityEditor", StringComparison.Ordinal) ||
			namespaceName.StartsWith("TMPro", StringComparison.Ordinal) ||
			namespaceName.StartsWith("System", StringComparison.Ordinal) ||
			namespaceName.StartsWith("Microsoft", StringComparison.Ordinal);
	}

	private void LogCapabilityDebug(string format, params object[] args)
	{
		if (!capabilityDebugLogging)
		{
			return;
		}

		Debug.Log("[ModuleCapabilities] " + string.Format(format, args));
	}

	private List<string> BuildAllowedFeatures(SourceScriptInfo sourceInfo)
	{
		HashSet<string> features = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"component:" + sourceInfo.className
		};

		foreach (SourceMethodInfo method in sourceInfo.methods)
		{
			if (Array.IndexOf(UnityCallbackNames, method.name) >= 0)
			{
				features.Add("callback:" + method.name);
			}
		}

		foreach (SourceFieldInfo field in sourceInfo.fields.Where(field => field.serialized && !IsSourceEventField(field)))
		{
			features.Add("serialized-field:" + sourceInfo.className + "." + field.name);
		}

		return features.OrderBy(value => value).ToList();
	}

	private string GetSourceFieldSummary(SourceScriptInfo sourceInfo, string fieldName)
	{
		if (sourceInfo == null)
		{
			return "Serialized field";
		}

		SourceFieldInfo field = sourceInfo.fields.FirstOrDefault(entry => string.Equals(entry.name, fieldName, StringComparison.Ordinal));
		return field != null && !string.IsNullOrWhiteSpace(field.summary) ? field.summary : "Serialized field";
	}

	private List<CapabilityMethodParameterInfo> BuildMethodParameters(MethodInfo method, SourceMethodInfo sourceMethod)
	{
		ParameterInfo[] reflected = method.GetParameters();
		List<CapabilityMethodParameterInfo> parameters = new List<CapabilityMethodParameterInfo>();
		for (int index = 0; index < reflected.Length; index++)
		{
			ParameterInfo parameter = reflected[index];
			CapabilityMethodParameterInfo sourceParameter = sourceMethod != null && sourceMethod.parameters.Count > index ? sourceMethod.parameters[index] : null;
			parameters.Add(new CapabilityMethodParameterInfo
			{
				name = parameter.Name,
				type = GetFriendlyTypeName(parameter.ParameterType),
				description = sourceParameter != null ? sourceParameter.description : "",
				required = !parameter.IsOptional
			});
		}

		return parameters;
	}

	private List<CapabilityMethodParameterInfo> CloneMethodParameterList(List<CapabilityMethodParameterInfo> parameters)
	{
		return parameters == null
			? new List<CapabilityMethodParameterInfo>()
			: parameters.Select(parameter => new CapabilityMethodParameterInfo
			{
				name = parameter.name,
				type = parameter.type,
				description = parameter.description,
				required = parameter.required
			}).ToList();
	}

	private Dictionary<string, T> BuildSourceLookup<T>(IEnumerable<T> entries, Func<T, string> keySelector)
	{
		Dictionary<string, T> map = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
		if (entries == null || keySelector == null)
		{
			return map;
		}

		foreach (T entry in entries)
		{
			if (entry == null)
			{
				continue;
			}

			string key = keySelector(entry);
			if (string.IsNullOrWhiteSpace(key))
			{
				continue;
			}

			key = key.Trim();
			if (!map.ContainsKey(key))
			{
				map[key] = entry;
			}
		}

		return map;
	}

	private string GetBaseTypeLabel(string baseTypeName)
	{
		if (string.Equals(baseTypeName, "MonoBehaviour", StringComparison.Ordinal))
		{
			return "MonoBehaviour";
		}

		if (string.Equals(baseTypeName, "ScriptableObject", StringComparison.Ordinal))
		{
			return "ScriptableObject";
		}

		return string.IsNullOrWhiteSpace(baseTypeName) ? "PlainClass" : baseTypeName;
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
		capabilities.supportedFeatures = new List<CapabilityFeatureInfo>();
		capabilities.constraints = new List<CapabilityConstraintInfo>();
		capabilities.unity.components = NormalizeComponents(capabilities.unity.components);
		capabilities.unity.systems = new List<UnityCapabilitySystemInfo>();
		capabilities.unity.gameObjectRoles = new List<UnityCapabilityGameObjectRoleInfo>();
		capabilities.unity.behaviorShapes = new List<UnityCapabilityBehaviorShapeInfo>();
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
			(capabilities.unity != null &&
				(capabilities.unity.components != null && capabilities.unity.components.Count > 0));
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
