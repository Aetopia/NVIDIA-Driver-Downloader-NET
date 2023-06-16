# NVIDIA Driver Downloader .NET
A tool to download NVIDIA Driver Packages.
![image](https://github.com/Aetopia/NVIDIA-Driver-Downloader-NET/assets/41850963/12a5a6e6-4e3c-416d-81d8-90b09f193caa)

## Usage
1. Download the latest release and open the program.
2. The program will auto detect your system's NVIDIA GPU.
3. Driver Options: 

    |Property|Operation|
    |-|-|
    |Driver Version|Select a driver version.
    |Driver Type|Select a driver type.
    |Driver Components|Select which driver components should be extracted alongside the display driver. 
4. 
    - Hit the `[Download]` button to download a NVIDIA Driver Package.
    - Hit the `[Extract]` button to extract a NVIDIA Driver Package.

## Building
1. Download and install the .NET SDK and .NET Framework 4.8.1 Developer Pack from:<br>https://dotnet.microsoft.com/en-us/download/visual-studio-sdks
2. Open a Command Prompt or PowerShell window where the `.csproj` file is located.
3. Run the following command:

    ```cmd
    dotnet.exe build "NVIDIA-Driver-Downloader-NET\NVIDIA-Driver-Downloader-NET.csproj" --configuration Release
    ```
