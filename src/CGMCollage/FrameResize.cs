namespace CGMCollage;

[Flags]
public enum ResizeEdges { None=0, Left=1, Right=2, Top=4, Bottom=8 }

public static class FrameResize
{
 // Apply the same continuous coordinate map to every panel. Shared edges and
 // T-junctions stay connected, including layouts that use angled polygons.
 public static bool ApplyConnected(IReadOnlyList<Frame> frames,IReadOnlyList<Frame> originals,int selected,ResizeEdges edges,double dx,double dy)
 {
  if(selected<0||selected>=originals.Count||frames.Count!=originals.Count)return false;
  var selectedFrame=originals[selected];
  double? x=edges.HasFlag(ResizeEdges.Left)?selectedFrame.X:edges.HasFlag(ResizeEdges.Right)?selectedFrame.X+selectedFrame.W:null;
  double? y=edges.HasFlag(ResizeEdges.Top)?selectedFrame.Y:edges.HasFlag(ResizeEdges.Bottom)?selectedFrame.Y+selectedFrame.H:null;
  var horizontal=CreateMap(originals,x,dx,true);var vertical=CreateMap(originals,y,dy,false);
  bool changed=false;
  for(int i=0;i<frames.Count;i++)
  {
   var original=originals[i];var frame=frames[i];
   frame.X=horizontal.Map(original.X);frame.Y=vertical.Map(original.Y);
   frame.W=horizontal.Map(original.X+original.W)-frame.X;frame.H=vertical.Map(original.Y+original.H)-frame.Y;
   if(original.Shape=="poly"&&original.Points.Count>2)
   {
    // Split edges at map breakpoints so neighbouring polygons share the same
    // segments even when one side has an extra vertex.
    var points=new List<PointD>();
    for(int j=0;j<original.Points.Count;j++)
    {
     var a=original.Points[j];var b=original.Points[(j+1)%original.Points.Count];
     var cuts=new List<double>{0};
     foreach(double knot in horizontal.Knots)if(Math.Abs(b.X-a.X)>1e-10){double t=(knot-a.X)/(b.X-a.X);if(t>1e-10&&t<1-1e-10)cuts.Add(t);}
     foreach(double knot in vertical.Knots)if(Math.Abs(b.Y-a.Y)>1e-10){double t=(knot-a.Y)/(b.Y-a.Y);if(t>1e-10&&t<1-1e-10)cuts.Add(t);}
     foreach(double t in cuts.Distinct().Order())points.Add(new PointD(horizontal.Map(a.X+(b.X-a.X)*t),vertical.Map(a.Y+(b.Y-a.Y)*t)));
    }
    frame.Points=points;
   }
   else frame.Points=[..original.Points];
   changed|=Math.Abs(frame.X-original.X)>1e-10||Math.Abs(frame.Y-original.Y)>1e-10||Math.Abs(frame.W-original.W)>1e-10||Math.Abs(frame.H-original.H)>1e-10;
  }
  return changed;
 }
 sealed record AxisMap(double Lower,double Boundary,double Upper,double Moved)
 {
  public double[] Knots=>Math.Abs(Moved-Boundary)<1e-10?[]:[Lower,Boundary,Upper];
  public double Map(double value)
  {
   if(value<=Lower||value>=Upper||Math.Abs(Moved-Boundary)<1e-10)return value;
   return value<=Boundary?Lower+(value-Lower)/(Boundary-Lower)*(Moved-Lower):Moved+(value-Boundary)/(Upper-Boundary)*(Upper-Moved);
  }
 }
 static AxisMap CreateMap(IReadOnlyList<Frame> frames,double? boundary,double delta,bool horizontal)
 {
  if(boundary is not double at||at<1e-8||at>1-1e-8)return new(0,.5,1,.5);
  var coordinates=new List<double>{0,1};
  foreach(var frame in frames)
  {
   coordinates.Add(horizontal?frame.X:frame.Y);coordinates.Add(horizontal?frame.X+frame.W:frame.Y+frame.H);
   if(frame.Shape=="poly")coordinates.AddRange(frame.Points.Select(p=>horizontal?p.X:p.Y));
  }
  double lower=coordinates.Where(v=>v<at-1e-8).DefaultIfEmpty(0).Max(),upper=coordinates.Where(v=>v>at+1e-8).DefaultIfEmpty(1).Min();
  // A locked panel pins any boundary passing through its interior.
  if(frames.Any(f=>f.Locked&&(horizontal?f.X<upper&&f.X+f.W>lower:f.Y<upper&&f.Y+f.H>lower)))delta=0;
  double margin=Math.Min(.02,Math.Min(at-lower,upper-at)/2);
  return new(lower,at,upper,Math.Clamp(at+delta,lower+margin,upper-margin));
 }
}
