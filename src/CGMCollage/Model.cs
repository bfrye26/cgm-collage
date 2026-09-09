namespace CGMCollage;
public record PointD(double X,double Y);
public record EmbeddedImage(string Name,string Mime,string Data);
public class Frame
{
 public string Id{get;set;}=Guid.NewGuid().ToString("N"); public string Name{get;set;}="Panel"; public double X{get;set;} public double Y{get;set;} public double W{get;set;}=1; public double H{get;set;}=1; public string Shape{get;set;}="rect"; public List<PointD> Points{get;set;}=[]; public EmbeddedImage? Image{get;set;}
 public double CropX{get;set;} public double CropY{get;set;} public double Zoom{get;set;}=1; public double Rotation{get;set;} public double Brightness{get;set;} public double Contrast{get;set;}=1; public double Saturation{get;set;}=1; public double Opacity{get;set;}=1; public double BorderWidth{get;set;} public string BorderColor{get;set;}="#ffffff"; public bool Shadow{get;set;} public double ShadowOpacity{get;set;}=.28; public double ShadowDistance{get;set;}=10; public double ShadowAngle{get;set;}=135; public double ShadowBlur{get;set;}=10; public string FitMode{get;set;}="fill"; public bool FlipX{get;set;} public bool FlipY{get;set;} public bool Locked{get;set;} public bool Hidden{get;set;}
 public Frame Clone(){var f=(Frame)MemberwiseClone();f.Points=[..Points];return f;} public void ResetCrop(){CropX=CropY=Rotation=0;Zoom=1;FlipX=FlipY=false;} public void Move(double dx,double dy){X+=dx;Y+=dy;Points=Points.Select(p=>new PointD(p.X+dx,p.Y+dy)).ToList();}
}
public class TextLayer { public string Id{get;set;}=Guid.NewGuid().ToString("N");public string Text{get;set;}="Your headline";public double X{get;set;}=.075;public double Y{get;set;}=.72;public double W{get;set;}=.85;public double H{get;set;}=.22;public double FontSize{get;set;}=52;public string FontFamily{get;set;}="Segoe UI";public bool Bold{get;set;}=true;public string Color{get;set;}="#ffffff";public string Align{get;set;}="left";public bool Shadow{get;set;}=true;public bool Hidden{get;set;} public TextLayer Clone()=>(TextLayer)MemberwiseClone();}
public class Project
{
 public int Format{get;set;}=5;public string AppVersion{get;set;}="3.0.2";public string Name{get;set;}="Untitled project";public int CanvasWidth{get;set;}=1200;public int CanvasHeight{get;set;}=675;public string Background{get;set;}="#101216";public string GapColor{get;set;}="#101216";public double Gap{get;set;} public int ExportQuality{get;set;}=90;public int TargetKB{get;set;} public string Template{get;set;}="Angled Triptych";public List<Frame> Frames{get;set;}=Templates.Create("Angled Triptych");public List<TextLayer> Texts{get;set;}=[];
 public Project Clone(){var p=(Project)MemberwiseClone();p.Frames=Frames.Select(f=>f.Clone()).ToList();p.Texts=Texts.Select(t=>t.Clone()).ToList();return p;}
 public void ApplyTemplate(string name){var n=Templates.Create(name);for(int i=0;i<n.Count&&i<Frames.Count;i++){var s=Frames[i].Clone();var g=n[i];s.X=g.X;s.Y=g.Y;s.W=g.W;s.H=g.H;s.Shape=g.Shape;s.Points=g.Points;n[i]=s;}Frames=n;Template=name;}
}
public sealed class History
{
 readonly List<Project> states=[];int cursor;public bool CanUndo=>cursor>0;public bool CanRedo=>cursor<states.Count-1;public void Reset(Project p){states.Clear();states.Add(p.Clone());cursor=0;}public void Push(Project p){if(CanRedo)states.RemoveRange(cursor+1,states.Count-cursor-1);states.Add(p.Clone());if(states.Count>60)states.RemoveAt(0);cursor=states.Count-1;}public Project Undo()=>states[CanUndo?--cursor:cursor].Clone();public Project Redo()=>states[CanRedo?++cursor:cursor].Clone();
}
