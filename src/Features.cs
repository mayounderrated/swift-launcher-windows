using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Swift {
 enum EntryAction { None, Google, Calculator, Settings }

 sealed class SwiftSettings {
  public string Theme="Dark",Accent="Purple",Modifier="Ctrl",Key="Space",DefaultMode="All",Animation="Fast";
  public bool Google=true,Calculator=true,ClearSearch=true,StartWithWindows=false;
  public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SwiftLauncher");
  public static readonly string FilePath=Path.Combine(Folder,"settings.ini");
  public static SwiftSettings Load(){
   var value=new SwiftSettings();
   try{if(File.Exists(FilePath))foreach(string raw in File.ReadAllLines(FilePath)){
    int split=raw.IndexOf('=');if(split<1)continue;string key=raw.Substring(0,split).Trim(),text=raw.Substring(split+1).Trim();bool flag;
    switch(key){
     case "Theme":value.Theme=text;break;case "Accent":value.Accent=text;break;case "Modifier":value.Modifier=text;break;case "Key":value.Key=text;break;
     case "DefaultMode":value.DefaultMode=text;break;case "Animation":value.Animation=text;break;
     case "Google":if(Boolean.TryParse(text,out flag))value.Google=flag;break;case "Calculator":if(Boolean.TryParse(text,out flag))value.Calculator=flag;break;
     case "ClearSearch":if(Boolean.TryParse(text,out flag))value.ClearSearch=flag;break;case "StartWithWindows":if(Boolean.TryParse(text,out flag))value.StartWithWindows=flag;break;
    }
   }}catch{}
   return value;
  }
  public void Save(){
   Directory.CreateDirectory(Folder);
   File.WriteAllLines(FilePath,new[]{"Theme="+Theme,"Accent="+Accent,"Modifier="+Modifier,"Key="+Key,"DefaultMode="+DefaultMode,"Animation="+Animation,"Google="+Google,"Calculator="+Calculator,"ClearSearch="+ClearSearch,"StartWithWindows="+StartWithWindows});
   try{using(var run=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true)){if(StartWithWindows)run.SetValue("Swift Launcher","\""+Application.ExecutablePath+"\"");else run.DeleteValue("Swift Launcher",false);}}catch{}
  }
  public uint HotkeyModifiers {get{uint value=0x4000;if(Modifier.IndexOf("Ctrl",StringComparison.OrdinalIgnoreCase)>=0)value|=0x2;if(Modifier.IndexOf("Alt",StringComparison.OrdinalIgnoreCase)>=0)value|=0x1;if(Modifier.IndexOf("Shift",StringComparison.OrdinalIgnoreCase)>=0)value|=0x4;if(Modifier.IndexOf("Win",StringComparison.OrdinalIgnoreCase)>=0)value|=0x8;return value;}}
  public uint HotkeyKey {get{Keys parsed;return Enum.TryParse<Keys>(Key,true,out parsed)?(uint)parsed:(uint)Keys.Space;}}
  public string HotkeyText {get{return Modifier+"+"+Key;}}
  public int Mode {get{return DefaultMode=="Apps"?0:DefaultMode=="Files"?1:2;}}
  public double AnimationScale {get{return Animation=="Off"?0:Animation=="Normal"?1.35:1;}}
 }

 sealed class Palette {
  public Color Back,Inner,Panel,Text,Muted,Button,Accent;
  public static Palette From(SwiftSettings settings){
   bool light=settings.Theme=="Light"||(settings.Theme=="System"&&SystemColors.Window.GetBrightness()>0.5f);
   Color accent=Color.FromArgb(203,185,255);
   if(settings.Accent=="Blue")accent=Color.FromArgb(105,177,255);else if(settings.Accent=="Green")accent=Color.FromArgb(105,214,155);else if(settings.Accent=="Orange")accent=Color.FromArgb(255,166,94);else if(settings.Accent=="Pink")accent=Color.FromArgb(255,130,190);
   return light?new Palette{Back=Color.FromArgb(244,244,247),Inner=Color.White,Panel=Color.FromArgb(244,244,247),Text=Color.FromArgb(24,24,29),Muted=Color.FromArgb(103,103,113),Button=Color.FromArgb(226,224,232),Accent=accent}:new Palette{Back=Color.FromArgb(17,17,17),Inner=Color.FromArgb(32,32,32),Panel=Color.FromArgb(17,17,17),Text=Color.White,Muted=Color.FromArgb(145,143,161),Button=Color.FromArgb(35,32,45),Accent=accent};
  }
 }

 static class CalculatorEngine {
  public static bool TryEvaluate(string text,out string result){
   result=null;if(String.IsNullOrWhiteSpace(text)||text.Length>100)return false;
   bool looks=false;foreach(char c in text)if(Char.IsDigit(c)){looks=true;break;}if(!looks)return false;
   try{var parser=new Parser(text);double value=parser.Expression();parser.Space();if(!parser.End||Double.IsNaN(value)||Double.IsInfinity(value))return false;result=value.ToString("G12",CultureInfo.InvariantCulture);return true;}catch{return false;}
  }
  sealed class Parser {
   readonly string s;int p;public Parser(string value){s=value;}public bool End{get{return p==s.Length;}}public void Space(){while(p<s.Length&&Char.IsWhiteSpace(s[p]))p++;}
   bool Eat(char c){Space();if(p<s.Length&&s[p]==c){p++;return true;}return false;}
   public double Expression(){double x=Term();for(;;){if(Eat('+'))x+=Term();else if(Eat('-'))x-=Term();else return x;}}
   double Term(){double x=Unary();for(;;){if(Eat('*'))x*=Unary();else if(Eat('/'))x/=Unary();else if(Eat('%'))x%=Unary();else return x;}}
   double Power(){double x=Primary();if(Eat('^'))x=Math.Pow(x,Unary());return x;}
   double Unary(){if(Eat('+'))return Unary();if(Eat('-'))return-Unary();return Power();}
   double Primary(){
    if(Eat('(')){double x=Expression();if(!Eat(')'))throw new FormatException();return x;}
    Space();int start=p;if(p<s.Length&&(Char.IsLetter(s[p])||s[p]=='π')){while(p<s.Length&&(Char.IsLetter(s[p])||s[p]=='π'))p++;string name=s.Substring(start,p-start).ToLowerInvariant();if(name=="pi"||name=="π")return Math.PI;if(name=="e")return Math.E;if(!Eat('('))throw new FormatException();double x=Expression();if(!Eat(')'))throw new FormatException();switch(name){case "sqrt":return Math.Sqrt(x);case "sin":return Math.Sin(x);case "cos":return Math.Cos(x);case "tan":return Math.Tan(x);case "abs":return Math.Abs(x);case "ln":return Math.Log(x);case "log":return Math.Log10(x);case "floor":return Math.Floor(x);case "ceil":return Math.Ceiling(x);case "round":return Math.Round(x);default:throw new FormatException();}}
    start=p;bool dot=false;while(p<s.Length&&(Char.IsDigit(s[p])||s[p]=='.')){if(s[p]=='.'){if(dot)throw new FormatException();dot=true;}p++;}if(start==p)throw new FormatException();return Double.Parse(s.Substring(start,p-start),CultureInfo.InvariantCulture);
   }
  }
 }

 sealed class SettingsForm : Form {
  readonly ComboBox theme=new ComboBox(),accent=new ComboBox(),modifier=new ComboBox(),key=new ComboBox(),mode=new ComboBox(),animation=new ComboBox();
  readonly CheckBox google=new CheckBox(),calculator=new CheckBox(),clear=new CheckBox(),startup=new CheckBox();readonly Label hotkeyHint=new Label();readonly Func<SwiftSettings,bool> applied;
  public SettingsForm(SwiftSettings current,Func<SwiftSettings,bool> onApply){
   applied=onApply;Text="Swift Settings";Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(800,620);MinimumSize=new Size(760,600);Font=new Font("Segoe UI",10);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
   var nav=new Panel{Dock=DockStyle.Left,Width=190,BackColor=Color.FromArgb(24,24,27)};Controls.Add(nav);var brand=new Label{Text="SWIFT  2",ForeColor=Color.White,Font=new Font("Segoe UI",16,FontStyle.Bold),AutoSize=true,Location=new Point(25,27)};nav.Controls.Add(brand);
   var navText=new Label{Text="GENERAL\n\nAPPEARANCE\n\nSEARCH",ForeColor=Color.FromArgb(185,185,195),Font=new Font("Segoe UI",10,FontStyle.Bold),AutoSize=true,Location=new Point(27,92)};nav.Controls.Add(navText);
   var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(32),AutoScroll=true};Controls.Add(body);body.BringToFront();
   int y=20;AddTitle(body,"General",ref y);AddCombo(body,"Global shortcut modifier",modifier,new[]{"Ctrl","Alt","Ctrl+Alt","Ctrl+Shift","Alt+Shift","Win"},current.Modifier,ref y);AddCombo(body,"Key",key,new[]{"Space","Q","K","L","F1","F2","F3","F4","F8","F9","F10","F11","F12"},current.Key,ref y);hotkeyHint.SetBounds(220,y-4,290,24);hotkeyHint.ForeColor=Color.FromArgb(115,115,125);body.Controls.Add(hotkeyHint);modifier.SelectedIndexChanged+=delegate{UpdateHint();};key.SelectedIndexChanged+=delegate{UpdateHint();};y+=28;
   AddCombo(body,"Default mode",mode,new[]{"All","Apps","Files"},current.DefaultMode,ref y);AddCheck(body,"Clear search whenever Swift opens",clear,current.ClearSearch,ref y);AddCheck(body,"Start Swift when I sign in",startup,current.StartWithWindows,ref y);
   AddTitle(body,"Appearance",ref y);AddCombo(body,"Theme",theme,new[]{"Dark","Light","System"},current.Theme,ref y);AddCombo(body,"Accent",accent,new[]{"Purple","Blue","Green","Orange","Pink"},current.Accent,ref y);AddCombo(body,"Animation speed",animation,new[]{"Fast","Normal","Off"},current.Animation,ref y);
   AddTitle(body,"Search",ref y);AddCheck(body,"Google search with g query",google,current.Google,ref y);AddCheck(body,"Math calculator and copy result",calculator,current.Calculator,ref y);
   var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,FlatStyle=FlatStyle.Flat};cancel.SetBounds(560,565,90,34);Controls.Add(cancel);var save=new Button{Text="Save",BackColor=Color.FromArgb(115,91,180),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};save.FlatAppearance.BorderSize=0;save.SetBounds(660,565,105,34);save.Click+=SaveClicked;Controls.Add(save);AcceptButton=save;CancelButton=cancel;ApplyColors(current);save.BackColor=Palette.From(current).Accent;save.ForeColor=Color.White;UpdateHint();
  }
  static void AddTitle(Control parent,string text,ref int y){var label=new Label{Text=text,Font=new Font("Segoe UI",15,FontStyle.Bold),AutoSize=true};label.SetBounds(15,y,300,30);parent.Controls.Add(label);y+=42;}
  static void AddCombo(Control parent,string label,ComboBox box,string[] values,string selected,ref int y){var l=new Label{Text=label,AutoSize=false};l.SetBounds(15,y+6,215,24);parent.Controls.Add(l);box.DropDownStyle=ComboBoxStyle.DropDownList;box.Items.AddRange(values);box.SetBounds(230,y,300,32);box.SelectedItem=selected;if(box.SelectedIndex<0)box.SelectedIndex=0;parent.Controls.Add(box);y+=43;}
  static void AddCheck(Control parent,string text,CheckBox box,bool value,ref int y){box.Text=text;box.Checked=value;box.AutoSize=true;box.SetBounds(220,y,330,28);parent.Controls.Add(box);y+=36;}
  void UpdateHint(){hotkeyHint.Text="Shortcut: "+modifier.Text+"+"+key.Text;}
  static void ColorTree(Control parent,Palette p){foreach(Control c in parent.Controls){if(c is Panel&&c.Dock==DockStyle.Left)continue;if(c is Label||c is CheckBox)c.ForeColor=p.Text;if(c is ComboBox){c.BackColor=p.Panel;c.ForeColor=p.Text;}if(c is Panel)c.BackColor=p.Inner;ColorTree(c,p);}}
  void ApplyColors(SwiftSettings settings){var p=Palette.From(settings);BackColor=p.Inner;ForeColor=p.Text;ColorTree(this,p);}
  void SaveClicked(object sender,EventArgs e){var next=new SwiftSettings{Theme=theme.Text,Accent=accent.Text,Modifier=modifier.Text,Key=key.Text,DefaultMode=mode.Text,Animation=animation.Text,Google=google.Checked,Calculator=calculator.Checked,ClearSearch=clear.Checked,StartWithWindows=startup.Checked};if(applied!=null&&!applied(next)){MessageBox.Show(this,"That shortcut is already used by another application. Choose a different one.","Swift shortcut unavailable",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}next.Save();DialogResult=DialogResult.OK;Close();}
 }
}
