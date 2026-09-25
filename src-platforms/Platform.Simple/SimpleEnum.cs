#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CodeBrix.Platform.Simple;

public interface ISimpleEnumInfo
{
    string Description { get; }

    // Trimming: not annotated on purpose - an annotated return value on a public property makes every trimmed
    // application that keeps the info type's properties warn (IL2111). SimpleEnumHelper relies on EnumType being
    // typeof(TEnum) of a SimpleEnumInfo<TEnum>, whose TEnum is annotated (see CheckDictionaries).
    Type EnumType { get; }
}

public abstract class SimpleEnumInfo<[DynamicallyAccessedMembers(SimpleEnumHelper.EnumTypeMembers)] TEnum> : ISimpleEnumInfo
    where TEnum : Enum
{
    public TEnum Member { get; }

    protected SimpleEnumInfo(TEnum member)
    {
        if (!Enum.IsDefined(typeof(TEnum), member))
        {
            throw new ArgumentOutOfRangeException(nameof(member),
                $"Not a valid member of {typeof(TEnum).Name}");
        }
        Member = member;
    }

    protected static TInfo FindInfo<[DynamicallyAccessedMembers(SimpleEnumHelper.InfoTypeMembers)] TInfo>(TEnum member)
        where TInfo : class, ISimpleEnumInfo =>
        SimpleEnumHelper.FindMemberInfo<TEnum, TInfo>(member);

    protected static Dictionary<TEnum, TInfo> GetDictionary<[DynamicallyAccessedMembers(SimpleEnumHelper.InfoTypeMembers)] TInfo>()
        where TInfo : class, ISimpleEnumInfo =>
        SimpleEnumHelper.GetInfoDictionary<TEnum, TInfo>();

    #region | ISimpleEnumInfo implementation |

    public string Description { get; protected set; }
    public Type EnumType => typeof(TEnum);

    #endregion
}

public interface ISimpleEnumInfoAttribute
{
    // Trimming: SimpleEnumHelper reads the info type's static properties by reflection.
    [DynamicallyAccessedMembers(SimpleEnumHelper.InfoTypeMembers)]
    Type InfoType { get; }
    string InfoMemberName { get; }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class SimpleEnumAttribute<[DynamicallyAccessedMembers(SimpleEnumHelper.InfoTypeMembers)] TInfo> : Attribute, ISimpleEnumInfoAttribute
    where TInfo : class, ISimpleEnumInfo
{
    public SimpleEnumAttribute(string infoMemberName) =>
        InfoMemberName = (string.IsNullOrWhiteSpace(infoMemberName))
            ? null
            : infoMemberName.Trim();

    #region | ISimpleEnumInfoAttribute implementation |

    [DynamicallyAccessedMembers(SimpleEnumHelper.InfoTypeMembers)]
    public Type InfoType => typeof(TInfo);
    public string InfoMemberName { get; }

    #endregion
}

public static class SimpleEnumHelper
{
    /// <summary>
    /// What trimming must keep of an enum type: every public member, because its members are looked up by name with
    /// Type.GetMember (for their SimpleEnumAttribute).
    /// </summary>
    internal const DynamicallyAccessedMemberTypes EnumTypeMembers =
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes
        | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents;

    /// <summary>What trimming must keep of an info type: its public (static) properties, the enum's info instances.</summary>
    internal const DynamicallyAccessedMemberTypes InfoTypeMembers = DynamicallyAccessedMemberTypes.PublicProperties;

    // ReSharper disable InconsistentNaming

    private static readonly Lock Locker = new();

    //Item1 = the Enum type
    private static readonly Dictionary<Type, Dictionary<string, object>> EnumDictionary = [];

    //Item1 = the SimpleEnumInfo type
    private static readonly Dictionary<Type, Dictionary<string, object>> InfoDictionary = [];

