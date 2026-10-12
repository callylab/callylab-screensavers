using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
// Local scenery-motion study: no people, vehicles, animals or audio.
sealed class City:IDisposable {
 public const int W=640,H=360;public static string Theme="ROME";
 readonly Bitmap background;readonly int[] pixels=new int[W*H];readonly bool[] sky=new bool[W*H];
 readonly List<Point> lights=new List<Point>(),stars=new List<Point>();readonly Point[] fires,chimneys;
 readonly Dictionary<int,SolidBrush> brushes=new Dictionary<int,SolidBrush>();
 public City(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("scene.png"))using(var b=new Bitmap(s)){background=new Bitmap(W,H,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(background)){g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(b,new Rectangle(0,0,W,H));}}
  var d=background.LockBits(new Rectangle(0,0,W,H),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);Marshal.Copy(d.Scan0,pixels,0,pixels.Length);background.UnlockBits(d);
  for(int y=0;y<130;y++)for(int x=0;x<W;x++){int c=pixels[y*W+x],r=(c>>16)&255,gg=(c>>8)&255,b=c&255;sky[y*W+x]=y<Horizon(x)&&b>r*1.35&&b>gg*1.15;}
  var rnd=new Random(1992);for(int i=0;i<22000&&lights.Count<300;i++){var p=new Point(rnd.Next(W),rnd.Next(55,250));int c=pixels[p.Y*W+p.X],r=(c>>16)&255,gg=(c>>8)&255,b=c&255;if(r>175&&gg>85&&b<95&&r>gg*1.25)lights.Add(p);}
  for(int i=0;i<4000&&stars.Count<55;i++){var p=new Point(rnd.Next(W),rnd.Next(4,105));if(sky[p.Y*W+p.X])stars.Add(p);}
  if(Theme=="ROME"){fires=new[]{new Point(94,152),new Point(165,153),new Point(251,173),new Point(277,214),new Point(393,214),new Point(530,211),new Point(577,129)};chimneys=new[]{new Point(94,149),new Point(165,149),new Point(251,167),new Point(277,207),new Point(393,207),new Point(530,204),new Point(192,98)};}
  else if(Theme=="EDO"){fires=new[]{new Point(9,236),new Point(97,234),new Point(240,233),new Point(516,232),new Point(589,232)};chimneys=new[]{new Point(118,118),new Point(260,177),new Point(564,145),new Point(318,147)};}
  else{fires=new[]{new Point(45,171),new Point(130,185),new Point(179,197),new Point(458,205),new Point(540,205),new Point(600,211)};chimneys=new[]{new Point(207,54),new Point(307,123),new Point(582,54),new Point(129,103)};}
 }
 int Horizon(int x){int[] xs={0,90,130,160,205,250,280,310,380,425,465,500,540,570,640};int[] hs=Theme=="ROME"?new[]{10,13,16,28,40,37,45,48,44,43,34,24,23,12,9}:Theme=="EDO"?new[]{0,0,3,31,44,48,67,82,83,69,45,35,34,40,48}:new[]{0,5,44,49,50,62,65,40,36,55,58,30,25,5,0};for(int i=0;i<xs.Length-1;i++)if(x<=xs[i+1]){double a=(x-xs[i])/(double)(xs[i+1]-xs[i]);return (int)(hs[i]*(1-Smooth(a))+hs[i+1]*Smooth(a));}return 0;} static double Mod(double x,double n){return (x%n+n)%n;}static double Smooth(double x){return x*x*(3-2*x);}
 static double Hash(int x,int y){unchecked{int n=x*374761393+y*668265263;n=(n^(n>>13))*1274126177;return (n&0x7fffffff)/2147483647.0;}}
 static double Noise(double x,double y){int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);double fx=Smooth(x-ix),fy=Smooth(y-iy);double a=Hash(ix,iy)*(1-fx)+Hash(ix+1,iy)*fx,b=Hash(ix,iy+1)*(1-fx)+Hash(ix+1,iy+1)*fx;return a*(1-fy)+b*fy;}
 static int Blend(int c,int o,double a){int r=(int)(((c>>16)&255)*(1-a)+((o>>16)&255)*a),g=(int)(((c>>8)&255)*(1-a)+((o>>8)&255)*a),b=(int)((c&255)*(1-a)+(o&255)*a);return unchecked((int)0xff000000)|(r<<16)|(g<<8)|b;}
 SolidBrush B(int c,int a){int key=(a<<24)|c;SolidBrush b;if(!brushes.TryGetValue(key,out b)){b=new SolidBrush(Color.FromArgb(a,(c>>16)&255,(c>>8)&255,c&255));brushes[key]=b;}return b;}
 void R(Graphics g,int c,int x,int y,int w,int h){g.FillRectangle(B(c,255),x,y,w,h);}void A(Graphics g,int c,int a,int x,int y,int w,int h){if(a>0)g.FillRectangle(B(c,a),x,y,w,h);}
 public Bitmap Frame(double seconds,int speed){double t=seconds*speed/100.0;int[] frame=(int[])pixels.Clone();
  // Independently moving cloud layers clip against a sky mask, preserving roofs.
  for(int y=0;y<125;y++)for(int x=0;x<W;x++){int k=y*W+x;if(!sky[k])continue;double n=Noise((x-t*7)/48.0,y/13.0)+Noise((x+t*3)/23.0,(y+40)/8.0)*.45;double cloud=Math.Max(0,(n-.55)*.8);frame[k]=Blend(frame[k],0x6455bc,Math.Min(.55,cloud+.035+.025*Math.Sin(t*.45+x*.009))*Math.Min(1,(Horizon(x)-y)/15.0));}
  // Full canal motion; on stone, move only the painted light reflections.
  for(int y=Theme=="EDO"?278:265;y<H;y++)for(int x=2;x<W-2;x++){int k=y*W+x,c=pixels[k],r=(c>>16)&255,gg=(c>>8)&255,b=c&255;bool reflection=r>110&&gg>60&&b<110||b>105&&gg>65&&r<100;if(Theme=="EDO"||reflection){int shift=(int)Math.Round(Math.Sin(y*.36+t*2.8)*1.4+Math.Sin(x*.12+y*.17-t*1.7));int xx=Math.Max(0,Math.Min(W-1,x+shift));int sample=pixels[y*W+xx];frame[k]=Blend(sample,((sample>>16)&255)>120?0xffab39:0x55bbee,.025+.04*(1+Math.Sin(t*3+y*.2+x*.03)));}}
  if(Theme=="EDO")for(int y=15;y<215;y++)for(int x=0;x<W;x++){int k=y*W+x,c=pixels[k],r=(c>>16)&255,gg=(c>>8)&255,b=c&255;if((x<145||x>425||x>270&&x<340)&&r>80&&b>75&&r>gg*1.45&&b>gg*1.2){int shift=(int)Math.Round(Math.Sin(t*1.7+y*.055+x*.008)*1.2);frame[k]=pixels[y*W+Math.Max(0,Math.Min(W-1,x+shift))];}}
  var f=new Bitmap(W,H,PixelFormat.Format32bppArgb);var d=f.LockBits(new Rectangle(0,0,W,H),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);Marshal.Copy(frame,0,d.Scan0,frame.Length);f.UnlockBits(d);
  using(var g=Graphics.FromImage(f)){g.SmoothingMode=SmoothingMode.None;g.InterpolationMode=InterpolationMode.NearestNeighbor;
   for(int i=0;i<stars.Count;i++){Point p=stars[i];A(g,0xb9ccff,(int)(60+150*(.5+.5*Math.Sin(t*(1.1+i%4*.15)+i))),p.X,p.Y,1,1);}
   double meteor=Mod(t+2,14);if(meteor<1.6){int mx=210+(int)(meteor*110),my=8+(int)(meteor*15);for(int j=0;j<14;j++){int xx=mx-j,yy=my-j/6;if(xx>=0&&xx<W&&yy>=0&&yy<125&&sky[yy*W+xx])A(g,0xc1ddff,Math.Max(0,210-j*14),xx,yy,1,1);}}
   for(int i=0;i<lights.Count;i++){Point p=lights[i];A(g,0xffba4c,(int)(25+(.5+.5*Math.Sin(t*2.2+i*.7))*65),p.X-1,p.Y,2,1);}
   foreach(var p in chimneys)Smoke(g,p.X,p.Y,t,p.X*.1);for(int i=0;i<fires.Length;i++)Fire(g,fires[i].X,fires[i].Y,t,i,Theme=="EDO"?3:7);
   if(Theme=="ROME")Fountain(g,t);if(Theme=="EDO")Petals(g,t);Rain(g,t);Mist(g,t);
  }return f;
 }
 void Fire(Graphics g,int x,int y,double t,int id,int height){double pulse=.5+.5*Math.Sin(t*6.4+id)+.2*Math.Sin(t*13.7+id*2);int glow=16+(int)(pulse*10);for(int ring=3;ring>0;ring--)g.FillEllipse(B(0xff8c26,glow),x-ring*5,y-ring*4,ring*10,ring*9);
  for(int col=-2;col<=2;col++){double wave=Math.Sin(t*10+col*2+id)+.5*Math.Sin(t*18+id+col);int h=Math.Max(2,height+(int)(wave*3)-Math.Abs(col)*2),lean=(int)(Math.Sin(t*5+id)*1.5);R(g,0xcf4028,x+col+lean,y-h,1,h);if(Math.Abs(col)<2)R(g,0xffa226,x+col,y-h+2,1,Math.Max(1,h-2));if(col==0)R(g,0xffee9b,x,y-3,1,3);}
  for(int i=0;i<12;i++){double age=Mod(t*(.7+i%3*.13)+i*.31+id*.5,3);A(g,i%3==0?0xffd57a:0xf57535,Math.Max(0,160-(int)(age*50)),x+(int)(Math.Sin(age*3+i)*3+age*2),y-height-(int)(age*10),1,1);}
  for(int i=0;i<7;i++)A(g,0xf7a139,20+(int)(pulse*8),x-3+(int)(Math.Sin(t*3+i)*3),270+i*11,5+i%3*2,1);
 }
 void Smoke(Graphics g,int x,int y,double t,double phase){for(int i=0;i<18;i++){double age=Mod(t*.65+i*.39+phase,7);int xx=x+(int)(age*2.5+Math.Sin(age*1.3+phase+t*.3)*5),yy=y-(int)(age*7),size=2+(int)(age*1.5),a=Math.Max(0,65-(int)(age*8));A(g,0x8c82b4,a,xx-size/2,yy,size,2);A(g,0x68648e,a/2,xx-size/2-2,yy+2,size+3,2);A(g,0xaaa1c4,a/2,xx,yy-1,Math.Max(1,size/2),1);}}
 void Fountain(Graphics g,double t){for(int i=0;i<42;i++){double age=Mod(t*1.4+i*.097,1);int x=334+(i%2==0?-1:1)*(int)(4+age*14),y=209+(int)(age*age*24);if(x>310&&x<355)A(g,i%3==0?0xb7ffff:0x53b7f8,100+i%4*25,x,y,1,2);}for(int i=0;i<10;i++){double r=Mod(t*6+i*2.1,13);using(var p=new Pen(Color.FromArgb((int)(100-r*6),100,225,255)))g.DrawEllipse(p,334-(int)r,233-(int)(r*.16),(int)(r*2)+1,2+(int)(r*.3));}for(int i=0;i<15;i++)A(g,0x62ceff,55,309+(int)Mod(i*13+t*8,47),236+i%2,3,1);}
 void Petals(Graphics g,double t){for(int i=0;i<92;i++){double age=Mod(t*.055+i*.073,1);int x=(int)Mod(i*91+t*(5+i%4)+Math.Sin(t*1.2+i)*8,690)-25,y=20+(int)(age*320),a=70+i%4*35;A(g,i%3==0?0xf9a2db:0xb967c0,a,x,y,1+i%2,1);if(i%4==0)A(g,0xffbddf,a/2,x+1,y+1,1,1);}}
 void Rain(Graphics g,double t){for(int i=0;i<155;i++){double age=Mod(t*(.8+i%5*.06)+i*.173,1);int x=(int)Mod(i*83+t*12+age*20,680)-20,y=(int)(age*390)-20,a=25+i%4*8;A(g,0x8aa5e2,a,x,y,1,3+i%3);A(g,0x8aa5e2,a/2,x-1,y+3,1,2);}for(int i=0;i<38;i++){double age=Mod(t*1.3+i*.219,1);int x=8+(i*71)%622,y=Theme=="EDO"?285+i*19%70:270+i*23%84,a=(int)((1-age)*80),r=1+(int)(age*4);A(g,0x8dc8ec,a,x-r,y,r*2,1);if(age<.25){A(g,0xc0e4ff,a,x,y-2,1,1);A(g,0x80b0df,a,x-2,y-1,1,1);}}}
 void Mist(Graphics g,double t){for(int i=0;i<32;i++){double age=Mod(t*.11+i*.37,8);int x=(int)Mod(i*53+age*13,710)-35,y=Theme=="EDO"?285+i%5*8:263+i%4*11,w=12+i%4*7,a=(int)(Math.Sin(age*Math.PI/8)*9);A(g,0x9a9ed0,a,x,y,w,1);A(g,0x8c92bb,a/2,x-6,y+1,w+15,1);}}
 public void Dispose(){background.Dispose();foreach(var b in brushes.Values)b.Dispose();}
}

