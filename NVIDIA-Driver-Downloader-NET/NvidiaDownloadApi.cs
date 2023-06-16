using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Management;
using System.Linq;

public class NvidiaDownloadApi
{
    public struct NvidiaGpu { public string name, psid, pfid; }
    public static NvidiaGpu GetGpu()
    {
        NvidiaGpu nvidiaGpu = new NvidiaGpu();
        string line = "";
        int i = 0;
        List<string> deviceIds = new List<string>();
        RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\PCI");

        foreach (string subKeyName in registryKey.GetSubKeyNames())
            if (subKeyName.StartsWith("VEN_10DE"))
                deviceIds.Add(subKeyName.Split(new String[] { "VEN_10DE&DEV_" }, 2, StringSplitOptions.RemoveEmptyEntries)[0].Split(new String[] { "&SUBSYS" }, 2, StringSplitOptions.None)[0].Trim());
        registryKey.Close();

        using (WebClient webClient = new WebClient())
        {
            string[] supportedNvidiaGpuProducts = webClient.DownloadString($"https://download.nvidia.com/XFree86/Linux-x86_64/{webClient.DownloadString(
            "https://download.nvidia.com/XFree86/Linux-x86_64/latest.txt").Split(
                new Char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries)[0].Trim()}/README/supportedchips.html").Split('\n');
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("https://www.nvidia.com/Download/API/lookupValueSearch.aspx?TypeID=3");
            XmlNodeList xmlNodeList = xmlDocument.SelectNodes("LookupValueSearch/LookupValues/LookupValue");

            for (i = 0; i < supportedNvidiaGpuProducts.Length - 1; i++)
            {
                line = supportedNvidiaGpuProducts[i].Trim();
                if (Regex.IsMatch(line, "<tr id=\"devid.{4}\">"))
                    if (deviceIds.Contains(line.Split(new String[] { "<tr id=\"devid" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new String[] { "\">" }, StringSplitOptions.RemoveEmptyEntries)[0]))
                    {
                        nvidiaGpu.name = supportedNvidiaGpuProducts[i + 1].Split('>')[1].Split('<')[0].Trim();
                        break;
                    }
            }

            for (i = 0; i < xmlNodeList.Count - 1; i++)
            {
                if (nvidiaGpu.name.EndsWith(xmlNodeList[i]["Name"].InnerText))
                {
                    nvidiaGpu.name = xmlNodeList[i]["Name"].InnerText;
                    nvidiaGpu.psid = xmlNodeList[i].Attributes["ParentID"].InnerText;
                    nvidiaGpu.pfid = xmlNodeList[i]["Value"].InnerText;
                    break;
                };
            }
        }

        return nvidiaGpu;
    }

    public static List<string> GetDriverVersions(NvidiaGpu nvidiaGpu, bool studio = false, bool standard = false)
    {
        string[] nvidiaGpuDriverPageLines = new string[] { };
        string line = "", whql = "1", dtcid = "1";
        List<string> driverVersions = new List<string>();

        if (studio) whql = "4";
        if (standard) dtcid = "0";
        using (WebClient webClient = new WebClient())
        {
            nvidiaGpuDriverPageLines = webClient.DownloadString($"https://www.nvidia.com/Download/processFind.aspx?psid={nvidiaGpu.psid}&pfid={nvidiaGpu.pfid}&osid=57&lid=1&whql={whql}&ctk=0&dtcid={dtcid}").Split('\n');
            for (int i = 0; i < nvidiaGpuDriverPageLines.Length - 1; i++)
            {
                line = nvidiaGpuDriverPageLines[i].Trim();
                if (Regex.IsMatch(line, "<td class=\"gridItem\">(.*)</td>") && !line.StartsWith("<td class=\"gridItem\"><img"))
                    driverVersions.Add(line.Split('>')[1].Split('<')[0].Trim());
            }
        }
        driverVersions.Sort();
        return driverVersions;
    }

    public static string GetDriverDownloadLink(NvidiaGpu nvidiaGpu, string driverVersion = "", bool studio = false, bool standard = false)
    {
        if (driverVersion.Length == 0)
            driverVersion = GetDriverVersions(nvidiaGpu, studio, standard).Last();
        string[] driverName = new string[] { "Game Ready", "DCH" };
        string
        channel = "",
        nsd = "",
        platform = "desktop",
        type = "-dch",
        downloadLink = "";

        if (studio)
        {
            nsd = "-nsd";
            driverName[0] = "Studio";
        }
        if (standard)
        {
            type = "";
            driverName[1] = "Standard";
        }

        foreach (ManagementObject managementObject in (new ManagementClass("Win32_SystemEnclosure")).GetInstances())
            if ((managementObject["ChassisTypes"] as ushort[]).Intersect(new ushort[] { 8, 9, 10, 11, 12, 14, 18, 21 }).Any())
                platform = "notebook";


        if (nvidiaGpu.name.StartsWith("Quadro"))
        {
            channel = "Quadro_Certified/";
            platform = "quadro-rtx-desktop-notebook";
        }
        else if (nvidiaGpu.name.StartsWith("RTX"))
            platform = "data-center-tesla-desktop";

        using (WebClient webClient = new WebClient())
            foreach (string windowsVersion in new string[] { "win10-win11", "win10" })
            {
                downloadLink = $"https://international.download.nvidia.com/Windows/{channel}{driverVersion}/{driverVersion}-{platform}-{windowsVersion}-64bit-international{nsd}{type}-whql.exe";
                try
                {
                    (webClient.OpenRead(downloadLink)).Close();
                    break;
                }
                catch (System.Net.WebException) { }
            }

        return downloadLink;
    }
}