using System.Diagnostics;
using System.Security;

// unused due to unreliable behavior; PlatformID.Unix is returned on macOS.
// var hostsFile = Environment.OSVersion.Platform switch
// {
//     PlatformID.MacOSX or PlatformID.Unix => "/etc/hosts",
//     PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers/etc/hosts"),
//     _ => ""
// };
//
// var minecraftFolder = Environment.OSVersion.Platform switch
// {
//     PlatformID.MacOSX or PlatformID.Unix => $"/User/{Environment.UserName}/Library/Application/.minecraft", // /home/USER/ for linucks
//     PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft"),
//     _ => ""
// };

var hostsFile = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux()
    ? "/etc/hosts"
    : OperatingSystem.IsWindows()
        ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers/etc/hosts")
        : "";

var minecraftFolder = OperatingSystem.IsMacOS()
    ? $"/Users/{Environment.UserName}/Library/Application Support/minecraft"
    : OperatingSystem.IsWindows()
        ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers/etc/hosts")
        : OperatingSystem.IsLinux()
            ? $"/home/{Environment.UserName}/.minecraft"
            : "";

var errorCount = 0;

Console.Clear(); //for macOS

Console.Title = "Cloaks+ Diagnostic - Created by seizure salad#3820";

Console.ForegroundColor = ConsoleColor.White;
SmoothWrite("Welcome to the unofficial ");
Console.ForegroundColor = ConsoleColor.Blue;
SmoothWrite("Cloaks");
Console.ForegroundColor = ConsoleColor.White;
SmoothWriteLine("+ diagnostic!\n");

if (Directory.Exists(minecraftFolder.Replace("minecraft", "tlauncher"))) 
    Error("Cloaks+ doesn't support cracked Minecraft and never will. Please use a premium account in order to use Cloaks+.");

InProgress("Checking for hosts file problems...");
if (HostsAvailable())
    Success("No hosts file problems detected!");
else
{
    errorCount += 1;
    Error("Problems with hosts file detected!");
}

InProgress("Checking for Cloaks+ installation...");
if (CloaksInstalled())
    Success("A Cloaks+ installation was found!");
else
{
    errorCount += 1;
    Error("A Cloaks+ installation was not found!");
}

InProgress("Checking if Cloaks+ is working...");
if (await OptifineWorking())
    Success("Cloaks+ successfully redirects Optifine traffic!");
else
{
    Error("Cloaks+ is failing to redirect traffic. Check if you have Cloaks+ installed or if Optifine servers are up.");
    errorCount += 1;
}

InProgress("Checking if Optifine is installed...");
if (OptifineInstalled())
    Success("An Optifine installation was found!");
else
{
    errorCount += 1;
    Error("An Optifine installation could not be found. The Optifine website will open in your browser where you can download it from.");
    Process.Start(new ProcessStartInfo("https://optifine.net") { UseShellExecute = true});
}

InProgress("Checking if vanilla capes are enabled...");
if (VanillaCapesEnabled())
    Success("Vanilla capes are enabled!");
else
{
    errorCount += 1;
    Error("Capes are not enabled in vanilla settings. You can enable them in Skin Customization... -> Cape.");
}

InProgress("Checking if Optifine capes are enabled...");
if (OptifineCapesEnabled())
    Success("Optifine capes are enabled!");
else
{
    errorCount += 1;
    Error("Capes are not enabled in Optifine settings. You can enable them in Video Settings... -> Details... -> Show Capes.");
}


InProgress("Checking if a Cloaks+ cape exists for any Minecraft usernames found...");

var users = GetMinecraftUsers();

if (users == null) Error("Unable to find any Minecraft accounts!");

for (var i = 0; i < GetMinecraftUsers()?.Count; i++)
{
    InProgress($"Getting cape for {users?[i]}...");
    if (await CapeExists(users?[i]))
        Success($"A Cloaks+ cape was found for {users?[i]}!");
    else
    {
        Error($"A Cloaks+ cape could not be found for {users?[i]}. Make sure you've verified and registered a cape. This will not be added to the total error count.");
    }
}

Success($"Cloaks+ diagnostic completed. Press any key to exit. Total error count: {errorCount}/6");

