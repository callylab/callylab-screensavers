using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Globalization;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

class Preferences {
 public int Volume=30; public bool Music=true; public int Speed=100;
 static string FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NightshiftSaver","settings.txt");
 public static Preferences Load(){var s=new Preferences();try{var v=File.ReadAllLines(FileName);s.Volume=Math.Max(0,Math.Min(100,int.Parse(v[0])));s.Music=bool.Parse(v[1]);s.Speed=Math.Max(50,Math.Min(150,int.Parse(v[2])));}catch{}return s;}
 public void Save(){Directory.CreateDirectory(Path.GetDirectoryName(FileName));File.WriteAllLines(FileName,new[]{Volume.ToString(),Music.ToString(),Speed.ToString()});}
}

sealed class Music:IDisposable {
 SoundPlayer player;MemoryStream stream;
 public void Start(int volume){Stop();if(volume<=0)return;using(var source=Assembly.GetExecutingAssembly().GetManifestResourceStream("nightshift.wav")){if(source==null)return;using(var copy=new MemoryStream()){source.CopyTo(copy);byte[] data=copy.ToArray();
 // The bundled WAV is PCM16 stereo, with a standard 44-byte header.
 for(int i=44;i+1<data.Length;i+=2){short sample=(short)(data[i]|data[i+1]<<8);int adjusted=sample*volume/100;data[i]=(byte)adjusted;data[i+1]=(byte)(adjusted>>8);}stream=new MemoryStream(data);player=new SoundPlayer(stream);player.Load();player.PlayLooping();}}}
 public void Stop(){if(player!=null){player.Stop();player.Dispose();player=null;}if(stream!=null){stream.Dispose();stream=null;}}
 public void Dispose(){Stop();}
}

