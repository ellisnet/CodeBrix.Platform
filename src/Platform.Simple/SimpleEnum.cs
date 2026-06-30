using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CodeBrix.Platform.Simple;

public interface ISimpleEnumInfo
{
    string Description { get; }
    Type EnumType { get; }
}

public abstract class SimpleEnumInfo<TEnum> : ISimpleEnumInfo
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

    protected static TInfo FindInfo<TInfo>(TEnum member)
        where TInfo : class, ISimpleEnumInfo =>
        SimpleEnumHelper.FindMemberInfo<TEnum, TInfo>(member);

    protected static Dictionary<TEnum, TInfo> GetDictionary<TInfo>()
        where TInfo : class, ISimpleEnumInfo =>
        SimpleEnumHelper.GetInfoDictionary<TEnum, TInfo>();

    #region | ISimpleEnumInfo implementation |

    public string Description { get; protected set; }
    public Type EnumType => typeof(TEnum);

    #endregion
}

public interface ISimpleEnumInfoAttribute
{
    Type InfoType { get; }
    string InfoMemberName { get; }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class SimpleEnumAttribute<TInfo> : Attribute, ISimpleEnumInfoAttribute
    where TInfo : class, ISimpleEnumInfo
{
    public SimpleEnumAttribute(string infoMemberName) =>
        InfoMemberName = (string.IsNullOrWhiteSpace(infoMemberName))
            ? null
            : infoMemberName.Trim();

    #region | ISimpleEnumInfoAttribute implementation |

    public Type InfoType => typeof(TInfo);
    public string InfoMemberName { get; }

    #endregion
}

public static class SimpleEnumHelper
{
    // ReSharper disable InconsistentNaming

    private static readonly Lock Locker = new();

    //Item1 = the Enum type
    private static readonly Dictionary<Type, Dictionary<string, object>> EnumDictionary = [];

    //Item1 = the SimpleEnumInfo type
    private static readonly Dictionary<Type, Dictionary<string, object>> InfoDictionary = [];

    // ReSharper restore InconsistentNaming

    private static bool CheckDictionaries(Type enumType = null, Type infoType = null)
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

                    foreach (var member in Enum.GetValues(enumType))
                    {
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

    public static TInfo FindMemberInfo<TInfo>(string memberName)
        where TInfo : class, ISimpleEnumInfo
    {
        TInfo result = null;

        if (!string.IsNullOrWhiteSpace(memberName))
        {
            var infoType = typeof(TInfo);

            if (CheckDictionaries(infoType: infoType)
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

    public static TInfo FindMemberInfo<TEnum, TInfo>(TEnum member)
        where TInfo : class, ISimpleEnumInfo
        where TEnum : Enum
    {
        TInfo result = null;
        var enumType = typeof(TEnum);

        if (Enum.IsDefined(enumType, member))
        {
            var infoType = typeof(TInfo);

            if (CheckDictionaries(infoType: infoType)
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

    public static Dictionary<TEnum, TInfo> GetInfoDictionary<TEnum, TInfo>()
        where TInfo : class, ISimpleEnumInfo
        where TEnum : Enum
    {
        var result = new Dictionary<TEnum, TInfo>();

        var enumType = typeof(TEnum);
        var infoType = typeof(TInfo);

        if (CheckDictionaries(enumType: enumType, infoType: typeof(TInfo))
            && EnumDictionary.TryGetValue(enumType, out var dictionary))
        {
            foreach (var member in Enum.GetValues(enumType).Cast<TEnum>())
            {
                if (dictionary.Any(a => a.Key == member.ToString()
                                        && a.Value.GetType().IsAssignableTo(infoType)))
                {
                    var value = (TInfo)dictionary.Single(s => s.Key == member.ToString()
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

    public static IList<TInfo> GetPossibleValues<TEnum, TInfo>()
        where TInfo : class, ISimpleEnumInfo
        where TEnum : Enum =>
        GetInfoDictionary<TEnum, TInfo>()
            .Select(s => s.Value)
            .Where(w => w != null)
            .Distinct()
            .ToArray();
}
