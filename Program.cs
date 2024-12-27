using OpenKNX.Toolbox.Sign;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Xml.Linq;

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
bool noOutput = false;

Console.WriteLine("Willkommen beim Kaenx-Creator.Console!!");
Console.WriteLine();
System.Version? clientVersion = typeof(Program).Assembly.GetName().Version;
if(clientVersion != null) {
    Console.WriteLine($"Version {clientVersion.Major}.{clientVersion.Minor}.{clientVersion.Build}");
}

if(args.Length > 0 && args[0] == "--version")
    return;

Kaenx.Creator.Models.MainModel General;

if(args.Length < 2)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Es wurden nicht genügend Parameter angegeben.");
    Console.WriteLine("       Kaenx.Creator.Console.exe <Befehl> <In_Pfad> <Out_Pfad> (silent)");
    Console.ResetColor();
    if(args.Length == 0)
        args = ["help"];
    else
        args[0] = "help";
}

if(args[0] == "help")
{
    Console.WriteLine("Folgende Befehle werden unterstützt:");
    Console.WriteLine("  publish   - erstellt eine fertige knxprod");
    Console.WriteLine("  release   - erstellt eine release xml");
    Console.WriteLine("  help      - zeigt diese Hilfe an");
    Console.WriteLine("  --version - zeigt die Version an");
    Console.WriteLine("  silent    - reduziert die Ausgaben");
    return;
}

if(!System.IO.File.Exists(args[1]))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: Die angegebene Datei konnte nicht gefunden werden.\r\n\t({args[1]})");
    Console.ResetColor();
    return;
}

if(args.Length >=4 && args[args.Length - 1] == "silent")
{
    Console.WriteLine("Info:  Ausgaben werden reduziert.");
    noOutput = true;
}

string general = System.IO.File.ReadAllText(args[1]);

System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("\"ImportVersion\":[ ]?([0-9]+)");
System.Text.RegularExpressions.Match match = reg.Match(general);

int VersionToOpen = 0;
if (match.Success)
{
    VersionToOpen = int.Parse(match.Groups[1].Value);
}

if (VersionToOpen < Kaenx.Creator.Classes.Helper.CurrentVersion)
{
    Console.WriteLine("Info:  Das Projekt wurde mit einer älteren Version erstellt. Es wird versucht es zu konvertieren.");
    general = Kaenx.Creator.Classes.Helper.CheckImportVersion(general, VersionToOpen);
}
if (VersionToOpen > Kaenx.Creator.Classes.Helper.CurrentVersion)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Das Projekt wurde mit einer neueren Version erstellt und kann somit nicht geöffnet werden.");
    Console.ResetColor();
    return;
}

try
{
    General = Newtonsoft.Json.JsonConvert.DeserializeObject<Kaenx.Creator.Models.MainModel>(general, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Beim öffnen des Projekts trat ein Fehler auf.");
    Console.ResetColor();
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);
    return;
}
Kaenx.Creator.Classes.Helper.LoadBcus();
Kaenx.Creator.Classes.Helper.LoadDpts();
Kaenx.Creator.Classes.Helper.LoadVersion(General, General.Application);
if(!noOutput) Console.WriteLine("Info:  Projekt wurde geladen");

System.Collections.ObjectModel.ObservableCollection<Kaenx.Creator.Models.PublishAction> PublishActions = new();
if(!noOutput)
{
    PublishActions.CollectionChanged += (object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
    {
        foreach(Kaenx.Creator.Models.PublishAction act in e.NewItems)
            if(act.State != Kaenx.Creator.Models.PublishState.Info)
                Console.WriteLine("       " + act.Text);
    };
}
Console.WriteLine("Info:  Projekt wird überprüft");
Console.WriteLine("       Noch nicht implementiert...");
//CheckHelper.CheckThis(General, PublishActions);
string rootPath = Path.GetDirectoryName(args[1]) ?? "";
if(rootPath == "")
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Der Pfad konnte nicht ermittelt werden.");
    Console.ResetColor();
    return;
}
string headerPath = Path.Combine(rootPath, "knxprod.h");
if(Directory.Exists(Path.Combine(rootPath, "include")))
    headerPath = Path.Combine(rootPath, "include", "knxprod.h");

