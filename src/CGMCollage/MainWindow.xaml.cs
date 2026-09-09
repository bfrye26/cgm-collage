using Microsoft.Win32;
using SkiaSharp.Views.Desktop;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace CGMCollage;

public partial class MainWindow : Window
{
 Project project=new();readonly History history=new();int selected,dragTarget=-1;bool syncing=true,dirty,dragging;Point dragStart;double dragCropX,dragCropY;string? currentPath;readonly DispatcherTimer autosave=new(){Interval=TimeSpan.FromSeconds(25)};
 Frame? Selected=>selected>=0&&selected<project.Frames.Count?project.Frames[selected]:null;
 public MainWindow()
 {
  InitializeComponent();BuildTemplates();history.Reset(project);SyncUi();KeyDown+=OnKeyDown;Closing+=OnClosing;autosave.Tick+=(_,_)=>Autosave();autosave.Start();
 }
 void BuildTemplates()
 {
  const double width=84,height=36;
  Color[] palette=[Color.FromRgb(207,214,224),Color.FromRgb(145,155,170),Color.FromRgb(86,96,111)];
  foreach(var name in Templates.Names)
  {
   var preview=new System.Windows.Controls.Canvas{Width=width,Height=height,Background=new SolidColorBrush(Color.FromRgb(14,17,21)),ClipToBounds=true};
   int panelIndex=0;
   foreach(var frame in Templates.Create(name))
   {
    var fill=new SolidColorBrush(palette[panelIndex++%palette.Length]);
    var stroke=new SolidColorBrush(Color.FromRgb(31,36,44));
    if(frame.Shape=="poly"&&frame.Points.Count>2)
    {
     var points=new PointCollection(frame.Points.Select(point=>new Point(point.X*width,point.Y*height)));
     preview.Children.Add(new System.Windows.Shapes.Polygon{Points=points,Fill=fill,Stroke=stroke,StrokeThickness=1});
    }
    else if(frame.Shape=="ellipse")
    {
     var ellipse=new System.Windows.Shapes.Ellipse{Fill=fill,Stroke=stroke,StrokeThickness=1,Width=Math.Max(3,frame.W*width),Height=Math.Max(3,frame.H*height)};
     System.Windows.Controls.Canvas.SetLeft(ellipse,frame.X*width);System.Windows.Controls.Canvas.SetTop(ellipse,frame.Y*height);preview.Children.Add(ellipse);
    }
    else
    {
     var rectangle=new Border{Background=fill,BorderBrush=stroke,BorderThickness=new Thickness(1),Width=Math.Max(3,frame.W*width),Height=Math.Max(3,frame.H*height)};
     System.Windows.Controls.Canvas.SetLeft(rectangle,frame.X*width);System.Windows.Controls.Canvas.SetTop(rectangle,frame.Y*height);preview.Children.Add(rectangle);
    }
   }
   var stack=new StackPanel{Width=width};stack.Children.Add(preview);stack.Children.Add(new TextBlock{Text=name,FontSize=10,Margin=new Thickness(0,4,0,0),TextWrapping=TextWrapping.Wrap,Height=23,LineHeight=11,TextTrimming=TextTrimming.CharacterEllipsis});
   var button=new Button{Tag=name,Content=stack,Padding=new Thickness(5),Margin=new Thickness(0,0,7,6),HorizontalContentAlignment=HorizontalAlignment.Center,VerticalContentAlignment=VerticalAlignment.Stretch,ToolTip=$"Apply {name} layout"};
   button.Click+=Template_Click;TemplateList.Items.Add(button);
  }
 }
 void Changed(string message,bool push=true){dirty=true;if(push)history.Push(project);StatusText.Text=message;SyncUi();Canvas.InvalidateVisual();}
 void SyncUi()
 {
  syncing=true;var f=Selected;PanelNumber.Text=(selected+1).ToString();PanelBadge.Visibility=f is null?Visibility.Collapsed:Visibility.Visible;ImageName.Text=f?.Image?.Name??"Drop or choose an image";FitButton.Content=f?.FitMode=="fit"?"Fit image":"Fill frame";
  if(f is not null){ZoomSlider.Value=f.Zoom;CropXSlider.Value=f.CropX;CropYSlider.Value=f.CropY;RotationSlider.Value=f.Rotation;BrightnessSlider.Value=f.Brightness;ContrastSlider.Value=f.Contrast;SaturationSlider.Value=f.Saturation;OpacitySlider.Value=f.Opacity;BorderSlider.Value=f.BorderWidth;ShadowToggle.IsChecked=f.Shadow;FlipXButton.Background=f.FlipX?(Brush)FindResource("AccentBrush"):(Brush)FindResource("RaisedBrush");FlipYButton.Background=f.FlipY?(Brush)FindResource("AccentBrush"):(Brush)FindResource("RaisedBrush");}
  GapSlider.Value=project.Gap;GapValue.Text=$"{project.Gap:0} px";ProjectStatus.Text=project.Name+(dirty?" · Unsaved":"");UndoButton.IsEnabled=history.CanUndo;RedoButton.IsEnabled=history.CanRedo;foreach(Button b in TemplateList.Items){bool active=Equals(b.Tag,project.Template);b.BorderBrush=active?(Brush)FindResource("AccentBrush"):(Brush)FindResource("StrokeBrush");b.Background=active?new SolidColorBrush(Color.FromRgb(67,20,39)):(Brush)FindResource("RaisedBrush");}syncing=false;
 }
 void Template_Click(object sender,RoutedEventArgs e){if(sender is Button{Tag:string n}){project.ApplyTemplate(n);selected=Math.Min(selected,project.Frames.Count-1);Changed($"Applied {n}");}}
 void New_Click(object s,RoutedEventArgs e){if(!CanDiscard())return;project=new();selected=0;currentPath=null;dirty=false;history.Reset(project);StatusText.Text="New project";SyncUi();Canvas.InvalidateVisual();}
 void Open_Click(object s,RoutedEventArgs e){if(!CanDiscard())return;var d=new OpenFileDialog{Filter="CGM Collage project (*.cgmcollage)|*.cgmcollage",Title="Open collage project"};if(d.ShowDialog()==true)OpenPath(d.FileName);}
 public void OpenPath(string path){try{project=ProjectIO.Load(path);currentPath=path;project.Name=Path.GetFileNameWithoutExtension(path);selected=0;dirty=false;history.Reset(project);StatusText.Text=$"Opened {project.Name}";SyncUi();Canvas.InvalidateVisual();}catch(Exception ex){MessageBox.Show(this,$"This project could not be opened.\n\n{ex.Message}","Open project",MessageBoxButton.OK,MessageBoxImage.Error);}}
 void Save_Click(object s,RoutedEventArgs e)=>SaveProject(false);
 bool SaveProject(bool copy)
 {
  var path=copy?null:currentPath;if(path is null){var d=new SaveFileDialog{Filter="CGM Collage project (*.cgmcollage)|*.cgmcollage",DefaultExt=".cgmcollage",FileName=project.Name=="Untitled project"?"My collage":project.Name};if(d.ShowDialog()!=true)return false;path=d.FileName;}
  try{project.Name=Path.GetFileNameWithoutExtension(path);ProjectIO.Save(path,project);currentPath=path;dirty=false;StatusText.Text="Project saved";SyncUi();return true;}catch(Exception ex){MessageBox.Show(this,$"The project could not be saved.\n\n{ex.Message}","Save project",MessageBoxButton.OK,MessageBoxImage.Error);return false;}
 }
 bool CanDiscard(){if(!dirty)return true;var r=MessageBox.Show(this,"Save changes to this collage?","CGM Collage",MessageBoxButton.YesNoCancel,MessageBoxImage.Question);return r==MessageBoxResult.No||(r==MessageBoxResult.Yes&&SaveProject(false));}
 void Undo_Click(object s,RoutedEventArgs e){if(history.CanUndo){project=history.Undo();selected=Math.Min(selected,project.Frames.Count-1);dirty=true;StatusText.Text="Undid last change";SyncUi();Canvas.InvalidateVisual();}}
 void Redo_Click(object s,RoutedEventArgs e){if(history.CanRedo){project=history.Redo();selected=Math.Min(selected,project.Frames.Count-1);dirty=true;StatusText.Text="Redid change";SyncUi();Canvas.InvalidateVisual();}}
 void AddImages_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif;*.tif;*.tiff",Multiselect=true,Title="Add images"};if(d.ShowDialog()==true)AddImages(d.FileNames);}
 void Replace_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif;*.tif;*.tiff",Title="Choose an image"};if(d.ShowDialog()==true&&Selected is { } f){try{f.Image=ProjectIO.ReadImage(d.FileName);Changed("Image replaced");}catch(Exception ex){MessageBox.Show(this,ex.Message,"Image error",MessageBoxButton.OK,MessageBoxImage.Error);}}}
 void AddImages(IEnumerable<string> files,int? preferredPanel=null){var paths=files.Where(IsImage).ToList();if(paths.Count==0)return;int first=preferredPanel??project.Frames.FindIndex(f=>f.Image is null);if(first<0)first=Math.Clamp(selected,0,project.Frames.Count-1);var panels=PanelPlacement.Plan(project,first,paths.Count);int added=0,last=first;try{for(int i=0;i<panels.Count;i++){project.Frames[panels[i]].Image=ProjectIO.ReadImage(paths[i]);last=panels[i];added++;}selected=last;Changed(added==1?$"Placed image in panel {last+1}":$"Placed {added} images, starting at panel {first+1}");}catch(Exception ex){MessageBox.Show(this,$"An image could not be added.\n\n{ex.Message}","Add images",MessageBoxButton.OK,MessageBoxImage.Error);}}
 void Remove_Click(object s,RoutedEventArgs e){if(Selected is { } f&&f.Image is not null){f.Image=null;Changed("Image removed");}}
 void Fit_Click(object s,RoutedEventArgs e){if(Selected is { } f){f.FitMode=f.FitMode=="fit"?"fill":"fit";Changed(f.FitMode=="fit"?"Image fitted inside panel":"Image fills panel");}}
 void Centre_Click(object s,RoutedEventArgs e){if(Selected is { } f){f.CropX=f.CropY=0;Changed("Image centred");}}
 void ResetCrop_Click(object s,RoutedEventArgs e){if(Selected is { } f){f.ResetCrop();Changed("Crop reset");}}
 void FlipX_Click(object s,RoutedEventArgs e){if(Selected is { } f){f.FlipX=!f.FlipX;Changed("Horizontal flip updated");}}
 void FlipY_Click(object s,RoutedEventArgs e){if(Selected is { } f){f.FlipY=!f.FlipY;Changed("Vertical flip updated");}}
 void Duplicate_Click(object s,RoutedEventArgs e){if(Selected is { } f){var n=f.Clone();n.Id=Guid.NewGuid().ToString("N");n.Name=$"Panel {project.Frames.Count+1}";n.Move(.02,.02);project.Frames.Add(n);selected=project.Frames.Count-1;Changed("Panel duplicated");}}
 void PropertySlider_Changed(object s,RoutedPropertyChangedEventArgs<double> e){if(syncing||Selected is not { } f)return;f.Zoom=ZoomSlider.Value;f.CropX=CropXSlider.Value;f.CropY=CropYSlider.Value;f.Rotation=RotationSlider.Value;f.Brightness=BrightnessSlider.Value;f.Contrast=ContrastSlider.Value;f.Saturation=SaturationSlider.Value;f.Opacity=OpacitySlider.Value;f.BorderWidth=BorderSlider.Value;dirty=true;history.Push(project);UndoButton.IsEnabled=true;ProjectStatus.Text=project.Name+" · Unsaved";Canvas.InvalidateVisual();}
 void Gap_Changed(object s,RoutedPropertyChangedEventArgs<double> e){if(syncing)return;project.Gap=GapSlider.Value;GapValue.Text=$"{project.Gap:0} px";dirty=true;history.Push(project);UndoButton.IsEnabled=true;Canvas.InvalidateVisual();}
 void FrameOption_Changed(object s,RoutedEventArgs e){if(syncing||Selected is not { } f)return;f.Shadow=ShadowToggle.IsChecked==true;Changed("Frame style updated");}
 void CanvasOption_Changed(object s,RoutedEventArgs e)=>Canvas.InvalidateVisual();
 void CanvasPreset_Changed(object s,SelectionChangedEventArgs e){if(syncing||CanvasPreset.SelectedItem is not ComboBoxItem{Tag:string tag})return;var a=tag.Split(',');if(a.Length==2){project.CanvasWidth=int.Parse(a[0]);project.CanvasHeight=int.Parse(a[1]);Changed($"Canvas set to {project.CanvasWidth} × {project.CanvasHeight}");}}
 void Canvas_PaintSurface(object? s,SKPaintSurfaceEventArgs e)=>Renderer.Draw(project,e.Surface.Canvas,e.Info.Width,e.Info.Height,dragTarget>=0?dragTarget:selected,GuidesToggle.IsChecked==true);
 void Canvas_MouseDown(object s,MouseButtonEventArgs e){Canvas.Focus();var p=e.GetPosition(Canvas);int hit=Hit(p);if(hit>=0)selected=hit;SyncUi();Canvas.InvalidateVisual();if(hit>=0&&e.LeftButton==MouseButtonState.Pressed&&Selected?.Image is not null){dragging=true;dragStart=p;dragCropX=Selected.CropX;dragCropY=Selected.CropY;Canvas.CaptureMouse();}}
 void Canvas_MouseMove(object s,MouseEventArgs e){if(!dragging||Selected is not { } f)return;var p=e.GetPosition(Canvas);f.CropX=Math.Clamp(dragCropX+(p.X-dragStart.X)/Math.Max(1,Canvas.ActualWidth)*2,-1,1);f.CropY=Math.Clamp(dragCropY+(p.Y-dragStart.Y)/Math.Max(1,Canvas.ActualHeight)*2,-1,1);dirty=true;Canvas.InvalidateVisual();}
 void Canvas_MouseUp(object s,MouseButtonEventArgs e){if(dragging){dragging=false;Canvas.ReleaseMouseCapture();history.Push(project);StatusText.Text="Image repositioned";SyncUi();}}
 void Canvas_MouseWheel(object s,MouseWheelEventArgs e){if(Selected is { } f){f.Zoom=Math.Clamp(f.Zoom+(e.Delta>0?.08:-.08),1,4);Changed("Image zoom updated");e.Handled=true;}}
 int Hit(Point p){double nx=p.X/Math.Max(1,Canvas.ActualWidth),ny=p.Y/Math.Max(1,Canvas.ActualHeight);for(int i=project.Frames.Count-1;i>=0;i--){var f=project.Frames[i];if(nx<f.X||ny<f.Y||nx>f.X+f.W||ny>f.Y+f.H)continue;if(f.Shape!="poly"||Inside(nx,ny,f.Points))return i;}return -1;}
 static bool Inside(double x,double y,List<PointD> pts){bool inside=false;for(int i=0,j=pts.Count-1;i<pts.Count;j=i++)if((pts[i].Y>y)!=(pts[j].Y>y)&&x<(pts[j].X-pts[i].X)*(y-pts[i].Y)/(pts[j].Y-pts[i].Y)+pts[i].X)inside=!inside;return inside;}
 void Canvas_DragEnter(object s,DragEventArgs e){e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;Canvas_DragOver(s,e);e.Handled=true;}
 void Canvas_DragOver(object s,DragEventArgs e){if(!e.Data.GetDataPresent(DataFormats.FileDrop)){e.Effects=DragDropEffects.None;return;}dragTarget=Hit(e.GetPosition(Canvas));StatusText.Text=dragTarget>=0?$"Drop into panel {dragTarget+1}":"Move over a panel to place the image";Canvas.InvalidateVisual();e.Effects=dragTarget>=0?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;}
 void Canvas_DragLeave(object s,DragEventArgs e){dragTarget=-1;StatusText.Text="Ready · Drop images to begin";Canvas.InvalidateVisual();}
 void Canvas_Drop(object s,DragEventArgs e){int target=dragTarget>=0?dragTarget:Hit(e.GetPosition(Canvas));dragTarget=-1;if(target>=0&&e.Data.GetData(DataFormats.FileDrop) is string[] f)AddImages(f,target);else StatusText.Text="Move over a panel to place the image";Canvas.InvalidateVisual();e.Handled=true;}
 static bool IsImage(string p)=>new[]{".jpg",".jpeg",".png",".webp",".bmp",".gif",".tif",".tiff"}.Contains(Path.GetExtension(p).ToLowerInvariant());
 void Export_Click(object s,RoutedEventArgs e){var d=new SaveFileDialog{Filter="JPEG image (*.jpg)|*.jpg|PNG image (*.png)|*.png|WebP image (*.webp)|*.webp",DefaultExt=".jpg",FileName=project.Name};if(d.ShowDialog()==true)Export(d.FileName);}
 void QuickExport_Click(object s,RoutedEventArgs e){var dir=currentPath is null?Environment.GetFolderPath(Environment.SpecialFolder.MyPictures):Path.GetDirectoryName(currentPath)!;Export(Path.Combine(dir,$"{project.Name}.jpg"));}
 void Export(string path){try{StatusText.Text="Exporting…";Renderer.Export(project,path);StatusText.Text=$"Exported {Path.GetFileName(path)}";}catch(Exception ex){MessageBox.Show(this,$"The collage could not be exported.\n\n{ex.Message}","Export",MessageBoxButton.OK,MessageBoxImage.Error);}}
 void OnKeyDown(object s,KeyEventArgs e){if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)&&e.Key==Key.S){SaveProject(false);e.Handled=true;}else if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)&&e.Key==Key.Z){Undo_Click(s,e);e.Handled=true;}else if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)&&e.Key==Key.Y){Redo_Click(s,e);e.Handled=true;}else if((e.Key==Key.Delete||e.Key==Key.Back)&&Selected is { } f&&f.Image is not null){f.Image=null;Changed("Image removed");e.Handled=true;}}
 void Autosave(){if(!dirty)return;try{Directory.CreateDirectory(Path.GetDirectoryName(ProjectIO.AutosavePath)!);ProjectIO.Save(ProjectIO.AutosavePath,project);}catch{}}
 public void CheckRecovery(){if(!File.Exists(ProjectIO.AutosavePath))return;if(MessageBox.Show(this,"CGM Collage found recovered work from an earlier session. Open it?","Recover project",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)OpenPath(ProjectIO.AutosavePath);else try{File.Delete(ProjectIO.AutosavePath);}catch{}}
 void OnClosing(object? s,CancelEventArgs e){if(!CanDiscard())e.Cancel=true;else{autosave.Stop();if(!dirty)try{File.Delete(ProjectIO.AutosavePath);}catch{}}}
}
