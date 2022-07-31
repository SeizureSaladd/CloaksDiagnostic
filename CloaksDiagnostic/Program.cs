using System.Diagnostics;
using System.Security;

var hostsFile = Environment.OSVersion.Platform switch
{
    PlatformID.MacOSX or PlatformID.Unix => "/etc/hosts",
    PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers/etc/hosts"),
    _ => ""
};

var minecraftFolder = Environment.OSVersion.Platform switch
{
    PlatformID.MacOSX or PlatformID.Unix => "/User/USER/Library/Application/.minecraft", // /home/USER/ for linucks
    PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft"),
    _ => ""
};

var errorCount = 0;

Console.Title = "Cloaks+ Diagnostic - Created by seizure salad#3820";

Console.ForegroundColor = ConsoleColor.White;
Console.Write("Welcome to the unofficial ");
Console.ForegroundColor = ConsoleColor.Blue;
Console.Write("Cloaks");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("+ diagnostic!\n");

if (Directory.Exists(minecraftFolder.Replace(".minecraft", ".tlauncher"))) 
    Error("Cloaks+ doesn't support cracked Minecraft and never will. Please use a premium account to use Cloaks+.");

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

InProgress("Checking if a Cloaks+ cape exists for any Minecraft usernames found...");

var users = GetMinecraftUsers();

if (users.Count == 0) Error("Unable to find any Minecraft accounts!");

for (var i = 0; i < GetMinecraftUsers().Count; i++)
{
    InProgress($"Getting cape for {users[i]}...");
    if (await CapeExists(users[i]))
        Success($"A Cloaks+ cape was found for {users[i]}! Make sure you have a Minecraft installation present and you aren't using cracked!");
    else
    {
        Error($"A Cloaks+ cape could not be found for {users}. Make sure you've verified and registered a cape. This will not be added to the total error count.");
    }
}

Success($"Cloaks+ diagnostic completed. Press any key to exit. Total error count: {errorCount}/4");

Console.ReadKey();


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

List<string> GetMinecraftUsers()
{
    string[] launcherAccounts;
    try
    {
        launcherAccounts = File.ReadAllLines($"{minecraftFolder}\\launcher_accounts.json");
    }
    catch (Exception e)
    {
        Error($"Error while trying to find Minecraft username: {e.Message}");
        return new List<string>();
    }

    return (from t in launcherAccounts where t.Contains("\"name\"") select t[18..].Replace("\"", "")).ToList();
}

async Task<bool> CapeExists(string username)
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
    return File.Exists($"{minecraftFolder}\\optionsof.txt");
}

void InProgress(string message)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("[");
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.Write("-");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"] {message}");
}

void Error(string message)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("[");
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write("X");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"] {message}");
}

void Success(string message)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("[");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("✓");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"] {message}");
}