sealed class City:IDisposable {
 public const int W=640,H=360;readonly Bitmap background;readonly List<Light> lights=new List<Light>();
 readonly Dictionary<int,SolidBrush> brushes=new Dictionary<int,SolidBrush>();
 struct Light {public int X,Y;public Color Color;public double Phase;}
 public City(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("city.png"))using(var original=new Bitmap(stream)){background=new Bitmap(W,H,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(background)){g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(original,new Rectangle(0,0,W,H));}}
 // Select actual illuminated background pixels so twinkles stay on building windows.
 var random=new Random(1992);for(int i=0;i<2500 && lights.Count<90;i++){int x=random.Next(18,622),y=random.Next(18,202);Color c=background.GetPixel(x,y);if(c.R+c.G+c.B>440 && Math.Max(c.R,c.B)>170){lights.Add(new Light{X=x,Y=y,Color=c,Phase=random.NextDouble()*Math.PI*2});}}
 }
 SolidBrush B(int hex){SolidBrush b;if(!brushes.TryGetValue(hex,out b)){b=new SolidBrush(Color.FromArgb(255,(hex>>16)&255,(hex>>8)&255,hex&255));brushes.Add(hex,b);}return b;}
 void R(Graphics g,int c,int x,int y,int w,int h){g.FillRectangle(B(c),x,y,w,h);}
 static double Mod(double x,double n){return (x%n+n)%n;}
 public Bitmap Frame(double time,int speed){double t=time*speed/100.0;var frame=new Bitmap(W,H,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(frame)){g.SmoothingMode=SmoothingMode.None;g.CompositingQuality=CompositingQuality.HighSpeed;g.DrawImageUnscaled(background,0,0);
 foreach(var l in lights){double pulse=Math.Sin(t*.32+l.Phase);if(pulse>.74)R(g,0xf5d69b,l.X,l.Y,1,1);else if(pulse<-.91)R(g,0x292741,l.X,l.Y,1,2);}
 // Small neon flickers and a shifting signal, confined to purpose-drawn fixtures.
 if(Math.Sin(t*.7)>.95)R(g,0xffe6a8,154,140,2,1);if(Math.Sin(t*.63+2)>.95)R(g,0xffe6a8,551,150,2,1);Signal(g,469,253,t);
 // People occupy the pavement, with no portrait or close-up framing.
 Person(g,72,251,t,0,false);Person(g,83,251,t,1,false);Person(g,402,251,t,2,false);Person(g,415,250,t,3,false);Person(g,547,251,t,4,false);
 Person(g,(int)Mod(122+t*3,680)-20,252,t,5,true);Person(g,640-(int)Mod(260+t*2.4,680),254,t,6,true);Person(g,(int)Mod(338+t*1.8,690)-25,250,t,7,true);
 // A transit bus pauses at the corner, then continues; the other lane stays moving.
 double cycle=Mod(t,66);double busX=cycle<30?-114+cycle*16:cycle<38?366:366+(cycle-38)*18;
 Bus(g,(int)busX,284,t,cycle>=30&&cycle<38,false);
 Car(g,(int)Mod(t*19+350,920)-80,299,t,1);Car(g,660-(int)Mod(t*23+130,850),315,t,0);
 double second=Mod(t+31,83);if(second<43)Bus(g,660-(int)(second*20),329,t,false,true);
 // Street steam rises as a handful of deliberately hard-edged dithered pixels.
 for(int i=0;i<20;i++){double life=Mod(t*.48+i*.17,3.8);int x=106+(int)(Math.Sin(life*2+i)*3);int y=272-(int)(life*7);if((i+(int)t)%3!=0)R(g,life>2?0x343049:0x5a5269,x,y,2,1);}
 // Occasional reflected pixels of traffic, kept low contrast against the asphalt.
 for(int i=0;i<14;i++){int x=(int)Mod(busX+i*9,640);R(g,i%3==0?0x594c54:0x263444,x,337+i%4*2,3+i%5,1);}
 }return frame;}
 void Lamp(Graphics g,int x,int ground,double t){R(g,0x121929,x,ground-39,2,39);R(g,0x4e5060,x+1,ground-39,1,39);R(g,0x333348,x-5,ground-41,12,3);R(g,Math.Sin(t*.7)>-.96?0xffc47c:0xb8775f,x-3,ground-38,8,2);R(g,0x392d41,x-9,ground,19,1);}
 void Signal(Graphics g,int x,int ground,double t){R(g,0x202538,x,ground-25,2,25);R(g,0x14172b,x-2,ground-30,6,13);int phase=(int)(t/12)%3;R(g,phase==0?0xeb626f:0x502736,x,ground-28,2,2);R(g,phase==1?0xffd37b:0x493d38,x,ground-24,2,2);R(g,phase==2?0x73ccab:0x263b3b,x,ground-20,2,2);}
 void Person(Graphics g,int x,int feet,double t,int id,bool walking){int[] coats={0x855f91,0x447c93,0xa46c48,0x777794,0xa35468,0x3c6579,0x77705a,0x72559c};int[] skins={0xc29677,0x92654f,0xdeb18b,0x6e493f};int c=coats[id%8],skin=skins[id%4];int step=walking?(int)(t*5+id)%4:0;int bob=walking&&step%2==1?1:0;int y=feet-19+bob;
 R(g,0x1a1c30,x-3,feet,9,1);R(g,0x191d32,x,y+12,3,6-bob);R(g,0x2d3550,x+3,y+12,3,6-bob);if(walking){R(g,0x141a2b,x+(step<2?-1:1),feet-2,3,2);R(g,0x141a2b,x+3+(step<2?1:-1),feet-2,3,2);}R(g,0xc2bcb0,x-1,feet-1,3,1);R(g,0xa6abb1,x+3,feet-1,4,1);
 R(g,0x1a1c30,x-2,y+5,9,8);R(g,c,x-1,y+5,7,8);R(g,c,x-2,y+6,2,6);R(g,c,x+6,y+6,2,5);R(g,skin,x+6,y+11,2,2);R(g,skin,x-2,y+11,2,2);R(g,skin,x+1,y+1,4,4);R(g,0x272333,x,y,6,2);R(g,0x272333,x,y+2,1,2);
 if(id%3==0){R(g,0x364662,x-1,y,7,2);R(g,0x516886,x+4,y+1,3,1);}if(id%3==1){R(g,0xbdc0ba,x+2,y+6,2,5);R(g,0x55536c,x-1,y+5,2,7);}if(id==4){R(g,0x765846,x+7,y+10,3,5);}
 }
 void Wheel(Graphics g,int x,int y,double t){R(g,0x111827,x,y,9,7);R(g,0x111827,x+1,y-1,7,9);R(g,0x424354,x+2,y+1,5,5);R(g,0x777583,x+3,y+2,3,3);R(g,0x252738,x+4,y+((int)(t*9)%2==0?1:4),1,2);}
 void Bus(Graphics g,int x,int feet,double t,bool stop,bool reverse){var state=g.Save();if(reverse){g.TranslateTransform(x+106,0);g.ScaleTransform(-1,1);x=0;}int y=feet-34;
 R(g,0x18192c,x+3,feet,104,2);R(g,0x151c2d,x,y+4,111,27);R(g,0x8497a6,x+3,y,101,3);R(g,0xb4b5b0,x+1,y+3,106,23);R(g,0x758a99,x+1,y+23,109,7);R(g,reverse?0xa46a76:0x427c9a,x+1,y+19,108,5);R(g,0xddd0aa,x+5,y+4,86,2);
 for(int i=0;i<7;i++){R(g,0x142c43,x+5+i*12,y+7,10,10);R(g,0x476578,x+5+i*12,y+7,10,2);R(g,0x63818b,x+5+i*12,y+9,1,7);if(i%2==0){R(g,0x30364d,x+9+i*12,y+11,3,3);R(g,0x30364d,x+7+i*12,y+14,6,3);}}
 R(g,0x222d41,x+92,y+6,13,7);R(g,0xe4b56e,x+94,y+8,8,2);R(g,0x1b3044,x+94,y+15,10,13);R(g,0x82969d,x+99,y+15,1,13);if(stop){R(g,0x10182a,x+94,y+15,10,14);R(g,0xbf9b78,x+94,y+27,10,1);}R(g,0xdcc68a,x+106,y+22,4,3);R(g,0xb85069,x+1,y+24,2,3);R(g,0x26344d,x+3,y+29,106,2);Wheel(g,x+16,y+28,t);Wheel(g,x+78,y+28,t);R(g,0xd1c6a8,x+43,y+25,10,1);
 // Small destination display evokes period amber dot-matrix signage.
 R(g,0x172136,x+58,y+2,30,4);for(int i=0;i<10;i++)R(g,0xc39a5b,x+60+i*2,y+3,1,1);
 g.Restore(state);}
 void Car(Graphics g,int x,int feet,double t,int kind){int y=feet-17,c=kind==0?0xd2a05c:0x70618e,hi=kind==0?0xe8bc72:0x9b84ad;R(g,0x171c2c,x,feet,49,2);R(g,0x202239,x+2,y+8,46,9);R(g,c,x+3,y+7,43,9);R(g,c,x+11,y+2,24,7);R(g,hi,x+13,y+1,18,2);R(g,0x213b51,x+12,y+4,10,5);R(g,0x29435a,x+24,y+4,10,5);R(g,hi,x+4,y+10,39,1);R(g,0xe9d3a5,x+2,y+10,3,3);R(g,0xbd536b,x+43,y+11,3,2);R(g,0x85818a,x+1,y+15,47,2);Wheel(g,x+7,y+13,t);Wheel(g,x+34,y+13,t);if(kind==0){R(g,0xc6b09b,x+21,y-2,9,3);R(g,0x44364d,x+24,y-1,3,1);}}
 public void Dispose(){background.Dispose();foreach(var b in brushes.Values)b.Dispose();}
}

