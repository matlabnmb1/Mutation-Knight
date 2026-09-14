using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace GhostBlade3D
{
    static class Entry
    {
        [STAThread] static void Main(string[] args)
        {
            try
            {
                if(args.Length>0&&args[0]=="--test"){Checks.Run();return;}
                var app=new Application();var window=new GameWindow(args.Length>0&&args[0]=="--verify",args.Length>0&&args[0]=="--benchmark");app.Run(window);
            }
            catch(Exception e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"3d-error.log"),e.ToString());Environment.ExitCode=1;}
        }
    }
    sealed class Spark
    {
        public Model3DGroup Mesh;public double X,Y,Z,VX,VY,VZ,Life,MaxLife;
        public bool Burst;
    }
    sealed class GameWindow:Window
    {
        readonly Combat game=new Combat();readonly Grid surface=new Grid();
        readonly Viewport3D viewport=new Viewport3D(),armsView=new Viewport3D();
        readonly ModelVisual3D worldVisual=new ModelVisual3D();
        readonly Model3DGroup world=new Model3DGroup(),hands=new Model3DGroup(),weapon,offhand,rightForearm,leftForearm;
        readonly PerspectiveCamera camera=new PerspectiveCamera();
        readonly Dictionary<Target,TerminatorRig> rigs=new Dictionary<Target,TerminatorRig>();
        readonly List<Spark> sparks=new List<Spark>();readonly HashSet<Key> keys=new HashSet<Key>();
        readonly Stopwatch watch=Stopwatch.StartNew();readonly Audio audio;readonly Hud hud;
        readonly Random random=new Random(41);double last,sensitivity=.00105,fps=60,cameraY=double.NaN;bool captured,showHelp=true,verify;
        int verifyStage;double recoil;
        int targetFps=120;double nextFrame;
        bool benchmark;int benchmarkFrames;double benchmarkStart,benchmarkCpu;
        Border settings;
        Slider sensitivitySlider;
        sealed class Scar {public Model3DGroup Mesh;public SolidColorBrush Ink;public double Life=4;}
        readonly List<Scar> scars=new List<Scar>();
        void MascotVoice(int mascot)
        {
            if(game.Muted)return;
            audio.StopAnnouncement();
            audio.Play(mascot==0?"mascot-cow":mascot==1?"mascot-chubby":"mascot-laugh");
        }
        void WallScar(Point3D point,Vector3D normal)
        {
            Vector3D right=Vector3D.CrossProduct(normal,new Vector3D(0,1,0));if(right.Length<.1)right=new Vector3D(1,0,0);right.Normalize();Vector3D up=Vector3D.CrossProduct(right,normal);up.Normalize();
            Vector3D diagonal=right*.32+up*.26;
            var ink=new SolidColorBrush(Color.FromRgb(195,175,144));var material=new DiffuseMaterial(ink);var mesh=Models.Group(world);
            for(int i=0;i<3;i++){Vector3D offset=right*(i-1)*.035;Models.Tube(mesh,material,point+offset-diagonal,point+offset+diagonal,.007,.002,4);}
            scars.Add(new Scar{Mesh=mesh,Ink=ink});if(scars.Count>24){world.Children.Remove(scars[0].Mesh);scars.RemoveAt(0);}
        }
        bool settingsOpen,settingsWasPaused;
        void ToggleSettings()
        {
            if(settingsOpen){surface.Children.Remove(settings);sensitivitySlider=null;settingsOpen=false;game.Paused=settingsWasPaused;if(!game.Paused)Capture();return;}
            settingsWasPaused=game.Paused;game.Paused=true;Release();settingsOpen=true;
            var stack=new StackPanel{Margin=new Thickness(34,28,34,30)};
            stack.Children.Add(new TextBlock{Text="游 戏 设 置",Foreground=new SolidColorBrush(Color.FromRgb(23,145,162)),FontSize=11,FontWeight=FontWeights.SemiBold});
            stack.Children.Add(new TextBlock{Text="操作与画面",Foreground=new SolidColorBrush(Color.FromRgb(246,249,247)),FontSize=27,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,5,0,2)});
            stack.Children.Add(new TextBlock{Text="调整视角手感与渲染帧率",Foreground=new SolidColorBrush(Color.FromRgb(184,205,211)),FontSize=13,Margin=new Thickness(0,0,0,25)});
            var sensitivityHeader=new Grid();
            sensitivityHeader.ColumnDefinitions.Add(new ColumnDefinition());sensitivityHeader.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
            sensitivityHeader.Children.Add(new TextBlock{Text="鼠标灵敏度",Foreground=new SolidColorBrush(Color.FromRgb(246,249,247)),FontSize=14,FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center});
            var value=new TextBlock{Foreground=new SolidColorBrush(Color.FromRgb(223,91,57)),Background=new SolidColorBrush(Color.FromRgb(220,242,240)),Padding=new Thickness(9,4,9,4),FontSize=14,FontFamily=new FontFamily("Microsoft YaHei UI")};Grid.SetColumn(value,1);sensitivityHeader.Children.Add(value);stack.Children.Add(sensitivityHeader);
            var slider=new Slider{Minimum=.00025,Maximum=.004,Value=sensitivity,Width=448,Height=28,Margin=new Thickness(0,10,0,0),IsMoveToPointEnabled=true,SmallChange=.00005,LargeChange=.0002};
            sensitivitySlider=slider;
            value.Text=sensitivity.ToString("0.00000");slider.ValueChanged+=(s,e)=>{sensitivity=slider.Value;value.Text=sensitivity.ToString("0.00000");};stack.Children.Add(slider);
            var micro=new TextBlock{Text="拖动滑块调整  ·  [ ] 或 - + 可微调",Foreground=new SolidColorBrush(Color.FromRgb(184,205,211)),FontSize=11,Margin=new Thickness(0,2,0,17)};stack.Children.Add(micro);
            var reset=new Button{Content="恢复默认灵敏度",Height=36,Foreground=new SolidColorBrush(Color.FromRgb(28,57,66)),Background=new SolidColorBrush(Color.FromRgb(231,247,245)),BorderBrush=new SolidColorBrush(Color.FromRgb(70,163,172)),BorderThickness=new Thickness(1),Margin=new Thickness(0,0,0,24),Padding=new Thickness(10,0,10,0)};reset.Click+=(s,e)=>slider.Value=.00105;stack.Children.Add(reset);
            stack.Children.Add(new TextBlock{Text="目标帧率",Foreground=new SolidColorBrush(Color.FromRgb(246,249,247)),FontSize=14,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,8)});
            var rate=new ComboBox{ItemsSource=new[]{"每秒 60 帧  ·  稳定模式","每秒 120 帧  ·  流畅模式"},SelectedIndex=targetFps==60?0:1,Height=38,Padding=new Thickness(10,5,10,5),Foreground=new SolidColorBrush(Color.FromRgb(48,39,29)),Background=new SolidColorBrush(Color.FromRgb(247,241,225)),BorderBrush=new SolidColorBrush(Color.FromRgb(145,119,78)),BorderThickness=new Thickness(1)};rate.SelectionChanged+=(s,e)=>{targetFps=rate.SelectedIndex==0?60:120;nextFrame=watch.Elapsed.TotalSeconds;};stack.Children.Add(rate);
            stack.Children.Add(new TextBlock{Text="实际帧率受显示器刷新率与硬件性能影响",Foreground=new SolidColorBrush(Color.FromRgb(184,205,211)),FontSize=11,Margin=new Thickness(0,7,0,24)});
            var close=new Button{Content="保存并返回游戏",Height=40,Foreground=Brushes.White,Background=new SolidColorBrush(Color.FromRgb(151,57,39)),BorderBrush=new SolidColorBrush(Color.FromRgb(105,42,31)),BorderThickness=new Thickness(1),FontWeight=FontWeights.SemiBold};close.Click+=(s,e)=>ToggleSettings();stack.Children.Add(close);
            stack.Children.Add(new TextBlock{Text="F2 / 退出键  关闭设置",Foreground=new SolidColorBrush(Color.FromRgb(184,205,211)),FontSize=11,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,13,0,0)});
            settings=new Border{Width=520,Background=new LinearGradientBrush(Color.FromRgb(15,27,34),Color.FromRgb(26,46,54),90),BorderBrush=new SolidColorBrush(Color.FromRgb(80,190,202)),BorderThickness=new Thickness(2),CornerRadius=new CornerRadius(12),Child=stack,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            settings.Effect=new System.Windows.Media.Effects.DropShadowEffect{Color=Colors.Black,BlurRadius=28,ShadowDepth=8,Opacity=.65};surface.Children.Add(settings);
        }
        readonly Model3DGroup bladeTrail=new Model3DGroup();
        readonly List<Point3D> trailTips=new List<Point3D>(),trailBases=new List<Point3D>();
        [DllImport("user32.dll")] static extern bool SetCursorPos(int x,int y);
        public GameWindow(bool verifyMode,bool benchmarkMode=false)
        {
            benchmark=benchmarkMode;
            InputMethod.SetIsInputMethodEnabled(this,false);
            InputMethod.SetIsInputMethodEnabled(surface,false);
            InputMethod.SetPreferredImeState(this,InputMethodState.Off);
            PreviewTextInput+=(s,e)=>e.Handled=true;
            verify=verifyMode;Title="恶魔剑客";Width=1280;Height=800;MinWidth=960;MinHeight=640;WindowStartupLocation=WindowStartupLocation.CenterScreen;
            surface.Background=new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#58C8EC"),(Color)ColorConverter.ConvertFromString("#BDEFFC"),90);
            Content=surface;surface.Children.Add(viewport);surface.Children.Add(armsView);viewport.IsHitTestVisible=false;
            camera.FieldOfView=82;camera.NearPlaneDistance=.06;camera.FarPlaneDistance=150;viewport.Camera=camera;
            world.Children.Add(new AmbientLight(Color.FromRgb(170,181,190)));
            world.Children.Add(new DirectionalLight(Color.FromRgb(255,247,220),new Vector3D(-.5,-1,.3)));
            world.Children.Add(new DirectionalLight(Color.FromRgb(126,185,211),new Vector3D(.7,-.3,-1)));
            worldVisual.Content=world;viewport.Children.Add(worldVisual);
            game.Map=new Village();world.Children.Add(game.Map.Build());
            hands.Children.Add(new AmbientLight(Color.FromRgb(130,137,146)));
            hands.Children.Add(new DirectionalLight(Color.FromRgb(244,230,212),new Vector3D(-.3,-.6,-1)));
            hands.Children.Add(new DirectionalLight(Color.FromRgb(120,139,155),new Vector3D(.5,0,1)));
            weapon=Models.Blade();hands.Children.Add(weapon);
            hands.Children.Add(bladeTrail);
            var right=Models.Hand(false);weapon.Children.Add(right);Models.Pose(right,0,-.09,.01,0,0,0);
            rightForearm=Models.Forearm();hands.Children.Add(rightForearm);
            offhand=Models.Hand(true);weapon.Children.Add(offhand);
            leftForearm=Models.Forearm();hands.Children.Add(leftForearm);
            armsView.Camera=new PerspectiveCamera(new Point3D(0,0,0),new Vector3D(0,0,-1),new Vector3D(0,1,0),72){NearPlaneDistance=.03,FarPlaneDistance=8};
            armsView.Children.Add(new ModelVisual3D{Content=hands});armsView.IsHitTestVisible=false;
            hud=new Hud(game,this);surface.Children.Add(hud);hud.IsHitTestVisible=false;
            audio=new Audio();game.Sound=audio.Play;game.Impact=Impact;game.MascotImpact=MascotVoice;game.WallImpact=WallScar;game.Announce=audio.PlayStreak;
            game.Reset(true);game.Paused=true;SyncScene(0);
            PreviewKeyDown+=KeyPressed;KeyUp+=(s,e)=>keys.Remove(e.Key);MouseDown+=Click;MouseMove+=Look;
            Deactivated+=(s,e)=>{if(!verify&&!benchmark){game.Paused=true;Release();}};
            Closed+=(s,e)=>{CompositionTarget.Rendering-=Frame;Release();audio.Dispose();};
            ContentRendered+=(s,e)=>
            {
                if(verify){VerifyShot();}
                else{if(benchmark){game.Reset(false);game.Targets.Clear();for(int i=0;i<12;i++){double a=(i-5.5)*.12;game.Targets.Add(new Target{Id=i,X=Math.Sin(a)*2.1,Z=Math.Cos(a)*2.1,Dummy=true});}game.Paused=false;benchmarkStart=watch.Elapsed.TotalSeconds;}last=watch.Elapsed.TotalSeconds;nextFrame=last;CompositionTarget.Rendering+=Frame;}
            };
        }
        void BuildGround()
        {
            var floor=Models.Mat("#E2E3DF",0);var lines=Models.Mat("#B5BCBD",0);
            var mesh=new MeshGeometry3D();mesh.Positions=new Point3DCollection(new[]{new Point3D(-50,0,-50),new Point3D(50,0,-50),new Point3D(50,0,50),new Point3D(-50,0,50)});Models.Tri(mesh,0,2,1);Models.Tri(mesh,0,3,2);mesh.Freeze();world.Children.Add(Models.Geometry(mesh,floor));
            for(int i=-24;i<=24;i+=2){Models.Tube(world,lines,new Point3D(i,.006,-24),new Point3D(i,.006,24),.006,.006,4);Models.Tube(world,lines,new Point3D(-24,.006,i),new Point3D(24,.006,i),.006,.006,4);}
            var rim=Models.Mat("#718084",0);
            Models.Tube(world,rim,new Point3D(-24,.015,-24),new Point3D(-24,.015,24),.04,.04,4);Models.Tube(world,rim,new Point3D(24,.015,-24),new Point3D(24,.015,24),.04,.04,4);
            Models.Tube(world,rim,new Point3D(-24,.015,-24),new Point3D(24,.015,-24),.04,.04,4);Models.Tube(world,rim,new Point3D(-24,.015,24),new Point3D(24,.015,24),.04,.04,4);
        }
        void Capture()
        {if(captured||verify)return;captured=true;Cursor=Cursors.None;CenterMouse();}
        void Release(){captured=false;Cursor=Cursors.Arrow;keys.Clear();}
        void CenterMouse(){Point p=PointToScreen(new Point(ActualWidth/2,ActualHeight/2));SetCursorPos((int)p.X,(int)p.Y);}
        void Look(object sender,MouseEventArgs e)
        {
            if(!captured||game.Paused||!IsActive)return;
            Point center=PointToScreen(new Point(ActualWidth/2,ActualHeight/2));var p=Forms.Cursor.Position;
            double dx=p.X-center.X,dy=p.Y-center.Y;if(Math.Abs(dx)+Math.Abs(dy)<1)return;
            game.Yaw=Combat.Wrap(game.Yaw-dx*sensitivity);game.Pitch=Combat.Clamp(game.Pitch-dy*sensitivity,-1.30,1.30);CenterMouse();
        }
        double Forward(){return(keys.Contains(Key.W)?1:0)-(keys.Contains(Key.S)?1:0);}
        double Side(){return(keys.Contains(Key.D)?1:0)-(keys.Contains(Key.A)?1:0);}
        void KeyPressed(object sender,KeyEventArgs e)
        {
            Key input=e.Key==Key.ImeProcessed?e.ImeProcessedKey:e.Key;
            if(input==Key.OemOpenBrackets||input==Key.OemCloseBrackets||input==Key.Subtract||input==Key.Add||input==Key.OemMinus||input==Key.OemPlus)
            {bool increase=input==Key.OemCloseBrackets||input==Key.Add||input==Key.OemPlus;sensitivity=Combat.Clamp(sensitivity+(increase?.00005:-.00005),.00025,.004);if(sensitivitySlider!=null)sensitivitySlider.Value=sensitivity;game.Say("灵敏度 "+sensitivity.ToString("0.00000"),1.5);e.Handled=true;return;}
            if(e.Key==Key.F2&&!e.IsRepeat){ToggleSettings();e.Handled=true;return;}
            if(settingsOpen){if(e.Key==Key.Escape&&!e.IsRepeat)ToggleSettings();return;}
            if(e.Key==Key.System&&e.SystemKey==Key.F4)return;
            keys.Add(e.Key);if(e.IsRepeat)return;
            if(e.Key==Key.Escape){game.Paused=!game.Paused;if(game.Paused)Release();else Capture();}
            else if(e.Key==Key.Enter){if(game.Health<=0)Restart(game.Practice);game.Paused=false;Capture();}
            else if(e.Key==Key.F)game.Switch();
            else if(e.Key==Key.Space){game.Dash(Forward(),Side());e.Handled=true;}
            else if(e.Key==Key.F5)Restart(game.Practice);
            else if(e.Key==Key.F6)Restart(!game.Practice);
            else if(e.Key==Key.F1)showHelp=!showHelp;
            else if(e.Key==Key.M){game.Muted=!game.Muted;if(game.Muted)audio.StopAnnouncement();}
            else if(e.Key==Key.OemOpenBrackets)sensitivity=Math.Max(.00025,sensitivity-.00015);
            else if(e.Key==Key.OemCloseBrackets)sensitivity=Math.Min(.004,sensitivity+.00015);
            else if(e.Key==Key.F11){WindowStyle=WindowStyle==WindowStyle.None?WindowStyle.SingleBorderWindow:WindowStyle.None;WindowState=WindowStyle==WindowStyle.None?WindowState.Maximized:WindowState.Normal;}
        }
        void Click(object sender,MouseButtonEventArgs e)
        {
            if(settingsOpen)return;
            if(game.Paused){game.Paused=false;if(game.Health<=0)Restart(game.Practice);Capture();return;}
            if(!captured){Capture();return;}
            if(e.ChangedButton==MouseButton.Left)game.Attack(false);if(e.ChangedButton==MouseButton.Right)game.Attack(true);
        }
        void Restart(bool practice)
        {
            audio.StopAnnouncement();
            foreach(var scar in scars)world.Children.Remove(scar.Mesh);scars.Clear();cameraY=double.NaN;
            foreach(var rig in rigs.Values)world.Children.Remove(rig.Root);rigs.Clear();foreach(var p in sparks)world.Children.Remove(p.Mesh);sparks.Clear();game.Reset(practice);Capture();
        }
        void Frame(object sender,EventArgs e)
        {
            double now=watch.Elapsed.TotalSeconds;if(now+.0003<nextFrame)return;
            nextFrame+=1.0/targetFps;if(nextFrame<now-1.0/targetFps)nextFrame=now+1.0/targetFps;
            double elapsed=now-last,dt=Math.Min(.05,elapsed);last=now;
            if(dt<.001)return;fps=fps*.94+(.06/elapsed);
            game.Tick(dt,Forward(),Side(),keys.Contains(Key.LeftCtrl)||keys.Contains(Key.RightCtrl));SyncScene(game.Paused?0:dt);hud.InvalidateVisual();
            if(benchmark&&now-benchmarkStart>2){benchmarkFrames++;benchmarkCpu+=watch.Elapsed.TotalSeconds-now;if(now-benchmarkStart>8){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"benchmark.txt"),"12 close enemies; target 120; WPF update/render callbacks, not GPU present telemetry\r\nMeasured FPS: "+(benchmarkFrames/(now-benchmarkStart-2)).ToString("0.0")+"\r\nMean update CPU ms: "+(benchmarkCpu*1000/benchmarkFrames).ToString("0.00"));Close();}}
        }
        void SyncScene(double dt)
        {
            for(int i=scars.Count-1;i>=0;i--){var scar=scars[i];scar.Life-=dt;if(scar.Life<=0){world.Children.Remove(scar.Mesh);scars.RemoveAt(i);}else scar.Ink.Opacity=Math.Min(1,scar.Life);}
            Vector3D direction=new Vector3D(Math.Sin(game.Yaw)*Math.Cos(game.Pitch),Math.Sin(game.Pitch),Math.Cos(game.Yaw)*Math.Cos(game.Pitch));
            // Collision follows every stair tread, but the view follows a critically
            // damped vertical track. This removes the one-pixel step bounce without
            // changing player height, jump arcs, hit tests or rooftop access.
            double targetCameraY=game.EyeHeight+game.JumpY;
            if(double.IsNaN(cameraY)||dt<=0)cameraY=targetCameraY;
            else
            {
                double floor=game.Map==null?0:game.Map.Floor(game.X,game.Z,game.JumpY+.025,.24);
                bool airborne=Math.Abs(game.JumpY-floor)>.035||Math.Abs(game.JumpSpeed)>.05;
                double response=airborne?24:10;
                cameraY+=(targetCameraY-cameraY)*(1-Math.Exp(-response*dt));
                if(Math.Abs(targetCameraY-cameraY)<.0005)cameraY=targetCameraY;
            }
            camera.Position=new Point3D(game.X,cameraY,game.Z);camera.LookDirection=direction;camera.UpDirection=new Vector3D(0,1,0);
            camera.FieldOfView=82+(game.DashLeft>0?4:0);recoil=Math.Max(0,recoil-dt*5);
            foreach(var t in game.Targets)
            {
                TerminatorRig rig;if(!rigs.TryGetValue(t,out rig)){rig=new TerminatorRig();rigs[t]=rig;world.Children.Add(rig.Root);AddShadow(rig.Root);}
                double face=Math.Atan2(game.X-t.X,game.Z-t.Z)*180/Math.PI+180;
                double fall=t.Dead?Math.Min(1,t.DeathTime/.48):0;
                rig.Animate(t.Dummy?0:game.Clock+t.Phase,!t.Dummy&&t.AttackTimer> .88?(1.2-t.AttackTimer)/.32:0,t.Hurt*3);
                Models.Pose(rig.Root,t.X,t.Y+(t.Dead?-fall*.02:0),t.Z,fall*86,face,0);
                if(t.Dead&&t.DeathTime>2)world.Children.Remove(rig.Root);
            }
            for(int i=sparks.Count-1;i>=0;i--)
            {var p=sparks[i];p.Life-=dt;if(p.Life<=0){world.Children.Remove(p.Mesh);sparks.RemoveAt(i);continue;}p.X+=p.VX*dt;p.Y+=p.VY*dt;p.Z+=p.VZ*dt;if(!p.Burst)p.VY-=dt*5;Models.Pose(p.Mesh,p.X,p.Y,p.Z,p.Burst?90:0,0,p.Burst?0:p.Life*400);var m=p.Mesh.Transform.Value;var scale=Matrix3D.Identity;double fade=p.Burst?.3+(1-p.Life/p.MaxLife)*3:Math.Max(.05,p.Life/p.MaxLife);scale.Scale(new Vector3D(fade,fade,fade));scale.Append(m);p.Mesh.Transform=new MatrixTransform3D(scale);}
            PoseWeapon();
            UpdateBladeTrail(dt);
        }
        void UpdateBladeTrail(double dt)
        {
            bool cutting=(game.Action==Move.Light&&game.ActionAge>.11&&game.ActionAge<.32)||(game.Action==Move.Heavy&&game.ActionAge>.29&&game.ActionAge<.51)||(game.Action==Move.Execute&&game.ActionAge>.12&&game.ActionAge<.34);
            if(!cutting){trailTips.Clear();trailBases.Clear();bladeTrail.Children.Clear();return;}
            if(dt<=0)return;
            trailTips.Add(weapon.Transform.Transform(new Point3D(-.04,1.10,0)));
            trailBases.Add(weapon.Transform.Transform(new Point3D(-.09,.31,0)));
            if(trailTips.Count>7){trailTips.RemoveAt(0);trailBases.RemoveAt(0);}
            bladeTrail.Children.Clear();
            for(int i=1;i<trailTips.Count;i++)
            {
                var mesh=new MeshGeometry3D();mesh.Positions=new Point3DCollection(new[]{trailBases[i-1],trailTips[i-1],trailTips[i],trailBases[i]});Models.Tri(mesh,0,1,2);Models.Tri(mesh,0,2,3);
                byte alpha=(byte)(18+95.0*i/trailTips.Count);
                var mat=new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(alpha,255,133,20)));bladeTrail.Children.Add(Models.Geometry(mesh,mat));
                Models.Tube(bladeTrail,Models.Flame,trailTips[i-1],trailTips[i],.003,.002,6);
            }
        }
        void AddShadow(Model3DGroup parent)
        {
            // Small contact footprint; not a simulated directional shadow.
            var material=Models.Mat("#ADB0AB",0);Models.Ball(parent,material,0,.008,0,.36,.006,.24,6,18);
        }
        static double Smooth(double x){x=Combat.Clamp(x,0,1);return x*x*(3-2*x);}
        void PoseWeapon()
        {
            double guard=game.Slash?0:1;
            if(game.Action==Move.Switch){double t=Smooth(game.ActionAge/.38);guard=game.Slash?1-t:t;}
            double x=.57-guard*.29,y=-.36+guard*.05,z=-1.28,rx=-8-guard*55,ry=14,rz=-8+guard*3;
            double a=game.ActionAge;
            if(game.Action==Move.Light)
            {double wind=Smooth(a/.12),cut=Smooth((a-.12)/.12),back=Smooth((a-.24)/.19);double pulse=1-back;x+=(.15*wind-.95*cut)*pulse;y+=(.18*wind-.58*cut)*pulse;rz+=(-40*wind+120*cut)*pulse;ry-=cut*pulse*15;}
            else if(game.Action==Move.Heavy)
            {double wind=Smooth(a/.30),cut=Smooth((a-.30)/.13),back=Smooth((a-.43)/.42);double pulse=1-back;y+=(.33*wind-.84*cut)*pulse;x+=(.12*wind-1.10*cut)*pulse;rx+=(15*wind-20*cut)*pulse;rz+=(-46*wind+133*cut)*pulse;}
            else if(game.Action==Move.Thrust)
            {double wind=Smooth(a/.15),push=Smooth((a-.15)/.10),back=Smooth((a-.25)/.30);double pulse=1-back;z+=.18*wind*pulse-.53*push*pulse;x-=.13*push*pulse;rx-=34*push*pulse;rz=0;}
            else if(game.Action==Move.Execute)
            {double wind=Smooth(a/.13),cut=Smooth((a-.13)/.08),back=Smooth((a-.24)/.38);double pulse=1-back;x+=(.18*wind-1.16*cut)*pulse;y+=(.25*wind-.75*cut)*pulse;rz+=(-50*wind+142*cut)*pulse;ry+=cut*15*pulse;}
            if(game.BlockLeft>0)
            {double b=Smooth((Tuning.BlockDuration-game.BlockLeft)/.13)*Smooth(game.BlockLeft/.16);x+=(.25-x)*b;y+=(-.03-y)*b;rx+=(-8-rx)*b;rz+=(76-rz)*b;z+=(-1.3-z)*b;}
            double bob=Math.Sin(game.WalkTime*2)*.009;
            Models.Pose(weapon,x,y+bob-recoil*.015,z,rx,ry,rz);
            var transform=weapon.Transform.Value;var scaled=Matrix3D.Identity;scaled.Scale(new Vector3D(.78,.78,.78));scaled.Append(transform);weapon.Transform=new MatrixTransform3D(scaled);
            // Both hands grip the SAME blade in guard stance.
            double grip=guard;
            if(game.BlockLeft>0||game.Action==Move.Heavy)grip=1;
            Models.Pose(offhand,-.008-(1-grip)*.87,-.235-(1-grip)*.17,.018+(1-grip)*.02,0,0,grip*8);
            Point3D rightWrist=weapon.Transform.Transform(new Point3D(0,-.15,.02));
            Point3D leftWrist=weapon.Transform.Transform(offhand.Transform.Transform(new Point3D(0,-.065,0)));
            AttachArm(rightForearm,rightWrist,new Point3D(.76,-.76,-.25));
            AttachArm(leftForearm,leftWrist,new Point3D(-.69,-.76,-.20));
        }
        void AttachArm(Model3DGroup arm,Point3D wrist,Point3D elbow)
        {
            Vector3D direction=elbow-wrist;double length=direction.Length;direction.Normalize();
            var axis=Vector3D.CrossProduct(new Vector3D(0,-1,0),direction);
            var m=Matrix3D.Identity;m.Scale(new Vector3D(.78,length/.38,.78));
            if(axis.Length>.0001){axis.Normalize();double angle=Math.Acos(Combat.Clamp(Vector3D.DotProduct(new Vector3D(0,-1,0),direction),-1,1))*180/Math.PI;m.Rotate(new Quaternion(axis,angle));}
            m.Translate(new Vector3D(wrist.X,wrist.Y,wrist.Z));arm.Transform=new MatrixTransform3D(m);
        }
        void Impact(Target t,bool execute)
        {
            recoil=.8;
            if(t.Dead)
            {
                var ring=Models.Group(world);Models.Ring(ring,Models.Flame,0,0,0,.4,.014);
                var burst=new Spark{Mesh=ring,X=t.X,Y=t.Y+.08,Z=t.Z,Life=.32,MaxLife=.32,Burst=true};
                Models.Pose(ring,burst.X,burst.Y,burst.Z,90,0,0);sparks.Add(burst);
            }
            for(int i=0;i<(t.Dead?56:game.Action==Move.Heavy?32:24);i++)
            {
                var m=Models.Group(world);
                if(i%3==0)Models.Tube(m,Models.Flame,new Point3D(0,-.065,0),new Point3D(0,.065,0),.009,.001,5);
                else Models.Ball(m,Models.Red,0,0,0,.025,.035,.015,5,6);
                double life=.25+random.NextDouble()*.25;
                double distance=Math.Max(.01,game.Distance(t)),nx=(game.X-t.X)/distance,nz=(game.Z-t.Z)/distance;
                var p=new Spark{Mesh=m,X=t.X+nx*.18,Y=t.Y+1.30,Z=t.Z+nz*.18,VX=(random.NextDouble()-.5)*4,VY=random.NextDouble()*2.5,VZ=(random.NextDouble()-.5)*4,Life=life,MaxLife=life};
                Models.Pose(m,p.X,p.Y,p.Z,0,0,i*37);sparks.Add(p);
            }
        }
        public bool ProjectHead(Target target,out Point screen)
        {
            screen=new Point();TerminatorRig rig;
            if(target.Dead||!rigs.TryGetValue(target,out rig))return false;
            // Use the rendered head hierarchy and WPF camera projection, not a second
            // hand-written camera whose handedness can disagree with Viewport3D.
            Point3D anchor=rig.Root.Transform.Transform(rig.Body.Transform.Transform(new Point3D(0,2.55,-.035)));
            var look=camera.LookDirection;look.Normalize();
            if(Vector3D.DotProduct(anchor-camera.Position,look)<=camera.NearPlaneDistance)return false;
            if(!worldVisual.TransformToAncestor(surface).TryTransform(anchor,out screen))return false;
            screen.Y-=50;
            return !double.IsNaN(screen.X)&&!double.IsNaN(screen.Y)&&screen.X>-120&&screen.X<surface.ActualWidth+120&&screen.Y>-70&&screen.Y<surface.ActualHeight+70;
        }
        void VerifyShot()
        {
            // Deterministic in-app render capture, not desktop screen automation.
            game.Reset(true);game.Paused=false;
            if(verifyStage==1){game.Slash=false;}
            if(verifyStage==2){game.Slash=false;game.Block();game.Tick(.2,0,0);}
            if(verifyStage==3){game.Slash=false;game.Attack(false);game.Tick(.22,0,0);}
            if(verifyStage==4){game.Attack(false);game.Tick(.18,0,0);}
            if(verifyStage==5){game.X=1.1;game.Z=-.5;game.Yaw=-.23;game.Pitch=-.08;}
            if(verifyStage==6){game.X=-1.4;game.Z=.2;game.Yaw=.30;game.Pitch=.10;}
            if(verifyStage==7||verifyStage==8){game.Targets[0].Health=2200;game.Attack(verifyStage==8);double end=verifyStage==8?.39:.21;for(double t=0;t<end;t+=.016){game.Tick(.016,0,0);SyncScene(.016);}}
            if(verifyStage==9){for(int i=0;i<30;i++)game.Tick(.016,0,0,true);}
            if(verifyStage==10){game.X=0;game.Z=14;game.JumpY=4.22;game.Yaw=Math.PI;game.Pitch=-.15;game.MessageLeft=0;}
            if(verifyStage==11){game.Z=2;game.Targets[0].Health=2200;game.Attack(true);for(int i=0;i<23;i++){game.Tick(.016,0,0);SyncScene(.016);}}
            if(verifyStage==12){game.Z=2;game.Targets[0].Health=1100;game.Attack(true);for(int i=0;i<34;i++){game.Tick(.016,0,0);SyncScene(.016);}game.MessageLeft=0;}
            if(verifyStage==13){ToggleSettings();}
            if(verifyStage==14){game.X=14;game.Z=-21;game.Attack(true);game.Tick(.36,0,0);game.MessageLeft=0;}
            SyncScene(0);hud.InvalidateVisual();surface.UpdateLayout();
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>
            {
                string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"verification");Directory.CreateDirectory(dir);
                var bmp=new RenderTargetBitmap((int)surface.ActualWidth,(int)surface.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(surface);
                var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(bmp));using(var fs=File.Create(Path.Combine(dir,new[]{"slash","guard","block","thrust","execute","look-right","look-left","light","heavy","crouch","village-rooftop","impact","kill","settings","wall-scar"}[verifyStage]+".png")))enc.Save(fs);
                if(settingsOpen)ToggleSettings();
                if(verifyStage==14){SyncScene(4.1);if(scars.Count!=0)throw new Exception("Scars failed to expire");}
                verifyStage++;if(verifyStage<15){foreach(var r in rigs.Values)world.Children.Remove(r.Root);rigs.Clear();foreach(var p in sparks)world.Children.Remove(p.Mesh);sparks.Clear();VerifyShot();}else Close();
            }));
        }
        sealed class Hud:FrameworkElement
        {
            readonly Combat g;readonly GameWindow host;readonly Typeface font=new Typeface("Microsoft YaHei UI");
            readonly Brush ink=new SolidColorBrush(Color.FromRgb(246,249,247)),gold=new SolidColorBrush(Color.FromRgb(255,176,72)),cyan=new SolidColorBrush(Color.FromRgb(91,220,232)),muted=new SolidColorBrush(Color.FromRgb(184,205,211));
            readonly Brush panel=new LinearGradientBrush(Color.FromArgb(246,16,29,36),Color.FromArgb(242,25,43,51),90);
            readonly Pen panelEdge=new Pen(new SolidColorBrush(Color.FromArgb(245,80,190,202)),1.5);
            public Hud(Combat game,GameWindow window){g=game;host=window;}
            void Text(DrawingContext d,string text,double x,double y,double size,Brush brush,bool center=false)
            {var t=new FormattedText(text,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,font,size,brush,1.0);d.DrawText(t,new Point(center?x-t.Width/2:x,y));}
            void Panel(DrawingContext d,double x,double y,double w,double h)
            {
                d.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(145,0,8,12)),null,new Rect(x+3,y+7,w,h),15,15);
                d.DrawRoundedRectangle(panel,panelEdge,new Rect(x,y,w,h),14,14);
                d.DrawRoundedRectangle(null,new Pen(new SolidColorBrush(Color.FromArgb(74,255,255,255)),1),new Rect(x+1.5,y+1.5,w-3,h-3),12,12);
                d.DrawEllipse(new SolidColorBrush(Color.FromArgb(210,255,255,255)),null,new Point(x+w-15,y+12),2.5,2.5);
            }
            void Bar(DrawingContext d,double x,double y,double w,double v,Brush brush)
            {
                Polygon(d,new SolidColorBrush(Color.FromRgb(70,91,98)),null,new Point(x+4,y),new Point(x+w,y),new Point(x+w-4,y+8),new Point(x,y+8));
                double fill=w*Combat.Clamp(v,0,1);if(fill>4)Polygon(d,brush,null,new Point(x+4,y),new Point(x+fill,y),new Point(x+fill-4,y+8),new Point(x,y+8));
                for(int i=1;i<4;i++){double sx=x+w*i/4;d.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(115,7,13,18)),1),new Point(sx,y),new Point(sx,y+8));}
            }
            void Accent(DrawingContext d,double x,double y,double h,Brush brush){d.DrawRoundedRectangle(brush,null,new Rect(x,y,4,h),2,2);}
            void Polygon(DrawingContext d,Brush fill,Pen pen,params Point[] points)
            {var shape=new StreamGeometry();using(var c=shape.Open()){c.BeginFigure(points[0],true,true);for(int i=1;i<points.Length;i++)c.LineTo(points[i],true,false);}shape.Freeze();d.DrawGeometry(fill,pen,shape);}
            void KillBadge(DrawingContext d,double x,double y)
            {
                int tier=Math.Min(8,g.Kills);Brush metal=tier>=5?Brushes.OrangeRed:tier>=3?gold:Brushes.LightSteelBlue;
                var edge=new Pen(metal,2);double age=1.8-g.KillNotice,scale=1+Math.Max(0,1-age/.18)*.22;
                d.PushTransform(new ScaleTransform(scale,scale,x,y));
                for(int side=-1;side<=1;side+=2)for(int feather=0;feather<Math.Min(5,tier+1);feather++)
                {double reach=65+feather*11;Polygon(d,panel,edge,new Point(x+side*30,y-22+feather*9),new Point(x+side*reach,y-40+feather*8),new Point(x+side*(reach-12),y-13+feather*9),new Point(x+side*32,y+22));}
                Polygon(d,panel,edge,new Point(x,y-58),new Point(x+46,y-28),new Point(x+38,y+29),new Point(x,y+57),new Point(x-38,y+29),new Point(x-46,y-28));
                // Stylized skull and crossed blades: scalable native artwork.
                d.DrawLine(new Pen(metal,5),new Point(x-28,y-33),new Point(x+28,y+29));d.DrawLine(new Pen(metal,5),new Point(x+28,y-33),new Point(x-28,y+29));
                d.DrawEllipse(metal,new Pen(Brushes.Black,2),new Point(x,y-14),22,20);
                d.DrawRoundedRectangle(metal,null,new Rect(x-13,y-4,26,19),3,3);
                d.DrawEllipse(Brushes.Black,null,new Point(x-9,y-15),6,5);d.DrawEllipse(Brushes.Black,null,new Point(x+9,y-15),6,5);
                Polygon(d,Brushes.Black,null,new Point(x,y-9),new Point(x-4,y-2),new Point(x+4,y-2));
                for(int i=-1;i<=1;i++)d.DrawLine(new Pen(Brushes.Black,2),new Point(x+i*7,y+6),new Point(x+i*7,y+14));
                Text(d,g.Kills.ToString(),x,y+16,22,Brushes.White,true);d.Pop();
                string[] names={"","刀杀","双杀","三连杀","四连杀","五连杀","六连杀","七连杀","八连杀"};
                Text(d,g.Kills>8?g.Kills+" 连杀":names[tier],x,y+62,24,metal,true);
                Text(d,g.LastKillExecution?"一闪处决":"近战击杀",x,y+93,12,ink,true);
            }
            protected override void OnRender(DrawingContext d)
            {
                double w=ActualWidth,h=ActualHeight,cx=w/2,cy=h/2;var target=g.ExecutionTarget();
                foreach(var t in g.Targets)
                {
                    if(t.Dead)continue;Point p;if(!host.ProjectHead(t,out p))continue;
                    double bw=132;Panel(d,p.X-bw/2,p.Y-5,bw,51);Accent(d,p.X-bw/2+1,p.Y+3,34,t.Health<=Tuning.ExecuteHealth?gold:cyan);Text(d,(t.Dummy?"训练感染体  ":"感染护士  ")+t.Id,p.X,p.Y-1,11,muted,true);Text(d,((int)t.Health)+" / "+(int)t.MaxHealth,p.X,p.Y+17,15,ink,true);Bar(d,p.X-bw/2+10,p.Y+38,bw-20,t.Health/t.MaxHealth,t.Health<=Tuning.ExecuteHealth?gold:Brushes.IndianRed);
                    if(t==target){d.DrawRectangle(null,new Pen(gold,2),new Rect(p.X-64,p.Y-6,128,53));}
                }
                Panel(d,22,20,224,54);Accent(d,23,29,34,gold);Text(d,g.Practice?"训练场":"生存 · 第 "+g.Wave+" 波",38,29,11,muted);Text(d,"恶魔剑客",38,44,17,ink);
                Panel(d,w-222,20,200,54);Accent(d,w-221,29,34,cyan);Text(d,"击杀  "+g.Kills,w-205,29,17,ink);Text(d,((int)host.fps)+" / "+host.targetFps+" 帧  ·  "+(g.Muted?"静音":"音效"),w-205,51,10,g.Muted?Brushes.IndianRed:muted);
                Panel(d,22,h-112,306,90);Accent(d,23,h-101,67,Brushes.IndianRed);Text(d,"生命值",42,h-99,11,muted);Text(d,((int)g.Health).ToString(),42,h-80,31,ink);Text(d,"/ "+Tuning.PlayerHealth,126,h-65,13,muted);Bar(d,42,h-43,266,g.Health/Tuning.PlayerHealth,Brushes.IndianRed);
                Panel(d,w-334,h-126,312,104);Accent(d,w-333,h-115,81,g.Slash?gold:cyan);Text(d,"战斗姿态",w-313,h-113,10,muted);Text(d,g.Slash?"斩剑式":"御剑式",w-313,h-94,23,g.Slash?gold:cyan);
                if(g.Slash){Text(d,"一闪  "+(g.ExecuteCd<=0?"就绪":g.ExecuteCd.ToString("0.0")+" 秒"),w-313,h-64,14,ink);Bar(d,w-313,h-43,270,1-g.ExecuteCd/Tuning.ExecuteCooldown,gold);}
                else{Text(d,"一闪 "+(g.ExecuteCd<=0?"就绪":g.ExecuteCd.ToString("0.0"))+"   位移 "+(g.DashCd<=0?"就绪":g.DashCd.ToString("0.0"))+"   防御 "+(g.BlockCd<=0?"就绪":g.BlockCd.ToString("0.0")),w-313,h-64,12,ink);Bar(d,w-313,h-43,128,1-g.DashCd/Tuning.DashCooldown,gold);Bar(d,w-175,h-43,132,1-g.BlockCd/Tuning.BlockCooldown,cyan);}
                var pen=new Pen(target!=null?gold:new SolidColorBrush(Color.FromRgb(31,39,36)),2);
                d.DrawLine(pen,new Point(cx-11,cy),new Point(cx-4,cy));d.DrawLine(pen,new Point(cx+4,cy),new Point(cx+11,cy));d.DrawLine(pen,new Point(cx,cy-11),new Point(cx,cy-4));d.DrawLine(pen,new Point(cx,cy+4),new Point(cx,cy+11));
                if(target!=null){Panel(d,cx-103,cy+36,206,53);Text(d,"左键 · 一闪",cx,cy+40,20,gold,true);Text(d,"锁定 "+(int)target.Health+" 血",cx,cy+67,12,ink,true);}
                else if(g.ExecuteCd>0){Panel(d,cx-96,cy+36,192,44);Text(d,"一闪冷却  "+g.ExecuteCd.ToString("0.0")+" 秒",cx,cy+41,16,ink,true);Bar(d,cx-83,cy+69,166,1-g.ExecuteCd/Tuning.ExecuteCooldown,gold);}
                if(g.BlockLeft>0){Panel(d,cx-80,cy+105,160,28);Text(d,"正面防御 "+g.BlockLeft.ToString("0.0")+" s",cx,cy+109,14,gold,true);}
                if(g.HitFlash>0){var hit=new Pen(Brushes.OrangeRed,3);for(int i=0;i<4;i++){double a=Math.PI/4+i*Math.PI/2;d.DrawLine(hit,new Point(cx+Math.Cos(a)*13,cy+Math.Sin(a)*13),new Point(cx+Math.Cos(a)*24,cy+Math.Sin(a)*24));}Text(d,"−"+g.LastDamage.ToString("0"),cx+35,cy-42,24,gold);}
                if(g.Crouching)Text(d,"下蹲",cx,cy+140,14,ink,true);
                if(g.KillNotice>0)
                {
                    double age=1.8-g.KillNotice,ky=h-182;
                    d.PushOpacity(Math.Min(1,g.KillNotice/.35));
                    KillBadge(d,cx,ky);
                    if(age<.32){double radius=58+age*120;d.DrawEllipse(null,new Pen(new SolidColorBrush(Color.FromArgb((byte)(170*(1-age/.32)),255,186,78)),2),new Point(cx,ky),radius,radius);}
                    d.Pop();
                }
                if(g.MessageLeft>0){Panel(d,cx-220,91,440,36);Text(d,g.Message,cx,98,16,ink,true);}
                if(host.showHelp){Panel(d,22,100,300,176);Accent(d,23,112,151,cyan);Text(d,"操作指南",40,111,10,cyan);Text(d,"F  切换姿态   空格  跳跃 / 位移",40,133,13,ink);Text(d,g.Slash?"左键  轻击 / 一闪   右键  重击":"左键  突刺 / 一闪   右键  防御",40,158,13,ink);Text(d,"按住控制键下蹲 · 松开站起",40,183,13,ink);Text(d,"F5 重置   F6 训练 / 生存   M 静音",40,210,12,muted);Text(d,"F2 设置   [ ] 或 - + 微调   F1 隐藏",40,233,12,muted);Text(d,"灵敏度  "+host.sensitivity.ToString("0.00000"),40,254,12,gold);}
                if(g.Action==Move.Execute&&g.ActionAge>.12&&g.ActionAge<.29)
                {double a=1-(g.ActionAge-.12)/.17;var streak=new Pen(new SolidColorBrush(Color.FromArgb((byte)(a*150),155,20,19)),4);d.DrawLine(streak,new Point(w*.15,h*.67),new Point(w*.9,h*.31));d.DrawRectangle(null,new Pen(new SolidColorBrush(Color.FromArgb((byte)(a*80),183,20,24)),18),new Rect(8,8,w-16,h-16));}
                if(g.DashLeft>0)for(int i=0;i<16;i++){double a=i*Math.PI/8;var p=new Pen(new SolidColorBrush(Color.FromArgb(90,93,109,126)),2);d.DrawLine(p,new Point(cx+Math.Cos(a)*w*.38,cy+Math.Sin(a)*h*.38),new Point(cx+Math.Cos(a)*w*.6,cy+Math.Sin(a)*h*.6));}
                if(g.DamageFlash>0)d.DrawRectangle(null,new Pen(new SolidColorBrush(Color.FromArgb((byte)(g.DamageFlash*400),167,15,22)),26),new Rect(12,12,w-24,h-24));
                if((g.Paused||g.Health<=0)&&!host.settingsOpen)
                {
                    d.DrawRectangle(new SolidColorBrush(Color.FromArgb(178,5,10,14)),null,new Rect(0,0,w,h));Panel(d,cx-286,cy-190,572,365);
                    Text(d,"生化剑客模式",cx,cy-158,11,cyan,true);Text(d,g.Health<=0?"战斗结束":"恶魔剑客",cx,cy-132,38,ink,true);Text(d,"斩剑式  /  御剑式",cx,cy-76,17,gold,true);
                    Text(d,"方向键移动  ·  鼠标观察  ·  F 切换姿态",cx,cy-40,15,ink,true);
                    Text(d,"斩剑：轻击、重击，550 血以下左键一闪",cx,cy-10,15,ink,true);
                    Text(d,"御剑：突刺 / 一闪、定向位移、正面防御",cx,cy+20,15,ink,true);
                    Text(d,"回车键 / 点击进入战斗",cx,cy+87,18,gold,true);
                    Text(d,"F5 重置   ·   F6 切换生存   ·   F11 全屏",cx,cy+126,13,muted,true);
                }
            }
        }
    }
}
