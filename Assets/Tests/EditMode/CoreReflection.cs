using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

internal static class CoreReflection
{
    public static Type RequiredType(string typeName)
    {
        Type type = Type.GetType(typeName + ", Assembly-CSharp")
            ?? AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "Assembly-CSharp")
                ?.GetType(typeName);

        Assert.That(type, Is.Not.Null, "Expected " + typeName + " to exist in Assembly-CSharp.");
        return type;
    }

    public static object EnumValue(Type enumType, string name)
    {
        return Enum.Parse(enumType, name);
    }

    public static object New(Type type, params object[] args)
    {
        return Activator.CreateInstance(type, args);
    }

    public static T GetProperty<T>(object target, string propertyName)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName);
        Assert.That(property, Is.Not.Null, "Missing property " + propertyName + ".");
        return (T)property.GetValue(target);
    }

    public static T GetField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName);
        Assert.That(field, Is.Not.Null, "Missing field " + fieldName + ".");
        return (T)field.GetValue(target);
    }

    public static object Call(object target, string methodName, params object[] args)
    {
        MethodInfo method = FindMethod(target.GetType(), methodName, BindingFlags.Public | BindingFlags.Instance, args);
        return Invoke(method, target, args);
    }

    public static object CallStatic(Type type, string methodName, params object[] args)
    {
        MethodInfo method = FindMethod(type, methodName, BindingFlags.Public | BindingFlags.Static, args);
        return Invoke(method, null, args);
    }

    private static MethodInfo FindMethod(Type type, string methodName, BindingFlags flags, object[] args)
    {
        MethodInfo[] matches = type.GetMethods(flags)
            .Where(method => method.Name == methodName && ParametersMatch(method.GetParameters(), args))
            .ToArray();

        Assert.That(
            matches,
            Has.Length.EqualTo(1),
            "Expected one method named " + methodName + " on " + type.Name + " for the supplied arguments, but found " + matches.Length + ".");

        return matches[0];
    }

    private static bool ParametersMatch(ParameterInfo[] parameters, object[] args)
    {
        if (parameters.Length != args.Length)
        {
            return false;
        }

        for (int i = 0; i < parameters.Length; i += 1)
        {
            if (!ParameterMatches(parameters[i], args[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParameterMatches(ParameterInfo parameter, object arg)
    {
        Type parameterType = parameter.ParameterType;
        if (parameterType.IsByRef)
        {
            parameterType = parameterType.GetElementType();
        }

        if (arg == null)
        {
            return parameter.IsOut
                || !parameterType.IsValueType
                || Nullable.GetUnderlyingType(parameterType) != null;
        }

        Type argType = arg.GetType();
        Type nullableType = Nullable.GetUnderlyingType(parameterType);
        return parameterType.IsAssignableFrom(argType)
            || (nullableType != null && nullableType.IsAssignableFrom(argType));
    }

    private static object Invoke(MethodInfo method, object target, object[] args)
    {
        try
        {
            return method.Invoke(target, args);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }
}
