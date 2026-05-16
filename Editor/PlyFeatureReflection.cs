using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public static class PlyFeatureReflectionScanner
{
    public static PlyFeatureTypeCacheSnapshot ScanProject()
    {
        List<PlyFeatureComponentDescriptor> components = new List<PlyFeatureComponentDescriptor>();
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(assembly => assembly.FullName))
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(type => type != null).ToArray();
            }
            catch
            {
                continue;
            }

            foreach (Type type in types)
            {
                if (type == null || type.IsAbstract || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                bool isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(type);
                bool isScriptableObject = typeof(ScriptableObject).IsAssignableFrom(type);
                if (!isMonoBehaviour && !isScriptableObject)
                {
                    continue;
                }

                components.Add(BuildDescriptor(type, isMonoBehaviour, isScriptableObject));
            }
        }

        return new PlyFeatureTypeCacheSnapshot
        {
            generatedAtUtc = DateTime.UtcNow.ToString("o"),
            components = components
                .OrderBy(component => component.typeName, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static PlyFeatureComponentDescriptor BuildDescriptor(Type type, bool isMonoBehaviour, bool isScriptableObject)
    {
        PlyFeatureComponentDescriptor descriptor = new PlyFeatureComponentDescriptor
        {
            typeName = type.Name,
            fullName = type.FullName ?? type.Name,
            assemblyQualifiedName = type.AssemblyQualifiedName ?? "",
            namespaceName = type.Namespace ?? "",
            baseTypeName = type.BaseType != null ? type.BaseType.Name : "",
            isMonoBehaviour = isMonoBehaviour,
            isScriptableObject = isScriptableObject,
            members = new List<PlyFeatureMemberDescriptor>()
        };

        BindingFlags instanceAndStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (MethodInfo method in type.GetMethods(instanceAndStatic))
        {
            if (!method.IsPublic || method.IsSpecialName)
            {
                continue;
            }

            descriptor.members.Add(new PlyFeatureMemberDescriptor
            {
                componentTypeName = descriptor.typeName,
                componentAssemblyQualifiedName = descriptor.assemblyQualifiedName,
                memberName = method.Name,
                displayName = BuildMethodDisplayName(method),
                memberKind = PlyFeatureMemberKind.Method,
                dataType = GetMethodPayloadType(method),
                access = PlyFeatureParameterAccess.ReadWrite,
                isStatic = method.IsStatic,
                parameterCount = method.GetParameters().Length,
                isLifecycleMethod = IsLifecycleMethod(method.Name)
            });
        }

        foreach (FieldInfo field in type.GetFields(instanceAndStatic))
        {
            bool isSerializedField = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
            if (!isSerializedField)
            {
                continue;
            }

            if (typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
            {
                descriptor.members.Add(new PlyFeatureMemberDescriptor
                {
                    componentTypeName = descriptor.typeName,
                    componentAssemblyQualifiedName = descriptor.assemblyQualifiedName,
                    memberName = field.Name,
                    displayName = field.Name + " : " + field.FieldType.Name,
                    memberKind = PlyFeatureMemberKind.UnityEvent,
                    dataType = GetUnityEventPayloadType(field.FieldType),
                    access = PlyFeatureParameterAccess.ReadWrite,
                    isStatic = field.IsStatic,
                    parameterCount = GetUnityEventParameterCount(field.FieldType)
                });
                continue;
            }

            descriptor.members.Add(new PlyFeatureMemberDescriptor
            {
                componentTypeName = descriptor.typeName,
                componentAssemblyQualifiedName = descriptor.assemblyQualifiedName,
                memberName = field.Name,
                displayName = field.Name + " : " + field.FieldType.Name,
                memberKind = PlyFeatureMemberKind.Field,
                dataType = MapType(field.FieldType),
                access = field.IsInitOnly ? PlyFeatureParameterAccess.ReadOnly : PlyFeatureParameterAccess.ReadWrite,
                isStatic = field.IsStatic
            });
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            MethodInfo getter = property.GetGetMethod();
            MethodInfo setter = property.GetSetMethod();
            if (getter == null && setter == null)
            {
                continue;
            }

            descriptor.members.Add(new PlyFeatureMemberDescriptor
            {
                componentTypeName = descriptor.typeName,
                componentAssemblyQualifiedName = descriptor.assemblyQualifiedName,
                memberName = property.Name,
                displayName = property.Name + " : " + property.PropertyType.Name,
                memberKind = PlyFeatureMemberKind.Property,
                dataType = MapType(property.PropertyType),
                access = GetPropertyAccess(getter, setter),
                isStatic = (getter != null && getter.IsStatic) || (setter != null && setter.IsStatic)
            });
        }

        foreach (EventInfo eventInfo in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            descriptor.members.Add(new PlyFeatureMemberDescriptor
            {
                componentTypeName = descriptor.typeName,
                componentAssemblyQualifiedName = descriptor.assemblyQualifiedName,
                memberName = eventInfo.Name,
                displayName = eventInfo.Name + " : " + (eventInfo.EventHandlerType != null ? eventInfo.EventHandlerType.Name : "event"),
                memberKind = PlyFeatureMemberKind.CSharpEvent,
                dataType = GetCSharpEventPayloadType(eventInfo.EventHandlerType),
                access = PlyFeatureParameterAccess.ReadOnly,
                isStatic = (eventInfo.AddMethod != null && eventInfo.AddMethod.IsStatic) || (eventInfo.RemoveMethod != null && eventInfo.RemoveMethod.IsStatic),
                parameterCount = GetCSharpEventParameterCount(eventInfo.EventHandlerType)
            });
        }

        descriptor.members = descriptor.members
            .GroupBy(member => member.memberKind + "|" + member.memberName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(member => member.memberKind.ToString())
            .ThenBy(member => member.memberName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return descriptor;
    }

    private static string BuildMethodDisplayName(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        string parameterList = string.Join(", ", parameters.Select(parameter => parameter.ParameterType.Name + " " + parameter.Name).ToArray());
        return method.Name + "(" + parameterList + ")";
    }

    private static PlyFeatureParameterAccess GetPropertyAccess(MethodInfo getter, MethodInfo setter)
    {
        if (getter != null && setter != null)
        {
            return PlyFeatureParameterAccess.ReadWrite;
        }

        return getter != null ? PlyFeatureParameterAccess.ReadOnly : PlyFeatureParameterAccess.WriteOnly;
    }

    private static PlyFeatureDataType GetMethodPayloadType(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return PlyFeatureDataType.Void;
        }

        if (parameters.Length == 1)
        {
            return MapType(parameters[0].ParameterType);
        }

        return PlyFeatureDataType.Any;
    }

    private static PlyFeatureDataType GetUnityEventPayloadType(Type eventType)
    {
        Type current = eventType;
        while (current != null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(UnityEvent<>))
            {
                return MapType(current.GetGenericArguments()[0]);
            }

            current = current.BaseType;
        }

        return PlyFeatureDataType.Void;
    }

    private static PlyFeatureDataType GetCSharpEventPayloadType(Type eventHandlerType)
    {
        if (eventHandlerType == null)
        {
            return PlyFeatureDataType.Void;
        }

        MethodInfo invokeMethod = eventHandlerType.GetMethod("Invoke");
        if (invokeMethod == null)
        {
            return PlyFeatureDataType.Any;
        }

        ParameterInfo[] parameters = invokeMethod.GetParameters();
        if (parameters.Length == 0)
        {
            return PlyFeatureDataType.Void;
        }

        return MapType(parameters[parameters.Length - 1].ParameterType);
    }

    private static int GetUnityEventParameterCount(Type eventType)
    {
        Type current = eventType;
        while (current != null)
        {
            if (current == typeof(UnityEvent))
            {
                return 0;
            }

            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(UnityEvent<>))
            {
                return 1;
            }

            current = current.BaseType;
        }

        return 0;
    }

    private static int GetCSharpEventParameterCount(Type eventHandlerType)
    {
        MethodInfo invokeMethod = eventHandlerType?.GetMethod("Invoke");
        return invokeMethod?.GetParameters().Length ?? 0;
    }

    private static bool IsLifecycleMethod(string methodName)
    {
        switch (methodName)
        {
            case "Awake":
            case "Start":
            case "Update":
            case "LateUpdate":
            case "FixedUpdate":
            case "OnEnable":
            case "OnDisable":
                return true;
            default:
                return false;
        }
    }

    public static PlyFeatureDataType MapType(Type type)
    {
        if (type == null)
        {
            return PlyFeatureDataType.Any;
        }

        if (type == typeof(void))
        {
            return PlyFeatureDataType.Void;
        }

        if (type == typeof(bool))
        {
            return PlyFeatureDataType.Bool;
        }

        if (type == typeof(float) || type == typeof(double))
        {
            return PlyFeatureDataType.Float;
        }

        if (type == typeof(int) || type == typeof(short) || type == typeof(long))
        {
            return PlyFeatureDataType.Int;
        }

        if (type == typeof(string) || type == typeof(char))
        {
            return PlyFeatureDataType.String;
        }

        if (type == typeof(GameObject))
        {
            return PlyFeatureDataType.GameObject;
        }

        if (type == typeof(Vector3))
        {
            return PlyFeatureDataType.Vector3;
        }

        return PlyFeatureDataType.Any;
    }
}

public static class PlyFeatureTypeCache
{
    private static PlyFeatureTypeCacheSnapshot snapshot;
    private static Dictionary<string, PlyFeatureComponentDescriptor> byShortName = new Dictionary<string, PlyFeatureComponentDescriptor>(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, PlyFeatureComponentDescriptor> byAssemblyName = new Dictionary<string, PlyFeatureComponentDescriptor>(StringComparer.OrdinalIgnoreCase);

    public static PlyFeatureTypeCacheSnapshot Snapshot
    {
        get
        {
            if (snapshot == null)
            {
                Refresh();
            }

            return snapshot;
        }
    }

    public static void Refresh()
    {
        snapshot = PlyFeatureReflectionScanner.ScanProject();
        byShortName = new Dictionary<string, PlyFeatureComponentDescriptor>(StringComparer.OrdinalIgnoreCase);
        byAssemblyName = new Dictionary<string, PlyFeatureComponentDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (PlyFeatureComponentDescriptor component in snapshot.components)
        {
            byShortName[component.typeName] = component;
            if (!string.IsNullOrWhiteSpace(component.assemblyQualifiedName))
            {
                byAssemblyName[component.assemblyQualifiedName] = component;
            }
        }
    }

    public static PlyFeatureComponentDescriptor FindComponent(string typeName, string assemblyQualifiedName = "")
    {
        if (!string.IsNullOrWhiteSpace(assemblyQualifiedName) && byAssemblyName.TryGetValue(assemblyQualifiedName, out PlyFeatureComponentDescriptor byAssembly))
        {
            return byAssembly;
        }

        if (!string.IsNullOrWhiteSpace(typeName) && byShortName.TryGetValue(typeName, out PlyFeatureComponentDescriptor byName))
        {
            return byName;
        }

        return null;
    }
}

public static class PlyFeatureComponentRegistry
{
    public static List<PlyFeatureComponentDescriptor> SearchComponents(string searchTerm)
    {
        IEnumerable<PlyFeatureComponentDescriptor> query = PlyFeatureTypeCache.Snapshot.components;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string needle = searchTerm.Trim();
            query = query.Where(component =>
                Contains(component.typeName, needle) ||
                Contains(component.fullName, needle) ||
                Contains(component.namespaceName, needle));
        }

        return query.OrderBy(component => component.typeName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static PlyFeatureComponentDescriptor GetComponent(string typeName, string assemblyQualifiedName = "")
    {
        return PlyFeatureTypeCache.FindComponent(typeName, assemblyQualifiedName);
    }

    public static List<PlyFeatureMemberDescriptor> GetMembers(string componentType, string assemblyQualifiedName = "", params PlyFeatureMemberKind[] memberKinds)
    {
        PlyFeatureComponentDescriptor component = GetComponent(componentType, assemblyQualifiedName);
        if (component == null)
        {
            return new List<PlyFeatureMemberDescriptor>();
        }

        List<PlyFeatureMemberDescriptor> members = component.members ?? new List<PlyFeatureMemberDescriptor>();
        if (memberKinds == null || memberKinds.Length == 0)
        {
            return new List<PlyFeatureMemberDescriptor>(members);
        }

        HashSet<PlyFeatureMemberKind> allowedKinds = new HashSet<PlyFeatureMemberKind>(memberKinds);
        return members.Where(member => allowedKinds.Contains(member.memberKind)).ToList();
    }

    private static bool Contains(string haystack, string needle)
    {
        return !string.IsNullOrWhiteSpace(haystack) &&
            haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
