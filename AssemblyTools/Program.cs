using System.Reflection;
using System.Text;

namespace Program;


public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: AssemblyTools.exe <old assembly> <new assembly> (textfile)");
            return;
        }
        
        var pathToGameAssemblies = @"C:\Program Files (x86)\Steam\steamapps\common\Wrestling Empire\Wrestling Empire_Data\Managed";
        if (File.Exists("config.txt"))
        {
            pathToGameAssemblies = File.ReadAllText("config.txt");
        }
        while (pathToGameAssemblies == null || !File.Exists(Path.Combine(pathToGameAssemblies, "Assembly-CSharp.dll")))
        {
            Console.WriteLine("Please enter the path to the game's assemblies:");
            pathToGameAssemblies = Console.ReadLine();
        }
        
        File.WriteAllText("config.txt", pathToGameAssemblies);

        foreach (var file in Directory.GetFiles(pathToGameAssemblies, "*.dll"))
        {
            Assembly.LoadFrom(file);
        }
        
        var assemblyOld = Assembly.LoadFile(args[0]);
        var assemblyNew = Assembly.LoadFile(args[1]);
        var txtfile = args.Length > 2 ? args[2] : null;

        var typesOld = assemblyOld.GetTypes();
        var typesNew = assemblyNew.GetTypes();

        typesOld = typesOld.Where(t => t.Name.All(char.IsLetterOrDigit)).ToArray();
        typesNew = typesNew.Where(t => t.Name.All(char.IsLetterOrDigit)).ToArray();
        
        var mappings = new Dictionary<string, string>();
        var mappingCounts = new Dictionary<KeyValuePair<string, string>, int>();
        
        var typeMappings = new Dictionary<string, string>();
        var typeMappingsTypes = new Dictionary<Type, Type>();
        
        var unmappedOld = new List<string>();
        var unmappedNew = new List<string>();
        
        var mappedValues = new List<string>();

        int current = 0;
        int offset = 0;
        int nonMatches = 0;
        while (current < typesOld.Length)
        {
            var typeOld = typesOld[current];
            var typeNew = typesNew[(current + offset) % typesNew.Length];
            
            if (typeOld.FullName == null)
            {
                current++;
                continue;
            }
            if (typeNew.FullName == null)
            {
                offset++;
                nonMatches++;
                continue;
            }

            if (TypeCompare(typeOld, typeNew))
            {
                if (!mappings.ContainsKey(typeOld.FullName))
                {
                    mappings.Add(typeOld.FullName, typeNew.FullName);
                    typeMappings.Add(typeOld.FullName, typeNew.FullName);
                    typeMappingsTypes.Add(typeOld, typeNew);
                    mappingCounts.Add(new KeyValuePair<string, string>(typeOld.FullName, typeNew.FullName), 1);
                }
                else
                {
                    if (mappings[typeOld.FullName] != typeNew.FullName)
                    {
                        var append = "*";
                        while (mappings.ContainsKey(typeOld.FullName + append) && mappings[typeOld.FullName + append] != typeNew.FullName)
                        {
                            append += "*";
                        }
                        if (!mappings.ContainsKey(typeOld.FullName + append))
                        {
                            mappings.Add(typeOld.FullName + append, typeNew.FullName);
                            mappingCounts.Add(new KeyValuePair<string, string>(typeOld.FullName + append, typeNew.FullName), 1);
                        }
                        else
                        {
                            mappingCounts[new KeyValuePair<string, string>(typeOld.FullName + append, typeNew.FullName)]++;
                        }
                    }
                    else
                    {
                        mappingCounts[new KeyValuePair<string, string>(typeOld.FullName, typeNew.FullName)]++;
                    }
                }
                mappedValues.Add(typeNew.FullName);
                current++;
                nonMatches = 0;
            }
            else
            {
                offset++;
                nonMatches++;
                if (nonMatches > typesNew.Length)
                {
                    current++;
                    nonMatches = 0;
                }
            }
        }

        foreach (var pair in typeMappingsTypes)
        {
            var typeOld = pair.Key;
            var typeNew = pair.Value;
            
            int currentMember = 0;
            int offsetMember = 0;
            int nonMatchesMember = 0;
            while (currentMember < typeOld.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Length)
            {
                var memberOld = typeOld.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)[currentMember];
                var memberNew = typeNew.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)[(currentMember + offsetMember) % typeNew.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Length];
                
                if (memberOld.Name.Length != 11 || !memberOld.Name.All(char.IsUpper))
                {
                    currentMember++;
                    nonMatchesMember = 0;
                    continue;
                }
                if (memberNew.Name.Length != 11 || !memberNew.Name.All(char.IsUpper))
                {
                    offsetMember++;
                    nonMatchesMember++;
                    if (nonMatchesMember > typeNew.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Length)
                    {
                        currentMember++;
                        nonMatchesMember = 0;
                    }
                    continue;
                }
                
                if (MemberCompare(memberOld, memberNew, mappings, out var isMethod))
                {
                    if (!mappings.ContainsKey(memberOld.Name))
                    {
                        mappings.Add(memberOld.Name, memberNew.Name);
                        mappingCounts.Add(new KeyValuePair<string, string>(memberOld.Name, memberNew.Name), 1);
                    }
                    else
                    {
                        if (mappings[memberOld.Name] != memberNew.Name)
                        {
                            var append = "*";
                            while (mappings.ContainsKey(memberOld.Name + append) && mappings[memberOld.Name + append] != memberNew.Name)
                            {
                                append += "*";
                            }
                            if (!mappings.ContainsKey(memberOld.Name + append))
                            {
                                mappings.Add(memberOld.Name + append, memberNew.Name);
                                mappingCounts.Add(new KeyValuePair<string, string>(memberOld.Name + append, memberNew.Name), 1);
                            }
                            else
                            {
                                mappingCounts[new KeyValuePair<string, string>(memberOld.Name + append, memberNew.Name)]++;
                            }
                        }
                        else
                        {
                            mappingCounts[new KeyValuePair<string, string>(memberOld.Name, memberNew.Name)]++;
                        }
                    }
                    mappedValues.Add(memberNew.Name);
                    if (isMethod)
                    {
                        var methodOld = (MethodInfo)memberOld;
                        var methodNew = (MethodInfo)memberNew;
                        
                        var parametersOld = methodOld.GetParameters();
                        var parametersNew = methodNew.GetParameters();
                        for (int i = 0; i < parametersOld.Length; i++)
                        {
                            var parameterOldName = parametersOld[i].Name;
                            var parameterNewName = parametersNew[i].Name;
                            
                            if (!mappings.ContainsKey(parameterOldName))
                            {
                                mappings.Add(parameterOldName, parameterNewName);
                                mappingCounts.Add(new KeyValuePair<string, string>(parameterOldName, parameterNewName), 1);
                            }
                            else
                            {
                                if (mappings[parameterOldName] != parameterNewName)
                                {
                                    var append = "*";
                                    while (mappings.ContainsKey(parameterOldName + append) && mappings[parameterOldName + append] != parameterNewName)
                                    {
                                        append += "*";
                                    }
                                    if (!mappings.ContainsKey(parameterOldName + append))
                                    {
                                        mappings.Add(parameterOldName + append, parameterNewName);
                                        mappingCounts.Add(new KeyValuePair<string, string>(parameterOldName + append, parameterNewName), 1);
                                    }
                                    else
                                    {
                                        mappingCounts[new KeyValuePair<string, string>(parameterOldName + append, parameterNewName)]++;
                                    }
                                }
                                else
                                {
                                    mappingCounts[new KeyValuePair<string, string>(parameterOldName, parameterNewName)]++;
                                }
                            }
                        }
                    }
                    
                    currentMember++;
                    nonMatchesMember = 0;
                }
                else
                {
                    offsetMember++;
                    nonMatchesMember++;
                    if (nonMatchesMember > typeNew.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Length)
                    {
                        currentMember++;
                        nonMatchesMember = 0;
                    }
                }
            }
        }
        
        foreach (var typeOld in typesOld)
        {
            if (typeOld.FullName.Length != 11 || !typeOld.FullName.All(char.IsUpper))
            {
                continue;
            }
            if (typeOld.FullName != null && !mappings.ContainsKey(typeOld.FullName))
            {
                unmappedOld.Add(typeOld.FullName);
            }
            foreach (var memberOld in typeOld.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (memberOld.Name.Length != 11 || !memberOld.Name.All(char.IsUpper))
                {
                    continue;
                }
                if (!mappings.ContainsKey(memberOld.Name))
                {
                    unmappedOld.Add(memberOld.Name);
                }
            }
        }
        
        foreach (var typeNew in typesNew)
        {
            if (typeNew.FullName.Length != 11 || !typeNew.FullName.All(char.IsUpper))
            {
                continue;
            }
            if (typeNew.FullName != null && !mappedValues.Contains(typeNew.FullName))
            {
                unmappedNew.Add(typeNew.FullName);
            }
            foreach (var memberNew in typeNew.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (memberNew.Name.Length != 11 || !memberNew.Name.All(char.IsUpper))
                {
                    continue;
                }
                if (!mappedValues.Contains(memberNew.Name))
                {
                    unmappedNew.Add(memberNew.Name);
                }
            }
        }
        
        foreach (var mapping in mappings)
        {
            Console.WriteLine($"{mapping.Key} -> {mapping.Value} ({mappingCounts[new KeyValuePair<string, string>(mapping.Key, mapping.Value)]})");
        }

        Console.WriteLine($"Mapped {mappings.Count} names out of {mappings.Count + unmappedOld.Count} ({(float) mappings.Count / (mappings.Count + unmappedOld.Count) * 100}%)");
        Console.WriteLine($"Unmapped names in old assembly: {unmappedOld.Count}");
        Console.WriteLine($"Unmapped names in new assembly: {unmappedNew.Count}");

        var csv = new StringBuilder("Count,Type,old,new,CommunityMapping,Notes\r\n");
        foreach (var type in typesNew)
        {
            if (unmappedNew.Contains(type.FullName))
            {
                csv.AppendLine($",,TYPE {type.FullName},,");
            }
            else
            {
                List<string> possibleMappings =
                    mappings.Where(x => x.Value == type.FullName).Select(x => x.Key).ToList();
                int max = 0;
                int total = 0;
                string maxMapping = "";
                foreach (var mapping in possibleMappings)
                {
                    if (mappingCounts[new KeyValuePair<string, string>(mapping, type.FullName)] > max)
                    {
                        max = mappingCounts[new KeyValuePair<string, string>(mapping, type.FullName)];
                        maxMapping = mapping;
                    }

                    total += mappingCounts[new KeyValuePair<string, string>(mapping, type.FullName)];
                }

                if (max != total)
                {
                    csv.AppendLine(
                        $"{max},,TYPE {maxMapping},TYPE {type.FullName},,Ambiguous mapping ({total} total). Other possible mappings: {string.Join(", ", possibleMappings.Where(x => x != maxMapping).Select(x => x.Replace("*", "") + " (" + mappingCounts[new KeyValuePair<string, string>(x, type.FullName)] + ")"))}");
                }
                else
                {
                    csv.AppendLine($"{max},,TYPE {maxMapping},TYPE {type.FullName},,");
                }
            }
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (member.Name.Length != 11 || !member.Name.All(char.IsUpper))
                {
                    continue;
                }
                if (!mappings.ContainsValue(member.Name))
                {
                    csv.AppendLine($"1,{type.FullName},,{member.Name},,Added in new assembly");
                }
                else
                {
                    List<string> possibleMappings =
                        mappings.Where(x => x.Value == member.Name).Select(x => x.Key).ToList();
                    int max = 0;
                    int total = 0;
                    string maxMapping = "";
                    foreach (var mapping in possibleMappings)
                    {
                        if (mappingCounts[new KeyValuePair<string, string>(mapping, member.Name)] > max)
                        {
                            max = mappingCounts[new KeyValuePair<string, string>(mapping, member.Name)];
                            maxMapping = mapping;
                        }

                        total += mappingCounts[new KeyValuePair<string, string>(mapping, member.Name)];
                    }

                    if (max != total)
                    {
                        csv.AppendLine(
                            $"{max},{type.FullName},{maxMapping},{member.Name},,Ambiguous mapping ({total} total). Other possible mappings: {string.Join(", ", possibleMappings.Where(x => x != maxMapping).Select(x => x.Replace("*", "") + " (" + mappingCounts[new KeyValuePair<string, string>(x, member.Name)] + ")"))}");
                    }
                    else
                    {
                        csv.AppendLine($"{max},{type.FullName},{maxMapping},{member.Name},,");
                    }
                }
                foreach (var typeOld in typesOld)
                {
                    if (typeOld.FullName.Length != 11 || !typeOld.FullName.All(char.IsUpper))
                    {
                        continue;
                    }
                    if (!mappings.ContainsKey(typeOld.FullName))
                    {
                        csv.AppendLine($"1,{type.FullName},{typeOld.FullName},,,Removed in v1.58");
                    }
                }
            }
            var oldType = typesOld.FirstOrDefault(x => typeMappings.Where(y => y.Value == type.FullName).Select(y => y.Key).Contains(x.FullName));
            if (oldType != null)
            {
                foreach (var member in oldType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (unmappedOld.Contains(member.Name))
                    {
                        csv.AppendLine($"1,{type.FullName},{member.Name},,,Removed in v1.58");
                    }
                }
            }
        }
        
        csv = csv.Replace("*", "");
        
        File.WriteAllText("mappings.csv", csv.ToString());

        if (txtfile != null)
        {

            var lines = File.ReadAllLines(txtfile);
            var newLines = new List<string>();
            foreach (var line in lines)
            {
                if (mappings.ContainsKey(line))
                {
                    newLines.Add(mappings.First(x => x.Key.Replace("*", "") == line).Value.Replace("*", ""));
                }
                else
                {
                    newLines.Add("(NEW OR UNKNOWN)");
                }
            }

            File.WriteAllLines("textmappings.txt", newLines);
        }
    }


    private static bool TypeCompare(Type typeOld, Type typeNew)
    {
        if (typeOld.Attributes != typeNew.Attributes)
        {
            return false;
        }
        
        return true;
    }
    
    private static bool FieldCompare(FieldInfo fieldOld, FieldInfo fieldNew, Dictionary<string, string> mappings)
    {
        if (fieldOld.Attributes != fieldNew.Attributes)
        {
            return false;
        }
        if (fieldOld.FieldType != fieldNew.FieldType)
        {
            if (fieldOld.FieldType.FullName != null && mappings.TryGetValue(fieldOld.FieldType.FullName.Replace("[","").Replace("]",""), out var typeNewName))
            {
                if (typeNewName != fieldNew.FieldType.FullName?.Replace("[","").Replace("]",""))
                {
                    var append = "*";
                    do
                    {
                        if (mappings.TryGetValue(fieldOld.FieldType.FullName.Replace("[","").Replace("]","") + append, out typeNewName))
                        {
                            if (typeNewName != fieldNew.FieldType.FullName?.Replace("[","").Replace("]",""))
                            {
                                append += "*";
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    } while (true);
                }
            }
            else
            {
                return false;
            }
        }
        
        return true;
    }
    
    private static bool MethodCompare(MethodInfo methodOld, MethodInfo methodNew, Dictionary<string, string> mappings)
    {
        if (methodOld.Attributes != methodNew.Attributes)
        {
            return false;
        }
        if (methodOld.ReturnType != methodNew.ReturnType)
        {
            if (methodOld.ReturnType.FullName != null && mappings.TryGetValue(methodOld.ReturnType.FullName.Replace("[","").Replace("]",""), out var typeNewName))
            {
                if (typeNewName != methodNew.ReturnType.FullName?.Replace("[","").Replace("]",""))
                {
                    var append = "*";
                    do
                    {
                        if (mappings.TryGetValue(methodOld.ReturnType.FullName.Replace("[","").Replace("]","") + append, out typeNewName))
                        {
                            if (typeNewName != methodNew.ReturnType.FullName?.Replace("[","").Replace("]",""))
                            {
                                append += "*";
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    } while (true);
                }
            }
            else
            {
                return false;
            }
        }
        if (methodOld.GetParameters().Length != methodNew.GetParameters().Length)
        {
            return false;
        }
        for (int i = 0; i < methodOld.GetParameters().Length; i++)
        {
            var parameterOld = methodOld.GetParameters()[i];
            var parameterNew = methodNew.GetParameters()[i];
            if (parameterOld.ParameterType != parameterNew.ParameterType)
            {
                if (parameterOld.ParameterType.FullName != null && mappings.TryGetValue(parameterOld.ParameterType.FullName.Replace("[","").Replace("]",""), out var typeNewName))
                {
                    if (typeNewName != parameterNew.ParameterType.FullName?.Replace("[","").Replace("]",""))
                    {
                        var append = "*";
                        do
                        {
                            if (mappings.TryGetValue(parameterOld.ParameterType.FullName.Replace("[","").Replace("]","") + append, out typeNewName))
                            {
                                if (typeNewName != parameterNew.ParameterType.FullName?.Replace("[","").Replace("]",""))
                                {
                                    append += "*";
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        } while (true);
                    }
                }
                else
                {
                    return false;
                }
            }
        }
        return true;
    }
    
    private static bool PropertyCompare(PropertyInfo propertyOld, PropertyInfo propertyNew, Dictionary<string, string> mappings)
    {
        if (propertyOld.Attributes != propertyNew.Attributes)
        {
            return false;
        }
        if (propertyOld.PropertyType != propertyNew.PropertyType)
        {
            if (propertyOld.PropertyType.FullName != null && mappings.TryGetValue(propertyOld.PropertyType.FullName.Replace("[","").Replace("]",""), out var typeNewName))
            {
                if (typeNewName != propertyNew.PropertyType.FullName?.Replace("[","").Replace("]",""))
                {
                    var append = "*";
                    do
                    {
                        if (mappings.TryGetValue(propertyOld.PropertyType.FullName.Replace("[","").Replace("]","") + append, out typeNewName))
                        {
                            if (typeNewName != propertyNew.PropertyType.FullName?.Replace("[","").Replace("]",""))
                            {
                                append += "*";
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    } while (true);
                }
            }
            else
            {
                return false;
            }
        }
        
        return true;
    }
    
    private static bool EventCompare(EventInfo eventOld, EventInfo eventNew, Dictionary<string, string> mappings)
    {
        if (eventOld.Attributes != eventNew.Attributes)
        {
            return false;
        }
        if (eventOld.EventHandlerType != eventNew.EventHandlerType)
        {
            if (eventOld.EventHandlerType?.FullName != null && mappings.TryGetValue(eventOld.EventHandlerType.FullName.Replace("[","").Replace("]",""), out var typeNewName))
            {
                if (typeNewName != eventNew.EventHandlerType?.FullName?.Replace("[","").Replace("]",""))
                {
                    var append = "*";
                    do
                    {
                        if (mappings.TryGetValue(eventOld.EventHandlerType.FullName.Replace("[","").Replace("]","") + append, out typeNewName))
                        {
                            if (typeNewName != eventNew.EventHandlerType?.FullName?.Replace("[","").Replace("]",""))
                            {
                                append += "*";
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    } while (true);
                }
            }
            else
            {
                return false;
            }
        }
        
        return true;
    }
    
    private static bool ConstructorCompare(ConstructorInfo constructorOld, ConstructorInfo constructorNew, Dictionary<string, string> mappings)
    {
        if (constructorOld.Attributes != constructorNew.Attributes)
        {
            return false;
        }
        if (constructorOld.GetParameters().Length != constructorNew.GetParameters().Length)
        {
            return false;
        }
        for (int i = 0; i < constructorOld.GetParameters().Length; i++)
        {
            var parameterOld = constructorOld.GetParameters()[i];
            var parameterNew = constructorNew.GetParameters()[i];
            if (parameterOld.ParameterType != parameterNew.ParameterType)
            {
                if (parameterOld.ParameterType.FullName != null && mappings.TryGetValue(parameterOld.ParameterType.FullName.Replace("[","").Replace("]",""), out var typeNewName))
                {
                    if (typeNewName != parameterNew.ParameterType.FullName?.Replace("[","").Replace("]",""))
                    {
                        var append = "*";
                        do
                        {
                            if (mappings.TryGetValue(parameterOld.ParameterType.FullName.Replace("[","").Replace("]","") + append, out typeNewName))
                            {
                                if (typeNewName != parameterNew.ParameterType.FullName?.Replace("[","").Replace("]",""))
                                {
                                    append += "*";
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        } while (true);
                    }
                }
                else
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    private static bool MemberCompare(MemberInfo memberOld, MemberInfo memberNew, Dictionary<string, string> mappings, out bool isMethod)
    {
        isMethod = false;
        if (memberOld.MemberType != memberNew.MemberType)
        {
            return false;
        }
        if (memberOld is FieldInfo fieldOld && memberNew is FieldInfo fieldNew)
        {
            return FieldCompare(fieldOld, fieldNew, mappings);
        }
        if (memberOld is MethodInfo methodOld && memberNew is MethodInfo methodNew)
        {
            isMethod = true;
            return MethodCompare(methodOld, methodNew, mappings);
        }
        if (memberOld is PropertyInfo propertyOld && memberNew is PropertyInfo propertyNew)
        {
            return PropertyCompare(propertyOld, propertyNew, mappings);
        }
        if (memberOld is EventInfo eventOld && memberNew is EventInfo eventNew)
        {
            return EventCompare(eventOld, eventNew, mappings);
        }
        if (memberOld is ConstructorInfo constructorOld && memberNew is ConstructorInfo constructorNew)
        {
            return ConstructorCompare(constructorOld, constructorNew, mappings);
        }
        throw new Exception("Unknown member type: " + memberOld.MemberType);
    }
}