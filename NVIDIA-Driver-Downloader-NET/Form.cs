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
    private ProgressBar progressBar = new ProgressBar() { Visible = false, Dock = DockStyle.Fill, Anchor = AnchorStyles.Right };
    private StatusBar statusBar = new StatusBar() { Visible = false, SizingGrip = false };
    private ComboBox driverComponentsComboBox = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };

    public Form()
    {
        if (nvidiaGpu.name.Length == 0)
        {
            MessageBox.Show("NVIDIA GPU not detected!", "NVIDIA Driver Downloader .NET", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
        this.Text = "NVIDIA Driver Downloader .NET";
        this.Font = SystemFonts.MessageBoxFont;
        this.MinimumSize = new Size(0, 0);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MinimizeBox = false;
        this.MaximizeBox = false;
        this.FormClosed += (sender, e) => Environment.Exit(1);
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
        nvidiaGpuLabel = new Label() { Text = "GPU:", AutoSize = true, AutoEllipsis = true, Padding = new Padding(0, 5, 0, 0), Dock = DockStyle.Fill },
        nvidiaGpuNameLabel = new Label() { Text = nvidiaGpu.name, AutoSize = true, AutoEllipsis = true, Padding = new Padding(0, 5, 0, 0), Dock = DockStyle.Fill },
        driverVersionLabel = new Label() { Text = "Driver Version:", Padding = new Padding(0, 5, 0, 0), AutoSize = true, AutoEllipsis = true, Dock = DockStyle.Fill },
        driverTypeLabel = new Label() { Text = "Driver Type:", Padding = new Padding(0, 5, 0, 0), AutoSize = true, AutoEllipsis = true, Dock = DockStyle.Fill },
        driverComponentsLabel = new Label() { Text = "Driver Components:", Padding = new Padding(0, 5, 0, 0), AutoSize = true, AutoEllipsis = true, Dock = DockStyle.Fill },
        extractLabel = new Label() { Text = "Extract:", Padding = new Padding(0, 5, 0, 0), AutoSize = true, AutoEllipsis = true, Dock = DockStyle.Fill };

        ComboBox
        driverVersionsComboBox = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill },
        driverTypeComboBox = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };

        Button
        extractButton = new Button()
        {
            Text = "Extract",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        },
        downloadButton = new Button()
        {
            Text = "Download",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };

        Action<bool> nvidiaDownloadApiInvoked = (bool invoked) =>
        {
            downloadButton.Visible = !invoked;
            extractButton.Visible = !invoked;
            driverVersionsComboBox.Enabled = !invoked;
            driverTypeComboBox.Enabled = !invoked;
            this.driverComponentsComboBox.Enabled = !invoked;
            statusBar.Visible = invoked;
            this.progressBar.Visible = invoked;
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
            downloadButton.Enabled = false;
            extractButton.Enabled = false;
            driverTypeComboBox.Enabled = false;
            driverVersionsComboBox.Enabled = false;
            this.driverComponentsComboBox.Enabled = false;
            driverVersionsComboBox.Items.Clear();
            driverVersionsComboBox.Items.AddRange(NvidiaDownloadApi.GetDriverVersions(nvidiaGpu, studio, standard).ToArray());
            this.statusBar.ResetText();
            driverVersionsComboBox.SelectedIndex = driverVersionsComboBox.Items.Count - 1;
            downloadButton.Enabled = true;
            extractButton.Enabled = true;
            driverTypeComboBox.Enabled = true;
            driverVersionsComboBox.Enabled = true;
            this.driverComponentsComboBox.Enabled = true;
        });

        driverComponentsComboBox.Items.AddRange(new string[] { "Core", "Core + PhysX", "Core + HD Audio", "Core + PhysX + HD Audio", "All" });
        driverComponentsComboBox.SelectedIndex = 0;

        downloadButton.Click += (sender, e) =>
        {
            if (MessageBox.Show(
            owner: this,
         $"Driver Version: {driverVersionsComboBox.Text}\nDriver Type: {driverTypeComboBox.Text}\nDriver Components: {this.driverComponentsComboBox.Text}",
          "Download?",
           MessageBoxButtons.YesNo,
           MessageBoxIcon.Information) == DialogResult.No)
                return;
            this.statusBar.Text = "Downloading...";
            nvidiaDownloadApiInvoked(true);
            this.progressBar.Visible = true;
            this.statusBar.Visible = true;
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
                            this.statusBar.Text = $"Downloading: {this.progressBar.Value}%";
                        }
                    };
                    webClient.DownloadFileCompleted += (sender, e) => this.progressBar.Value = 0;
                    await webClient.DownloadFileTaskAsync(downloadLink, fileName);
                }
                this.ExtractDriverPackage(fileName, this.driverComponentsComboBox.Text);
                nvidiaDownloadApiInvoked(false);
            })).Start();
        };

        extractButton.Click += (sender, e) =>
        {
            OpenFileDialog openFileDialog = (new OpenFileDialog() { Filter = "Executable Files (*.exe)|*.exe" });
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (MessageBox.Show(
                    owner: this,
                    $"Driver File: {Path.GetFileName(openFileDialog.FileName)}\nDriver Components: {this.driverComponentsComboBox.Text}",
                    "Extract?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information) == DialogResult.No)
                    return;
                nvidiaDownloadApiInvoked(true);
                (new Thread(() =>
                {
                    this.ExtractDriverPackage(openFileDialog.FileName, driverComponentsComboBox.Text);
                    nvidiaDownloadApiInvoked(false);
                })).Start();
            }
        };

        tableLayoutPanel.Controls.Add(nvidiaGpuLabel, 1, 0);
        tableLayoutPanel.Controls.Add(nvidiaGpuNameLabel, 2, 0);
        tableLayoutPanel.Controls.Add(driverVersionLabel, 1, 1);
        tableLayoutPanel.Controls.Add(driverVersionsComboBox, 2, 1);
        tableLayoutPanel.Controls.Add(driverTypeLabel, 1, 2);
        tableLayoutPanel.Controls.Add(driverTypeComboBox, 2, 2);
        tableLayoutPanel.Controls.Add(driverComponentsLabel, 1, 3);
        tableLayoutPanel.Controls.Add(driverComponentsComboBox, 2, 3);
        tableLayoutPanel.Controls.Add(extractButton, 1, 4);
        tableLayoutPanel.Controls.Add(downloadButton, 2, 4);
        this.statusBar.Controls.Add(progressBar);
        this.Controls.Add(tableLayoutPanel);
        this.Controls.Add(this.statusBar);
        driverTypeComboBox.SelectedIndex = 0;
    }


    private void ExtractDriverPackage(string fileName, string componentOption)
    {
        this.statusBar.Text = $"Extracting...";
        string components = "Display.Driver NVI2 EULA.txt ListDevices.txt setup.cfg setup.exe ";
        int i = 0;
        string
        sevenZipFileName = $"{Environment.GetEnvironmentVariable("TEMP")}/7zr.exe",
        path = $"{Path.GetDirectoryName(fileName)}\\{Path.GetFileNameWithoutExtension(fileName)}";
        Process process;
        string[] contents;

        switch (this.driverComponentsComboBox.Text)
        {
            case "Core + PhysX":
                components += "PhysX";
                break;
            case "Core + HD Audio":
                components += "HDAudio";
                break;
            case "Core + PhysX + HD Audio":
                components += "PhysX HDAudio";
                break;
            case "All":
                components = "";
                break;
        }

        if (Directory.Exists(path))
            Directory.Delete(path, true);
        using (WebClient webClient = new WebClient())
            webClient.DownloadFile("https://www.7-zip.org/a/7zr.exe", sevenZipFileName);
        process = Process.Start(
            new ProcessStartInfo()
            {
                FileName = sevenZipFileName,
                Arguments = $"x -bso0 -bsp1 -bse1 -aoa \"{Path.GetFullPath(fileName).Trim()}\" {components} -o\"{path}\"",
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
                this.FormClosing += (sender, e) => { try { process.Kill(); } catch (System.InvalidOperationException) { } };
                while ((line = streamReader.ReadLine().Split('%')[0].Trim()) != null)
                {
                    if (line.Length != 0 && int.TryParse(line, out result))
                    {
                        this.progressBar.Value = result;
                        this.statusBar.Text = $"Extracting: {this.progressBar.Value}%";
                    }
                }
            }
            catch (NullReferenceException) { }
        }
        this.progressBar.Value = 0;
        this.statusBar.ResetText();
        process.WaitForExit();
        process.Close();

        try
        {
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
                    if (contents[i].Trim().StartsWith(element))
                        contents[i] = $"\t\t{element}\"\"/>";
            File.WriteAllLines($"{path}\\NVI2\\presentations.cfg", contents, System.Text.Encoding.ASCII);

            Process.Start(new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = $"/c explorer.exe /select,\"{path}\\setup.exe\"",
                WindowStyle = ProcessWindowStyle.Hidden
            }).Close();
        }
        catch (System.IO.DirectoryNotFoundException) { return; }
    }
}