    // ReSharper restore InconsistentNaming

    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "enumType comes from ISimpleEnumInfo.EnumType only when the caller named no enum type; every info "
            + "instance is a SimpleEnumInfo<TEnum> whose EnumType is typeof(TEnum), and TEnum is annotated with EnumTypeMembers "
            + "on that class, so the enum's members are kept whenever such an info exists.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "enumType comes from ISimpleEnumInfo.EnumType only when the caller named no enum type; every info "
            + "instance is a SimpleEnumInfo<TEnum> whose EnumType is typeof(TEnum), and TEnum is annotated with EnumTypeMembers "
            + "on that class, so the enum's members are kept whenever such an info exists.")]
    private static bool CheckDictionaries(
        [DynamicallyAccessedMembers(EnumTypeMembers)] Type enumType = null,
        [DynamicallyAccessedMembers(InfoTypeMembers)] Type infoType = null)
    {
        var dictionariesExist = false;

        if (infoType != null)
        {
            if (InfoDictionary.ContainsKey(infoType))
            {
                dictionariesExist = true;
            }
        }
        else if (enumType != null)
        {
            if (EnumDictionary.ContainsKey(enumType))
            {
                dictionariesExist = true;
            }
        }

        if (((infoType != null) || (enumType != null)) && (!dictionariesExist))
        {
            lock (Locker)
            {
                do
                {
                    if (infoType != null && InfoDictionary.ContainsKey(infoType)) { break; }
                    if (enumType != null && EnumDictionary.ContainsKey(enumType)) { break; }

                    var dictionary = new Dictionary<string, object>();
                    PropertyInfo[] staticProps = null;

                    if (enumType == null)
                    {
                        //Need to get at least one instance of infoType
                        staticProps = infoType
                            .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                            .Where(w => w.PropertyType == infoType)
                            .ToArray();
                        if (staticProps.Length < 1) { break; }

                        foreach (var prop in staticProps)
                        {
                            if (prop.GetValue(infoType) is ISimpleEnumInfo info)
                            {
                                enumType = info.EnumType;
                                break;
                            }
                        }
                    }
                    if (enumType == null) { break; }

                    // GetValuesAsUnderlyingType + ToObject: the same members in the same order as Enum.GetValues(Type),
                    // without creating an array of the enum type at run time (native AOT safe).
                    foreach (var rawMember in Enum.GetValuesAsUnderlyingType(enumType))
                    {
                        var member = Enum.ToObject(enumType, rawMember);
                        var memberName = member.ToString();
                        if (memberName != null)
                        {
                            object memberInfo = null;
                            // ReSharper disable once ConstantConditionalAccessQualifier
                            var attributes = enumType
                                .GetMember(memberName)
                                .FirstOrDefault(f => f.DeclaringType == enumType)?
                                .GetCustomAttributes(true)?
                                .Where(w => w.GetType().IsAssignableTo(typeof(ISimpleEnumInfoAttribute)))
                                .Select(s => s as ISimpleEnumInfoAttribute)
                                .ToArray() ?? [];

                            if (attributes.Length > 1)
                            {
                                throw new TypeLoadException(
                                    $"The {enumType.Name}.{memberName} enum member cannot have more than one instance of SimpleEnumAttribute assigned to it.");
                            }
                            else if (attributes.Length == 1)
                            {
                                var attrib = attributes[0];
                                infoType ??= attrib.InfoType;

                                if (infoType != null && attrib.InfoType != null && infoType == attrib.InfoType)
                                {
                                    staticProps ??= infoType
                                        .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                        .Where(w => w.PropertyType == infoType)
                                        .ToArray();

                                    if (!string.IsNullOrWhiteSpace(attrib.InfoMemberName))
                                    {
                                        var prop = staticProps.FirstOrDefault(f =>
                                            f.Name.Equals(attrib.InfoMemberName.Trim(),
                                                StringComparison.InvariantCultureIgnoreCase));
                                        if (prop != null)
                                        {
                                            if (prop.GetValue(infoType) is ISimpleEnumInfo info)
                                            {
                                                memberInfo = info;
                                            }
                                        }
                                    }
                                }
                            }
                            dictionary.Add(memberName, memberInfo);
                        }
                    }

                    EnumDictionary.Add(enumType, dictionary);
                    if (infoType != null)
                    {
                        InfoDictionary.Add(infoType, dictionary);
                    }

                    dictionariesExist = true;
                } while (false);
            }
        }

        return dictionariesExist;
    }

    public static TInfo FindMemberInfo<[DynamicallyAccessedMembers(InfoTypeMembers)] TInfo>(string memberName)
        where TInfo : class, ISimpleEnumInfo
    {
        TInfo result = null;

        if (!string.IsNullOrWhiteSpace(memberName))
        {
            var infoType = typeof(TInfo);

            if (CheckDictionaries(null, infoType)
                && InfoDictionary.TryGetValue(infoType, out var dictionary))
            {
                if (dictionary.Any(a => a.Key.Equals(memberName.Trim(),
                        StringComparison.InvariantCultureIgnoreCase)
                    && a.Value != null))
                {
                    var kvp = dictionary.Single(s => s.Key.Equals(memberName.Trim(),
                                                                  StringComparison.InvariantCultureIgnoreCase)
                                                              && s.Value != null);
                    result = (TInfo)kvp.Value;
                }
            }
        }

        return result;
    }

    public static TInfo FindMemberInfo<[DynamicallyAccessedMembers(EnumTypeMembers)] TEnum, [DynamicallyAccessedMembers(InfoTypeMembers)] TInfo>(TEnum member)
        where TInfo : class, ISimpleEnumInfo
        where TEnum : Enum
    {
        TInfo result = null;
        var enumType = typeof(TEnum);

        if (Enum.IsDefined(enumType, member))
        {
            var infoType = typeof(TInfo);

            if (CheckDictionaries(null, infoType)
                && InfoDictionary.TryGetValue(infoType, out var dictionary))
            {
                if (dictionary.Any(a => a.Key.Equals(member.ToString(),
                                            StringComparison.InvariantCultureIgnoreCase)
                                        && a.Value != null))
                {
                    var kvp = dictionary.Single(s => s.Key.Equals(member.ToString(),
                                                         StringComparison.InvariantCultureIgnoreCase)
                                                     && s.Value != null);
                    var info = (TInfo)kvp.Value;
                    if (((ISimpleEnumInfo)info).EnumType == enumType)
                    {
                        result = info;
                    }
                }
            }
        }

        return result;
    }

    public static Dictionary<TEnum, TInfo> GetInfoDictionary<[DynamicallyAccessedMembers(EnumTypeMembers)] TEnum, [DynamicallyAccessedMembers(InfoTypeMembers)] TInfo>()
        where TInfo : class, ISimpleEnumInfo
        where TEnum : Enum
    {
        var result = new Dictionary<TEnum, TInfo>();

        var enumType = typeof(TEnum);
        var infoType = typeof(TInfo);

        if (CheckDictionaries(enumType: enumType, infoType: typeof(TInfo))
            && EnumDictionary.TryGetValue(enumType, out var dictionary))
        {
            // Same members and order as Enum.GetValues(Type).Cast<TEnum>(), native AOT safe.
            foreach (var member in Enum.GetValuesAsUnderlyingType(enumType).Cast<object>().Select(raw => (TEnum)Enum.ToObject(enumType, raw)))
            {
                //Members without a SimpleEnumAttribute are stored with a null info value
                //  (see CheckDictionaries), so the null check must come before GetType().
                if (dictionary.Any(a => a.Key == member.ToString()
                                        && a.Value != null
                                        && a.Value.GetType().IsAssignableTo(infoType)))
                {
                    var value = (TInfo)dictionary.Single(s => s.Key == member.ToString()
                                                    && s.Value != null
                                                    && s.Value.GetType().IsAssignableTo(infoType)).Value;
                    result.Add(member, value);
                }
                else
                {
                    result.Add(member, null);
                }
            }
        }

        return result;
    }

    public static IList<TInfo> GetPossibleValues<[DynamicallyAccessedMembers(EnumTypeMembers)] TEnum, [DynamicallyAccessedMembers(InfoTypeMembers)] TInfo>()
        where TInfo : class, ISimpleEnumInfo
        where TEnum : Enum =>
        GetInfoDictionary<TEnum, TInfo>()
            .Select(s => s.Value)
            .Where(w => w != null)
            .Distinct()
            .ToArray();
}
