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

##### IMPORTANT:
FTP settings are only a obfuscated for now, not securely encrypted!
Anyone who reads through this code or decompiles the tool, will find out how to read
the obfuscated settings in plain text, if they are determined to do so!
You can change the code to use your own password/key in Obfuscator.cs, but even then
the program can still be decompiled.

Be sure to restrict the FTP users rights to what is necessary.
