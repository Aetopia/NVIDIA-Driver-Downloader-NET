# NVIDIA Driver Downloader .NET
A tool to download NVIDIA Driver Packages.<br><br>
![image](https://github.com/Aetopia/NVIDIA-Driver-Downloader-NET/assets/41850963/32644629-73d5-415a-9d0f-e4ab2542bfc3)

## Usage
1. Download the latest release and open the program.
2. The program will auto detect your system's NVIDIA GPU.
3. Graphic User Inteface Explained:<br>
    - Driver Options
        |Property|Operation|
        |-|-|
        |Driver Version|Select a driver version.
        |Driver Type|Select a driver type.
        |Driver Components|Select which driver components should be extracted alongside the graphics driver. 

    - Driver Type
        |Type|
        |-|
        |Game Ready DCH| 
        |Game Ready STD|
        |Studio DCH|
        |Studio STD|

    - Driver Components
    
        |Component|Included|
        |-|-|
        |Core|Graphics Driver Only.|
        |Core + PhysX|Graphics Driver + PhysX.|
        |Core + HD Audio|Graphics Driver + HDMI Audio.|
        |Core + PhysX + HD Audio|Graphics Driver + PhysX + HDMI Audio.|
        |All|All Driver Components.|

    - Buttons
    |Name|Operation|
    |-|-|
    |`[Download]`| Download a NVIDIA Driver Package.|
    |`[Extract]`| Extract a NVIDIA Driver Package.|

## Building
1. Download and install the .NET SDK and .NET Framework 4.8.1 Developer Pack from:<br>https://dotnet.microsoft.com/en-us/download/visual-studio-sdks
2. Open a Command Prompt or PowerShell window where the `.csproj` file is located.
3. Run the following command:

    ```cmd
    dotnet.exe build "NVIDIA-Driver-Downloader-NET\NVIDIA-Driver-Downloader-NET.csproj" --configuration Release
    ```
