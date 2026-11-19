using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using Range = SemanticVersioning.Range;

namespace ItemMod;

// The DLL name (GTA6GameCase) is set in the project's .csproj via <AssemblyName>GTA6GameCase</AssemblyName>.
internal static class ModIdentity
{
    // Only letters and numbers in Name and Author
    public const string Author = "ShashaCZ";
    public const string Name = "ShashaCZGTA6GameCase";

    // SPT only accepts lowercase letters and digits here (no underscores or spaces)
    public const string Guid = "com.shashacz.gta6gamecase";
}

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = ModIdentity.Guid;
    public string Name { get; init; } = ModIdentity.Name;
    public string Author { get; init; } = ModIdentity.Author;
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public Range SptVersion { get; init; } = new("~4.1.6");
    public string License { get; init; } = "MIT";
    public Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new Range(">=2.0.20") }
    };
    public string? Url { get; init; }
    public List<string>? Contributors { get; init; }
    public List<string>? Incompatibilities { get; init; }
    public bool HasPrepatcher { get; init; } = false;
}

[Injectable(TypePriority = OnLoadOrder.GameCallbacks + 1)]
public class ItemModLoader(WTTServerCommonLib.WTTServerCommonLib wttCommon, ISptLogger<ItemModLoader> logger) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var modDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var db = Path.Combine(modDir, "db");
        var loaded = new List<string>();

        // Item JSON files: db\CustomItems (WTT's default folder) and/or db\Items
        if (Directory.Exists(Path.Combine(db, "CustomItems")))
        {
            await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
            loaded.Add("db\\CustomItems");
        }

        if (Directory.Exists(Path.Combine(db, "Items")))
        {
            await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly, Path.Join("db", "Items"));
            loaded.Add("db\\Items");
        }

        // Locale files (en.json, ru.json, ...): db\CustomLocales
        if (Directory.Exists(Path.Combine(db, "CustomLocales")))
        {
            await wttCommon.CustomLocaleService.CreateCustomLocales(assembly);
            loaded.Add("db\\CustomLocales");
        }

        // bundles\ and bundles.json are picked up by SPT itself when they sit in the mod folder.
        var hasBundles = File.Exists(Path.Combine(modDir, "bundles.json"));

        logger.Info(
            $"[{ModIdentity.Name}] loaded from: {(loaded.Count > 0 ? string.Join(", ", loaded) : "no db folders found")}; " +
            $"bundles.json {(hasBundles ? "found" : "NOT found")}");
    }
}