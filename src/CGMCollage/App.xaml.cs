using System.IO;
using System.Windows;
namespace CGMCollage;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e) { base.OnStartup(e); if(e.Args.Contains("--smoke-test")){Shutdown(SmokeTests.Run(e.Args));return;} var w=new MainWindow(); MainWindow=w; w.Show(); if(e.Args.FirstOrDefault() is { } p && File.Exists(p)) w.OpenPath(p); else w.CheckRecovery(); }
}
