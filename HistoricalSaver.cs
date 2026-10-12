using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
class Preferences {
 public int Speed=100;
 static string FileName { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CallyLab",City.Theme,"settings.txt"); } }
 public static Preferences Load(){var s=new Preferences();try{s.Speed=Math.Max(50,Math.Min(150,int.Parse(File.ReadAllText(FileName))));}catch{}return s;}
 public void Save(){Directory.CreateDirectory(Path.GetDirectoryName(FileName));File.WriteAllText(FileName,Speed.ToString());}
}
class SaverForm:Form {
 readonly City city=new City();readonly Preferences settings;readonly Stopwatch clock;readonly Timer timer=new Timer();Bitmap frame;readonly bool preview,demo;Point initial;bool haveInitial;
 [DllImport("user32.dll")]static extern IntPtr SetParent(IntPtr child,IntPtr parent);
 [DllImport("user32.dll")]static extern int SetWindowLong(IntPtr h,int index,int value);
 [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr h,out RECT rect);
 [DllImport("user32.dll")]static extern bool IsWindow(IntPtr h);
 [StructLayout(LayoutKind.Sequential)]struct RECT{public int L,T,R,B;}
 public SaverForm(Preferences s,Stopwatch watch,bool isDemo,IntPtr parent,Rectangle bounds){settings=s;clock=watch;preview=parent!=IntPtr.Zero;demo=isDemo;Text=City.Theme;BackColor=Color.Black;DoubleBuffered=true;KeyPreview=true;FormBorderStyle=demo?FormBorderStyle.Sizable:FormBorderStyle.None;ShowInTaskbar=demo;TopMost=!demo&&!preview;
 if(demo){ClientSize=new Size(1280,720);StartPosition=FormStartPosition.CenterScreen;}else{StartPosition=FormStartPosition.Manual;Bounds=bounds;}
 if(preview){SetWindowLong(Handle,-16,0x50000000);SetParent(Handle,parent);RECT r;GetClientRect(parent,out r);Bounds=new Rectangle(0,0,r.R,r.B);}
 timer.Interval=33;timer.Tick+=(o,e)=>{if(preview){if(!IsWindow(parent)){Close();return;}RECT r;GetClientRect(parent,out r);Size=new Size(Math.Max(1,r.R),Math.Max(1,r.B));}var old=frame;frame=city.Frame(clock.Elapsed.TotalSeconds,settings.Speed);if(old!=null)old.Dispose();Invalidate();};timer.Start();
 if(!preview){KeyDown+=(o,e)=>{Application.Exit();};MouseDown+=(o,e)=>Application.Exit();MouseMove+=(o,e)=>{if(demo||clock.Elapsed.TotalSeconds<2)return;Point p=Cursor.Position;if(!haveInitial){initial=p;haveInitial=true;return;}if(Math.Abs(p.X-initial.X)+Math.Abs(p.Y-initial.Y)>12)Application.Exit();};}
 FormClosed+=(o,e)=>{timer.Stop();timer.Dispose();city.Dispose();if(frame!=null)frame.Dispose();if(!preview)Application.Exit();};
 }
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Color.Black);if(frame==null)return;double scale=Math.Min(ClientSize.Width/(double)City.W,ClientSize.Height/(double)City.H);int w=(int)(City.W*scale),h=(int)(City.H*scale);e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(frame,new Rectangle((ClientSize.Width-w)/2,(ClientSize.Height-h)/2,w,h),0,0,City.W,City.H,GraphicsUnit.Pixel);}
}


static class Program {
 [STAThread] static int Main(string[] args){try{return Run(args);}catch(Exception e){if(args.Length>0&&args[0]=="/render")return 1;MessageBox.Show(e.Message,City.Theme);return 1;}}
 static int Run(string[] args){
#if EDO
 City.Theme="EDO";
#elif VILLAGE
 City.Theme="HEARTH";
#else
 City.Theme="ROME";
#endif
 Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
 string mode=args.Length==0?"/c":args[0].ToLowerInvariant();var settings=Preferences.Load();
 if(mode=="/render"){double t=args.Length>2?double.Parse(args[2],CultureInfo.InvariantCulture):20;using(var city=new City())using(var frame=city.Frame(t,100))frame.Save(args[1],ImageFormat.Png);return 0;}
 if(mode.StartsWith("/c")){var w=new Form{Text=City.Theme+" - Settings",ClientSize=new Size(420,215),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false};w.Controls.Add(new Label{Text="Silent historical pixel scenery\nAmbient pace (50-150%)",Location=new Point(20,20),Size=new Size(380,45)});var speed=new TrackBar{Minimum=50,Maximum=150,Value=settings.Speed,TickFrequency=25,Location=new Point(20,75),Width=380};w.Controls.Add(speed);var save=new Button{Text="Save",Location=new Point(290,150),Size=new Size(100,35)};save.Click+=(o,e)=>{settings.Speed=speed.Value;settings.Save();w.Close();};w.Controls.Add(save);Application.Run(w);return 0;}
 bool preview=mode.StartsWith("/p"),demo=mode=="/demo";if(!preview&&!demo&&mode!="/s")return 0;IntPtr parent=IntPtr.Zero;if(preview){string v=mode.Contains(":")?mode.Split(':')[1]:(args.Length>1?args[1]:"0");long h;if(!long.TryParse(v,out h)||h==0)return 0;parent=new IntPtr(h);if(!IsWindow(parent))return 0;}
 var watch=Stopwatch.StartNew();var forms=new List<SaverForm>();for(int i=0;i<(preview||demo?1:Screen.AllScreens.Length);i++){var f=new SaverForm(settings,watch,demo,parent,Screen.AllScreens[i].Bounds);forms.Add(f);f.Show();}if(!preview&&!demo)Cursor.Hide();try{Application.Run(forms[0]);}finally{if(!preview&&!demo)Cursor.Show();foreach(var f in forms)if(!f.IsDisposed)f.Dispose();}return 0;
 }
 [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
}

