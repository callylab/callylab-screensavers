using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Collections.Generic;

// 640x360 scene compositor. Detailed pixel backdrop + deterministic ambient sprites.
// No audio dependency, playback path, network access or external runtime assets.
sealed class City : IDisposable {
 public const int W=640,H=360;
 public static string Theme="ROME";
 readonly Bitmap background;
 readonly Dictionary<int,SolidBrush> brushes=new Dictionary<int,SolidBrush>();
 readonly List<Point> lights=new List<Point>();
 readonly List<Point> reflections=new List<Point>();
 public City(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("scene.png"))using(var original=new Bitmap(s)){background=new Bitmap(W,H,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(background)){g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(original,new Rectangle(0,0,W,H));}}
  var rnd=new Random(1992);for(int i=0;i<15000&&lights.Count<130;i++){var p=new Point(rnd.Next(W),rnd.Next(70,240));Color c=background.GetPixel(p.X,p.Y);if(c.R>175&&c.G>85&&c.B<100&&c.R>c.G*1.3)lights.Add(p);}
  for(int i=0;i<10000&&reflections.Count<170;i++){var p=new Point(rnd.Next(W),rnd.Next(Theme=="EDO"?280:275,H));Color c=background.GetPixel(p.X,p.Y);if(c.R>140&&c.G>65&&c.B<100||c.B>110&&c.G>85&&c.R<100)reflections.Add(p);}
 }
 SolidBrush B(int c){SolidBrush b;if(!brushes.TryGetValue(c,out b)){b=new SolidBrush(Color.FromArgb(255,(c>>16)&255,(c>>8)&255,c&255));brushes[c]=b;}return b;}
 void R(Graphics g,int c,int x,int y,int w,int h){g.FillRectangle(B(c),x,y,w,h);}
 void P(Graphics g,int c,params int[] xy){var p=new Point[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Point(xy[i*2],xy[i*2+1]);g.FillPolygon(B(c),p);}
 static double Mod(double x,double n){return (x%n+n)%n;}
 public Bitmap Frame(double seconds,int speed){double t=seconds*speed/100.0;var f=new Bitmap(W,H,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(f)){g.SmoothingMode=SmoothingMode.None;g.InterpolationMode=InterpolationMode.NearestNeighbor;g.DrawImageUnscaled(background,0,0);
  // Water refracts real painted reflections instead of scattering unrelated highlights.
  if(Theme=="EDO"){for(int y=280;y<H;y+=2){int shift=(int)Math.Round(Math.Sin(t*1.3+y*.21)*1.5);g.DrawImage(background,new Rectangle(2+shift,y,W-4,1),new Rectangle(2,y,W-4,1),GraphicsUnit.Pixel);}}
  for(int i=0;i<lights.Count;i++){Point p=lights[i];Color c=background.GetPixel(p.X,p.Y);double pulse=.90+.12*Math.Sin(t*(1.4+i%5*.17)+i);int r=Math.Min(255,(int)(c.R*pulse)),gg=Math.Min(255,(int)(c.G*pulse)),b=Math.Min(255,(int)(c.B*pulse));R(g,(r<<16)|(gg<<8)|b,p.X,p.Y,1,1);}
  for(int i=0;i<reflections.Count;i++){Point p=reflections[i];if(Math.Sin(t*.9+i)>.55){Color c=background.GetPixel(p.X,p.Y);int x=p.X+(int)(Math.Sin(t*1.1+i)*2);R(g,c.ToArgb()&0xffffff,x,p.Y,1+i%3,1);}}
  if(Theme=="ROME"){
   // Fountain streams aligned to the central cyan basin in the background.
   for(int i=0;i<16;i++){int x=311+i*2,y=213+(int)Mod(t*9+i*2,19);if(i<3||i>12)R(g,i%3==0?0x91e9ee:0x386cbd,x,y,1,1);}
   for(int i=0;i<6;i++)Person(g,(int)Mod(i*117+t*(i%2==0?3.7:-2.9),730)-45,252+i%2*6,t,i);
   Cart(g,(int)Mod(t*6+150,880)-120,311,t,false);
   Smoke(g,166,164,t);Smoke(g,496,168,t+2);
  }else if(Theme=="EDO"){
   for(int i=0;i<6;i++){int x=(int)Mod(i*117+t*(i%2==0?3.1:-2.5),730)-45;int y=244;if(x>257&&x<492)y=244-(int)(18*Math.Sin((x-257)*Math.PI/235));Person(g,x,y,t,i);}
   int boat=(int)Mod(t*5+70,840)-100;P(g,0x14152f,boat-4,326,boat+59,326,boat+50,333,boat+5,333);P(g,0x96725c,boat,323,boat+56,323,boat+48,329,boat+7,329);R(g,0xc8a579,boat+6,322,43,2);Person(g,boat+32,322,t,2);R(g,0x947a62,boat+41,304,1,23);R(g,0x373579,boat+9,335,40,1);
   for(int i=0;i<26;i++){double life=Mod(t*.13+i*.139,1);int x=(int)Mod(i*81+t*(2+i%3)+Math.Sin(t*.8+i)*6,650)-5,y=100+(int)(life*170);if(x<135||x>510||y>185)R(g,i%3==0?0xee98d7:0xac64ba,x,y,1,1);}
   Smoke(g,260,181,t);
  }else{
   for(int i=0;i<7;i++)Person(g,(int)Mod(i*103+t*(i%2==0?3.4:-2.6),735)-45,274+i%3*7,t,i);
   Cart(g,(int)Mod(t*5.5+370,880)-130,327,t,true);
   Smoke(g,207,55,t);Smoke(g,307,126,t+2);
   for(int i=0;i<3;i++){int x=428+i*19+(int)(Math.Sin(t*.2+i)*9),y=308+i%2*10;R(g,0x383046,x-2,y,17,2);R(g,0xb3a68c,x,y-7,11,7);R(g,0xe2caa1,x+1,y-7,8,2);R(g,0x433349,x+10,y-6,4,4);R(g,0x766385,x+2,y,1,3);R(g,0x766385,x+8,y,1,3);}
  }
  // Passing fireflies and a rare high-sky bird give subtle activity between traffic cycles.
  for(int i=0;i<9;i++){int x=30+i*71+(int)(Math.Sin(t*.3+i)*7),y=243+i%3*7;double v=Math.Sin(t*.6+i*1.7);if(v>.85)R(g,0xc3ad6f,x,y,1,1);}
 }return f;}
 void Person(Graphics g,int x,int feet,double t,int id){int step=(int)(t*4+id)%4,bob=step%2;int y=feet-16+bob;int[] clothes=Theme=="ROME"?new[]{0xae9cc7,0xa44d73,0xc8b0a0,0x467f91}:Theme=="EDO"?new[]{0x487fae,0xa75d9f,0x888476,0x426b85}:new[]{0x88618f,0x538275,0xaa7854,0x6c65a8};int coat=clothes[id%4];R(g,0x14132e,x-3,feet,12,1);R(g,0x282441,x+(step<2?-1:1),feet-4,2,4);R(g,0x282441,x+4+(step<2?1:-1),feet-4,2,4);R(g,0x1b1935,x-2,y+4,10,9);R(g,coat,x-1,y+5,8,8);R(g,0xc29b79,x+1,y+1,4,4);R(g,0x2b203a,x,y,6,2);R(g,coat,x-3,y+6,2,6);R(g,0xc29b79,x+7,y+10,2,2);R(g,0xd6b697,x+5,y+5,1,6);if(Theme=="EDO"){R(g,0xab8f70,x-3,y-1,11,2);R(g,0x362c59,x,y+9,6,2);}if(Theme=="ROME")R(g,0xe1c6b6,x+1,y+5,2,8);if(feet>265){R(g,0x30305b,x,feet+3,7,1);R(g,0x40365b,x+1,feet+6,5,1);}}
 void Smoke(Graphics g,int x,int y,double t){for(int i=0;i<12;i++){double life=Mod(t*.35+i*.24,4);int xx=x+(int)(Math.Sin(life+i)*2+life),yy=y-(int)(life*5);if(i%3!=0)R(g,life>2?0x252259:0x4c3d73,xx,yy,1+i%2,1);}}
 void Cart(Graphics g,int x,int y,double t,bool horse){int hy=y-15;R(g,0x211b37,x-4,y,85,1);R(g,horse?0x78604e:0x8f7f8f,x,hy-6,25,11);R(g,0xb19c85,x+1,hy-6,21,2);R(g,0x554059,x+19,hy-15,6,14);R(g,0xb29c85,x+20,hy-17,12,6);R(g,0x221a37,x+30,hy-16,2,2);for(int i=0;i<4;i++)R(g,0x463451,x+i*7+(int)(Math.Sin(t*4+i)*2),hy+4,2,10);R(g,0x7f6649,x+26,hy+1,13,1);R(g,0x89615b,x+39,hy-4,37,13);R(g,0xc5a271,x+39,hy-5,37,2);for(int i=0;i<4;i++)R(g,0x4a3455,x+43+i*9,hy-2,1,10);for(int i=0;i<2;i++){int wx=x+43+i*24;g.FillEllipse(B(0x17162f),wx,hy+4,10,10);R(g,0x8e78a0,wx+4,hy+6,2,6);R(g,0x8e78a0,wx+2,hy+8,6,2);}Person(g,x+55,hy-4,t,1);R(g,0x393059,x+7,y+4,63,1);}
 public void Dispose(){background.Dispose();foreach(var b in brushes.Values)b.Dispose();}
}
