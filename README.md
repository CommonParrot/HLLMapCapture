# Hell Let Loose Map Capture

This is a tool to create screenshots of the map screen in HLL.
It listens on the M Key, to make a screenshot of the game, then detects if the 
the map is open and if it is zoomed out to 1x. If configured that way, 
it will upload this cut out map as a .jpg (or .png if compression is off) 
to a FTP server for further use.

It's written with .NET 8 and only compatible with Windows 10 upwards.

## How to use:

When first started a settings.xml will be generated, there you can configure the tool.
The username and local screenshot save folder can be configured while the app is running.
FTP settings have to be made in the xml, when the tool is closed.

Once the FTP settings are there and IsObfuscated is set to false, the app will obfuscate the values automatically, overwriting the settings.xml and setting IsObfuscated to true.
This is to ensure that the FTP settings are not sent around in plain text, when you share
the tool with your team.

Setting UseCompression to false will result in PNGs being saved and uploaded to the FTP
server instead of much smaller JPEGs.

You can use your team logo as the app icon, just place an .ico in the Icon folder besides the .exe.

## How to build:


1. Install the .NET 8 SDK: https://learn.microsoft.com/en-us/dotnet/core/sdk#how-to-install-the-net-sdk
2. Run this command in this repositories folder:
	```dotnet publish -p:Password=$YourPassword .\HLLMapCapture.sln```
3. You can then find the build under: HLLMapCapture\bin\Release\net8.0-windows\win-x64\publish
#### How to release the build:
1. Start the .exe once
2. Set the FTP settings in the .xml
3. Include your team icon
4. Start the app again to obfuscate the settings with your custom password
5. Zip the publish folder and send to your mates.


## IMPORTANT:
FTP settings are only obfuscated, not securely encrypted!
Anyone who reads through this code (if you use the pre-built release)
or decompiles the tool, will find out how to read the obfuscated settings 
in plain text, if they are determined to do so!

It's best to build the tool once for your team with your custom password.

Be sure to restrict the FTP users rights to what is necessary.
