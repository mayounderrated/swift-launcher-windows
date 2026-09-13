// Substantially developed with OpenAI Codex under human direction.
// Independent Windows recreation inspired by Vinyl Launcher; see README.md for credits.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Swift {
 static class Program {
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string kind,string name);
  [DllImport("user32.dll")]static extern bool PostMessage(IntPtr window,int message,IntPtr w,IntPtr l);
  static void NotifyExisting(int message){var window=FindWindowEx(new IntPtr(-3),IntPtr.Zero,null,"Swift hotkey");if(window!=IntPtr.Zero)PostMessage(window,message,IntPtr.Zero,IntPtr.Zero);}
  [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
  [STAThread] static void Main(string[] args) {
   if(args.Length>0 && args[0]=="--quit"){NotifyExisting(0x8002);return;}
   SetProcessDPIAware();
   Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   if(args.Length>0 && args[0]=="--self-test") { Environment.Exit(Search.Test()); return; }
   if(args.Length>0 && args[0]=="--catalog-test") {
    string note; var apps=Catalog.Apps(out note);
    File.WriteAllText(args[1],"Applications: "+apps.Count+Environment.NewLine+note+Environment.NewLine+"Shell entries: "+apps.FindAll(e=>e.Shell).Count);
    Environment.Exit(apps.Count>0?0:1); return;
   }
   if(args.Length>0 && args[0]=="--render-demo") {
    using(var form=new Launcher(true)) { form.LoadDemo(); form.Show(); var watch=Stopwatch.StartNew();while(watch.ElapsedMilliseconds<1500){Application.DoEvents();Thread.Sleep(10);} using(var bitmap=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));using(var clipped=new Bitmap(form.Width,form.Height)){using(var g=Graphics.FromImage(clipped)){g.Clear(Color.Transparent);g.SetClip(form.Region,CombineMode.Replace);g.DrawImageUnscaled(bitmap,0,0);}clipped.Save(args[1]);} } }
    return;
   }
   if(args.Length>1 && args[0]=="--render-settings") {
    using(var form=new SettingsForm(SwiftSettings.Load(),null)){form.Show();Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));bitmap.Save(args[1]);}}return;
   }
   bool created; using(var mutex=new Mutex(true,"Local\\SwiftLauncher.1",out created)) {
    if(!created){NotifyExisting(0x8001);return;}
    Application.Run(new Launcher(args.Length>0 && args[0]=="--smoke-test"));
   }
  }
 }
 sealed class Entry {
  public readonly string Path,Name,Lower,Payload; public readonly bool App,Shell;public readonly EntryAction Action;
  public Entry(string path):this(path,System.IO.Path.GetFileName(path),false,false){}
  public Entry(string path,string name,bool app,bool shell):this(path,name,app,shell,EntryAction.None,null){}
  Entry(string path,string name,bool app,bool shell,EntryAction action,string payload) { Path=path; Name=name; Lower=name.ToLowerInvariant(); App=app; Shell=shell;Action=action;Payload=payload; }
  public static Entry Special(string name,EntryAction action,string payload){return new Entry("swift:"+action+":"+payload,name,false,false,action,payload);}
  public string IconKey {get{return Action!=EntryAction.None?"action:"+Action:!App && FileIcons.Supports(Path)?"filetype:"+System.IO.Path.GetExtension(Path).ToLowerInvariant():Path;}}
  public ProcessStartInfo LaunchInfo(bool reveal) {
   if(reveal) return new ProcessStartInfo("explorer.exe",Shell?"shell:AppsFolder":"/select,\""+Path+"\""){UseShellExecute=true};
   return Shell?new ProcessStartInfo("explorer.exe","\"shell:AppsFolder\\"+Path+"\""){UseShellExecute=true}:new ProcessStartInfo(Path){UseShellExecute=true};
  }
 }
 static class Search {
  public static int Score(string name,string query) {
   if(query.Length==0)return 0;
   if(name==query)return 10000;
   if(name.StartsWith(query,StringComparison.Ordinal))return 8000-name.Length;
   int at=name.IndexOf(query,StringComparison.Ordinal); if(at>=0)return 6000-at-name.Length;
   int pos=0,first=-1,last=0;
   foreach(char ch in query) { int found=name.IndexOf(ch,pos); if(found<0)return -1; if(first<0)first=found; last=found; pos=found+1; }
   return Math.Max(0,3000-(last-first)*5-name.Length);
  }
  public static List<Entry> Find(Entry[] entries,string query,int mode,CancellationToken token) {
   var best=new List<KeyValuePair<int,Entry>>(61);
   foreach(var entry in entries) {
    if(token.IsCancellationRequested)return new List<Entry>();
    if((mode==0 && !entry.App)||(mode==1 && entry.App))continue;
    int score=Score(entry.Lower,query); if(score<0)continue; if(entry.App)score+=100;
    int place=best.Count; while(place>0 && best[place-1].Key<score)place--;
    if(place>=60)continue; best.Insert(place,new KeyValuePair<int,Entry>(score,entry)); if(best.Count>60)best.RemoveAt(60);
   }
   return best.ConvertAll(pair=>pair.Value);
  }
  public static int Test() {
   if(Score("report.pdf","report")<=Score("annual report.pdf","report"))return 1;
   if(Score("report.pdf","rpf")<0 || Score("report.pdf","xyz")!=-1)return 2;
   var app=new Entry("Example.Package!App","Calculator",true,true);
   var file=new Entry(@"C:\docs\Calculator.pdf"); var entries=new[]{file,app};
   if(Find(entries,"calc",0,CancellationToken.None).Count!=1)return 3;
   if(Find(entries,"calc",1,CancellationToken.None)[0]!=file)return 4;
   if(Find(entries,"calc",2,CancellationToken.None)[0]!=app)return 5;
   if(app.LaunchInfo(false).Arguments!="\"shell:AppsFolder\\Example.Package!App\"")return 6;
   if(file.LaunchInfo(false).FileName!=file.Path)return 7;
   if(Find(entries,"xyz",2,CancellationToken.None).Count!=0)return 8;
   string answer;if(!CalculatorEngine.TryEvaluate("(12+4)*3",out answer)||answer!="48")return 9;
   if(!CalculatorEngine.TryEvaluate("sqrt(81)+2^3",out answer)||answer!="17")return 10;
   if(CalculatorEngine.TryEvaluate("hello",out answer)||CalculatorEngine.TryEvaluate("2+",out answer))return 11;
   if(!CalculatorEngine.TryEvaluate("-2^2",out answer)||answer!="-4")return 12;
   var action=Entry.Special("Search Google",EntryAction.Google,"swift launcher");if(action.IconKey!="action:Google"||action.Payload!="swift launcher")return 13;
   return 0;
  }
 }
 static class Catalog {
  static void Release(object value) { if(value!=null && Marshal.IsComObject(value))Marshal.FinalReleaseComObject(value); }
  public static List<Entry> Apps(out string note) {
   var result=new List<Entry>(); var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase); note="";
   object shell=null,folder=null,items=null;
   try {
    shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
    folder=((dynamic)shell).NameSpace("shell:AppsFolder"); items=((dynamic)folder).Items();
    int count=((dynamic)items).Count;
    for(int i=0;i<count;i++) { object item=null; try {
     item=((dynamic)items).Item(i); string name=((dynamic)item).Name; string path=((dynamic)item).Path;
     if(!String.IsNullOrWhiteSpace(name) && !String.IsNullOrWhiteSpace(path) && names.Add(name))result.Add(new Entry(path,name,true,!System.IO.Path.IsPathRooted(path)));
    }catch(COMException){}finally{Release(item);} }
   }catch(Exception ex){note="Windows app catalogue unavailable: "+ex.Message;}finally{Release(items);Release(folder);Release(shell);}
   foreach(var root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)}) {
    var pending=new Stack<string>(); pending.Push(root);
    while(pending.Count>0) { var dir=pending.Pop(); try {
     if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0)continue;
     foreach(var path in Directory.EnumerateFiles(dir)) {
      string ext=System.IO.Path.GetExtension(path).ToLowerInvariant(); if(ext!=".lnk" && ext!=".appref-ms" && ext!=".url")continue;
      string name=System.IO.Path.GetFileNameWithoutExtension(path); if(names.Add(name))result.Add(new Entry(path,name,true,false));
     }
     foreach(var sub in Directory.EnumerateDirectories(dir))pending.Push(sub);
    }catch(IOException){}catch(UnauthorizedAccessException){} }
   }
   result.Sort((a,b)=>StringComparer.OrdinalIgnoreCase.Compare(a.Name,b.Name)); return result;
  }
 }
 static class IconWorker {
  static readonly System.Collections.Concurrent.BlockingCollection<Action> queue=new System.Collections.Concurrent.BlockingCollection<Action>();
  static IconWorker(){var worker=new Thread(delegate(){foreach(var action in queue.GetConsumingEnumerable()){try{action();}catch{}}});worker.IsBackground=true;worker.SetApartmentState(ApartmentState.STA);worker.Start();}
  public static void Enqueue(Action action){queue.Add(action);}
 }
 static class FileIcons {
  public static bool Supports(string path){string ext=System.IO.Path.GetExtension(path).ToLowerInvariant();return ext!=".exe"&&ext!=".lnk"&&ext!=".appref-ms"&&ext!=".ico"&&ext!=".url";}
  public static Bitmap Load(string path){
   string ext=System.IO.Path.GetExtension(path).TrimStart('.').ToUpperInvariant();string label=ext.Length==0?"FILE":ext.Length<=5?ext:ext.Substring(0,5);
   Color color=Color.FromArgb(104,119,139);
   switch(ext){
    case "PDF":color=Color.FromArgb(222,64,70);break;
    case "DOC":case "DOCX":case "RTF":color=Color.FromArgb(57,120,224);break;
    case "XLS":case "XLSX":case "CSV":case "ODS":color=Color.FromArgb(36,162,112);break;
    case "PPT":case "PPTX":color=Color.FromArgb(234,114,59);break;
    case "ZIP":case "7Z":case "RAR":case "TAR":case "GZ":color=Color.FromArgb(171,114,225);break;
    case "PNG":case "JPG":case "JPEG":case "WEBP":case "GIF":case "SVG":case "HEIC":color=Color.FromArgb(33,155,181);break;
    case "MP4":case "MKV":case "MOV":case "AVI":case "WEBM":color=Color.FromArgb(191,77,156);break;
    case "MP3":case "WAV":case "FLAC":case "M4A":case "OGG":color=Color.FromArgb(127,101,222);break;
    case "JS":case "TS":case "TSX":case "PY":case "CS":case "HTML":case "CSS":case "JSON":color=Color.FromArgb(67,151,177);break;
   }
   var bitmap=new Bitmap(256,256,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
   using(var g=Graphics.FromImage(bitmap)){
    g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
    using(var page=new GraphicsPath()){
     page.AddLines(new[]{new Point(48,18),new Point(156,18),new Point(212,74),new Point(212,220)});page.AddArc(188,208,24,24,0,90);page.AddLine(200,232,48,232);page.AddArc(36,208,24,24,90,90);page.AddLine(36,220,36,30);page.AddArc(36,18,24,24,180,90);page.CloseFigure();
     using(var fill=new SolidBrush(Color.FromArgb(243,246,251)))g.FillPath(fill,page);
    }
    using(var fold=new SolidBrush(Color.FromArgb(194,206,224)))g.FillPolygon(fold,new[]{new Point(156,18),new Point(156,74),new Point(212,74)});
    using(var line=new Pen(Color.FromArgb(214,222,234),7)){line.StartCap=LineCap.Round;line.EndCap=LineCap.Round;g.DrawLine(line,66,99,175,99);g.DrawLine(line,66,120,149,120);}
    using(var band=new GraphicsPath()){
     band.AddArc(16,144,24,24,180,90);band.AddArc(216,144,24,24,270,90);band.AddArc(216,198,24,24,0,90);band.AddArc(16,198,24,24,90,90);band.CloseFigure();using(var fill=new SolidBrush(color))g.FillPath(fill,band);
    }
    using(var font=new Font("Segoe UI",label.Length>4?36:44,FontStyle.Bold,GraphicsUnit.Pixel))using(var white=new SolidBrush(Color.White))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(label,font,white,new RectangleF(20,145,216,74),format);
   }
   return bitmap;
  }
 }
 static class ShellIcons {
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct Info { public IntPtr icon; public int index; public uint attributes; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string name; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)]public string type; }
  [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern int SHParseDisplayName(string name,IntPtr bind,out IntPtr pidl,uint mask,out uint attrs);
  [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SHGetFileInfo(IntPtr pidl,uint attrs,out Info info,uint size,uint flags);
  [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
  [StructLayout(LayoutKind.Sequential)] struct ImageSize {public int X,Y;public ImageSize(int size){X=size;Y=size;}}
  [ComImport,Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface ImageFactory {
   [PreserveSig]int GetImage(ImageSize size,uint flags,out IntPtr bitmap);
  }
  [DllImport("shell32.dll",CharSet=CharSet.Unicode,PreserveSig=true)] static extern int SHCreateItemFromParsingName(string path,IntPtr bind,ref Guid iid,[MarshalAs(UnmanagedType.Interface)]out ImageFactory factory);
  [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr value);
  // Preserve the alpha channel of Shell's 32-bit DIB; FromHbitmap alone can discard it.
  [StructLayout(LayoutKind.Sequential)]struct NativeBitmap {public int Type,Width,Height,Stride;public ushort Planes,BitsPixel;public IntPtr Bits;}
  [DllImport("gdi32.dll",EntryPoint="GetObjectW")]static extern int GetObject(IntPtr handle,int size,out NativeBitmap data);
  static Bitmap CopyBitmap(IntPtr handle){
   NativeBitmap data;GetObject(handle,Marshal.SizeOf(typeof(NativeBitmap)),out data);
   if(data.Bits!=IntPtr.Zero && data.BitsPixel==32){
    var owned=new Bitmap(data.Width,Math.Abs(data.Height),System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
    var locked=owned.LockBits(new Rectangle(0,0,owned.Width,owned.Height),System.Drawing.Imaging.ImageLockMode.WriteOnly,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
    try {var row=new byte[data.Width*4];for(int y=0;y<owned.Height;y++){Marshal.Copy(IntPtr.Add(data.Bits,(owned.Height-1-y)*data.Stride),row,0,row.Length);Marshal.Copy(row,0,IntPtr.Add(locked.Scan0,y*locked.Stride),row.Length);}}finally{owned.UnlockBits(locked);}return owned;
   }
   return Image.FromHbitmap(handle);
  }
  static Bitmap TrimPadding(Bitmap image){
   int left=image.Width,top=image.Height,right=-1,bottom=-1;
   for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>=128){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
   if(right>=left && right-left<image.Width*0.65 && bottom-top<image.Height*0.65){
    left=Math.Max(0,left-2);top=Math.Max(0,top-2);right=Math.Min(image.Width-1,right+2);bottom=Math.Min(image.Height-1,bottom+2);
    var cropped=image.Clone(new Rectangle(left,top,right-left+1,bottom-top+1),System.Drawing.Imaging.PixelFormat.Format32bppPArgb);image.Dispose();return cropped;
   }
   return image;
  }
  public static Bitmap Load(Entry entry) {
   if(entry.Action!=EntryAction.None)return ActionIcons.Load(entry.Action);
   if(!entry.App&&FileIcons.Supports(entry.Path))return FileIcons.Load(entry.Path);
   ImageFactory factory=null;IntPtr bitmap=IntPtr.Zero;
   try {
    var iid=typeof(ImageFactory).GUID;
    if(SHCreateItemFromParsingName(entry.Shell?"shell:AppsFolder\\"+entry.Path:entry.Path,IntPtr.Zero,ref iid,out factory)==0 && factory.GetImage(new ImageSize(256),0x1|0x4,out bitmap)==0 && bitmap!=IntPtr.Zero)return TrimPadding(CopyBitmap(bitmap));
   }catch{}finally{if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);if(factory!=null)Marshal.ReleaseComObject(factory);}
   return LoadSmall(entry);
  }
  static Bitmap LoadSmall(Entry entry) {

   IntPtr pidl=IntPtr.Zero; Info info=new Info(); uint attrs;
   try {
    if(SHParseDisplayName(entry.Shell?"shell:AppsFolder\\"+entry.Path:entry.Path,IntPtr.Zero,out pidl,0,out attrs)!=0)return null;
    SHGetFileInfo(pidl,0,out info,(uint)Marshal.SizeOf(typeof(Info)),0x100|0x8);
    if(info.icon==IntPtr.Zero)return null;
    using(var icon=Icon.FromHandle(info.icon))return icon.ToBitmap();
   }catch{return null;}finally{if(info.icon!=IntPtr.Zero)DestroyIcon(info.icon);if(pidl!=IntPtr.Zero)Marshal.FreeCoTaskMem(pidl);}
  }
 }
 static class ActionIcons {
  public static Bitmap Load(EntryAction action){var image=new Bitmap(256,256,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;Color color=action==EntryAction.Google?Color.FromArgb(66,133,244):action==EntryAction.Calculator?Color.FromArgb(92,184,120):Color.FromArgb(154,126,220);using(var brush=new SolidBrush(color))g.FillEllipse(brush,18,18,220,220);string text=action==EntryAction.Google?"G":action==EntryAction.Calculator?"=":"⚙";using(var font=new Font("Segoe UI",action==EntryAction.Settings?100:120,FontStyle.Bold,GraphicsUnit.Pixel))using(var white=new SolidBrush(Color.White))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(text,font,white,new RectangleF(18,10,220,226),format);}return image;}
 }
 sealed class Dial : Control {
  public List<Entry> Entries=new List<Entry>(); public int Selected;
  public Action Changed,Launch;
  readonly Dictionary<string,Bitmap> icons=new Dictionary<string,Bitmap>();
  readonly HashSet<string> loading=new HashSet<string>();
  readonly System.Windows.Forms.Timer animation=new System.Windows.Forms.Timer();
  readonly Font large=new Font("Segoe UI",19,FontStyle.Bold),small=new Font("Segoe UI",9),micro=new Font("Segoe UI",8);
  double offset,dragAngle,offsetStart;double animationDuration=180,outgoingOffset;List<Entry> outgoingRing;int outgoingSelected; bool dragging,moved;
  readonly Stopwatch selectionClock=new Stopwatch();Entry previous;double selectionMix=1;
  Palette palette=Palette.From(new SwiftSettings());double speed=1;
  public Dial() {
   DoubleBuffered=true; BackColor=Color.FromArgb(17,17,17); Cursor=Cursors.Hand;
   animation.Interval=16; animation.Tick+=delegate {double t=Math.Min(1,selectionClock.Elapsed.TotalMilliseconds/animationDuration);selectionMix=1-Math.Pow(1-t,3);offset=offsetStart*(1-selectionMix);if(t>=1){offset=0;previous=null;outgoingRing=null;animation.Stop();}Invalidate(); };
   MouseWheel+=delegate(object s,MouseEventArgs e){MoveSelection(e.Delta>0?-1:1);};
   MouseDown+=delegate(object s,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;dragging=true;moved=false;dragAngle=Angle(e.Location);Capture=true;};
   MouseMove+=delegate(object s,MouseEventArgs e){if(!dragging)return;double now=Angle(e.Location),delta=now-dragAngle;if(delta>Math.PI)delta-=Math.PI*2;if(delta< -Math.PI)delta+=Math.PI*2;if(Math.Abs(delta)>0.20){MoveSelection(delta>0?-1:1);dragAngle=now;moved=true;}};
   MouseUp+=delegate(object s,MouseEventArgs e){if(!dragging)return;dragging=false;Capture=false;if(moved)return;int dx=(int)(e.X/CanvasScale)-250,dy=(int)(e.Y/CanvasScale)-250;if(dx*dx+dy*dy<140*140){if(Launch!=null)Launch();return;}int step=(int)Math.Round(Angle(e.Location)/(Math.PI*2/10));MoveSelection(step);};
   VisibleChanged+=delegate { if(!Visible){animation.Stop();offset=0;previous=null;outgoingRing=null;selectionMix=1;} };
  }
  public void ApplySettings(SwiftSettings settings){palette=Palette.From(settings);speed=settings.AnimationScale;BackColor=palette.Back;Invalidate();}
  float CanvasScale {get{return Width/500f;}}
  double Angle(Point p){return Math.Atan2(p.Y/CanvasScale-250,p.X/CanvasScale-250);}
  Size regionSize;
  protected override void OnResize(EventArgs e){base.OnResize(e);if(regionSize==ClientSize || Width<=0 || Height<=0)return;regionSize=ClientSize;using(var circle=new GraphicsPath()){circle.AddEllipse(0,0,Width,Height);var previous=Region;Region=new Region(circle);if(previous!=null)previous.Dispose();}}
  public void SetEntries(List<Entry> entries){
   bool same=entries.Count==Entries.Count;for(int i=0;same&&i<entries.Count;i++)same=entries[i].Path==Entries[i].Path;if(same)return;
   bool animate=Visible&&IsHandleCreated&&(Entries.Count>0||entries.Count>0);
   previous=Current;outgoingRing=animate?Entries:null;outgoingSelected=Selected;outgoingOffset=offset;
   Entries=entries;Selected=0;
   if(animate&&speed>0){offsetStart=offset=Math.PI*0.4;selectionMix=0;animationDuration=220*speed;selectionClock.Restart();animation.Start();}
   else{offset=0;previous=null;outgoingRing=null;selectionMix=1;animation.Stop();}
   QueueIcons();Invalidate();if(Changed!=null)Changed();
  }
  public void MoveSelection(int delta){if(Entries.Count==0||delta==0)return;outgoingRing=null;previous=Current;Selected=(Selected+delta%Entries.Count+Entries.Count)%Entries.Count;if(speed>0){animationDuration=180*speed;offset+=delta*Math.PI*2/10;offsetStart=offset;selectionMix=0;selectionClock.Restart();animation.Start();}else{offset=0;previous=null;selectionMix=1;}QueueIcons();Invalidate();if(Changed!=null)Changed();}
  public Entry Current {get{return Entries.Count==0?null:Entries[Selected];}}
  void QueueIcons(){if(!IsHandleCreated)return;for(int step=-4;step<=5;step++){if(Entries.Count==0)break;var entry=Entries[(Selected+step%Entries.Count+Entries.Count)%Entries.Count];if(icons.ContainsKey(entry.IconKey)||!loading.Add(entry.IconKey))continue;
   IconWorker.Enqueue(delegate { if(IsDisposed)return;var bitmap=ShellIcons.Load(entry);try { BeginInvoke((Action)delegate {loading.Remove(entry.IconKey);if(IsDisposed){if(bitmap!=null)bitmap.Dispose();return;}if(icons.Count>=96){foreach(var old in icons.Values)if(old!=null)old.Dispose();icons.Clear();}icons[entry.IconKey]=bitmap;Invalidate();}); }catch(InvalidOperationException){if(bitmap!=null)bitmap.Dispose();} });
  }}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);QueueIcons();}
  protected override void Dispose(bool disposing){if(disposing){animation.Dispose();large.Dispose();small.Dispose();micro.Dispose();foreach(var icon in icons.Values)if(icon!=null)icon.Dispose();}base.Dispose(disposing);}
  void Label(Graphics g,string text,Font font,Color color,Rectangle rect){var saved=g.Save();g.ResetTransform();float scale=CanvasScale;var physical=new Rectangle((int)(rect.X*scale),(int)(rect.Y*scale),(int)(rect.Width*scale),(int)(rect.Height*scale));TextRenderer.DrawText(g,text,font,physical,color,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);g.Restore(saved);}
  static void DrawIcon(Graphics g,Bitmap image,int x,int y,int size){float ratio=Math.Min((float)size/image.Width,(float)size/image.Height);int w=(int)Math.Round(image.Width*ratio),h=(int)Math.Round(image.Height*ratio);g.DrawImage(image,x+(size-w)/2,y+(size-h)/2,w,h);}
  void DrawSelected(Graphics g,Bitmap current){
   Bitmap old;if(previous!=null && icons.TryGetValue(previous.IconKey,out old)&&old!=null)DrawFaded(g,old,80,(float)(1-selectionMix));
   DrawFaded(g,current,(int)(72+8*selectionMix),(float)selectionMix);
  }
  static void DrawFaded(Graphics g,Bitmap icon,int size,float alpha){if(alpha<=0)return;float ratio=Math.Min((float)size/icon.Width,(float)size/icon.Height);int w=(int)(icon.Width*ratio),h=(int)(icon.Height*ratio);using(var attributes=new System.Drawing.Imaging.ImageAttributes()){var matrix=new System.Drawing.Imaging.ColorMatrix();matrix.Matrix33=alpha;attributes.SetColorMatrix(matrix);g.DrawImage(icon,new Rectangle(250-w/2,234-h/2,w,h),0,0,icon.Width,icon.Height,GraphicsUnit.Pixel,attributes);}}
  void DrawRing(Graphics g,List<Entry> entries,int selected,double angle,float alpha){
   if(alpha<0.01f||entries.Count==0)return;var drawn=new HashSet<int>();int[] steps={0,1,-1,2,-2,3,-3,4,-4,5};
   foreach(int step in steps){int idx=(selected+step%entries.Count+entries.Count)%entries.Count;if(!drawn.Add(idx))continue;double a=step*Math.PI*2/10+angle;int x=250+(int)(Math.Cos(a)*201),y=250+(int)(Math.Sin(a)*201);Bitmap icon;
    if(icons.TryGetValue(entries[idx].IconKey,out icon)&&icon!=null){float ratio=Math.Min(44f/icon.Width,44f/icon.Height);int w=(int)(icon.Width*ratio),h=(int)(icon.Height*ratio);using(var attr=new System.Drawing.Imaging.ImageAttributes()){var matrix=new System.Drawing.Imaging.ColorMatrix();matrix.Matrix33=alpha;attr.SetColorMatrix(matrix);g.DrawImage(icon,new Rectangle(x-w/2,y-h/2,w,h),0,0,icon.Width,icon.Height,GraphicsUnit.Pixel,attr);}}
    else{using(var brush=new SolidBrush(Color.FromArgb((int)(255*alpha),palette.Button)))g.FillEllipse(brush,x-22,y-22,44,44);Label(g,entries[idx].App?entries[idx].Name.Substring(0,1).ToUpperInvariant():"F",small,Color.FromArgb((int)(255*alpha),palette.Text),new Rectangle(x-22,y-22,44,44));}
   }
  }
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.ScaleTransform(CanvasScale,CanvasScale);
   using(var brush=new SolidBrush(palette.Back))g.FillEllipse(brush,1,1,498,498);
   using(var brush=new SolidBrush(palette.Inner))g.FillEllipse(brush,100,100,300,300);
   using(var pen=new Pen(Color.FromArgb(150,palette.Accent),3))g.DrawEllipse(pen,204,188,92,92);
   if(Current!=null){
    Bitmap selectedIcon;
    if(icons.TryGetValue(Current.IconKey,out selectedIcon)&&selectedIcon!=null)DrawSelected(g,selectedIcon);
    else {using(var brush=new SolidBrush(palette.Button))g.FillEllipse(brush,210,194,80,80);Label(g,Current.App?Current.Name.Substring(0,1).ToUpperInvariant():"F",large,palette.Text,new Rectangle(210,194,80,80));}
   }
   Label(g,Current==null?"No matches":Current.Name,small,palette.Text,new Rectangle(116,290,268,34));
   Label(g,Current==null?"Type to search":Current.Action==EntryAction.Calculator?"ENTER TO COPY":"ENTER TO OPEN",micro,palette.Muted,new Rectangle(150,324,200,20));
   if(outgoingRing!=null)DrawRing(g,outgoingRing,outgoingSelected,outgoingOffset-Math.PI*0.4*selectionMix,(float)(1-selectionMix));
   DrawRing(g,Entries,Selected,offset,outgoingRing!=null?(float)selectionMix:1f);
   Label(g,Entries.Count==0?"":(Selected+1)+" / "+Entries.Count,micro,palette.Muted,new Rectangle(200,357,100,20));
  }

 }
 // A short-lived, non-activating snapshot window animates the entire launcher.
 // Live controls remain at their final size, so text and layout do not jitter.
 sealed class TransitionWindow : Form {
  [StructLayout(LayoutKind.Sequential)]struct Pair {public int X,Y;public Pair(int x,int y){X=x;Y=y;}}
  [StructLayout(LayoutKind.Sequential,Pack=1)]struct Blend {public byte Op,Flags,Alpha,Format;}
  [StructLayout(LayoutKind.Sequential)]struct BitmapHeader {public uint Size;public int Width,Height;public ushort Planes,Bits;public uint Compression,ImageSize;public int Xppm,Yppm;public uint Used,Important;}
  [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
  [DllImport("gdi32.dll")]static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapHeader info,uint usage,out IntPtr bits,IntPtr section,uint offset);
  [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
  [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
  [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dst,ref Pair pos,ref Pair size,IntPtr src,ref Pair origin,uint key,ref Blend blend,uint flags);
  readonly Bitmap frame,canvas;readonly Graphics graphics;readonly IntPtr dc,dib,oldBitmap;
  readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();readonly Stopwatch clock=new Stopwatch();readonly Action<bool> completed;
  double progress,start,target;bool disposed;internal int FrameCount;
  readonly double durationScale;
  public TransitionWindow(Bitmap image,Region outline,Rectangle bounds,bool opening,double speed,Action<bool> done){
   durationScale=speed;
   completed=done;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;Bounds=bounds;
   frame=new Bitmap(image.Width,image.Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
   using(var g=Graphics.FromImage(frame)){g.Clear(Color.Transparent);g.SetClip(outline,CombineMode.Replace);g.DrawImageUnscaled(image,0,0);}image.Dispose();outline.Dispose();
   dc=CreateCompatibleDC(IntPtr.Zero);var header=new BitmapHeader{Size=40,Width=frame.Width,Height=-frame.Height,Planes=1,Bits=32};IntPtr pixels;
   dib=CreateDIBSection(dc,ref header,0,out pixels,IntPtr.Zero,0);if(dib==IntPtr.Zero)throw new System.ComponentModel.Win32Exception();oldBitmap=SelectObject(dc,dib);
   canvas=new Bitmap(frame.Width,frame.Height,frame.Width*4,System.Drawing.Imaging.PixelFormat.Format32bppPArgb,pixels);graphics=Graphics.FromImage(canvas);graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
   progress=opening?0:1;timer.Interval=15;timer.Tick+=delegate{TickFrame();};
  }
  protected override bool ShowWithoutActivation {get{return true;}}
  protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80000|0x80|0x20;return p;}}
  public void Play(bool opening){start=progress;target=opening?1:0;RenderFrame();if(!Visible)Show();clock.Restart();timer.Start();}
  void TickFrame(){FrameCount++;double t=Math.Min(1,clock.Elapsed.TotalMilliseconds/((target>start?220:180)*durationScale));progress=start+(target-start)*(1-Math.Pow(1-t,3));RenderFrame();if(t>=1){timer.Stop();completed(target==1);}}
  void RenderFrame(){float scale=(float)(0.92+0.08*progress);graphics.Clear(Color.Transparent);graphics.DrawImage(frame,new RectangleF(frame.Width*(1-scale)/2,frame.Height*(1-scale)/2,frame.Width*scale,frame.Height*scale));graphics.Flush();var pos=new Pair(Left,Top);var size=new Pair(frame.Width,frame.Height);var origin=new Pair(0,0);var blend=new Blend{Alpha=(byte)(255*progress),Format=1};if(!UpdateLayeredWindow(Handle,IntPtr.Zero,ref pos,ref size,dc,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
  protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;timer.Dispose();if(graphics!=null)graphics.Dispose();if(canvas!=null)canvas.Dispose();if(dc!=IntPtr.Zero)SelectObject(dc,oldBitmap);if(dib!=IntPtr.Zero)DeleteObject(dib);if(dc!=IntPtr.Zero)DeleteDC(dc);if(frame!=null)frame.Dispose();}base.Dispose(disposing);}
 }
 sealed class HotkeyListener : NativeWindow,IDisposable {
  [DllImport("user32.dll",SetLastError=true)]static extern bool RegisterHotKey(IntPtr hwnd,int id,uint mods,uint key);
  [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr hwnd,int id);
  readonly Action toggle,show,quit;public readonly bool Registered;
  public HotkeyListener(Action action,uint modifiers,uint key,Action showAction=null,Action quitAction=null){toggle=action;show=showAction;quit=quitAction;CreateHandle(new CreateParams{Caption="Swift hotkey",Parent=new IntPtr(-3)});Registered=RegisterHotKey(Handle,1,modifiers,key);}
  protected override void WndProc(ref Message m){if(m.Msg==0x8001&&show!=null){show();return;}if(m.Msg==0x8002&&quit!=null){quit();return;}if(m.Msg==0x312&&m.WParam.ToInt32()==1){toggle();return;}base.WndProc(ref m);}
  public void Dispose(){UnregisterHotKey(Handle,1);DestroyHandle();}
 }
 // Event-driven: no polling while Swift sits in the tray.
 sealed class FocusMonitor : IDisposable {
  delegate void WinEvent(IntPtr hook,uint evt,IntPtr hwnd,int obj,int child,uint thread,uint time);
  [DllImport("user32.dll")]static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr module,WinEvent callback,uint process,uint thread,uint flags);
  [DllImport("user32.dll")]static extern bool UnhookWinEvent(IntPtr hook);
  readonly WinEvent callback;readonly IntPtr hook;
  public FocusMonitor(Action lost){callback=delegate(IntPtr h,uint e,IntPtr window,int o,int c,uint t,uint ms){if(window!=IntPtr.Zero)lost();};hook=SetWinEventHook(3,3,IntPtr.Zero,callback,0,0,2);}
  public void Dispose(){if(hook!=IntPtr.Zero)UnhookWinEvent(hook);GC.KeepAlive(callback);}
 }
 sealed class Launcher : Form {
  [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr handle);
  [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wParam,string text);
  [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h,int id);
  readonly TextBox input=new TextBox();readonly Dial dial=new Dial();readonly Label status=new Label(),name=new Label(),kind=new Label(),detail=new Label(),glyph=new Label();readonly Panel searchPanel=new Panel();
  readonly Button[] tabs=new Button[3];readonly NotifyIcon tray=new NotifyIcon();readonly System.Windows.Forms.Timer debounce=new System.Windows.Forms.Timer();
  readonly string config=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SwiftLauncher","roots.txt");
  Entry[] index=new Entry[0];CancellationTokenSource searchCancel;bool exiting,indexing,dirty;int generation,mode;string indexNote="Loading apps…";string appWarning="";SwiftSettings settings=SwiftSettings.Load();ToolStripMenuItem openMenu;
  internal Action<string> Trace {get;set;}void Note(string text){if(Trace!=null)Trace(text+" visible="+Visible+" wanted="+wantedVisible+" moving="+(transition!=null));}
  internal int LastMotionFrames;
  [DllImport("dwmapi.dll")]static extern int DwmFlush();
  FocusMonitor focusMonitor;
  HotkeyListener hotkey;
  TransitionWindow transition;bool wantedVisible,changingVisibility;internal string TransitionError;
  const int Limit=60000;
  public Launcher(bool smoke){
   Icon=System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
   Text="Swift";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;ClientSize=new Size(540,620);AutoScaleMode=AutoScaleMode.None;
   BackColor=Color.FromArgb(17,17,17);ForeColor=Color.White;Font=new Font("Segoe UI",10);KeyPreview=true;TopMost=true;DoubleBuffered=true;
   using(var shape=new GraphicsPath()){
    shape.AddEllipse(20,104,500,500);
    shape.StartFigure();shape.AddArc(20,16,48,48,180,90);shape.AddArc(472,16,48,48,270,90);shape.AddArc(472,36,48,48,0,90);shape.AddArc(20,36,48,48,90,90);shape.CloseFigure();
    Region=new Region(shape);
   }
   searchPanel.SetBounds(35,22,468,56);Controls.Add(searchPanel);
   glyph.Text="⌕";glyph.Font=new Font("Segoe UI",22);glyph.SetBounds(0,5,38,42);searchPanel.Controls.Add(glyph);
   input.BorderStyle=BorderStyle.None;input.Font=new Font("Segoe UI",16);input.BackColor=searchPanel.BackColor;input.ForeColor=ForeColor;input.SetBounds(39,15,321,33);input.AccessibleName="Fuzzy search apps and files";searchPanel.Controls.Add(input);
   tabs[0]=MakeButton("Apps ▾",405,34,90,33);searchPanel.Controls.Add(tabs[0]);tabs[0].Location=new Point(370,11);tabs[0].BringToFront();tabs[0].Click+=delegate{SetMode((mode+1)%3);};
   dial.SetBounds(20,104,500,500);using(var circle=new GraphicsPath()){circle.AddEllipse(0,0,500,500);dial.Region=new Region(circle);}Controls.Add(dial);dial.Changed=SelectionChanged;dial.Launch=delegate{OpenSelected(false);};
   status.Font=new Font("Segoe UI",7.5f);status.ForeColor=Color.FromArgb(125,125,125);status.BackColor=Color.FromArgb(32,32,32);status.TextAlign=ContentAlignment.MiddleCenter;status.SetBounds(151,155,198,24);dial.Controls.Add(status);
   input.TextChanged+=delegate{dirty=true;debounce.Stop();debounce.Start();};debounce.Interval=45;debounce.Tick+=delegate{debounce.Stop();RefreshResults();};
   KeyDown+=KeysPressed;MouseWheel+=delegate(object s,MouseEventArgs e){dial.MoveSelection(e.Delta>0?-1:1);};
   Deactivate+=delegate{Note("deactivate");if(wantedVisible && !changingVisibility)Dismiss();};
   var menu=new ContextMenuStrip();openMenu=(ToolStripMenuItem)menu.Items.Add("Open",null,delegate{Reveal();});menu.Items.Add("Settings",null,delegate{OpenSettings();});menu.Items.Add("Refresh apps and files",null,delegate{Reindex();});menu.Items.Add("Edit file search folders",null,delegate{try{EnsureConfig();Process.Start("notepad.exe","\""+config+"\"");}catch(Exception ex){status.Text=ex.Message;}});menu.Items.Add("Exit",null,delegate{exiting=true;Close();});
   tray.Icon=Icon;tray.ContextMenuStrip=menu;tray.Visible=!smoke;tray.DoubleClick+=delegate{Reveal();};
   Shown+=delegate{SendMessage(input.Handle,0x1501,new IntPtr(1),"Apps, files, g web, or maths…");if(!smoke){RegisterConfiguredHotkey(true);focusMonitor=new FocusMonitor(delegate{if(wantedVisible)Dismiss();});Reveal();Reindex();}else{var timer=new System.Windows.Forms.Timer();timer.Interval=2200;timer.Tick+=delegate{timer.Dispose();exiting=true;Close();};timer.Start();}};
   float scale;using(var screen=Graphics.FromHwnd(IntPtr.Zero))scale=screen.DpiX/96f;
   if(Math.Abs(scale-1)>0.01f){Scale(new SizeF(scale,scale));ClientSize=new Size((int)(540*scale),(int)(620*scale));using(var matrix=new Matrix()){matrix.Scale(scale,scale);Region.Transform(matrix);}}
   SetMode(settings.Mode);ApplySettings();
  }
  Button MakeButton(string text,int x,int y,int w,int h){var b=new Button{Text=text,FlatStyle=FlatStyle.Flat,ForeColor=ForeColor,BackColor=Color.FromArgb(35,32,45),Cursor=Cursors.Hand,TabStop=false};b.FlatAppearance.BorderSize=0;b.SetBounds(x,y,w,h);Controls.Add(b);return b;}
  protected override bool ProcessCmdKey(ref Message msg,Keys key){if(key==Keys.Tab||key==(Keys.Shift|Keys.Tab)){SetMode((mode+(key==Keys.Tab?1:2))%3);return true;}return base.ProcessCmdKey(ref msg,key);}
  void SetMode(int value){dirty=true;mode=value;if(tabs[0]!=null){var p=Palette.From(settings);tabs[0].Text=new[]{"Apps ▾","Files ▾","All ▾"}[mode];tabs[0].BackColor=p.Button;tabs[0].ForeColor=p.Text;}RefreshResults();input.Focus();}
  void SelectionChanged(){var e=dial.Current;status.Text=e==null?"":e.Action==EntryAction.Google?"GOOGLE SEARCH":e.Action==EntryAction.Calculator?"CALCULATOR":e.Action==EntryAction.Settings?"SETTINGS":e.App?"APPLICATION":"FILE";}
  void ApplySettings(){var p=Palette.From(settings);BackColor=p.Back;ForeColor=p.Text;searchPanel.BackColor=p.Panel;input.BackColor=p.Panel;input.ForeColor=p.Text;glyph.ForeColor=p.Muted;status.BackColor=p.Inner;status.ForeColor=p.Accent;dial.ApplySettings(settings);if(tabs[0]!=null){tabs[0].BackColor=p.Button;tabs[0].ForeColor=p.Text;}string text="Swift · "+settings.HotkeyText;tray.Text=text.Length>63?"Swift":text;if(openMenu!=null)openMenu.Text="Open · "+settings.HotkeyText;Invalidate(true);}
  bool RegisterConfiguredHotkey(bool notify){if(hotkey!=null){hotkey.Dispose();hotkey=null;}hotkey=new HotkeyListener(delegate{if(wantedVisible)Dismiss();else Reveal();},settings.HotkeyModifiers,settings.HotkeyKey,Reveal,delegate{exiting=true;Close();});if(!hotkey.Registered&&notify){tray.BalloonTipTitle=settings.HotkeyText+" is already in use";tray.BalloonTipText="Choose another shortcut in Swift Settings.";tray.ShowBalloonTip(4000);}return hotkey.Registered;}
  bool TryApplySettings(SwiftSettings value){var previous=settings;if(hotkey!=null){hotkey.Dispose();hotkey=null;}settings=value;hotkey=new HotkeyListener(delegate{if(wantedVisible)Dismiss();else Reveal();},settings.HotkeyModifiers,settings.HotkeyKey,Reveal,delegate{exiting=true;Close();});if(!hotkey.Registered){hotkey.Dispose();hotkey=null;settings=previous;RegisterConfiguredHotkey(false);return false;}SetMode(settings.Mode);ApplySettings();return true;}
  void OpenSettings(){wantedVisible=false;if(transition!=null){transition.Dispose();transition=null;}if(Visible)Hide();using(var form=new SettingsForm(settings,TryApplySettings)){form.ShowDialog();}}
  void Reveal(){
   Note("reveal");
   if(settings.ClearSearch&&input.Text.Length>0){
    input.Clear();debounce.Stop();if(searchCancel!=null)searchCancel.Cancel();generation++;dirty=false;
    dial.SetEntries(Search.Find(index,"",mode,CancellationToken.None));
    if(transition!=null){transition.Dispose();transition=null;}
   }
   wantedVisible=true;
   if(transition!=null){transition.Play(true);return;}
   var area=Screen.FromPoint(Cursor.Position).WorkingArea;Location=new Point(area.Left+(area.Width-Width)/2,area.Top+Math.Max(0,(area.Height-Height)/3));
   input.SelectAll();RefreshResults();BeginTransition(true);
  }
  void Dismiss(){Note("dismiss");if(!wantedVisible && transition==null)return;wantedVisible=false;if(transition!=null){transition.Play(false);return;}if(!Visible)return;BeginTransition(false);}
  void BeginTransition(bool opening){
   try{
    var frame=new Bitmap(Width,Height);DrawToBitmap(frame,new Rectangle(0,0,Width,Height));
    if(settings.AnimationScale<=0){if(opening){Show();Activate();SetForegroundWindow(Handle);input.Focus();}else Hide();return;}
    transition=new TransitionWindow(frame,Region.Clone(),Bounds,opening,settings.AnimationScale,delegate(bool shown){
     Note("finish "+shown);var finished=transition;LastMotionFrames=finished==null?0:finished.FrameCount;transition=null;
     changingVisibility=true;
     try {if(shown){Opacity=1;Show();Activate();SetForegroundWindow(Handle);input.Focus();Invalidate(true);}else{Hide();Opacity=1;}if(finished!=null)finished.Dispose();}finally{changingVisibility=false;}
    });
    changingVisibility=true;try{
     if(opening){Opacity=0.01;Show();Activate();SetForegroundWindow(Handle);input.Focus();transition.Play(true);}
     else {transition.Play(false);DwmFlush();Hide();}
    }finally{changingVisibility=false;}
   }catch(Exception ex){TransitionError=ex.ToString();Opacity=1;if(transition!=null){transition.Dispose();transition=null;}if(opening){Show();Activate();input.Focus();}else Hide();}
  }
  protected override void WndProc(ref Message m){if(m.Msg==0x112 && (m.WParam.ToInt64()&0xfff0)==0xf100)return;base.WndProc(ref m);}
  protected override void OnFormClosing(FormClosingEventArgs e){if(!exiting && e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Dismiss();return;}if(searchCancel!=null)searchCancel.Cancel();UnregisterHotKey(Handle,1);tray.Dispose();debounce.Dispose();base.OnFormClosing(e);}
  protected override void Dispose(bool disposing){if(disposing){if(focusMonitor!=null){focusMonitor.Dispose();focusMonitor=null;}if(hotkey!=null){hotkey.Dispose();hotkey=null;}if(transition!=null){transition.Dispose();transition=null;}tray.Dispose();debounce.Dispose();}base.Dispose(disposing);}
  void KeysPressed(object s,KeyEventArgs e){
   if(e.KeyCode==Keys.Escape){Dismiss();e.SuppressKeyPress=true;}
   if(e.KeyCode==Keys.Down||e.KeyCode==Keys.Up){dial.MoveSelection(e.KeyCode==Keys.Down?1:-1);e.SuppressKeyPress=true;}
   if(e.KeyCode==Keys.Enter){OpenSelected(e.Control);e.SuppressKeyPress=true;}
   if(e.KeyCode==Keys.F5){Reindex();e.SuppressKeyPress=true;}
  }
  void OpenSelected(bool folder){if(dirty){dial.SetEntries(BuildResults(index,input.Text.Trim(),mode,CancellationToken.None));dirty=false;}var entry=dial.Current;if(entry==null)return;try{if(entry.Action==EntryAction.Google)Process.Start(new ProcessStartInfo("https://www.google.com/search?q="+Uri.EscapeDataString(entry.Payload)){UseShellExecute=true});else if(entry.Action==EntryAction.Calculator){Clipboard.SetText(entry.Payload);status.Text="COPIED "+entry.Payload;return;}else if(entry.Action==EntryAction.Settings){Dismiss();BeginInvoke((Action)OpenSettings);return;}else Process.Start(entry.LaunchInfo(folder));Dismiss();}catch(Exception ex){status.Text="Could not open: "+ex.Message;}}
  void EnsureConfig(){Directory.CreateDirectory(Path.GetDirectoryName(config));if(!File.Exists(config))File.WriteAllLines(config,new[]{"# One file search folder per line. Save and press F5. Apps are discovered separately.",Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads")});}
  void Reindex(){if(indexing)return;indexing=true;status.Text="Loading apps…";
   var thread=new Thread(delegate(){
    string warning;var apps=Catalog.Apps(out warning);appWarning=warning;
    Post(delegate{index=apps.ToArray();indexNote=apps.Count+" apps · indexing files…";RefreshResults();});
    var files=new List<Entry>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);int skipped=0;
    try{EnsureConfig();foreach(string raw in File.ReadAllLines(config)){
     string root=Environment.ExpandEnvironmentVariables(raw.Trim());if(root.Length==0||root.StartsWith("#"))continue;
     var pending=new Stack<string>();pending.Push(root);
     while(pending.Count>0&&files.Count<Limit){string dir=pending.Pop();try{
      if(!seen.Add(Path.GetFullPath(dir))||(File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0)continue;
      foreach(string file in Directory.EnumerateFiles(dir)){string ext=Path.GetExtension(file).ToLowerInvariant();if(ext==".lnk"||ext==".appref-ms")continue;files.Add(new Entry(file));if(files.Count>=Limit)break;}
      foreach(string sub in Directory.EnumerateDirectories(dir)){string n=Path.GetFileName(sub).ToLowerInvariant();if(n=="node_modules"||n==".git"||n==".venv"||n=="appdata")continue;pending.Push(sub);}
     }catch(IOException){skipped++;}catch(UnauthorizedAccessException){skipped++;}catch(System.Security.SecurityException){skipped++;}}
    }
    files.Sort((a,b)=>StringComparer.OrdinalIgnoreCase.Compare(a.Name,b.Name));var combined=new List<Entry>(apps);combined.AddRange(files);
    Post(delegate{index=combined.ToArray();indexing=false;indexNote=apps.Count+" apps · "+files.Count.ToString("N0")+" files"+(files.Count>=Limit?" (limit)":"")+(skipped>0?" · "+skipped+" skipped":"")+(appWarning.Length>0?" · app catalogue fallback":"");RefreshResults();});
    }catch(Exception ex){string message=ex.Message;Post(delegate{indexing=false;indexNote=apps.Count+" apps · file index failed: "+message;RefreshResults();});}
   });thread.IsBackground=true;thread.SetApartmentState(ApartmentState.STA);thread.Start();
  }
  void Post(Action action){try{if(!IsDisposed&&IsHandleCreated)BeginInvoke(action);}catch(InvalidOperationException){}}
  List<Entry> BuildResults(Entry[] snapshot,string raw,int filter,CancellationToken token){string query=raw.Trim(),lower=query.ToLowerInvariant();if(settings.Google&&lower.StartsWith("g ")&&query.Length>2)return new List<Entry>{Entry.Special("Search Google for “"+query.Substring(2).Trim()+"”",EntryAction.Google,query.Substring(2).Trim())};var matches=Search.Find(snapshot,lower,filter,token);string answer;if(settings.Calculator&&CalculatorEngine.TryEvaluate(query,out answer))matches.Insert(0,Entry.Special(query+" = "+answer,EntryAction.Calculator,answer));if(lower=="settings"||lower=="preferences")matches.Insert(0,Entry.Special("Swift Settings",EntryAction.Settings,""));return matches;}
  void RefreshResults(){if(!IsHandleCreated)return;if(searchCancel!=null)searchCancel.Cancel();searchCancel=new CancellationTokenSource();var token=searchCancel.Token;int version=++generation,filter=mode;string query=input.Text.Trim();Entry[] snapshot=index;
   Task.Run(delegate{var matches=BuildResults(snapshot,query,filter,token);Post(delegate{if(version!=generation)return;dirty=false;dial.SetEntries(matches);status.Text=matches.Count==0?(indexing?"INDEXING…":"NO RESULTS"):matches[0].Action==EntryAction.Google?"GOOGLE SEARCH":matches[0].Action==EntryAction.Calculator?"CALCULATOR":matches[0].Action==EntryAction.Settings?"SETTINGS":matches[0].App?"APPLICATION":"FILE";});});
  }
  public void LoadDemo(){string note;var apps=Catalog.Apps(out note);index=apps.ToArray();indexNote=apps.Count+" apps";dial.SetEntries(new List<Entry>(apps));}
 }
}
