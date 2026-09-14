using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GhostBlade3D
{
    // Clean-room, procedurally authored infected nurse inspired by the supplied
    // CrossFire reference. No extracted mesh or texture data is used here.
    sealed class TerminatorRig
    {
        public readonly Model3DGroup Root=Models.Group(null),Body,LeftArm,RightArm,LeftLeg,RightLeg,LeftWing,RightWing;
        static readonly Material Skin=Models.Texture("#B8C4C1",true);
        static readonly Material SkinDark=Models.Texture("#849392",true);
        static readonly Material Uniform=Models.Texture("#F0EEE5",false);
        static readonly Material ClothShade=Models.Mat("#BCC7C3",5);
        static readonly Material Hair=Models.Mat("#F3F0DF",32);
        static readonly Material Blood=Models.Mat("#681B1C",20);
        static readonly Material Eye=Models.Glow("#70E9FF");
        static readonly Material Bio=Models.Glow("#08B9DD");
        static readonly Material Dark=Models.Mat("#25292C",16);

        public TerminatorRig()
        {
            Body=Models.Group(Root);
            // A continuous, human silhouette comes first; the torn tunic is a
            // thin outer layer instead of a collection of rectangular blocks.
            Shell(Body,SkinDark,0,1.28,.015,.285,.48,.190);
            Shell(Body,Uniform,0,1.31,-.028,.292,.40,.196);
            Shell(Body,Uniform,0,.91,-.012,.305,.24,.205);
            Models.Plate(Body,ClothShade,new[]{new Point(-.235,.80),new Point(.235,.80),new Point(.19,1.46),new Point(-.18,1.51)},-.174,.008);
            for(int i=0;i<7;i++)
            {
                double x=-.22+i*.073;double drop=.10+(i%3)*.055;
                Shard(Body,Uniform,new Point3D(x,.87,-.17),new Point3D(x+.018,.67-drop,-.15),.050,.014);
            }
            // Blood stains and torn seams.
            Shard(Body,Blood,new Point3D(-.10,1.48,-.184),new Point3D(-.04,1.25,-.188),.047,.006);
            Shard(Body,Blood,new Point3D(.02,1.06,-.183),new Point3D(.14,.78,-.177),.055,.006);
            Shard(Body,Blood,new Point3D(-.16,.92,-.181),new Point3D(-.12,.72,-.172),.040,.006);
            for(int i=0;i<5;i++)Models.Tube(Body,ClothShade,new Point3D(-.20+i*.10,1.05,-.187),new Point3D(-.17+i*.095,.99,-.190),.004,.003,5);

            Models.Tube(Body,SkinDark,new Point3D(0,1.60,0),new Point3D(0,1.71,0),.073,.070,16);
            Models.Ball(Body,Skin,0,1.88,-.03,.145,.190,.125,18,26);
            // Human face: two luminous eyes, a small mouth and a single cheek scar.
            Models.Ball(Body,Dark,-.052,1.915,-.145,.031,.025,.010,10,14);
            Models.Ball(Body,Dark,.052,1.915,-.145,.031,.025,.010,10,14);
            Models.Ball(Body,Eye,-.052,1.915,-.158,.012,.010,.004,9,12);
            Models.Ball(Body,Eye,.052,1.915,-.158,.012,.010,.004,9,12);
            Models.Tube(Body,Blood,new Point3D(-.040,1.805,-.150),new Point3D(.040,1.805,-.150),.007,.005,8);
            Models.Tube(Body,Blood,new Point3D(.080,1.985,-.151),new Point3D(.120,1.845,-.156),.010,.004,8);
            Models.Tube(Body,Bio,new Point3D(-.145,1.72,.01),new Point3D(-.175,2.04,.025),.009,.006,7);
            Models.Tube(Body,Bio,new Point3D(.145,1.72,.01),new Point3D(.175,2.04,.025),.009,.006,7);

            BuildHairAndCap();
            LeftArm=Arm(-1);RightArm=Arm(1);Body.Children.Add(LeftArm);Body.Children.Add(RightArm);
            LeftLeg=Leg(-1);RightLeg=Leg(1);Root.Children.Add(LeftLeg);Root.Children.Add(RightLeg);
            // Compatibility anchors preserve the existing articulated interface.
            LeftWing=Models.Group(Body);RightWing=Models.Group(Body);
            Models.Batch(Root);Animate(0,0,0);
        }

        void BuildHairAndCap()
        {
            Models.Ball(Body,Hair,0,1.97,.055,.155,.175,.125,16,22);
            for(int i=0;i<9;i++)
            {
                double x=-.15+i*.038;double length=.23+(i%3)*.055;
                Models.Tube(Body,Hair,new Point3D(x,1.98,.075),new Point3D(x*.95,1.76-length,.105),.027,.010,8);
            }
            Models.Plate(Body,Uniform,new[]{new Point(-.105,2.005),new Point(-.075,2.105),new Point(.075,2.105),new Point(.105,2.005)},-.115,.018);
            Models.Tube(Body,Blood,new Point3D(0,2.030,-.137),new Point3D(0,2.085,-.137),.010,.010,7);
            Models.Tube(Body,Blood,new Point3D(-.028,2.057,-.137),new Point3D(.028,2.057,-.137),.010,.010,7);
        }

        static void Shell(Model3DGroup parent,Material material,double x,double y,double z,double w,double h,double depth)
        {
            var mesh=new MeshGeometry3D();int rings=12,sides=18;
            for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
            {
                double t=(double)j/rings,a=2*Math.PI*i/sides,r=Math.Sin(Math.PI*t)*.82+.10;
                mesh.Positions.Add(new Point3D(x+Math.Cos(a)*w*r,y+(t-.5)*h*2,z+Math.Sin(a)*depth*r));
                mesh.TextureCoordinates.Add(new Point((double)i/sides,1-t));
            }
            for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){int k=j*(sides+1)+i;Models.Tri(mesh,k,k+1,k+sides+1);Models.Tri(mesh,k+1,k+sides+2,k+sides+1);}
            mesh.Freeze();parent.Children.Add(Models.Geometry(mesh,material));
        }

        static void Shard(Model3DGroup parent,Material material,Point3D start,Point3D end,double width,double depth)
        {
            Vector3D axis=end-start;axis.Normalize();var side=Vector3D.CrossProduct(axis,new Vector3D(0,0,1));if(side.Length<.01)side=new Vector3D(1,0,0);side.Normalize();
            var center=start+(end-start)*.35;var front=center+new Vector3D(0,0,-depth);var back=center+new Vector3D(0,0,depth);
            var mesh=new MeshGeometry3D();mesh.Positions=new Point3DCollection(new[]{start,center+side*width,end,center-side*width,front,back});
            mesh.TextureCoordinates=new PointCollection(new[]{new Point(.5,0),new Point(0,.35),new Point(.5,1),new Point(1,.35),new Point(.5,.35),new Point(.5,.35)});
            for(int i=0;i<4;i++){Models.Tri(mesh,i,(i+1)%4,4);Models.Tri(mesh,(i+1)%4,i,5);}mesh.Freeze();parent.Children.Add(Models.Geometry(mesh,material));
        }

        Model3DGroup Arm(int side)
        {
            var arm=Models.Group(null);
            Models.Tube(arm,Uniform,new Point3D(0,.01,0),new Point3D(0,-.25,0),.110,.075,16);
            Models.Tube(arm,Skin,new Point3D(0,-.23,0),new Point3D(0,-.69,-.025),.069,.052,16);
            for(int i=0;i<3;i++)Models.Tube(arm,Bio,new Point3D((i-1)*.022,-.31,-.064),new Point3D((1-i)*.018,-.66,-.075),.005,.002,6);
            Models.Ball(arm,Skin,0,-.76,-.045,.072,.066,.055,14,20);
            for(int i=0;i<4;i++)
            {
                double x=-.052+i*.035;
                Models.Tube(arm,SkinDark,new Point3D(x,-.79,-.070),new Point3D(x*1.18,-.92,-.145),.017,.010,9);
                Models.Tube(arm,Bio,new Point3D(x*1.18,-.92,-.145),new Point3D(x*1.32,-.99,-.24),.009,.001,7);
            }
            return arm;
        }

        Model3DGroup Leg(int side)
        {
            var leg=Models.Group(null);
            Models.Tube(leg,Skin,new Point3D(0,-.06,0),new Point3D(0,-.48,0),.105,.078,16);
            Models.Tube(leg,Skin,new Point3D(0,-.45,0),new Point3D(0,-.84,-.035),.070,.055,16);
            Models.Tube(leg,Bio,new Point3D(side*.022,-.31,-.074),new Point3D(-side*.016,-.79,-.085),.005,.002,6);
            Models.Ball(leg,Dark,0,-.91,-.125,.100,.055,.185,14,20);
            for(int i=0;i<3;i++)Models.Tube(leg,Bio,new Point3D(-.040+i*.040,-.90,-.155),new Point3D(-.047+i*.046,-.92,-.245),.007,.001,7);
            return leg;
        }

        public void Animate(double time,double attack,double hurt)
        {
            double walk=Math.Sin(time*7),breath=Math.Sin(time*2.2);
            Models.Pose(Body,0,Math.Abs(walk)*.012,0,4+hurt*13,0,walk*.8);
            Models.Pose(LeftLeg,-.115,.91,0,walk*24,0,-2);Models.Pose(RightLeg,.115,.91,0,-walk*24,0,2);
            Models.Pose(LeftArm,-.255,1.49,0,10-walk*17+attack*72,0,-8-breath*2);
            Models.Pose(RightArm,.255,1.49,0,10+walk*17+attack*86,0,8+breath*2);
        }
    }
}
