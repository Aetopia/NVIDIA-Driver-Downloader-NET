# NVIDIA Driver Downloader .NET
A tool to download NVIDIA Driver Packages.

## Usage
1. Download the latest release and open the program.
2. The program will auto detect your system's NVIDIA GPU.
3. Select your desired driver version/type.
4. Hit the `[Download]` button to download and extract the NVIDIA Driver Package, this extracted package only provides the base display driver.

## Building
1. Download and install the .NET SDK and .NET Framework 4.8.1 Developer Pack from:<br>https://dotnet.microsoft.com/en-us/download/visual-studio-sdks
2. Open a Command Prompt or PowerShell window where the `.csproj` file is located.
3. Run the following command:

    ```cmd
    dotnet.exe build --configuration Release
    ```
