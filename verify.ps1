$ErrorActionPreference='Stop'
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class NativeQA {
 [DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]public static extern IntPtr GetParent(IntPtr h);
 [DllImport("user32.dll")]public static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")]public static extern bool EnumWindows(EnumProc p,IntPtr l);
 [DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr h,out uint id);
 public static IntPtr Find(int pid){IntPtr found=IntPtr.Zero;EnumWindows(delegate(IntPtr h,IntPtr l){uint id;GetWindowThreadProcessId(h,out id);if(id==pid){found=h;return false;}return true;},IntPtr.Zero);return found;}
 [DllImport("user32.dll")]public static extern bool EnumChildWindows(IntPtr h, EnumProc p, IntPtr l);
 public delegate bool EnumProc(IntPtr h,IntPtr l);
}
'@
Add-Type -AssemblyName System.Windows.Forms
$log=New-Object System.Collections.Generic.List[string]
function Check($condition,$message){if(!$condition){throw $message};$log.Add("PASS: $message");$log | Set-Content "$PSScriptRoot/release/validation.txt"}
function Run($file,$arguments){$si=New-Object System.Diagnostics.ProcessStartInfo;$si.FileName=$file;$si.Arguments=$arguments;$si.UseShellExecute=$false;return [System.Diagnostics.Process]::Start($si)}
function Finish($p){if(!$p.WaitForExit(10000)){throw "Process did not exit: $($p.Id)"};Check ($p.ExitCode -eq 0) 'Process exited successfully'}
$reg='HKCU:\Control Panel\Desktop'
$names=@('SCRNSAVE.EXE','ScreenSaveActive','ScreenSaveTimeOut','ScreenSaverIsSecure')
function Snapshot { $k=Get-Item $reg;$a=@{};foreach($n in $names){$a[$n]=$k.GetValue($n,$null)};return $a }
$before=Snapshot
foreach($n in @('ROME','EDO','HEARTH','NIGHTSHIFT')){
 $v=if($n -eq 'NIGHTSHIFT'){'1.0.1'}else{'1.0.0'}
 $dir="$PSScriptRoot/release/$n-$v";$setup=(Resolve-Path "$dir/$n-Setup.exe").Path;$scr=(Resolve-Path "$dir/$n.scr").Path
 $installed="$env:LOCALAPPDATA/Programs/CallyLab/$n";$p=$null
 try{
  $extract="$PSScriptRoot/release/$n-payload.scr";Finish (Run $setup ('/verify-payload "'+$extract+'"'))
  Check ((Get-FileHash $extract).Hash -eq (Get-FileHash $scr).Hash) "$n installer payload matches standalone SCR"
  Finish (Run $setup '/install /quiet')
  Check (Test-Path "$installed/$n.scr") "$n actual per-user installation created SCR"
  Check ((Get-FileHash "$installed/$n.scr").Hash -eq (Get-FileHash $scr).Hash) "$n installed bytes match release"
  Check ((Get-Item $reg).GetValue('SCRNSAVE.EXE') -eq "$installed/$n.scr".Replace('/','\')) "$n Windows selected installed saver"
  Check (Test-Path "HKCU:/Software/Microsoft/Windows/CurrentVersion/Uninstall/CallyLab-$n") "$n registered uninstall entry"
  Check ((Get-Item $reg).GetValue('ScreenSaverIsSecure',$null) -eq $before['ScreenSaverIsSecure']) "$n preserved sign-in setting"
  $p=Run $scr '/c';Start-Sleep -Milliseconds 1000;$p.Refresh();Check (!$p.HasExited -and $p.MainWindowHandle -ne 0) "$n configuration window opened";[NativeQA]::PostMessage($p.MainWindowHandle,0x10,[IntPtr]::Zero,[IntPtr]::Zero)|Out-Null;Finish $p
  # A real WinForms parent exercises the native /p embedding protocol.
  $hostForm=New-Object System.Windows.Forms.Form;$hostForm.ClientSize=New-Object System.Drawing.Size(320,180);$handle=$hostForm.Handle
  $p=Run $scr ("/p "+$handle.ToInt64());Start-Sleep -Milliseconds 1200
  $script:found=$false;[NativeQA]::EnumChildWindows($handle,{param($h,$l)if([NativeQA]::GetParent($h) -eq $handle){$script:found=$true};return $true},[IntPtr]::Zero)|Out-Null
  Check $script:found "$n preview embedded in native parent"
  $hostForm.Dispose();Finish $p
  $p=Run $scr '/s';$fullHandle=[IntPtr]::Zero;for($attempt=0;$attempt -lt 80;$attempt++){Start-Sleep -Milliseconds 100;$p.Refresh();if($p.HasExited){break};$fullHandle=[NativeQA]::Find($p.Id);if($fullHandle -ne [IntPtr]::Zero){break}};Check (!$p.HasExited -and $fullHandle -ne [IntPtr]::Zero) "$n fullscreen launch opened"
  [NativeQA]::PostMessage($fullHandle,0x100,[IntPtr]27,[IntPtr]::Zero)|Out-Null;Finish $p
 }finally{
  if($p -and !$p.HasExited){[NativeQA]::PostMessage($p.MainWindowHandle,0x10,[IntPtr]::Zero,[IntPtr]::Zero)|Out-Null;$p.WaitForExit(3000)|Out-Null}
  $uninstaller="$installed/Uninstall.exe";if(Test-Path $uninstaller){Finish (Run $uninstaller '/uninstall /quiet');Start-Sleep -Seconds 2}
 }
 Check (!(Test-Path "$installed/$n.scr")) "$n uninstall removed screensaver"
 Check (!(Test-Path "HKCU:/Software/Microsoft/Windows/CurrentVersion/Uninstall/CallyLab-$n")) "$n uninstall removed app registration"
 $after=Snapshot;foreach($key in $names){Check ($after[$key] -eq $before[$key]) "$n restored $key"}
}
$log | Set-Content "$PSScriptRoot/release/validation.txt"
$log



