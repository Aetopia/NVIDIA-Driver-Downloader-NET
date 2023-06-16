using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System;
using System.Drawing;
using System.Linq;
using System.IO;

public class Form : System.Windows.Forms.Form
{
    private NvidiaDownloadApi.NvidiaGpu nvidiaGpu = NvidiaDownloadApi.GetGpu();
    private ProgressBar progressBar = new ProgressBar() { Visible = false, Dock = DockStyle.Fill, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
    private StatusBar statusBar = new StatusBar() { SizingGrip = false };
    private Thread thread = Thread.CurrentThread;
    public Form()
    {
        if (nvidiaGpu.name.Length != 0)
        {
            MessageBox.Show("NVIDIA GPU not detected!", "NVIDIA Driver Downloader .NET", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
        this.Text = "NVIDIA Driver Downloader .NET";
        this.MinimumSize = new Size(0, 0);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MinimizeBox = false;
        this.MaximizeBox = false;
        this.CenterToScreen();
        bool studio = false, standard = false;
        string tempFolder = Environment.GetEnvironmentVariable("TEMP"), downloadLink = "", fileName = "";
        TableLayoutPanel tableLayoutPanel = new TableLayoutPanel()
        {
            AutoSizeMode = AutoSizeMode.GrowOnly,
            Dock = DockStyle.Fill,
            AutoSize = true
        };


        Label
        nvidiaGpuLabel = new Label() { Text = $"GPU:", AutoSize = true, AutoEllipsis = true, Padding = new Padding(0, 5, 0, 0) },
        nvidiaGpuNameLabel = new Label() { Text = nvidiaGpu.name, AutoSize = true, AutoEllipsis = true, Padding = new Padding(0, 5, 0, 0) },
        driverVersionLabel = new Label() { Text = "Driver Version:", Padding = new Padding(0, 5, 0, 0) },
        driverTypeLabel = new Label() { Text = "Driver Type:", Padding = new Padding(0, 5, 0, 0) };

        ComboBox
        driverVersionsComboBox = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList },
        driverTypeComboBox = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList };

        Button downloadButton = new Button()
        {
            Text = "Download",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };

        Action<bool> nvidiaDownloadApiInvoked = (bool invoked) =>
        {
            downloadButton.Enabled = !invoked;
            driverVersionsComboBox.Enabled = !invoked;
            driverTypeComboBox.Enabled = !invoked;
        };

        driverTypeComboBox.Items.AddRange(new string[] { "Game Ready DCH", "Game Ready STD", "Studio DCH", "Studio STD" });
        driverTypeComboBox.SelectedIndexChanged += async (sender, e) => await Task.Run(() =>
        {
            studio = false;
            standard = false;
            switch (driverTypeComboBox.Text)
            {
                case "Game Ready STD":
                    standard = true;
                    break;
                case "Studio DCH":
                    studio = true;
                    break;
                case "Studio STD":
                    studio = true;
                    standard = true;
                    break;
            }
            nvidiaDownloadApiInvoked(true);
            driverVersionsComboBox.Items.Clear();
            this.statusBar.Text = $"Getting {driverTypeComboBox.Text} Versions...";
            driverVersionsComboBox.Items.AddRange(NvidiaDownloadApi.GetDriverVersions(nvidiaGpu, studio, standard).ToArray());
            this.statusBar.ResetText();
            driverVersionsComboBox.SelectedIndex = driverVersionsComboBox.Items.Count - 1;
            nvidiaDownloadApiInvoked(false);
        });

        downloadButton.Click += (sender, e) =>
        {
            this.statusBar.Text = "Downloading NVIDIA Driver Package...";
            nvidiaDownloadApiInvoked(true);
            this.progressBar.Visible = true;
            (new Thread(async () =>
            {
                downloadLink = NvidiaDownloadApi.GetDriverDownloadLink(nvidiaGpu, driverVersionsComboBox.Text, studio, standard);
                fileName = $"{tempFolder}\\{downloadLink.Split('/').Last()}";
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadProgressChanged += (sender, e) =>
                    {
                        if (this.progressBar.Value != e.ProgressPercentage)
                        {
                            this.progressBar.Value = e.ProgressPercentage;
                            this.statusBar.Text = $"Downloading NVIDIA Driver Package: {this.progressBar.Value}%";
                        }
                    };
                    webClient.DownloadFileCompleted += (sender, e) => this.progressBar.Value = 0;
                    await webClient.DownloadFileTaskAsync(new Uri(downloadLink), fileName);
                }
                this.statusBar.Text = $"Extracting NVIDIA Driver Package....";
                this.ExtractDriverPackage(fileName);
                nvidiaDownloadApiInvoked(false);
                this.progressBar.Visible = false;
            })).Start();
        };

        tableLayoutPanel.Controls.Add(nvidiaGpuLabel, 1, 0);
        tableLayoutPanel.Controls.Add(nvidiaGpuNameLabel, 2, 0);
        tableLayoutPanel.Controls.Add(driverVersionLabel, 1, 1);
        tableLayoutPanel.Controls.Add(driverVersionsComboBox, 2, 1);
        tableLayoutPanel.Controls.Add(driverTypeLabel, 1, 2);
        tableLayoutPanel.Controls.Add(driverTypeComboBox, 2, 2);
        tableLayoutPanel.Controls.Add(progressBar, 1, 3);
        tableLayoutPanel.Controls.Add(downloadButton, 2, 3);
        this.Controls.Add(tableLayoutPanel);
        this.Controls.Add(this.statusBar);
        driverTypeComboBox.SelectedIndex = 0;
    }