class SaverForm:Form {
 readonly City city=new City();readonly Preferences settings;readonly Stopwatch clock;readonly Timer timer=new Timer();Bitmap frame;readonly bool preview,demo;Point initial;bool haveInitial;
 [DllImport("user32.dll")]static extern IntPtr SetParent(IntPtr child,IntPtr parent);
 [DllImport("user32.dll")]static extern int SetWindowLong(IntPtr h,int index,int value);
 [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr h,out RECT rect);
 [DllImport("user32.dll")]static extern bool IsWindow(IntPtr h);
 [StructLayout(LayoutKind.Sequential)]struct RECT{public int L,T,R,B;}
 public SaverForm(Preferences s,Stopwatch watch,bool isDemo,IntPtr parent,Rectangle bounds){settings=s;clock=watch;preview=parent!=IntPtr.Zero;demo=isDemo;Text="NIGHTSHIFT";BackColor=Color.Black;DoubleBuffered=true;KeyPreview=true;FormBorderStyle=demo?FormBorderStyle.Sizable:FormBorderStyle.None;ShowInTaskbar=demo;TopMost=!demo&&!preview;
 if(demo){ClientSize=new Size(1280,720);StartPosition=FormStartPosition.CenterScreen;}else{StartPosition=FormStartPosition.Manual;Bounds=bounds;}
 if(preview){SetWindowLong(Handle,-16,0x50000000);SetParent(Handle,parent);RECT r;GetClientRect(parent,out r);Bounds=new Rectangle(0,0,r.R,r.B);}
 timer.Interval=33;timer.Tick+=(o,e)=>{if(preview){if(!IsWindow(parent)){Close();return;}RECT r;GetClientRect(parent,out r);Size=new Size(Math.Max(1,r.R),Math.Max(1,r.B));}var old=frame;frame=city.Frame(clock.Elapsed.TotalSeconds,settings.Speed);if(old!=null)old.Dispose();Invalidate();};timer.Start();
 if(!preview){KeyDown+=(o,e)=>{if(e.KeyCode==Keys.M){Program.ToggleMusic();return;}Application.Exit();};MouseDown+=(o,e)=>Application.Exit();MouseMove+=(o,e)=>{if(demo||clock.Elapsed.TotalSeconds<2)return;Point p=Cursor.Position;if(!haveInitial){initial=p;haveInitial=true;return;}if(Math.Abs(p.X-initial.X)+Math.Abs(p.Y-initial.Y)>12)Application.Exit();};}
 FormClosed+=(o,e)=>{timer.Stop();timer.Dispose();city.Dispose();if(frame!=null)frame.Dispose();if(!preview)Application.Exit();};
 }
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Color.Black);if(frame==null)return;double scale=Math.Min(ClientSize.Width/(double)City.W,ClientSize.Height/(double)City.H);int w=(int)(City.W*scale),h=(int)(City.H*scale);e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(frame,new Rectangle((ClientSize.Width-w)/2,(ClientSize.Height-h)/2,w,h),0,0,City.W,City.H,GraphicsUnit.Pixel);}
}

