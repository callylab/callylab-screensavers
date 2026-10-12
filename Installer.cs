using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using Microsoft.Win32;

// Per-user setup. Does not require elevation or modify sign-in-on-resume.
class Installer : Form {
#if EDO
 new const string Name="EDO";
#elif VILLAGE
 new const string Name="HEARTH";
#elif NIGHTSHIFT
 new const string Name="NIGHTSHIFT";
#else
 new const string Name="ROME";
#endif
 static readonly string Dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","CallyLab",Name);
 static readonly string Saver=Path.Combine(Dir,Name+".scr");
 const string Desktop=@"Control Panel\Desktop";
 static readonly string UninstallKey=@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CallyLab-"+Name;
 static readonly string[] Names={"SCRNSAVE.EXE","ScreenSaveActive","ScreenSaveTimeOut"};
 [DllImport("user32.dll",SetLastError=true)] static extern bool SystemParametersInfo(uint a,uint v,IntPtr p,uint f);
 [STAThread]static int Main(string[] args){Application.EnableVisualStyles();try{
  if(args.Length>0&&args[0]=="/verify-payload"){Extract(args[1]);return 0;}
  if(args.Length>0&&args[0]=="/install"){Install();return 0;}
  if(args.Length>0&&args[0]=="/uninstall"){if(args.Length>1&&args[1]=="/quiet"||MessageBox.Show("Remove "+Name+" and restore the previous screensaver if still selected?",Name,MessageBoxButtons.YesNo)==DialogResult.Yes)Remove();return 0;}
  Application.Run(new Installer());return 0;
 }catch(Exception e){if(Array.IndexOf(args,"/quiet")<0)MessageBox.Show(e.Message,Name+" setup",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}}
 Installer(){Text=Name+" Setup";ClientSize=new Size(475,260);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;BackColor=Color.FromArgb(20,23,22);ForeColor=Color.FromArgb(210,215,205);Font=new Font("Segoe UI",10);
 Controls.Add(new Label{Text=Name,Font=new Font("Segoe UI",25),Location=new Point(25,20),AutoSize=true});Controls.Add(new Label{Text="Install for your Windows account. No administrator needed.\nSelects this screensaver with a five-minute idle timeout.\nYour sign-in-on-resume setting stays unchanged.\nRemove later in Windows Settings > Apps.",Location=new Point(25,85),Size=new Size(430,95)});
 var b=new Button{Text="Install",Location=new Point(310,195),Size=new Size(130,38)};b.Click+=(o,e)=>{try{Install();MessageBox.Show("Installed. Open Windows Screen Saver Settings to preview or change the timeout.",Name);Close();}catch(Exception ex){MessageBox.Show(ex.Message,Name+" setup");}};Controls.Add(b);var c=new Button{Text="Close",Location=new Point(25,195),Size=new Size(100,38)};c.Click+=(o,e)=>Close();Controls.Add(c);}
 static void Extract(string path){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.scr")){if(s==null)throw new IOException("Missing payload");using(var f=File.Create(path))s.CopyTo(f);}using(var sha=SHA256.Create())using(var f=File.OpenRead(path))using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.scr"))if(Convert.ToBase64String(sha.ComputeHash(f))!=Convert.ToBase64String(sha.ComputeHash(s)))throw new IOException("Payload checksum mismatch");}
 static void SPI(uint a,uint v){if(!SystemParametersInfo(a,v,IntPtr.Zero,3))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
 new static string[] Capture(){using(var k=Registry.CurrentUser.OpenSubKey(Desktop)){var a=new string[3];for(int i=0;i<3;i++){object v=k.GetValue(Names[i]);a[i]=v==null?"-":"+"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(v.ToString()));}return a;}}
 static string Decode(string v){return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(v.Substring(1)));}
 static void Restore(string[] values){if(values.Length!=3)throw new IOException("Invalid settings backup");uint timeout=600; if(values[2]!="-")uint.TryParse(Decode(values[2]),out timeout);SPI(15,timeout);SPI(17,values[1]!="-"&&Decode(values[1])=="1"?1u:0u);using(var k=Registry.CurrentUser.CreateSubKey(Desktop))for(int i=0;i<3;i++){if(values[i]=="-")k.DeleteValue(Names[i],false);else k.SetValue(Names[i],Decode(values[i]),RegistryValueKind.String);}}
 static void Install(){using(var policy=Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\Control Panel\Desktop"))if(policy!=null)foreach(var n in Names)if(policy.GetValue(n)!=null)throw new InvalidOperationException("Screensaver settings are managed by your organization.");
 var previous=Capture();bool existing=File.Exists(Saver);byte[] old=existing?File.ReadAllBytes(Saver):null;Directory.CreateDirectory(Dir);string stage=Saver+".new",backup=Path.Combine(Dir,"previous.txt");bool newBackup=!File.Exists(backup);
 try{Extract(stage);if(newBackup)File.WriteAllLines(backup,previous);string setup=Path.Combine(Dir,"Uninstall.exe");if(!String.Equals(Assembly.GetExecutingAssembly().Location,setup,StringComparison.OrdinalIgnoreCase))File.Copy(Assembly.GetExecutingAssembly().Location,setup,true);File.Copy(stage,Saver,true);File.Delete(stage);
 using(var k=Registry.CurrentUser.CreateSubKey(Desktop))k.SetValue("SCRNSAVE.EXE",Saver);SPI(15,300);SPI(17,1);
 using(var k=Registry.CurrentUser.CreateSubKey(UninstallKey)){k.SetValue("DisplayName","CallyLab "+Name+" Screensaver");k.SetValue("DisplayVersion","1.0.0");k.SetValue("Publisher","CallyLab");k.SetValue("InstallLocation",Dir);k.SetValue("UninstallString","\""+setup+"\" /uninstall");k.SetValue("QuietUninstallString","\""+setup+"\" /uninstall /quiet");k.SetValue("NoModify",1);k.SetValue("NoRepair",1);}
 }catch{Restore(previous);if(old!=null)File.WriteAllBytes(Saver,old);else if(File.Exists(Saver))File.Delete(Saver);if(File.Exists(stage))File.Delete(stage);if(newBackup&&File.Exists(backup))File.Delete(backup);throw;}}
 static void Remove(){string backup=Path.Combine(Dir,"previous.txt");using(var k=Registry.CurrentUser.OpenSubKey(Desktop))if(String.Equals(Convert.ToString(k.GetValue("SCRNSAVE.EXE")),Saver,StringComparison.OrdinalIgnoreCase)){if(!File.Exists(backup))throw new IOException("Previous settings backup is missing. Select another screensaver in Windows before uninstalling.");Restore(File.ReadAllLines(backup));}
 if(File.Exists(Saver))File.Delete(Saver);Registry.CurrentUser.DeleteSubKeyTree(UninstallKey,false);if(File.Exists(backup))File.Delete(backup);string setup=Path.Combine(Dir,"Uninstall.exe");if(!String.Equals(Assembly.GetExecutingAssembly().Location,setup,StringComparison.OrdinalIgnoreCase)){if(File.Exists(setup))File.Delete(setup);if(Directory.Exists(Dir)&&Directory.GetFileSystemEntries(Dir).Length==0)Directory.Delete(Dir);}else{
 // A running executable cannot delete itself: a hidden PowerShell helper waits for exit.
 string escaped=Dir.Replace("'","''");string script="Wait-Process -Id "+Process.GetCurrentProcess().Id+" -ErrorAction SilentlyContinue; Remove-Item -LiteralPath '"+escaped+"\\Uninstall.exe' -Force; if ((Get-ChildItem -LiteralPath '"+escaped+"' -Force | Measure-Object).Count -eq 0) { Remove-Item -LiteralPath '"+escaped+"' }";string encoded=Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -NonInteractive -EncodedCommand "+encoded){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});}
 }
}

