namespace CGMCollage;

[Flags]
public enum ResizeEdges { None=0, Left=1, Right=2, Top=4, Bottom=8 }

public static class FrameResize
{
 const double Epsilon=1e-8;
 sealed record Line(double X,double Y,double C)
 {
  public bool Same(Line other)=>Math.Abs(X-other.X)<Epsilon&&Math.Abs(Y-other.Y)<Epsilon&&Math.Abs(C-other.C)<Epsilon;
 }
 sealed record Panel(Frame Original,List<PointD> Points,List<Line> Lines);
 static List<PointD> Vertices(Frame frame)
 {
  var points=frame.Shape=="poly"&&frame.Points.Count>2?new List<PointD>(frame.Points):
   [new(frame.X,frame.Y),new(frame.X+frame.W,frame.Y),new(frame.X+frame.W,frame.Y+frame.H),new(frame.X,frame.Y+frame.H)];
  // Older custom layouts may contain redundant vertices along a straight side.
  bool removed=true;
  while(removed&&points.Count>3)
  {
   removed=false;
   for(int i=0;i<points.Count;i++)
   {
    var a=points[(i+points.Count-1)%points.Count];var b=points[i];var c=points[(i+1)%points.Count];
    double cross=(b.X-a.X)*(c.Y-b.Y)-(b.Y-a.Y)*(c.X-b.X);
    if(Math.Abs(cross)<1e-10){points.RemoveAt(i);removed=true;break;}
   }
  }
  return points;
 }
 static Line Side(PointD a,PointD b)
 {
  double x=b.Y-a.Y,y=a.X-b.X,length=Math.Sqrt(x*x+y*y);x/=length;y/=length;
  if(x<-Epsilon||Math.Abs(x)<Epsilon&&y<0){x=-x;y=-y;}
  return new Line(x,y,x*a.X+y*a.Y);
 }
 static Line? Divider(Panel panel,ResizeEdges edge)
 {
  var candidates=Enumerable.Range(0,panel.Points.Count).Where(i=>
  {
   var a=panel.Points[i];var b=panel.Points[(i+1)%panel.Points.Count];
   return edge is ResizeEdges.Left or ResizeEdges.Right?Math.Abs(a.Y-b.Y)>Epsilon:Math.Abs(a.X-b.X)>Epsilon;
  });
  bool horizontal=edge is ResizeEdges.Left or ResizeEdges.Right;
  bool largest=edge is ResizeEdges.Right or ResizeEdges.Bottom;
  var ordered=candidates.OrderBy(i=>
  {
   var a=panel.Points[i];var b=panel.Points[(i+1)%panel.Points.Count];
   double position=horizontal?(a.X+b.X)/2:(a.Y+b.Y)/2;return largest?-position:position;
  });
  foreach(int i in ordered)
  {
   var line=panel.Lines[i];
   // The outside of the canvas is fixed.
   if((Math.Abs(line.X)<Epsilon||Math.Abs(line.Y)<Epsilon)&&(Math.Abs(line.C)<Epsilon||Math.Abs(line.C-1)<Epsilon))return null;
   return line;
  }
  return null;
 }
 public static bool ApplyConnected(IReadOnlyList<Frame> frames,IReadOnlyList<Frame> originals,int selected,ResizeEdges edges,double dx,double dy)
 {
  if(selected<0||selected>=originals.Count||frames.Count!=originals.Count)return false;
  var panels=originals.Select(f=>
  {
   var points=Vertices(f);return new Panel(f,points,Enumerable.Range(0,points.Count).Select(i=>Side(points[i],points[(i+1)%points.Count])).ToList());
  }).ToList();
  var moves=new List<(Line Line,double Offset)>();
  foreach(var edge in new[]{ResizeEdges.Left,ResizeEdges.Right,ResizeEdges.Top,ResizeEdges.Bottom})
  {
   if(!edges.HasFlag(edge)||Divider(panels[selected],edge) is not { } line)continue;
   if(panels.Any(p=>p.Original.Locked&&p.Lines.Any(s=>s.Same(line))))continue;
   if(!moves.Any(m=>m.Line.Same(line)))moves.Add((line,line.X*dx+line.Y*dy));
  }
  if(moves.Count==0||moves.All(m=>Math.Abs(m.Offset)<1e-12))
  {
   Restore(frames,originals);return false;
  }
  List<List<PointD>>? Build(double fraction)
  {
   var result=new List<List<PointD>>();
   foreach(var panel in panels)
   {
    var lines=panel.Lines.Select(line=>
    {
     double offset=0;foreach(var move in moves)if(move.Line.Same(line))offset=move.Offset*fraction;
     return line with{C=line.C+offset};
    }).ToList();
    var points=new List<PointD>();
    for(int i=0;i<lines.Count;i++)
    {
     var a=lines[(i+lines.Count-1)%lines.Count];var b=lines[i];double determinant=a.X*b.Y-a.Y*b.X;
     if(Math.Abs(determinant)<1e-12)return null;
     var point=new PointD((a.C*b.Y-a.Y*b.C)/determinant,(a.X*b.C-a.C*b.X)/determinant);
     if(point.X<-Epsilon||point.X>1+Epsilon||point.Y<-Epsilon||point.Y>1+Epsilon)return null;
     points.Add(point);
    }
    // Every vertex must remain on the original interior side of each moved
    // supporting line. This prevents flipped or collapsed panels.
    for(int i=0;i<lines.Count;i++)
    {
     var line=lines[i];var old=panel.Lines[i];var interior=panel.Points[(i+2)%panel.Points.Count];
     double sign=Math.Sign(old.X*interior.X+old.Y*interior.Y-old.C);
     if(sign==0)return null;
     if(points.Any(p=>(line.X*p.X+line.Y*p.Y-line.C)*sign<-Epsilon))return null;
    }
    double width=points.Max(p=>p.X)-points.Min(p=>p.X),height=points.Max(p=>p.Y)-points.Min(p=>p.Y);
    if(width<Math.Min(.02,panel.Original.W)-Epsilon||height<Math.Min(.02,panel.Original.H)-Epsilon||Area(points)<Math.Max(1e-8,Area(panel.Points)*.02))return null;
    result.Add(points);
   }
   return result;
  }
  var geometry=Build(1);
  if(geometry is null)
  {
   double low=0,high=1;
   for(int i=0;i<45;i++){double middle=(low+high)/2;if(Build(middle) is null)high=middle;else low=middle;}
   geometry=Build(low);
  }
  if(geometry is null){Restore(frames,originals);return false;}
  bool changed=false;
  for(int i=0;i<frames.Count;i++)
  {
   var frame=frames[i];var original=originals[i];var points=geometry[i];
   frame.X=points.Min(p=>p.X);frame.Y=points.Min(p=>p.Y);
   frame.W=points.Max(p=>p.X)-frame.X;frame.H=points.Max(p=>p.Y)-frame.Y;
   frame.Points=original.Shape=="poly"?points:[..original.Points];
   changed|=Math.Abs(frame.X-original.X)>Epsilon||Math.Abs(frame.Y-original.Y)>Epsilon||Math.Abs(frame.W-original.W)>Epsilon||Math.Abs(frame.H-original.H)>Epsilon;
  }
  return changed;
 }
 static double Area(List<PointD> points)
 {
  double sum=0;for(int i=0;i<points.Count;i++){var a=points[i];var b=points[(i+1)%points.Count];sum+=a.X*b.Y-b.X*a.Y;}return Math.Abs(sum)/2;
 }
 static void Restore(IReadOnlyList<Frame> frames,IReadOnlyList<Frame> originals)
 {
  for(int i=0;i<frames.Count;i++){var f=frames[i];var o=originals[i];f.X=o.X;f.Y=o.Y;f.W=o.W;f.H=o.H;f.Points=[..o.Points];}
 }
}
