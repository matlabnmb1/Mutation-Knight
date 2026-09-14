using System;
using System.Collections.Generic;

namespace GhostBlade3D
{
    public enum Move { Idle, Switch, Light, Heavy, Thrust, Execute }
    public static class Tuning
    {
        // User-requested stance rules; timing/range are tunable reconstruction values.
        public const double PlayerHealth=1400, LightDamage=550, HeavyDamage=1100, ThrustDamage=550;
        public const double ExecuteHealth=550, ExecuteCooldown=3, ExecuteRange=6;
        public const double BlockDuration=3, BlockCooldown=5, DashCooldown=1, DashDistance=4.7;
        public const double SlashSpeed=6.2, GuardSpeed=1.24, Arena=55;
    }
    public sealed class Target
    {
        public double X,Y,Z,Health=2200,MaxHealth=2200,AttackTimer=1,Hurt,DeathTime,Phase;
        public double ThinkTime,AimX,AimZ;
        public bool Dummy,Dead;
        public int Id;
    }
    public sealed class Combat
    {
        public readonly List<Target> Targets=new List<Target>();
        public double X,Z,Yaw,Pitch,Health=Tuning.PlayerHealth,Clock,JumpY,JumpSpeed;
        public double ExecuteCd,BlockCd,BlockLeft,DashCd,Invulnerable,DashLeft,DamageFlash,HitFlash;
        public double ActionAge,ActionDuration,HitAt,MessageLeft;
        public double EyeHeight=1.66,WalkTime;
        public bool Crouching;
        bool airTuckUsed,airTucked;
        double safeX,safeY,safeZ;
        public Village Map;
        Navigation navigation;Village navigationMap;
        public double LastDamage;
        public double KillNotice,KillBurst;
        public bool LastKillExecution;
        public int LastVictim;
        public bool Slash=true,Paused=true,Practice=true,Muted,HasHit;
        public int Kills,Wave=1;public Move Action=Move.Idle;public string Message="";
        double dashX,dashZ,waveDelay;
        Target executionTarget;
        public Action<string> Sound;
        public Action<Target,bool> Impact;
        public Action<int> MascotImpact;
        public Func<int,bool> Announce;
        public Action<System.Windows.Media.Media3D.Point3D,System.Windows.Media.Media3D.Vector3D> WallImpact;
        public void Say(string text,double seconds){Message=text;MessageLeft=seconds;}
        public static double Clamp(double v,double a,double b){return Math.Max(a,Math.Min(b,v));}
        public static double Wrap(double a){return Math.Atan2(Math.Sin(a),Math.Cos(a));}
        public double Distance(Target t){return Math.Sqrt((t.X-X)*(t.X-X)+(t.Z-Z)*(t.Z-Z));}
        public void Reset(bool practice)
        {
            Targets.Clear();X=Z=Yaw=Pitch=Clock=0;Health=Tuning.PlayerHealth;ExecuteCd=BlockCd=BlockLeft=DashCd=Invulnerable=DashLeft=0;
            safeX=safeY=safeZ=0;
            ActionAge=0;Action=Move.Idle;Slash=true;Practice=practice;Paused=false;Kills=0;Wave=1;JumpY=JumpSpeed=0;Crouching=false;EyeHeight=1.66;LastDamage=0;airTuckUsed=airTucked=false;KillNotice=KillBurst=0;LastKillExecution=false;LastVictim=-1;DamageFlash=HitFlash=0;
            if(practice)
            {
                Targets.Add(new Target{Id=0,X=0,Z=4.3,Health=550,MaxHealth=2200,Dummy=true});
                Targets.Add(new Target{Id=1,X=-3.1,Z=6.5,Health=1100,MaxHealth=2200,Dummy=true,Phase=1});
                Targets.Add(new Target{Id=2,X=3.1,Z=6.5,Health=2200,MaxHealth=2200,Dummy=true,Phase=2});
            }else Spawn();
            Say(practice?"正前方 550 血目标：左键试一闪":"小红正在接近",3);
        }
        void Spawn()
        {
            int count=Math.Min(4+Wave,12);
            for(int i=0;i<count;i++){double a=i*Math.PI*2/count;double x=Math.Sin(a)*8,z=Math.Cos(a)*7;if(Map!=null&&!Map.Clear(x,0,z,1.95,.25)){x=i*.7-3;z=-1;}Targets.Add(new Target{Id=Targets.Count,X=x,Z=z,Phase=i*.7,Health=1650+550*(Wave/3),MaxHealth=1650+550*(Wave/3)});}
        }
        void Play(string key){if(Sound!=null&&!Muted)Sound(key);}
        public bool Switch()
        {
            if(Paused||Health<=0)return false;
            // Stance is immediate input, not an attack-animation lock. Cancel any
            // uncommitted contact; already applied damage and cooldowns stay spent.
            if(Action==Move.Execute)Invulnerable=Math.Min(Invulnerable,DashLeft>0?DashLeft+.03:0);
            executionTarget=null;
            Slash=!Slash;BlockLeft=0;Begin(Move.Switch,.38,2);Play("switch");return true;
        }
        public Target ExecutionTarget()
        {
            // "一闪" belongs to the swordsman, not to only one stance. In guard
            // stance the normal left-click thrust is replaced by execution too.
            if(ExecuteCd>0||BlockLeft>0)return null;
            Target best=null;double bestScore=100;
            foreach(var t in Targets)
            {
                if(t.Dead||t.Health>Tuning.ExecuteHealth)continue;
                double d=Distance(t),a=Math.Abs(Wrap(Math.Atan2(t.X-X,t.Z-Z)-Yaw));
                double vertical=Math.Atan2(t.Y+1.12-EyeHeight-JumpY,Math.Max(d,.01));
                if(d>Tuning.ExecuteRange||a>.23||Math.Abs(vertical-Pitch)>.33)continue;
                if(Map!=null&&!Map.Sight(X,JumpY+EyeHeight,Z,t.X,t.Y+1.12,t.Z))continue;
                double score=d+a*10;if(score<bestScore){best=t;bestScore=score;}
            }
            return best;
        }
        void Begin(Move a,double duration,double hit){Action=a;ActionAge=0;ActionDuration=duration;HitAt=hit;HasHit=false;}
        public bool Attack(bool right)
        {
            if(Paused||Health<=0)return false;
            if(right&&!Slash)return Block();
            if((Action!=Move.Idle&&Action!=Move.Switch)||BlockLeft>0)return false;
            executionTarget=!right?ExecutionTarget():null;
            if(executionTarget!=null){Begin(Move.Execute,.62,.16);ExecuteCd=Tuning.ExecuteCooldown;Invulnerable=.62;Play("execute");Say("一闪",.8);return true;}
            if(right){Begin(Move.Heavy,.85,.34);Play("heavy");}
            else if(Slash){Begin(Move.Light,.43,.15);Play("light");}
            else{Begin(Move.Thrust,.55,.20);Play("thrust");}
            return true;
        }
        public bool Block()
        {
            if(Paused||Health<=0||Slash||(Action!=Move.Idle&&Action!=Move.Switch)||BlockCd>0)return false;
            BlockLeft=Tuning.BlockDuration;BlockCd=Tuning.BlockCooldown;Play("block");return true;
        }
        public bool Dash(double forward,double side)
        {
            if(Paused||Health<=0)return false;
            if(Slash){double floor=Map==null?0:Map.Floor(X,Z,JumpY+.02);if(JumpY<=floor+.025&&JumpSpeed<=0){JumpSpeed=6.6;airTuckUsed=airTucked=false;return true;}return false;}
            if(DashCd>0||Action==Move.Execute)return false;
            double len=Math.Sqrt(forward*forward+side*side);if(len<.01){forward=1;len=1;}
            forward/=len;side/=len;
            dashX=Math.Sin(Yaw)*forward-Math.Cos(Yaw)*side;dashZ=Math.Cos(Yaw)*forward+Math.Sin(Yaw)*side;
            DashLeft=.13;DashCd=Tuning.DashCooldown;Invulnerable=Math.Max(Invulnerable,.16);Play("dash");return true;
        }
        public bool Receive(Target from,double damage)
        {
            if(Invulnerable>0||DashLeft>0)return false;
            double angle=Math.Abs(Wrap(Math.Atan2(from.X-X,from.Z-Z)-Yaw));
            if(!Slash&&BlockLeft>0&&angle<Math.PI*.48){Play("clang");Say("格挡成功",.45);return false;}
            Health=Math.Max(0,Health-damage);DamageFlash=.30;Play("hurt");return true;
        }
        public void Tick(double dt,double forward,double side,bool crouch=false)
        {
            if(Paused||Health<=0)return;
            Clock+=dt;ExecuteCd=Math.Max(0,ExecuteCd-dt);BlockCd=Math.Max(0,BlockCd-dt);BlockLeft=Math.Max(0,BlockLeft-dt);DashCd=Math.Max(0,DashCd-dt);Invulnerable=Math.Max(0,Invulnerable-dt);
            DamageFlash=Math.Max(0,DamageFlash-dt);HitFlash=Math.Max(0,HitFlash-dt);MessageLeft=Math.Max(0,MessageLeft-dt);
            KillNotice=Math.Max(0,KillNotice-dt);KillBurst=Math.Max(0,KillBurst-dt);
            double support=Map==null?0:Map.Floor(X,Z,JumpY+.025);
            bool airborne=JumpSpeed>0||JumpY>support+.03;
            if(!airborne){airTuckUsed=airTucked=false;}
            // Air crouch raises the feet, not the head, and adds no upward velocity.
            if(crouch&&!Crouching&&airborne&&!airTuckUsed&&(Map==null||Map.Clear(X,JumpY+.58,Z,1.18,.24)))
            {JumpY+=.58;EyeHeight-=.58;airTuckUsed=airTucked=true;}
            if(!crouch&&airTucked&&(Map==null||Map.Clear(X,JumpY-.58,Z,1.8,.24)))
            {JumpY-=.58;EyeHeight+=.58;airTucked=false;}
            Crouching=crouch||airTucked||(Map!=null&&!Map.Clear(X,JumpY,Z,1.8,.24));
            EyeHeight+=((Crouching?1.02:1.66)-EyeHeight)*Math.Min(1,dt*16);
            double bodyHeight=Crouching?1.18:1.8;
            if(Map!=null&&!Map.Resolve(ref X,ref JumpY,ref Z,bodyHeight)){X=safeX;JumpY=safeY;Z=safeZ;JumpSpeed=0;}
            if(JumpY>0||JumpSpeed>0)
            {
                double oldY=JumpY;JumpSpeed-=dt*14;double next=JumpY+JumpSpeed*dt;
                double floor=Map==null?0:Map.Floor(X,Z,oldY+.025);
                if(next<=floor){next=floor;JumpSpeed=0;}
                if(Map!=null&&next>oldY&&!Map.Clear(X,next,Z,bodyHeight,.24)){next=oldY;JumpSpeed=0;}
                JumpY=next;
            }
            if(DashLeft>0){double step=Math.Min(DashLeft,dt);MovePlayer(dashX*Tuning.DashDistance*step/.13,dashZ*Tuning.DashDistance*step/.13,bodyHeight);DashLeft-=step;}
            else if(Action!=Move.Execute)
            {
                double len=Math.Sqrt(forward*forward+side*side);
                if(len>0){forward/=len;side/=len;double speed=(Slash?Tuning.SlashSpeed:Tuning.GuardSpeed)*(Crouching&&!airborne?.46:1);MovePlayer((Math.Sin(Yaw)*forward-Math.Cos(Yaw)*side)*speed*dt,(Math.Cos(Yaw)*forward+Math.Sin(Yaw)*side)*speed*dt,bodyHeight);WalkTime+=dt*speed;}
            }
            X=Clamp(X,-Tuning.Arena,Tuning.Arena);Z=Clamp(Z,-Tuning.Arena,Tuning.Arena);
            if(Map!=null&&Map.Clear(X,JumpY,Z,bodyHeight,.24)){safeX=X;safeY=JumpY;safeZ=Z;}
            if(Action!=Move.Idle)
            {
                ActionAge+=dt;
                if(!HasHit&&ActionAge>=HitAt){HasHit=true;Hit();}
                if(ActionAge>=ActionDuration)Action=Move.Idle;
            }
            int living=0;
            if(!Practice&&Map!=null){if(navigationMap!=Map){navigation=new Navigation(Map);navigationMap=Map;}navigation.Update(dt,X,JumpY,Z);}
            foreach(var t in Targets)
            {
                t.Hurt=Math.Max(0,t.Hurt-dt);
                if(t.Dead){t.DeathTime+=dt;continue;}living++;
                if(t.Dummy)continue;
                double d=Math.Max(.001,Distance(t));
                t.ThinkTime-=dt;
                if(t.ThinkTime<=0){t.ThinkTime=.22+(t.Id%3)*.025;t.AimX=X;t.AimZ=Z;if(navigation!=null&&navigationMap==Map)navigation.Aim(t,X,JumpY,Z,out t.AimX,out t.AimZ);}
                if(d>1.18||Math.Abs(t.Y-JumpY)>.5){double dx=t.AimX-t.X,dz=t.AimZ-t.Z,len=Math.Sqrt(dx*dx+dz*dz);if(len>.02){double step=Math.Min(len,(t.Hurt>0?.75:1.85)*dt);dx=dx/len*step;dz=dz/len*step;if(Map==null){t.X+=dx;t.Z+=dz;}else{double climb=Map.Floor(t.X+dx,t.Z+dz,t.Y+.225,.24);if(climb>t.Y&&climb-t.Y<=.225)t.Y=climb;Map.Move(ref t.X,ref t.Y,ref t.Z,dx,dz,1.95);double floor=Map.Floor(t.X,t.Z,t.Y+.025);t.Y=Math.Max(floor,t.Y-5*dt);}}}
                d=Distance(t);
                t.AttackTimer-=dt;
                if(d<1.65&&Math.Abs(t.Y-JumpY)<1.4&&t.AttackTimer<=0&&(Map==null||Map.Sight(t.X,t.Y+1.3,t.Z,X,JumpY+EyeHeight,Z))){Receive(t,105);t.AttackTimer=1.2;}
            }
            // Separation prevents all bodies from collapsing into one point.
            for(int i=0;i<Targets.Count;i++)for(int j=i+1;j<Targets.Count;j++)
            {
                var a=Targets[i];var b=Targets[j];if(a.Dead||b.Dead||a.Dummy||b.Dummy)continue;
                double dx=a.X-b.X,dz=a.Z-b.Z,d=Math.Sqrt(dx*dx+dz*dz);
                if(Math.Abs(a.Y-b.Y)<1&&d<.95){if(d<.001){double angle=(i+1)*2.4;dx=Math.Cos(angle);dz=Math.Sin(angle);d=1;}double k=Math.Min(.035,Math.Max(.015,(.95-d)*.5));if(Map==null){a.X+=dx/d*k;a.Z+=dz/d*k;b.X-=dx/d*k;b.Z-=dz/d*k;}else{Map.Move(ref a.X,ref a.Y,ref a.Z,dx/d*k,dz/d*k,1.95);Map.Move(ref b.X,ref b.Y,ref b.Z,-dx/d*k,-dz/d*k,1.95);}}
            }
            if(!Practice&&living==0){waveDelay+=dt;if(waveDelay>2){Wave++;waveDelay=0;Spawn();Health=Math.Min(Tuning.PlayerHealth,Health+180);}}
        }
        void MovePlayer(double dx,double dz,double height)
        {
            if(Map==null){X+=dx;Z+=dz;return;}
            double floor=Map.Floor(X,Z,JumpY+.025,.24);
            bool grounded=JumpSpeed<=0&&JumpY<=floor+.025;
            Map.Move(ref X,ref JumpY,ref Z,dx,dz,height,grounded?.22:0);
        }
        void Hit()
        {
            if(Action==Move.Execute)
            {
                if(executionTarget==null||executionTarget.Dead)return;
                if(Map!=null&&(!Map.Sight(X,JumpY+EyeHeight,Z,executionTarget.X,executionTarget.Y+1.12,executionTarget.Z)||!Map.Clear(executionTarget.X,executionTarget.Y,executionTarget.Z,Crouching?1.18:1.8,.24)))return;
                X=Clamp(executionTarget.X,-Tuning.Arena,Tuning.Arena);Z=Clamp(executionTarget.Z,-Tuning.Arena,Tuning.Arena);JumpY=executionTarget.Y;JumpSpeed=0;LastDamage=executionTarget.Health;Play("execute-hit");Kill(executionTarget,true);return;
            }
            if(Action==Move.Switch)return;
            double range=Action==Move.Thrust?2.9:Action==Move.Heavy?2.7:2.45;
            double cone=Action==Move.Thrust?.19:Action==Move.Heavy?.75:.50;
            Target best=null;double distance=100;
            foreach(var t in Targets)
            {
                if(t.Dead)continue;
                double d=Distance(t),a=Math.Abs(Wrap(Math.Atan2(t.X-X,t.Z-Z)-Yaw));
                double vertical=Math.Atan2(t.Y+1.15-EyeHeight-JumpY,Math.Max(d,.01));
                if(Map!=null&&!Map.Sight(X,JumpY+EyeHeight,Z,t.X,t.Y+1.15,t.Z))continue;
                if(d<=range&&a<=cone&&Math.Abs(vertical-Pitch)<.65&&d<distance){best=t;distance=d;}
            }
            if(best==null)
            {
                // Rooftop mascots react to real blade contact, using the same
                // range, facing and vertical aim rules as an enemy strike.
                double[] mascotX={-3.1,0,3.1};
                for(int i=0;i<mascotX.Length;i++)
                {
                    double dx=mascotX[i]-X,dz=10.75-Z,d=Math.Sqrt(dx*dx+dz*dz);
                    double a=Math.Abs(Wrap(Math.Atan2(dx,dz)-Yaw));
                    double vertical=Math.Atan2(5.65-EyeHeight-JumpY,Math.Max(d,.01));
                    if(d<=range+.45&&a<=cone+.12&&Math.Abs(vertical-Pitch)<.72)
                    {HitFlash=.18;LastDamage=0;Play("impact");if(MascotImpact!=null)MascotImpact(i);return;}
                }
                System.Windows.Media.Media3D.Point3D point;System.Windows.Media.Media3D.Vector3D normal;
                if(Map!=null&&Map.SurfaceHit(X,JumpY+EyeHeight,Z,Math.Sin(Yaw)*Math.Cos(Pitch),Math.Sin(Pitch),Math.Cos(Yaw)*Math.Cos(Pitch),range,out point,out normal))
                {Play("clang");if(WallImpact!=null)WallImpact(point,normal);}return;
            }
            LastDamage=Action==Move.Heavy?Tuning.HeavyDamage:Action==Move.Thrust?Tuning.ThrustDamage:Tuning.LightDamage;
            best.Health-=LastDamage;
            best.Hurt=.24;HitFlash=.22;Play(Action==Move.Heavy?"heavy-hit":Action==Move.Thrust?"thrust-hit":"impact");
            if(best.Health<=0)Kill(best,false);else if(Impact!=null)Impact(best,false);
        }
        void Kill(Target t,bool execution)
        {if(t.Dead)return;t.Health=0;t.Dead=true;t.DeathTime=0;Kills++;HitFlash=.22;KillNotice=1.8;KillBurst=.32;LastKillExecution=execution;LastVictim=t.Id;bool voiced=!Muted&&Announce!=null&&Announce(Kills);if(!voiced)Play(execution?"execute-kill":"kill");if(Impact!=null)Impact(t,execution);}
    }
}
