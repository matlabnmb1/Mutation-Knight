using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

namespace GhostBlade3D
{
    public sealed class MapBlock
    {
        public double X,Z,W,D,Y,H;public string Color;public bool Solid;
        public bool Contains(double x,double z,double radius)
        {return x>X-W/2-radius&&x<X+W/2+radius&&z>Z-D/2-radius&&z<Z+D/2+radius;}
    }
    // Authored playable reconstruction from PC New Silent Village references.
    // Shared geometry is the source of truth for collision, support and rendering.
    public sealed class Village
    {
        public readonly List<MapBlock> Blocks=new List<MapBlock>();
        public readonly List<ClimbRoute> Routes=new List<ClimbRoute>();
        public sealed class ClimbRoute {public string Name;public double X,Z,EndZ,Height,RoofX,RoofZ;}
        public Village(bool legacy=false)
        {
            if(!legacy){Parkour();return;}
            Add(0,0,49,49,-.3,.3,"#D9D5CA",false);
            Add(-24.5,0,1,50,0,9,"#69655D");Add(24.5,0,1,50,0,9,"#69655D");
            Add(0,24.5,50,1,0,9,"#69655D");Add(0,-24.5,50,1,0,9,"#69655D");
            // Central ruined house: open ground floor, stairs, traversable roof.
            Add(-4,12,9,7,0,.25,"#A59A83");
            Add(-8.3,12,.4,7,0,3.1,"#B2A388");Add(.3,12,.4,7,0,3.1,"#B2A388");
            Add(-4,15.3,9,.4,0,3.1,"#A4967E");
            Add(-7,8.6,2.7,.35,0,3.1,"#B2A388");Add(-1,8.6,2.7,.35,0,3.1,"#B2A388");
            Add(-4,8.6,3.3,.35,2.15,.95,"#B2A388");
            Add(-4,12,9.5,7.5,3.1,.25,"#756F63");
            Stairs(2.2,8.2,3,7.5,3.35);
            Add(-4,15.5,9.5,.25,3.35,.75,"#9F947D");
            Add(-8.6,12,.25,7,3.35,.7,"#9F947D");
            // Flower-shop long gallery / rear street, accessible via broad steps.
            Building(-17,10,8,13,5.4,"#8D8074");
            Add(-11.6,10,2.7,13,2.5,.25,"#7A7468");
            for(int i=0;i<4;i++)Add(-10.4,4.1+i*3.8,.18,.18,0,4.3,"#595C58");
            Add(-11.6,10,3,13,4.2,.18,"#596160");
            Stairs(-11.6,-.1,2.7,5.5,2.75);
            // Clock tower and its stepped defensive approach on the opposite side.
            Building(16,15,6,6,8,"#8E8877");
            Add(16,15,6.5,6.5,8,.3,"#565A56");
            Add(16,15,4.4,4.4,8.3,2,"#B3A68A");
            Add(16,15,5,5,10.3,.3,"#5D6059");
            Add(11,12,3.8,7,3.4,.25,"#8D8370");Stairs(11,4.5,3.8,8,3.65);
            // Back street / two-storey houses and rooftop escape route.
            Building(-16,-16,10,9,5.6,"#A49680");
            Building(16,-16,10,9,5.0,"#968C7A");
            Building(17,0,7,9,4.6,"#9D927D");
            Add(0,-18,13,5,0,2.4,"#8D8577");Stairs(-4,-23,3,5,2.4);
            // Low passage (Ctrl crouch), with open ends.
            Add(6,-9,4,3,1.32,.28,"#696D66");
            Add(3.85,-9,.3,3,0,1.6,"#8E8879");Add(8.15,-9,.3,3,0,1.6,"#8E8879");
            Crate(-5,-5,1.1);Crate(-7,-5,1.7);Crate(-8.5,-4,2.3);
            Crate(7,17,1.1);Crate(8,18.8,1.9);
            // Paving seams and gutters are visual only.
            for(int i=-22;i<=22;i+=2){Add(i,0,.018,46,.004,.008,"#756F63",false);Add(0,i,46,.018,.004,.008,"#756F63",false);}
        }
        void Parkour()
        {
            // An open, super-flat training field with one climbable ancient hall.
            Add(0,0,120,120,-.3,.3,"#D9D5CA",false);
            Building(0,14,12,8,4,"#F1E8D8");
            Route("古楼屋顶",-7,4,14,4.22,0,14);
            for(int i=-56;i<=56;i+=4){Add(i,0,.012,116,.004,.006,"#AAA69D",false);Add(0,i,116,.012,.004,.006,"#AAA69D",false);}
        }
        void Route(string name,double x,double startZ,double endZ,double height,double roofX,double roofZ)
        {
            double length=endZ-startZ;int steps=(int)Math.Ceiling(height/.18);
            const double stairWidth=3.6;
            for(int i=0;i<steps;i++)Add(x,startZ+(i+.5)*length/steps+(i==steps-1?.1:0),stairWidth,length/steps+(i==steps-1?.2:0),0,height*(i+1)/steps,i%2==0?"#E8DDC8":"#D8CCB7");
            // Full-height landing overlaps roof and last tread; no invisible final gap.
            double edge=roofX;foreach(var b in Blocks)if(b.Solid&&Math.Abs(b.Y+b.H-height)<.001&&b.Contains(roofX,roofZ,0))edge=x<roofX?b.X-b.W/2:b.X+b.W/2;
            double stairEdge=x+(x<roofX?stairWidth/2:-stairWidth/2);
            if(Math.Abs(edge-stairEdge)>.001)Add((stairEdge+edge)/2,endZ,Math.Abs(edge-stairEdge),.4,height-.18,.18,"#E8DDC8");
            Routes.Add(new ClimbRoute{Name=name,X=x,Z=startZ-.5,EndZ=endZ,Height=height,RoofX=roofX,RoofZ=roofZ});
        }
        void Add(double x,double z,double w,double d,double y,double h,string color,bool solid=true)
        {Blocks.Add(new MapBlock{X=x,Z=z,W=w,D=d,Y=y,H=h,Color=color,Solid=solid});}
        void Stairs(double x,double start,double w,double length,double height)
        {int steps=(int)Math.Ceiling(height/.20);for(int i=0;i<steps;i++)Add(x,start+(i+.5)*length/steps,w,length/steps,0,height*(i+1)/steps,i%2==0?"#E8DDC8":"#D8CCB7");}
        void Crate(double x,double z,double size)
        {Add(x,z,size,size,0,size,"#B67B49");for(int i=0;i<3;i++)Add(x,z,size+.03,size+.03,.15+i*(size-.3)/2,.065,"#6D5943",false);}
        void Building(double x,double z,double w,double d,double height,string color)
        {
            Add(x,z,w,d,0,height,color);Add(x,z,w+.34,d+.34,height,.22,"#2F8792");
            for(int floor=0;floor<2;floor++)for(int i=0;i<3;i++)
            {
                double wx=x-w*.32+i*w*.32,wy=1.0+floor*2.4;
                if(wy+1.2>height-.05)continue;
                Add(wx,z-d/2-.04,.95,.08,wy,1.2,"#376A78",false);
                Add(wx,z-d/2-.09,1.12,.12,wy-.08,.09,"#FFF0C8",false);
                Add(wx,z-d/2-.09,.045,.10,wy,1.2,"#54A5AE",false);
                Add(wx,z+d/2+.04,.95,.08,wy,1.2,"#376A78",false);
            }
            for(int i=0;i<(int)height;i++)Add(x,z,w+.02,d+.02,i,.025,"#C39E7B",false);
        }
        public double Floor(double x,double z,double limit,double radius=0)
        {double result=0;foreach(var b in Blocks)if(b.Solid&&b.Contains(x,z,radius)&&b.Y+b.H<=limit+.001)result=Math.Max(result,b.Y+b.H);return result;}
        public bool Clear(double x,double y,double z,double height,double radius)
        {if(Math.Abs(x)>59.5||Math.Abs(z)>59.5)return false;foreach(var b in Blocks)if(b.Solid&&b.Contains(x,z,radius)&&y<b.Y+b.H-.015&&y+height>b.Y+.015)return false;return true;}
        public void Move(ref double x,ref double y,ref double z,double dx,double dz,double height,double stepHeight=.22)
        {
            Resolve(ref x,ref y,ref z,height);
            int steps=Math.Max(1,(int)Math.Ceiling(Math.Sqrt(dx*dx+dz*dz)/.09));
            for(int i=0;i<steps;i++)
            {
                double nx=x+dx/steps,ny=Math.Max(y,Floor(nx,z,y+stepHeight,.24));
                if(Clear(nx,ny,z,height,.24)){x=nx;y=ny;}
                double nz=z+dz/steps;ny=Math.Max(y,Floor(x,nz,y+stepHeight,.24));
                if(Clear(x,ny,nz,height,.24)){z=nz;y=ny;}
            }
        }
        public bool Resolve(ref double x,ref double y,ref double z,double height)
        {
            x=Combat.Clamp(x,-59.4,59.4);z=Combat.Clamp(z,-59.4,59.4);
            if(Clear(x,y,z,height,.24))return true;
            double best=double.MaxValue,bx=x,by=y,bz=z;
            foreach(var b in Blocks)
            {
                if(!b.Solid||!b.Contains(x,z,.24)||y>=b.Y+b.H-.015||y+height<=b.Y+.015)continue;
                double[,] candidates={{b.X-b.W/2-.251,y,z},{b.X+b.W/2+.251,y,z},{x,y,b.Z-b.D/2-.251},{x,y,b.Z+b.D/2+.251},{x,b.Y+b.H,z}};
                for(int i=0;i<5;i++){double cx=candidates[i,0],cy=candidates[i,1],cz=candidates[i,2],distance=(cx-x)*(cx-x)+(cy-y)*(cy-y)+(cz-z)*(cz-z);if(distance<best&&Clear(cx,cy,cz,height,.24)){best=distance;bx=cx;by=cy;bz=cz;}}
            }
            if(best==double.MaxValue)return false;x=bx;y=by;z=bz;return true;
        }
        public bool SurfaceHit(double x,double y,double z,double dx,double dy,double dz,double range,out Point3D hit,out Vector3D normal)
        {
            hit=new Point3D();normal=new Vector3D();
            for(double distance=.05;distance<=range;distance+=.035)
            {
                double px=x+dx*distance,py=y+dy*distance,pz=z+dz*distance;
                foreach(var b in Blocks)if((b.Solid||(b.Y>0&&b.H>.15))&&b.Contains(px,pz,0)&&py>b.Y&&py<b.Y+b.H)
                {
                    double[] gaps={Math.Abs(px-(b.X-b.W/2)),Math.Abs(px-(b.X+b.W/2)),Math.Abs(pz-(b.Z-b.D/2)),Math.Abs(pz-(b.Z+b.D/2)),Math.Abs(py-b.Y),Math.Abs(py-b.Y-b.H)};
                    int side=0;for(int i=1;i<6;i++)if(gaps[i]<gaps[side])side=i;
                    Vector3D[] normals={new Vector3D(-1,0,0),new Vector3D(1,0,0),new Vector3D(0,0,-1),new Vector3D(0,0,1),new Vector3D(0,-1,0),new Vector3D(0,1,0)};normal=normals[side];
                    hit=new Point3D(px,py,pz)+normal*(gaps[side]+.008);return true;
                }
            }return false;
        }
        public bool Sight(double ax,double ay,double az,double bx,double by,double bz)
        {
            double distance=Math.Sqrt((ax-bx)*(ax-bx)+(az-bz)*(az-bz)+(ay-by)*(ay-by));int count=Math.Max(1,(int)(distance/.12));
            for(int i=1;i<count;i++){double t=(double)i/count;double x=ax+(bx-ax)*t,y=ay+(by-ay)*t,z=az+(bz-az)*t;foreach(var b in Blocks)if(b.Solid&&b.Contains(x,z,0)&&y>b.Y&&y<b.Y+b.H)return false;}return true;
        }
        public Model3DGroup Build()
        {
            var root=Models.Group(null);var mats=new Dictionary<string,Material>();
            foreach(var b in Blocks){Material mat;bool brick=b.Solid&&b.Color!="#9D9686"&&b.Color!="#938B7B";string key=b.Color+(brick?"brick":"plain");if(!mats.TryGetValue(key,out mat)){mat=brick?Masonry(b.Color):Models.Mat(b.Color,0);mats[key]=mat;}Box(root,mat,b.X,b.Y,b.Z,b.W,b.H,b.D);}
            Sign(root,"bilibili @matlabnmb",0,2.25,9.94,5.2,.58);
            // Visual-only atmosphere keeps the tested collision and climb routes intact.
            Material eave=Models.Mat("#176F7B",25),lamp=Models.Glow("#FFF1A6"),frame=Models.Mat("#8C3C2E",8);
            // Layered upturned eaves give the single hall a recognizable silhouette.
            Box(root,eave,0,4.24,14,13.2,.18,9.2);Box(root,Models.Mat("#C95F45",18),0,4.43,14,11.8,.14,7.8);
            Box(root,eave,0,4.59,14,10.4,.12,6.5);
            double[,] lamps={{-4.5,2.25,9.75},{4.5,2.25,9.75}};
            for(int i=0;i<lamps.GetLength(0);i++){double x=lamps[i,0],y=lamps[i,1],z=lamps[i,2];Models.Tube(root,frame,new Point3D(x,y+.28,z),new Point3D(x,y+.58,z),.035,.025,6);Models.Ball(root,lamp,x,y,z,.12,.18,.12,6,10);root.Children.Add(new PointLight(Color.FromRgb(255,166,82),new Point3D(x,y,z)){Range=7,ConstantAttenuation=1,LinearAttenuation=.35});}
            RooftopMascots.Add(root);
            Models.Batch(root);root.Freeze();return root;
        }
        static void Box(Model3DGroup root,Material mat,double x,double y,double z,double w,double h,double d)
        {
            var mesh=new MeshGeometry3D();double l=x-w/2,r=x+w/2,f=z-d/2,b=z+d/2,t=y+h;
            Point3D[] p={new Point3D(l,y,f),new Point3D(r,y,f),new Point3D(r,t,f),new Point3D(l,t,f),new Point3D(l,y,b),new Point3D(r,y,b),new Point3D(r,t,b),new Point3D(l,t,b)};
            int[] faces={0,3,2,1,4,5,6,7,0,4,7,3,1,2,6,5,3,7,6,2,0,1,5,4};
            for(int face=0;face<6;face++)
            {
                int k=mesh.Positions.Count;for(int i=0;i<4;i++)mesh.Positions.Add(p[faces[face*4+i]]);
                double u=(p[faces[face*4+1]]-p[faces[face*4]]).Length/2,v=(p[faces[face*4+3]]-p[faces[face*4]]).Length/2;
                mesh.TextureCoordinates.Add(new Point(0,0));mesh.TextureCoordinates.Add(new Point(u,0));mesh.TextureCoordinates.Add(new Point(u,v));mesh.TextureCoordinates.Add(new Point(0,v));
                Models.Tri(mesh,k,k+1,k+2);Models.Tri(mesh,k,k+2,k+3);
            }
            mesh.Freeze();root.Children.Add(Models.Geometry(mesh,mat));
        }
        static Material Masonry(string hex)
        {
            var c=(Color)ColorConverter.ConvertFromString(hex);var visual=new DrawingVisual();var random=new Random(71);
            using(var dc=visual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb((byte)(c.R*.72),(byte)(c.G*.72),(byte)(c.B*.72))),null,new Rect(0,0,256,256));
                for(int row=0;row<8;row++)for(int col=-1;col<5;col++)
                {int shade=random.Next(-13,14);var color=Color.FromRgb((byte)Math.Max(0,Math.Min(255,c.R+shade)),(byte)Math.Max(0,Math.Min(255,c.G+shade)),(byte)Math.Max(0,Math.Min(255,c.B+shade)));dc.DrawRectangle(new SolidColorBrush(color),null,new Rect(col*64+(row%2)*32+1,row*32+1,62,30));}
                for(int i=0;i<1400;i++)dc.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)random.Next(8,35),35,29,20)),null,new Rect(random.Next(256),random.Next(256),random.Next(1,4),random.Next(1,3)));
            }
            var bitmap=new RenderTargetBitmap(256,256,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);bitmap.Freeze();
            var brush=new ImageBrush(bitmap){TileMode=TileMode.Tile,Viewport=new Rect(0,0,1,1),ViewportUnits=BrushMappingMode.Absolute};brush.Freeze();var mat=new DiffuseMaterial(brush);mat.Freeze();return mat;
        }
        static void Sign(Model3DGroup root,string text,double x,double y,double z,double width,double height)
        {
            var visual=new DrawingVisual();using(var dc=visual.RenderOpen()){dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(24,34,35)),null,new Rect(0,0,512,100));dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(224,176,57)),null,new Rect(0,0,7,100));var ft=new FormattedText(text,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),34,Brushes.White,1);dc.DrawText(ft,new Point((512-ft.Width)/2,27));}
            var bitmap=new RenderTargetBitmap(512,100,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);bitmap.Freeze();
            var mesh=new MeshGeometry3D();mesh.Positions=new Point3DCollection(new[]{new Point3D(x-width/2,y,z),new Point3D(x+width/2,y,z),new Point3D(x+width/2,y+height,z),new Point3D(x-width/2,y+height,z)});mesh.TextureCoordinates=new PointCollection(new[]{new Point(1,1),new Point(0,1),new Point(0,0),new Point(1,0)});Models.Tri(mesh,0,2,1);Models.Tri(mesh,0,3,2);root.Children.Add(Models.Geometry(mesh,new DiffuseMaterial(new ImageBrush(bitmap))));
        }
    }
}
