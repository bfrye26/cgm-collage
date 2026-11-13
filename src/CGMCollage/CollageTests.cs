namespace CGMCollage;

// Executed by --smoke-test before the Windows installer is packaged.
public static class CollageTests
{
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static EmbeddedImage Image(string name)=>new(name,"image/png","test-data");
 static void CheckCoverage(List<Frame> frames,string label)
 {
  Check(frames.All(f=>f.X>=-1e-8&&f.Y>=-1e-8&&f.X+f.W<=1+1e-8&&f.Y+f.H<=1+1e-8&&f.W>0&&f.H>0),$"Resize invalid bounds: {label}");
  double area=0;
  foreach(var frame in frames)
  {
   if(frame.Shape!="poly")area+=frame.W*frame.H;
   else
   {
    double sum=0;for(int i=0;i<frame.Points.Count;i++){var a=frame.Points[i];var b=frame.Points[(i+1)%frame.Points.Count];sum+=a.X*b.Y-b.X*a.Y;}
    area+=Math.Abs(sum)/2;
   }
  }
  Check(Math.Abs(area-1)<1e-8,$"Resize left uncovered area: {label}");
  for(int row=0;row<17;row++)for(int column=0;column<23;column++)
  {
   double x=(column+.371)/23,y=(row+.293)/17;
   int covering=frames.Count(f=>Contains(f,x,y));
   Check(covering==1,$"Resize produced a gap or overlap at {x},{y}: {label}");
  }
 }
 static bool Contains(Frame frame,double x,double y)
 {
  if(x<frame.X||x>frame.X+frame.W||y<frame.Y||y>frame.Y+frame.H)return false;
  if(frame.Shape!="poly")return true;
  bool inside=false;var points=frame.Points;
  for(int i=0,j=points.Count-1;i<points.Count;j=i++)
   if((points[i].Y>y)!=(points[j].Y>y)&&x<(points[j].X-points[i].X)*(y-points[i].Y)/(points[j].Y-points[i].Y)+points[i].X)inside=!inside;
  return inside;
 }
 public static void Run()
 {
  foreach(string layout in Templates.FlexibleNames)
  foreach(double aspect in new[]{1d,16d/9,4d/5})
  for(int count=1;count<=Templates.MaxPanels;count++)
  {
   var frames=Templates.CreateFlexible(layout,count,aspect);
   Check(frames.Count==count,$"{layout}: wrong count at {count}");
   Check(frames.All(f=>f.X>=0&&f.Y>=0&&f.W>0&&f.H>0&&f.X+f.W<=1.000001&&f.Y+f.H<=1.000001),"Panel outside canvas");
   Check(Math.Abs(frames.Sum(f=>f.W*f.H)-1)<.000001,"Layout does not fill canvas");
   for(int i=0;i<frames.Count;i++)for(int j=i+1;j<frames.Count;j++)
    Check(Math.Min(frames[i].X+frames[i].W,frames[j].X+frames[j].W)-Math.Max(frames[i].X,frames[j].X)<.000001||
          Math.Min(frames[i].Y+frames[i].H,frames[j].Y+frames[j].H)-Math.Max(frames[i].Y,frames[j].Y)<.000001,"Panels overlap");
  }
  foreach(int count in new[]{0,65})
  {
   bool rejected=false;try{Templates.CreateFlexible("Auto Grid",count);}catch(ArgumentOutOfRangeException){rejected=true;}
   Check(rejected,"Invalid count accepted");
  }
  var p=new Project();var first=Image("first");var second=Image("second");
  p.Frames[0].Image=first;p.Frames[0].CropX=.3;p.Frames[0].Brightness=.4;p.Frames[0].FlipX=true;
  p.Frames[0].BorderWidth=7;p.Frames[1].BorderWidth=2;
  var id=p.Frames[0].Id;p.SetPanelCount(12);
  Check(p.Frames[0].Image==first&&p.Frames[0].CropX==.3&&p.Frames[0].Id==id,"Resize lost image edits");
  p.ApplyTemplate("Columns");Check(p.Frames.Count==12&&p.Frames.All(f=>f.H==1),"Flexible layout lost count");
  p.Frames[1].Image=second;p.Frames[1].CropY=-.5;p.Frames[1].FitMode="fit";
  var geometry=p.Frames.Select(f=>(f.Id,f.X,f.Y,f.W,f.H,f.BorderWidth)).ToArray();
  var history=new History();history.Reset(p);
  Check(p.SwapImages(0,1),"Swap rejected");
  Check(p.Frames[1].Image==first&&p.Frames[1].CropX==.3&&p.Frames[1].Brightness==.4&&p.Frames[1].FlipX,"Swap lost source edits");
  Check(p.Frames[0].Image==second&&p.Frames[0].CropY==-.5&&p.Frames[0].FitMode=="fit","Swap lost destination edits");
  Check(geometry.SequenceEqual(p.Frames.Select(f=>(f.Id,f.X,f.Y,f.W,f.H,f.BorderWidth))),"Swap moved panel geometry");
  history.Push(p);Check(history.Undo().Frames[0].Image==first,"Undo swap failed");
  Check(history.Redo().Frames[0].Image==second,"Redo swap failed");
  Check(p.SwapImages(1,2)&&p.Frames[1].Image is null&&p.Frames[2].Image==first,"Move into empty panel failed");
  Check(!p.SwapImages(1,0)&&!p.SwapImages(2,2)&&!p.SwapImages(-1,0),"Invalid swap accepted");
  p=new Project();p.SetPanelCount(1);
  var batch=Enumerable.Range(0,20).Select(i=>Image($"image-{i}")).ToArray();
  PanelPlacement.Place(p,batch);
  Check(p.Frames.Count==20&&p.Frames.Select(f=>f.Image).SequenceEqual(batch),"Batch dropped images");
  PanelPlacement.Place(p,new[]{first});
  Check(p.Frames.Count==21&&p.Frames[0].Image==batch[0]&&p.Frames[20].Image==first,"Append overwrote an image");
  PanelPlacement.Place(p,new[]{second,first},3);
  Check(p.Frames.Count==22&&p.Frames[3].Image==second&&p.Frames[21].Image==first,"Targeted import did not grow correctly");
  p.SetPanelCount(64);foreach(var frame in p.Frames)frame.Image=first;
  bool full=false;try{PanelPlacement.Place(p,new[]{second});}catch(InvalidOperationException){full=true;}
  Check(full&&p.Frames.Count==64&&p.Frames.All(f=>f.Image==first),"Over-limit import changed project");
  PanelPlacement.Place(p,new[]{second},63);Check(p.Frames[63].Image==second,"Replacement at limit failed");
  history.Reset(p);p.SetPanelCount(1);history.Push(p);
  Check(history.Undo().Frames.Count==64&&history.Redo().Frames.Count==1,"Undo/redo count failed");
  p=new Project();p.ApplyTemplate("Two Panel");
  p.Frames[0].Image=first;p.Frames[0].CropX=.4;p.Frames[0].Zoom=1.6;
  var originals=p.Frames.Select(f=>f.Clone()).ToList();history.Reset(p);
  FrameResize.ApplyConnected(p.Frames,originals,0,ResizeEdges.Right,.1,0);
  Check(Math.Abs(p.Frames[0].W-.6)<1e-10&&Math.Abs(p.Frames[1].X-.6)<1e-10&&Math.Abs(p.Frames[1].W-.4)<1e-10,"Shared divider did not resize both panels");
  Check(p.Frames[0].Image==first&&p.Frames[0].CropX==.4&&p.Frames[0].Zoom==1.6,"Resize changed image edits");
  history.Push(p);Check(history.Undo().Frames[0].W==.5&&Math.Abs(history.Redo().Frames[1].X-.6)<1e-10,"Connected resize undo/redo failed");
  FrameResize.ApplyConnected(p.Frames,originals,0,ResizeEdges.Left,-.2,0);
  Check(p.Frames[0].X==0&&p.Frames[0].W==.5,"Outer canvas edge moved");
  foreach(string name in Templates.Names.Concat(Templates.FlexibleNames))
  foreach(int count in Templates.IsFlexible(name)?new[]{1,2,3,5,7,13,37,64}:new[]{3})
  {
   var frames=Templates.IsFlexible(name)?Templates.CreateFlexible(name,count):Templates.Create(name);
   var snapshot=frames.Select(f=>f.Clone()).ToList();
   foreach(var edges in new[]{ResizeEdges.Left,ResizeEdges.Right,ResizeEdges.Top,ResizeEdges.Bottom,ResizeEdges.Right|ResizeEdges.Bottom})
   foreach(double delta in new[]{-.9,-.11,.13,.9,0})
   {
    FrameResize.ApplyConnected(frames,snapshot,frames.Count/2,edges,delta,delta);
    CheckCoverage(frames,$"{name} {count} {edges} {delta}");
    for(int index=0;index<frames.Count;index++)
    {
     var before=snapshot[index];var after=frames[index];
     if(before.Shape!="poly")continue;
     Check(before.Points.Count==after.Points.Count,"Resize added bends to an angled side");
     for(int side=0;side<before.Points.Count;side++)
     {
      var a=before.Points[side];var b=before.Points[(side+1)%before.Points.Count];
      var c=after.Points[side];var d=after.Points[(side+1)%after.Points.Count];
      Check(Math.Abs((b.X-a.X)*(d.Y-c.Y)-(b.Y-a.Y)*(d.X-c.X))<1e-8,$"Divider angle changed: {name}");
     }
    }
   }
  }
  double slant=Math.Tan(40*Math.PI/180)*675/1200;
  var forty=new List<Frame>
  {
   new(){Shape="poly",X=0,Y=0,W=.25+slant,H=1,Points=[new(0,0),new(.25,0),new(.25+slant,1),new(0,1)]},
   new(){Shape="poly",X=.25,Y=0,W=.75,H=1,Points=[new(.25,0),new(1,0),new(1,1),new(.25+slant,1)]}
  };
  var fortySnapshot=forty.Select(f=>f.Clone()).ToList();
  FrameResize.ApplyConnected(forty,fortySnapshot,0,ResizeEdges.Right,.08,0);
  var top=forty[0].Points[1];var bottom=forty[0].Points[2];
  double angle=Math.Atan2((bottom.X-top.X)*1200,(bottom.Y-top.Y)*675)*180/Math.PI;
  Check(Math.Abs(angle-40)<1e-8&&Math.Abs(top.X-.33)<1e-8&&Math.Abs(bottom.X-(.33+slant))<1e-8,"40-degree side did not translate parallel");
  CheckCoverage(forty,"40-degree divider");
  p=new Project();p.SetPanelCount(4);originals=p.Frames.Select(f=>f.Clone()).ToList();
  FrameResize.ApplyConnected(p.Frames,originals,0,ResizeEdges.Right|ResizeEdges.Bottom,.13,.17);
  Check(Math.Abs(p.Frames[1].X-.63)<1e-10&&Math.Abs(p.Frames[2].Y-.67)<1e-10,"Grid corner did not move neighbours");
  CheckCoverage(p.Frames,"Grid corner");p.Template="Custom";
  p.Frames[0].Image=first;p.Frames[0].CropX=.4;
  string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),$"cgm-resize-{Guid.NewGuid():N}.cgmcollage");
  try
  {
   ProjectIO.Save(path,p);var loaded=ProjectIO.Load(path);
   Check(loaded.Template=="Custom"&&loaded.Frames.Count==4&&loaded.Frames[0].W==p.Frames[0].W&&loaded.Frames[0].H==p.Frames[0].H,"Saved resize was not restored");
   Check(loaded.Frames[0].Image==first&&loaded.Frames[0].CropX==.4,"Saved resize lost image edits");
   p.SetPanelCount(37);ProjectIO.Save(path,p);loaded=ProjectIO.Load(path);
   Check(loaded.Frames.Count==37&&loaded.Template=="Auto Grid","Flexible layout save/load failed");
  }
  finally{if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
 }
}
