System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
bool noOutput = false;

Console.WriteLine("Willkommen beim Kaenx-Creator.Console!!");
Console.WriteLine();

Kaenx.Creator.Models.MainModel General;

if(args.Length < 2)
{
    Console.WriteLine("Error: Es wurden nicht genügend Parameter angegeben.");
    Console.WriteLine("       Kaenx.Creator.Console <Befehl> <Pfad>");
    return;
}

if(!System.IO.File.Exists(args[1]))
{
    Console.WriteLine("Error: Die angegebene Datei konnte nicht gefunden werden.");
    return;
}

if(args.Length >=3 && args[2] == "silent")
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
    Console.WriteLine("Error: Das Projekt wurde mit einer neueren Version erstellt und kann somit nicht geöffnet werden.");
    return;
}

try
{
    General = Newtonsoft.Json.JsonConvert.DeserializeObject<Kaenx.Creator.Models.MainModel>(general, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
}
catch (Exception ex)
{
    Console.WriteLine("Error: Beim öffnen des Projekts trat ein Fehler auf.");
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
            Console.WriteLine(act.Text);
    };
}
Console.WriteLine("Info:  Projekt wird überprüft");
Console.WriteLine("       Noch nicht implementiert...");
//CheckHelper.CheckThis(General, PublishActions);
string rootPath = Path.GetDirectoryName(args[1]);
string headerPath = Path.Combine(rootPath, "knxprod.h");
if(Directory.Exists(Path.Combine(rootPath, "include")))
    headerPath = Path.Combine(rootPath, "include", "knxprod.h");
string filePath = Path.Combine(Path.GetDirectoryName(args[1]), General.FileName + ".knxprod");
string assPath = Kaenx.Creator.Classes.Helper.GetAssemblyPath(General.Application.NamespaceVersion);

if(string.IsNullOrEmpty(assPath))
{
    Console.WriteLine("Error: Es konnte keine ETS gefunden werden.");
    return;
}

switch(args[0])
{
    case "publish":
    {
        Console.WriteLine("Info:  Projekt wird erstellt");
        Kaenx.Creator.Classes.ExportHelper helper = new Kaenx.Creator.Classes.ExportHelper(General, assPath, filePath, headerPath);
        bool success = helper.ExportEts(noOutput ? null : PublishActions);
        if(!success)
        {
            //MessageBox.Show(Properties.Messages.main_export_error, Properties.Messages.main_export_title);
            Console.WriteLine($"Error: Beim Veröffentlichen sind x Fehler aufgetreten.");
            return;
        }
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
        helper.SignOutput(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Temp")).Wait();
        Console.WriteLine("Info:  Projekt wurde erfolgreich erstellt");
        Console.WriteLine($"       {filePath}");
        break;
    }

    default:
        Console.WriteLine($"Error: Befehl {args[0]} ist unbekannt");
        break;
}