    private void ExtractDriverPackage(string fileName)
    {
        const string Components = "Display.Driver NVI2 EULA.txt ListDevices.txt setup.cfg setup.exe";
        int i = 0;
        string
        sevenZipFileName = $"{Environment.GetEnvironmentVariable("TEMP")}/7zr.exe",
        path = $"{Path.GetDirectoryName(fileName)}\\{Path.GetFileNameWithoutExtension(fileName)}";
        Process process;
        string[] contents;

        if (Directory.Exists(path))
            Directory.Delete(path, true);
        using (WebClient webClient = new WebClient())
            webClient.DownloadFile("https://www.7-zip.org/a/7zr.exe", sevenZipFileName);
        process = Process.Start(
            new ProcessStartInfo()
            {
                FileName = sevenZipFileName,
                Arguments = $"x -bso0 -bsp1 -bse1 -aoa \"{Path.GetFullPath(fileName).Trim()}\" {Components} -o\"{path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

        using (StreamReader streamReader = process.StandardOutput)
        {
            string line = null;
            int result = 0;
            try
            {
                while ((line = streamReader.ReadLine().Split('%')[0].Trim()) != null)
                {
                    if (!thread.IsAlive)
                        process.Kill();
                    if (line.Length != 0 && int.TryParse(line, out result))
                    {
                        this.progressBar.Value = result;
                        this.statusBar.Text = $"Extracting NVIDIA Driver Package: {this.progressBar.Value}%";
                    }
                }
            }
            catch (NullReferenceException) { }
        }
        this.statusBar.ResetText();
        process.Close();


        contents = File.ReadAllLines($"{path}\\setup.cfg");
        for (i = 0; i < contents.Length - 1; i++)
        {
            if ((new string[]{"<file name=\"${{EulaHtmlFile}}\"/>",
                "<file name=\"${{FunctionalConsentFile}}\"/>",
                "<file name=\"${{PrivacyPolicyFile}}\"/>"}).Contains(contents[i].Trim()))
                contents[i] = "";
        }
        File.WriteAllLines($"{path}\\setup.cfg", contents, System.Text.Encoding.ASCII);

        contents = File.ReadAllLines($"{path}\\NVI2\\presentations.cfg");
        for (i = 0; i < contents.Length - 1; i++)
            foreach (string element in (new string[]{
            "<string name=\"ProgressPresentationUrl\" value=",
                "<string name=\"ProgressPresentationSelectedPackageUrl\" value="}))
                if (contents[i].Trim().StartsWith($"\t\t{element}"))
                    contents[i] = $"\t\t{element}\"\"/>";
        File.WriteAllLines($"{path}\\NVI2\\presentations.cfg", contents, System.Text.Encoding.ASCII);

        Process.Start("explorer.exe", $"/select,\"{path}\\setup.exe\"").Close();
    }
}