Console.ReadKey();

//************************************************************************
// Code should be self explanatory enough

async Task<bool> OptifineWorking()
{
    using var client = new HttpClient();
    try
    {
        var response = await client.GetStringAsync("http://s.optifine.net");
        return !response.Contains("Not found");
    }
    catch (Exception e)
    {
        Error($"Error while fetching Optifine servers: {e.Message}");
        return false;
    }
}

bool CloaksInstalled()
{
    try
    {
        return File.ReadAllText(hostsFile).Contains("161.35.130.99");
    }
    catch (Exception)
    {
        return false;
    }
}

List<string>? GetMinecraftUsers()
{
    if (!File.Exists($"{minecraftFolder}/launcher_accounts.json")) 
        return null;

    var launcherAccounts = File.ReadAllLines($"{minecraftFolder}/launcher_accounts.json");
    return (from t in launcherAccounts where t.Contains("\"name\"") select t[18..].Replace("\"", "")).ToList();
}

bool VanillaCapesEnabled()
{
    if (!File.Exists($"{minecraftFolder}/options.txt"))
        return false;

    var options = File.ReadAllLines($"{minecraftFolder}/options.txt");
    return (from t in options where t.StartsWith("modelPart_cape") select t == "modelPart_cape:true").FirstOrDefault();
}

bool OptifineCapesEnabled()
{
    if (!OptifineInstalled())
        return false;

    var options = File.ReadAllLines($"{minecraftFolder}/optionsof.txt");
    return (from t in options where t.StartsWith("ofShowCapes") select t == "ofShowCapes:true").FirstOrDefault();
}

async Task<bool> CapeExists(string? username)
{
    using var client = new HttpClient();
    try
    {
        var response = await client.GetAsync($"https://server.cloaksplus.com/capes/{username}.png");
        return response.IsSuccessStatusCode;
    }
    catch (Exception e)
    {
        Error($"Error while fetching Cloaks+ cape: {e.Message}");
        return false;
    }
}

bool HostsAvailable()
{
    try
    {
        using var stream = new FileInfo(hostsFile).Open(FileMode.Open, FileAccess.Read, FileShare.None);
        stream.Close();
    }
    catch (SecurityException e)
    {
        Error($"Error! Insufficient permissions: {e.Message}");
        return false;
    }
    catch (FileNotFoundException e)
    {
        Error($"Error! Hosts file can't be found: {e.Message}");
        return false;
    }
    catch (UnauthorizedAccessException e)
    {
        Error($"Error! Hosts file is read-only: {e.Message}");
        return false;
    }
    catch (IOException e)
    {
        Error($"Error! Hosts file is being used: {e.Message}");
        return false;
    }
    catch (Exception e)
    {
        Error($"Error while trying to check hosts availability: {e.Message}");
        return false;
    }

    return true;
}

bool OptifineInstalled()
{
    return File.Exists($"{minecraftFolder}/optionsof.txt");
}

void InProgress(string message)
{
    Console.ForegroundColor = ConsoleColor.White;
    SmoothWrite("[");
    Console.ForegroundColor = ConsoleColor.Blue;
    SmoothWrite("-");
    Console.ForegroundColor = ConsoleColor.White;
    SmoothWriteLine($"] {message}");
}

void Error(string message)
{
    Console.ForegroundColor = ConsoleColor.White;
    SmoothWrite("[");
    Console.ForegroundColor = ConsoleColor.Red;
    SmoothWrite("X");
    Console.ForegroundColor = ConsoleColor.White;
    SmoothWriteLine($"] {message}");
}

void Success(string message)
{
    Console.ForegroundColor = ConsoleColor.White;
    SmoothWrite("[");
    Console.ForegroundColor = ConsoleColor.Green;
    SmoothWrite("✓");
    Console.ForegroundColor = ConsoleColor.White;
    SmoothWriteLine($"] {message}");
}

void SmoothWriteLine(string text)
{
    foreach (char ch in text + "\n")
    {
        Thread.Sleep(20);
        Console.Write(ch);
    }
}

void SmoothWrite(string text)
{
    foreach (char ch in text)
    {
        Thread.Sleep(20);
        Console.Write(ch);
    }
}