static class Program {
 static Music music;static Preferences preferences;static bool playing;
 public static void ToggleMusic(){if(music==null)return;if(playing)music.Stop();else music.Start(preferences.Volume);playing=!playing;}
 [STAThread]static int Main(string[] args){try{return Run(args);}catch(Exception e){if(args.Length>0&&args[0]=="/render"){Console.Error.WriteLine(e);return 1;}MessageBox.Show(e.Message,"NIGHTSHIFT");return 1;}}
 static int Run(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);preferences=Preferences.Load();string mode=args.Length==0?"/c":args[0].ToLowerInvariant();
 if(mode=="/render"){double t=args.Length>2?double.Parse(args[2],CultureInfo.InvariantCulture):20;int width=args.Length>3?int.Parse(args[3]):3840;using(var city=new City())using(var frame=city.Frame(t,100))using(var output=new Bitmap(width,width*9/16)){using(var g=Graphics.FromImage(output)){g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(frame,new Rectangle(0,0,output.Width,output.Height),0,0,640,360,GraphicsUnit.Pixel);}output.Save(args[1],ImageFormat.Png);}return 0;}
 if(mode.StartsWith("/c")){Configure();return 0;}
 bool preview=mode.StartsWith("/p"),demo=mode=="/demo";if(!preview&&!demo&&mode!="/s")return 0;IntPtr parent=IntPtr.Zero;if(preview){string v=mode.Contains(":")?mode.Split(':')[1]:(args.Length>1?args[1]:"0");long h;if(!long.TryParse(v,out h)||h==0)return 0;parent=new IntPtr(h);}
 var watch=Stopwatch.StartNew();var forms=new List<SaverForm>();var screens=Screen.AllScreens;
 using(music=new Music()){if(!preview&&preferences.Music){try{music.Start(preferences.Volume);playing=preferences.Volume>0;}catch{playing=false;}}
 for(int i=0;i<(preview||demo?1:screens.Length);i++){var form=new SaverForm(preferences,watch,demo,parent,screens[i].Bounds);forms.Add(form);form.Show();}
 if(!preview&&!demo)Cursor.Hide();try{Application.Run(forms[0]);}finally{if(!preview&&!demo)Cursor.Show();foreach(var form in forms)if(!form.IsDisposed)form.Dispose();}}
 return 0;}
 static void Configure(){var s=preferences;var w=new Form{Text="NIGHTSHIFT · Settings",ClientSize=new Size(470,360),BackColor=Color.FromArgb(18,20,35),ForeColor=Color.White,StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,Font=new Font("Segoe UI",10)};
 w.Controls.Add(new Label{Text="N I G H T S H I F T",Font=new Font("Segoe UI",23,FontStyle.Bold),Location=new Point(25,20),AutoSize=true});
 var enabled=new CheckBox{Text="Play original FM-synth soundtrack",Checked=s.Music,Location=new Point(25,86),Width=400};w.Controls.Add(enabled);
 var volume=new TrackBar{Minimum=0,Maximum=100,Value=s.Volume,TickFrequency=10,Location=new Point(20,145),Width=420};var label=new Label{Location=new Point(25,121),AutoSize=true};Action text=()=>label.Text="Music volume: "+volume.Value+"%  ·  Press M to mute while running";volume.ValueChanged+=(o,e)=>text();text();w.Controls.Add(label);w.Controls.Add(volume);
 var speed=new TrackBar{Minimum=50,Maximum=150,Value=s.Speed,TickFrequency=25,Location=new Point(20,226),Width=420};var sl=new Label{Text="City pace",Location=new Point(25,201),AutoSize=true};w.Controls.Add(sl);w.Controls.Add(speed);
 var save=new Button{Text="Save settings",Location=new Point(267,297),Size=new Size(170,38),FlatStyle=FlatStyle.Flat};save.Click+=(o,e)=>{s.Volume=volume.Value;s.Music=enabled.Checked;s.Speed=speed.Value;try{s.Save();w.Close();}catch(Exception ex){MessageBox.Show(ex.Message);}};w.Controls.Add(save);Application.Run(w);}
}
