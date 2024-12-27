
using System.Xml.Linq;

internal class MergeHelper
{
    public static void MergeFiles(string folder, string outputFile, Kaenx.Creator.Models.MainModel general)
    {
        string manu = general.IsOpenKnx ? "M-00FA" : $"M-{general.ManufacturerId:X4}";
        Kaenx.Creator.Classes.ExportHelper helper = new Kaenx.Creator.Classes.ExportHelper(general);
        helper.SetNamespace(general.Application.NamespaceVersion);
        XElement xmerge = helper.CreateNewXML(manu);

        foreach(string file in Directory.GetFiles(folder))
        {
            XElement xroot = XElement.Load(file);
            XElement xmanu = xroot.Elements().ElementAt(0).Elements().ElementAt(0);
            foreach(XElement xele in xmanu.Elements())
            {
                if(xele.Name.LocalName == "Languages") {

                } else {
                    xele.Name = XName.Get(xele.Name.LocalName, xmerge.Name.NamespaceName);
                    xmerge.Add(xele);
                }
            }
        }

        string releaseFolder = Path.GetDirectoryName(outputFile);
        if(!Directory.Exists(releaseFolder))
            Directory.CreateDirectory(releaseFolder);
        File.WriteAllText(outputFile, xmerge.Document.ToString());
    }
}