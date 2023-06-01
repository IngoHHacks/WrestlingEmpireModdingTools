namespace MassRenamer;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: MassRenamer.exe <fromFile> <toFile> <targetFolder> [recursive]");
            return;
        }
        
        var fromFile = args[0];
        var toFile = args[1];
        var targetFolder = args[2];
        var recursive = args.Length <= 3 || !"nfd".Contains(args[3].ToLower()[0]);
        
        var from = File.ReadAllLines(fromFile);
        var to = File.ReadAllLines(toFile);
        var dict = new Dictionary<string, string>();
        for (var i = 0; i < from.Length; i++)
        {
            if (!Validate(from[i], to[i])) continue;
            if (dict.ContainsKey(from[i]))
            {
                Console.WriteLine($"Duplicate key: {from[i]}");
                continue;
            }
            dict[from[i]] = to[i];
        }

        Dictionary<string, Dictionary<string, string>> remappings = new();
        var files = Directory.GetFiles(targetFolder, "*.cs", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var (key, value) in dict)
            {
                if (!text.Contains(key)) continue;
                text = text.Replace(key, value);
                if (!remappings.ContainsKey(file))
                {
                    remappings[file] = new Dictionary<string, string>();
                }
                remappings[file][key] = value;
            }
            File.WriteAllText(file, text);
        }
        
        Console.WriteLine("Done!");
        foreach (var (file, remapping) in remappings)
        {
            Console.WriteLine(file);
            foreach (var (key, value) in remapping)
            {
                Console.WriteLine($"  {key} -> {value}");
            }
        }
    }
    
    private static bool Validate(params string[] strings)
    {
        return strings.All(s => s.Length == 11 && s.All(char.IsUpper));
    }
}