if(!Kaenx.Creator.Classes.Helper.CheckExportNamespace(General.Application.NamespaceVersion, true))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Es konnte keine ETS gefunden werden.");
    Console.ResetColor();
    return;
}

switch(args[0])
{
    case "publish":
    {
        Console.WriteLine("Info:  Projekt wird erstellt");
        string filePath = Path.Combine(rootPath, General.FileName + ".knxprod");
        Kaenx.Creator.Classes.ExportHelper helper = new Kaenx.Creator.Classes.ExportHelper(General, headerPath);
        bool success = helper.ExportEts(noOutput ? null : PublishActions);
        if(!success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Beim Veröffentlichen sind {PublishActions.Count(pa => pa.State == Kaenx.Creator.Models.PublishState.Fail)} Fehler aufgetreten.");
            Console.ResetColor();
            return;
        }
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
        Console.WriteLine("Info:  Projekt wird signiert");
        await SignHelper.CheckMaster(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Temp"), General.Application.NamespaceVersion);
        await helper.SignOutput(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Temp"), filePath, General.Application.NamespaceVersion);
        Console.WriteLine("Info:  Projekt wurde erfolgreich erstellt");
        Console.WriteLine($"       {filePath}");
        break;
    }

    case "release":
    {
        Console.WriteLine("Info:  Release wird erstellt");
        string knxprodPath = Path.Combine(rootPath, "release", General.FileName + ".knxprod");
        Kaenx.Creator.Classes.ExportHelper helper = new Kaenx.Creator.Classes.ExportHelper(General, headerPath);
        bool success = helper.ExportEts(noOutput ? null : PublishActions);
        if(!success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Beim Veröffentlichen sind {PublishActions.Count(pa => pa.State == Kaenx.Creator.Models.PublishState.Fail)} Fehler aufgetreten.");
            Console.ResetColor();
            return;
        }
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
        if(Directory.Exists(Path.Combine(rootPath, "release")))
            Directory.Delete(Path.Combine(rootPath, "release"), true);
        Directory.CreateDirectory(Path.Combine(rootPath, "release"));
        Console.WriteLine("Info:  Projekt wird signiert");
        await SignHelper.CheckMaster(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Temp"), General.Application.NamespaceVersion);
        await helper.SignOutput(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Temp"), knxprodPath, General.Application.NamespaceVersion);
        Console.WriteLine("Info:  Projekt wurde erfolgreich erstellt");

        Console.WriteLine("Info:  Baggages werden exportiert");
        ZipArchive archive = ZipFile.OpenRead(knxprodPath);
        string exportBase = Path.Combine(rootPath, "release", "data", General.FileName + ".baggages");
        foreach(ZipArchiveEntry entry in archive.Entries) {
            if(!entry.FullName.Contains("/Baggages/")) continue;
            string relPath = entry.FullName;
            relPath = relPath.Substring(relPath.IndexOf("/Baggages/") + 10);
            string exportFilePath = Path.Combine(exportBase, relPath);
            if(!Directory.Exists(Path.GetDirectoryName(exportFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(exportFilePath));
            entry.ExtractToFile(exportFilePath);
        }
        
        Console.WriteLine("Info:  XML wird erstellt");
        string manu = General.IsOpenKnx ? "00FA" : $"{General.ManufacturerId:X4}";
        string outputPath = helper.GetRelPath("Temp", "M-" + manu);
        string filePath = Path.Combine(rootPath, "release", "data", General.FileName + ".xml");

        MergeHelper.MergeFiles(outputPath, filePath, General);
        Console.WriteLine("Info:  Release wurde erfolgreich erstellt");
        Console.WriteLine($"       {filePath}");
        break;
    }

    default:
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: Befehl {args[0]} ist unbekannt");
        Console.ResetColor();
        break;
}