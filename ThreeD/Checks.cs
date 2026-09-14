using System;
using System.IO;
using System.Collections.Generic;
namespace GhostBlade3D
{
    static class Checks
    {
        static readonly List<string> results=new List<string>();
        static void Assert(bool pass,string name){if(!pass)throw new Exception("FAILED: "+name);results.Add("PASS: "+name);}
        static void Step(Combat g,double time){while(time>0){double dt=Math.Min(.01,time);g.Tick(dt,0,0);time-=dt;}}
        public static void Run()
        {
            var g=new Combat();g.Reset(true);Target t=g.Targets[0];
            Assert(g.ExecutionTarget()==t,"550 HP target locks in slash stance");
            Assert(g.Attack(false),"left click starts execution");Assert(!t.Dead,"execution waits for animation contact");
            Step(g,.18);Assert(t.Dead&&g.Kills==1,"execution kills once on contact");Assert(Math.Abs(g.Z-4.3)<.001,"execution arrives at victim location");
            Assert(g.ExecuteCd>2.7,"execution cooldown is consumed");
            double cd=g.ExecuteCd;g.Paused=true;Step(g,2);Assert(Math.Abs(g.ExecuteCd-cd)<.0001,"pause freezes cooldowns");g.Paused=false;
            Step(g,.5);g.Targets.Add(new Target{X=g.X,Z=g.Z+2,Health=550});Assert(g.ExecutionTarget()==null,"execution cannot re-trigger during cooldown");
            g.Reset(true);g.Targets[0].Health=1100;g.Z=2;g.Attack(false);Step(g,.10);Assert(g.Targets[0].Health==1100,"light attack has windup");Step(g,.08);Assert(g.Targets[0].Health==550,"light damage applies at contact");Step(g,.4);Assert(g.Targets[0].Health==550,"one swing cannot hit twice");
            g.Reset(true);g.Targets[0].Health=2200;g.Z=2;g.Attack(true);Step(g,.4);Assert(g.Targets[0].Health==1100,"heavy strike deals distinct damage");
            g.Reset(true);g.Switch();Step(g,.4);Assert(!g.Slash&&g.ExecutionTarget()==g.Targets[0],"guard stance can lock an execution target");
            Assert(g.Attack(false)&&g.Action==Move.Execute,"guard left click becomes execution below threshold");Step(g,.18);Assert(g.Targets[0].Dead,"guard execution kills on contact");
            g.Reset(true);g.Switch();Step(g,.4);g.Targets[0].Health=2200;g.Z=2;g.Attack(false);Assert(g.Action==Move.Thrust,"guard left click remains thrust above threshold");Step(g,.21);Assert(g.Targets[0].Health<2200,"thrust contacts target");
            g.Reset(true);g.Switch();Step(g,.4);Assert(g.Block(),"guard activates");double hp=g.Health;
            Assert(!g.Receive(new Target{X=0,Z=1},100)&&g.Health==hp,"guard stops frontal damage");
            Assert(g.Receive(new Target{X=0,Z=-1},100)&&g.Health==hp-100,"guard does not stop rear damage");
            Assert(!g.Block(),"guard cannot bypass cooldown");
            g.Reset(true);g.Switch();Step(g,.4);g.Dash(0,1);Step(g,.14);Assert(Math.Abs(g.X+4.7)<.01&&Math.Abs(g.Z)<.001,"right dash follows camera right (-X while facing +Z)");Assert(!g.Dash(1,0),"dash cooldown prevents spam");
            g.Reset(true);g.Tick(.1,0,1);Assert(g.X<0,"D strafes toward camera right");
            g.Reset(true);g.Yaw=Math.PI/2;g.Tick(.1,0,1);Assert(g.Z>0&&Math.Abs(g.X)<.001,"strafe rotates with camera heading");
            g.Reset(true);g.Pitch=1.1;Assert(g.ExecutionTarget()==null,"execution respects vertical aim");
            var a=new Combat();a.Reset(true);a.Tick(.1,1,0);var b=new Combat();b.Reset(true);b.Switch();Step(b,.4);b.Tick(.1,1,0);Assert(a.Z>b.Z*4.9,"guard movement reduces to twenty percent");
            g.Reset(true);g.Z=2;g.Targets[0].Health=2200;g.Attack(true);Step(g,.10);
            Assert(g.Switch()&&!g.Slash,"F immediately cancels heavy windup into guard");
            Assert(g.Dash(0,1),"space dashes immediately during stance transition");
            Assert(g.Switch()&&g.Slash,"F immediately returns to slash during dash");
            Step(g,.14);Assert(Math.Abs(g.X+4.7)<.01,"stance change preserves active directional dash");
            Step(g,.5);Assert(g.Targets[0].Health==2200,"cancelled windup cannot produce a delayed ghost hit");
            g.Switch();Assert(!g.Dash(1,0),"F sequence does not reset dash cooldown");
            g.Reset(true);g.Z=2;g.Targets[0].Health=2200;g.Attack(true);Step(g,.35);
            Assert(g.Switch(),"F cancels heavy recovery after contact");Step(g,.5);
            Assert(g.Targets[0].Health==1100,"recovery cancel preserves exactly one heavy hit");
            g.Reset(true);g.Targets[0].Health=2200;g.Switch();Assert(g.Attack(false)&&g.Action==Move.Thrust,"thrust responds during stance transition");
            Assert(g.Switch()&&g.Slash,"F can cancel thrust");
            g.Reset(true);g.Switch();Assert(g.Block(),"block responds during stance transition");
            g.Switch();g.Switch();Assert(g.BlockLeft==0&&!g.Block(),"stance switching drops block without resetting cooldown");
            g.Reset(true);g.Attack(false);g.Switch();Step(g,.2);
            Assert(!g.Targets[0].Dead&&g.ExecuteCd>0&&g.Invulnerable==0,"cancelled execution spends cooldown without delayed kill or invulnerability");
            g.Paused=true;Assert(!g.Switch()&&!g.Dash(1,0),"pause still rejects combat inputs");
            g.Reset(true);g.Switch();Step(g,1);Assert(Math.Abs(g.EyeHeight-1.66)<.001,"guard preserves standing camera height");
            for(int i=0;i<50;i++)g.Tick(.01,0,0,true);
            Assert(g.Crouching&&Math.Abs(g.EyeHeight-1.02)<.001,"Ctrl lowers camera independently of stance");
            g.Switch();g.Tick(.01,0,0,true);Assert(g.Crouching&&g.EyeHeight<1.03,"F does not cancel crouch");
            Step(g,1);Assert(!g.Crouching&&Math.Abs(g.EyeHeight-1.66)<.001,"releasing Ctrl restores camera");
            var village=new Village(true);g.Reset(true);g.Map=village;g.X=2.2;g.Z=7.7;
            for(int i=0;i<140;i++)g.Tick(.01,1,0);
            Assert(g.JumpY>3.2&&g.Z>14,"stairs reach central rooftop without jumping");
            g.Reset(true);g.X=6;g.Z=-12;g.Yaw=0;
            for(int i=0;i<60;i++)g.Tick(.01,1,0);
            Assert(g.Z<-10.65,"standing player cannot enter crouch tunnel");
            for(int i=0;i<100;i++)g.Tick(.01,1,0,true);
            Assert(g.Z>-10.4&&g.Z<-7.5,"crouched player enters low passage");
            g.Tick(.01,0,0,false);Assert(g.Crouching,"ceiling prevents standing into geometry");
            g.Reset(true);g.X=-7.5;g.Z=12;g.JumpY=.25;g.Slash=false;g.Yaw=-Math.PI/2;g.Dash(1,0);Step(g,.2);
            Assert(g.X>=-7.87&&g.X< -7.5,"dash stops at central house wall without tunnelling");
            Assert(!village.Sight(-9,1,12,-7,1,12),"map blocks melee line of sight through walls");
            g.Map=null;g.Reset(true);g.Z=2;g.Targets[0].Health=2200;var sounds=new List<string>();g.Sound=sounds.Add;
            g.Attack(true);Step(g,.2);Assert(!sounds.Contains("heavy-hit"),"impact sound waits for contact");Step(g,.2);
            Assert(sounds.Contains("heavy-hit")&&g.LastDamage==1100,"heavy contact emits damage and dedicated impact sound");
            g.Map=null;g.Reset(true);g.Dash(1,0);double normalTop=0;for(int i=0;i<100;i++){g.Tick(.01,1,0);normalTop=Math.Max(normalTop,g.JumpY);}
            g.Reset(true);g.Dash(1,0);Step(g,.39);double eyeBefore=g.JumpY+g.EyeHeight;double velocity=g.JumpSpeed;g.Tick(.01,1,0,true);
            Assert(g.JumpY>normalTop+.45,"apex crouch raises feet above normal jump apex");
            Assert(Math.Abs(g.JumpY+g.EyeHeight-eyeBefore)<.08&&g.JumpSpeed<=velocity,"air crouch preserves head height without adding upward impulse");
            double firstTuck=g.JumpY;g.Tick(.01,0,0,false);g.Tick(.01,0,0,true);Assert(g.JumpY<firstTuck-.4,"repeated crouch cannot stack air lifts");
            var parkour=new Village();foreach(var route in parkour.Routes)
            {
                g.Reset(true);g.Map=parkour;g.X=route.X;g.Z=route.Z;
                Assert(parkour.Clear(g.X,0,g.Z,1.8,.24),"route starts on clear ground: "+route.Name);
                WalkTo(g,route.X,route.EndZ);WalkTo(g,route.RoofX,route.RoofZ);
                Assert(Math.Abs(g.JumpY-route.Height)<.06,"walkable stair-to-roof route: "+route.Name);
            }
            g.Reset(true);g.Map=parkour;g.X=40;g.Z=-40;Assert(parkour.Clear(g.X,0,g.Z,1.8,.24)&&Math.Abs(parkour.Floor(g.X,g.Z,1))<.001,"open training field remains flat and unobstructed");
            Assert(parkour.Routes.Count==1,"open field contains exactly one climbable ancient building route");
            g.Map=null;
            g.Reset(true);sounds.Clear();g.Z=2;g.Targets[0].Health=1100;g.Attack(true);Step(g,.36);
            Assert(g.KillNotice>1.7&&g.KillBurst>0&&!g.LastKillExecution,"normal kill starts dedicated badge and burst");
            Assert(sounds.FindAll(s=>s=="kill").Count==1,"normal kill sound fires exactly once");Step(g,2);
            Assert(g.KillNotice==0&&sounds.FindAll(s=>s=="kill").Count==1,"kill badge expires without replaying sound");
            g.Reset(true);sounds.Clear();g.Attack(false);Step(g,.18);Assert(g.LastKillExecution&&sounds.Contains("execute-kill"),"execution emits its own kill sound and badge");
            g.Reset(true);g.Muted=true;sounds.Clear();g.Attack(false);Step(g,.18);Assert(sounds.Count==0&&g.KillNotice>0,"mute suppresses kill sound but keeps visual feedback");g.Muted=false;
            var terminator=new TerminatorRig();Assert(terminator.Body.Children.Count<20&&terminator.LeftWing.Children.Count<=4,"terminator geometry is batched while articulated groups remain");
            g.Reset(true);g.Switch();g.Dash(1,0);g.Invulnerable=0;double beforeDash=g.Health;Assert(!g.Receive(new Target{Z=1},100)&&g.Health==beforeDash,"dash state itself guarantees invulnerability");Step(g,.2);Assert(g.Receive(new Target{Z=g.Z+1},100),"dash invulnerability ends after movement");
            double px=0,py=1,pz=14;Assert(parkour.Resolve(ref px,ref py,ref pz,1.8)&&parkour.Clear(px,py,pz,1.8,.24),"overlapping player is ejected to clear space");
            System.Windows.Media.Media3D.Point3D hitPoint;System.Windows.Media.Media3D.Vector3D hitNormal;
            Assert(parkour.SurfaceHit(0,1,8,0,0,1,3,out hitPoint,out hitNormal)&&hitNormal.Z<0,"wall strike locates outward-facing surface");
            g.Reset(true);g.Map=parkour;g.X=0;g.Z=8;int wallHits=0;g.WallImpact=(p,n)=>wallHits++;g.Attack(true);Step(g,.9);Assert(wallHits==1,"one swing emits exactly one wall scar event");g.Map=null;
            int mascot=-1;g.Reset(true);g.Targets.Clear();g.X=-3.1;g.Z=8.4;g.JumpY=4.78;g.MascotImpact=i=>mascot=i;g.Attack(false);Step(g,.18);Assert(mascot==0,"blade contact triggers cow voice");
            mascot=-1;g.Reset(true);g.Targets.Clear();g.X=0;g.Z=8.4;g.JumpY=4.78;g.MascotImpact=i=>mascot=i;g.Attack(false);Step(g,.18);Assert(mascot==1,"blade contact triggers chubby voice");
            mascot=-1;g.Reset(true);g.Targets.Clear();g.X=3.1;g.Z=8.4;g.JumpY=4.78;g.MascotImpact=i=>mascot=i;g.Attack(false);Step(g,.18);Assert(mascot==2,"blade contact triggers laughing mascot voice");g.MascotImpact=null;
            Assert(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","combat-atlas-v1.png")),"generated combat atlas is shipped locally");
            var mesh=Models.Blade();Assert(mesh.Children.Count>20,"blade is multi-part 3D mesh");
            g.Map=null;g.Reset(true);g.Slash=false;sounds.Clear();var streaks=new List<int>();g.Announce=k=>{streaks.Add(k);return k>=2;};
            for(int i=0;i<9;i++){g.Targets.Clear();g.Targets.Add(new Target{Id=i,X=g.X,Z=g.Z+2,Health=550,Dummy=true});g.Attack(false);Step(g,.65);}
            Assert(streaks.Count==9&&streaks[0]==1&&streaks[8]==9,"kill sequence dispatches exact round streak counts");
            Assert(sounds.FindAll(s=>s=="execute-kill").Count==1&&sounds.FindAll(s=>s=="kill").Count==0,"voice announcement replaces generic execution sound after first kill");
            g.Reset(true);g.Muted=true;streaks.Clear();g.Attack(false);Step(g,.18);Assert(streaks.Count==0,"muted kills never dispatch announcement");g.Muted=false;g.Announce=null;
            g.Map=null;g.Reset(true);g.Attack(false);Step(g,.18);double remaining=g.ExecuteCd;g.Switch();g.Switch();
            Assert(Math.Abs(g.ExecuteCd-remaining)<.0001&&g.ExecutionTarget()==null,"F switching preserves execution cooldown");
            g.Targets.Clear();g.Targets.Add(new Target{Z=g.Z+4,Health=550,Dummy=true});Step(g,remaining-.05);
            Assert(g.ExecuteCd>0&&g.ExecutionTarget()==null,"execution stays locked until full cooldown expires");Step(g,.06);
            Assert(g.ExecuteCd==0&&g.ExecutionTarget()!=null,"execution unlock and HUD timer share exact state");
            g.Reset(true);g.Practice=false;g.Map=parkour;g.Health=100000;g.X=-2;g.Z=-12;g.Targets.Clear();g.Targets.Add(new Target{X=-2,Z=-4});
            bool clearPath=true;for(int i=0;i<1800;i++){g.Tick(.02,0,0);var enemy=g.Targets[0];if(!parkour.Clear(enemy.X,enemy.Y,enemy.Z,1.95,.24))clearPath=false;}
            Assert(clearPath&&g.Distance(g.Targets[0])<1.7,"enemy routes around central platform without entering walls");
            foreach(var route in parkour.Routes)
            {g.Reset(true);g.Practice=false;g.Health=100000;g.X=route.RoofX;g.Z=route.RoofZ;g.JumpY=route.Height;g.Targets.Clear();g.Targets.Add(new Target{X=route.X,Z=route.Z});
                for(int i=0;i<1600;i++)g.Tick(.02,0,0);
                Assert(g.Distance(g.Targets[0])<1.7&&Math.Abs(g.Targets[0].Y-route.Height)<.1,"enemy follows stairs onto roof: "+route.Name+" at "+g.Targets[0].X.ToString("0.00")+","+g.Targets[0].Y.ToString("0.00")+","+g.Targets[0].Z.ToString("0.00")+" aim "+g.Targets[0].AimX.ToString("0.00")+","+g.Targets[0].AimZ.ToString("0.00"));}
            g.Map=null;g.Reset(true);g.Practice=false;g.Targets.Clear();g.Targets.Add(new Target{X=0,Z=3});g.Targets.Add(new Target{X=0,Z=3});g.Tick(.02,0,0);
            Assert(Math.Abs(g.Targets[0].X-g.Targets[1].X)+Math.Abs(g.Targets[0].Z-g.Targets[1].Z)>.01,"coincident enemies separate deterministically");
            string localAudio=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"local-audio");
            if(Directory.Exists(localAudio))for(int i=1;i<=8;i++){var clip=Audio.ReadWave(Path.Combine(localAudio,"streak-"+i+".wav"));Assert(clip.Length>22050/2&&clip.Length<22050*3,"local streak WAV format and duration: "+i);}
            foreach(string clipName in new[]{"mascot-laugh.wav","mascot-cow.wav","mascot-chubby.wav"}){var clip=Audio.ReadWave(Path.Combine(localAudio,clipName));Assert(clip.Length>22050/2&&clip.Length<22050*6,"mascot voice WAV format and duration: "+clipName);}
            File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"verification-tests.txt"),results);
        }
        static void WalkTo(Combat g,double x,double z)
        {
            for(int i=0;i<1500;i++){double dx=x-g.X,dz=z-g.Z;if(Math.Sqrt(dx*dx+dz*dz)<.065)return;g.Yaw=Math.Atan2(dx,dz);g.Tick(.01,1,0);}
            throw new Exception("Route blocked at "+g.X+", "+g.JumpY+", "+g.Z+" toward "+x+", "+z);
        }
        static void JumpTo(Combat g,double x,double z,double height,int tuckFrame=35)
        {
            Step(g,.3);Assert(g.Dash(1,0),"can jump from previous landing");
            for(int i=0;i<130;i++){double dx=x-g.X,dz=z-g.Z;double distance=Math.Sqrt(dx*dx+dz*dz);g.Yaw=Math.Atan2(dx,dz);g.Tick(.01,distance>.065?1:0,0,i>=tuckFrame);}
            Assert(Math.Abs(g.JumpY-height)<.03&&Math.Abs(g.X-x)<.08&&Math.Abs(g.Z-z)<.08,"jump route lands on platform "+x+", "+z+" at height "+height);
        }
    }
}
