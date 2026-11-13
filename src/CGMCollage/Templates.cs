namespace CGMCollage;
public static class Templates
{
 public const int MaxPanels=64;
 public static readonly string[] FlexibleNames=["Auto Grid","Columns","Rows"];
 public static bool IsFlexible(string name)=>FlexibleNames.Contains(name);
 public static List<Frame> CreateFlexible(string name,int count,double aspect=16d/9)
 {
  if(count<1||count>MaxPanels)throw new ArgumentOutOfRangeException(nameof(count),$"Choose between 1 and {MaxPanels} images.");
  int rows=name=="Columns"?1:name=="Rows"?count:Math.Clamp((int)Math.Round(Math.Sqrt(count/Math.Max(.1,aspect))),1,count);
  var frames=new List<Frame>();int remaining=count;
  for(int row=0;row<rows;row++)
  {
   int columns=(int)Math.Ceiling(remaining/(double)(rows-row));remaining-=columns;
   for(int column=0;column<columns;column++)frames.Add(new Frame{Name=$"Panel {frames.Count+1}",X=column/(double)columns,Y=row/(double)rows,W=1d/columns,H=1d/rows});
  }
  return frames;
 }
 public static readonly string[] Names=["Angled Triptych","Centre Hero","Cinematic Wedge","Equal Triptych","Two Panel","Multi Strip","Four Columns","2 x 2 Grid","Top / Bottom","3 x 2 Grid","Left Hero + Stack","Right Hero + Stack","Five Panel Hero","Top Hero + Three","Bottom Hero + Three","Wide Left + Three","Editorial Mosaic","Freeform Mosaic"];
 public static List<Frame> Create(string name){List<Frame> f=[];void A(double x,double y,double w,double h)=>f.Add(new(){Name=$"Panel {f.Count+1}",X=x,Y=y,W=w,H=h});void P(params PointD[] p)=>f.Add(new(){Name=$"Panel {f.Count+1}",Shape="poly",Points=[..p],X=p.Min(q=>q.X),Y=p.Min(q=>q.Y),W=p.Max(q=>q.X)-p.Min(q=>q.X),H=p.Max(q=>q.Y)-p.Min(q=>q.Y)});const double a=.06495;
 switch(name){case"Angled Triptych":P(new(0,0),new(.30-a,0),new(.30+a,1),new(0,1));P(new(.30-a,0),new(.70-a,0),new(.70+a,1),new(.30+a,1));P(new(.70-a,0),new(1,0),new(1,1),new(.70+a,1));break;case"Cinematic Wedge":P(new(0,0),new(.31-a,0),new(.31+a,1),new(0,1));P(new(.31-a,0),new(.69+a,0),new(.69-a,1),new(.31+a,1));P(new(.69+a,0),new(1,0),new(1,1),new(.69-a,1));break;case"Centre Hero":A(0,0,.27,1);A(.27,0,.46,1);A(.73,0,.27,1);break;case"Equal Triptych":for(int i=0;i<3;i++)A(i/3d,0,1/3d,1);break;case"Two Panel":A(0,0,.5,1);A(.5,0,.5,1);break;case"Multi Strip":for(int i=0;i<5;i++)A(i/5d,0,.2,1);break;case"Four Columns":for(int i=0;i<4;i++)A(i/4d,0,.25,1);break;case"2 x 2 Grid":for(int y=0;y<2;y++)for(int x=0;x<2;x++)A(x*.5,y*.5,.5,.5);break;case"Top / Bottom":A(0,0,1,.5);A(0,.5,1,.5);break;case"3 x 2 Grid":for(int y=0;y<2;y++)for(int x=0;x<3;x++)A(x/3d,y*.5,1/3d,.5);break;case"Left Hero + Stack":A(0,0,.62,1);A(.62,0,.38,.5);A(.62,.5,.38,.5);break;case"Right Hero + Stack":A(0,0,.38,.5);A(0,.5,.38,.5);A(.38,0,.62,1);break;case"Five Panel Hero":A(0,0,.22,.5);A(0,.5,.22,.5);A(.22,0,.56,1);A(.78,0,.22,.5);A(.78,.5,.22,.5);break;case"Top Hero + Three":A(0,0,1,.68);for(int x=0;x<3;x++)A(x/3d,.68,1/3d,.32);break;case"Bottom Hero + Three":for(int x=0;x<3;x++)A(x/3d,0,1/3d,.32);A(0,.32,1,.68);break;case"Wide Left + Three":A(0,0,.58,1);for(int y=0;y<3;y++)A(.58,y/3d,.42,1/3d);break;case"Editorial Mosaic":A(0,0,.36,.62);A(0,.62,.36,.38);A(.36,0,.40,1);A(.76,0,.24,.48);A(.76,.48,.24,.52);break;case"Freeform Mosaic":A(0,0,.34,1);A(.34,0,.33,1);A(.67,0,.33,1);break;default:A(0,0,1,1);break;}return f;}
}
