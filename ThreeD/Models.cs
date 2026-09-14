using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

namespace GhostBlade3D
{
    // Authored triangle meshes. All geometry lives in three-dimensional local space.
    static class Models
    {
        public static readonly Material Steel = Atlas(1,0,"#454E53",false);
        public static readonly Material Edge = Mat("#BCC8CC", 140);
        public static readonly Material Gold = Textured("#A98548",false);
        public static readonly Material DarkGold = Mat("#5A4225", 30);
        public static readonly Material Leather = Mat("#282628", 8);
        public static readonly Material Skin = Mat("#B99676", 8);
        public static readonly Material Red = Textured("#862E27",true);
        public static readonly Material Flesh = Textured("#B64B3D",true);
        public static readonly Material Bone = Mat("#C3B9A1", 20);
        public static readonly Material Black = Mat("#272627", 5);
        public static readonly Material Armor = Atlas(1,0,"#242932",false);
        public static readonly Material Glove = Atlas(1,1,"#791D2C",false);
        public static readonly Material HeroGlove = Textured("#9A6138",true);
        public static readonly Material Engraved = Atlas(0,1,"#303237",false);
        public static readonly Material Demon = Atlas(0,0,"#391012",true);
        static Material Atlas(int column,int row,string fallback,bool organic)
        {
            string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","combat-atlas-v1.png");
            if(!File.Exists(path))return Textured(fallback,organic);
            var source=new BitmapImage();source.BeginInit();source.CacheOption=BitmapCacheOption.OnLoad;source.UriSource=new Uri(path);source.EndInit();source.Freeze();
            int w=source.PixelWidth/2,h=source.PixelHeight/2;var crop=new CroppedBitmap(source,new Int32Rect(column*w,row*h,w,h));crop.Freeze();
            var brush=new ImageBrush(crop);brush.Freeze();var group=new MaterialGroup();group.Children.Add(new DiffuseMaterial(brush));group.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(organic?(byte)45:(byte)40,200,180,168)),organic?65:110));if(organic)group.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(27,3,2))));group.Freeze();return group;
        }
        public static readonly Material Silver = Mat("#AEB9C4",110);
        public static readonly Material Flame = Glow("#FFBA36");
        public static Material Glow(string hex)
        {var m=new EmissiveMaterial(new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)));m.Freeze();return m;}
        public static void Ring(Model3DGroup g,Material mat,double x,double y,double z,double radius,double thickness)
        {for(int i=0;i<48;i++){double a=i*Math.PI/24,b=(i+1)*Math.PI/24;Tube(g,mat,new Point3D(x+Math.Cos(a)*radius,y+Math.Sin(a)*radius,z),new Point3D(x+Math.Cos(b)*radius,y+Math.Sin(b)*radius,z),thickness,thickness,6);}}
        static readonly Dictionary<string, MeshGeometry3D> spheres = new Dictionary<string, MeshGeometry3D>();
        static Material Textured(string hex,bool skin)
        {
            var visual=new DrawingVisual();var rng=new Random(93);
            using(var d=visual.RenderOpen())
            {
                var c=(Color)ColorConverter.ConvertFromString(hex);d.DrawRectangle(new SolidColorBrush(c),null,new Rect(0,0,256,256));
                for(int i=0;i<1800;i++)
                {
                    int shade=rng.Next(-27,28);Color grain=Color.FromArgb((byte)rng.Next(30,115),(byte)Math.Max(0,Math.Min(255,c.R+shade)),(byte)Math.Max(0,Math.Min(255,c.G+shade)),(byte)Math.Max(0,Math.Min(255,c.B+shade)));
                    double x=rng.Next(256),y=rng.Next(256);
                    d.DrawLine(new Pen(new SolidColorBrush(grain),skin?.7:.4),new Point(x,y),new Point(x+(skin?rng.Next(-4,5):rng.Next(-2,3)),y+rng.Next(2,skin?25:65)));
                }
                for(int i=0;i<(skin?32:16);i++)
                {
                    double x=rng.Next(256),y=rng.Next(256);var pen=new Pen(new SolidColorBrush(Color.FromArgb(90,skin?(byte)43:(byte)220,skin?(byte)11:(byte)215,skin?(byte)10:(byte)200)),skin?1.6:.65);
                    d.DrawLine(pen,new Point(x,y),new Point(x+rng.Next(-15,16),y+30));
                }
            }
            var bmp=new RenderTargetBitmap(256,256,96,96,PixelFormats.Pbgra32);bmp.Render(visual);bmp.Freeze();
            var group=new MaterialGroup();group.Children.Add(new DiffuseMaterial(new ImageBrush(bmp)));
            group.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(skin?(byte)42:(byte)125,220,217,207)),skin?24:80));group.Freeze();return group;
        }
        public static Material Texture(string hex,bool organic){return Textured(hex,organic);}
        public static Material Mat(string hex, double shine)
        {
            Color c=(Color)ColorConverter.ConvertFromString(hex);
            var m=new MaterialGroup(); m.Children.Add(new DiffuseMaterial(new SolidColorBrush(c)));
            if(shine>0)m.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(100,220,224,225)),shine));
            m.Freeze();return m;
        }
        public static GeometryModel3D Geometry(MeshGeometry3D mesh, Material mat)
        { var g=new GeometryModel3D(mesh,mat); g.BackMaterial=mat; return g; }
        public static void Batch(Model3DGroup group)
        {
            var batches=new Dictionary<Material,MeshGeometry3D>();var remove=new List<Model3D>();
            foreach(var child in group.Children)
            {
                var nested=child as Model3DGroup;if(nested!=null){Batch(nested);continue;}
                var model=child as GeometryModel3D;if(model==null)continue;var source=model.Geometry as MeshGeometry3D;if(source==null)continue;
                MeshGeometry3D mesh;if(!batches.TryGetValue(model.Material,out mesh)){mesh=new MeshGeometry3D();batches[model.Material]=mesh;}
                int offset=mesh.Positions.Count;foreach(var point in source.Positions)mesh.Positions.Add(model.Transform.Transform(point));
                for(int i=0;i<source.Positions.Count;i++)mesh.TextureCoordinates.Add(i<source.TextureCoordinates.Count?source.TextureCoordinates[i]:new Point());
                foreach(int index in source.TriangleIndices)mesh.TriangleIndices.Add(offset+index);remove.Add(child);
            }
            foreach(var child in remove)group.Children.Remove(child);
            foreach(var pair in batches){pair.Value.Freeze();var model=Geometry(pair.Value,pair.Key);model.Freeze();group.Children.Add(model);}
        }
        public static Model3DGroup Group(Model3DGroup parent)
        { var g=new Model3DGroup();if(parent!=null)parent.Children.Add(g);return g; }
        public static void Pose(Model3DGroup g,double x,double y,double z,double rx,double ry,double rz)
        {
            var m=Matrix3D.Identity;
            m.Rotate(new Quaternion(new Vector3D(1,0,0),rx));m.Rotate(new Quaternion(new Vector3D(0,1,0),ry));m.Rotate(new Quaternion(new Vector3D(0,0,1),rz));m.Translate(new Vector3D(x,y,z));
            g.Transform=new MatrixTransform3D(m);
        }
        public static void Ball(Model3DGroup parent, Material material,double x,double y,double z,double sx,double sy,double sz,int rings=10,int sides=16)
        {
            string key=rings+":"+sides; MeshGeometry3D mesh;
            if(!spheres.TryGetValue(key,out mesh))
            {
                mesh=new MeshGeometry3D();
                for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
                {
                    double p=Math.PI*j/rings,a=Math.PI*2*i/sides;
                    var n=new Vector3D(Math.Sin(p)*Math.Cos(a),Math.Cos(p),Math.Sin(p)*Math.Sin(a));
                    mesh.Positions.Add(new Point3D(n.X,n.Y,n.Z));mesh.Normals.Add(n);mesh.TextureCoordinates.Add(new Point((double)i/sides,(double)j/rings));
                }
                for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){int k=j*(sides+1)+i;Tri(mesh,k,k+sides+1,k+1);Tri(mesh,k+1,k+sides+1,k+sides+2);}
                mesh.Freeze();spheres[key]=mesh;
            }
            var obj=Geometry(mesh,material); var m=Matrix3D.Identity;m.Scale(new Vector3D(sx,sy,sz));m.Translate(new Vector3D(x,y,z));obj.Transform=new MatrixTransform3D(m);parent.Children.Add(obj);
        }
        public static void Tri(MeshGeometry3D m,int a,int b,int c){m.TriangleIndices.Add(a);m.TriangleIndices.Add(b);m.TriangleIndices.Add(c);}
        public static void Tube(Model3DGroup parent,Material material,Point3D a,Point3D b,double ra,double rb,int sides=10)
        {
            Vector3D axis=b-a;axis.Normalize();Vector3D u=Vector3D.CrossProduct(axis,Math.Abs(axis.Y)>.9?new Vector3D(1,0,0):new Vector3D(0,1,0));u.Normalize();var v=Vector3D.CrossProduct(axis,u);
            var mesh=new MeshGeometry3D();
            for(int end=0;end<2;end++)for(int i=0;i<sides;i++){double t=2*Math.PI*i/sides;Vector3D r=(u*Math.Cos(t)+v*Math.Sin(t))*(end==0?ra:rb);mesh.Positions.Add((end==0?a:b)+r);mesh.TextureCoordinates.Add(new Point((double)i/sides,end));}
            mesh.Positions.Add(a);mesh.Positions.Add(b);mesh.TextureCoordinates.Add(new Point(.5,0));mesh.TextureCoordinates.Add(new Point(.5,1));
            for(int i=0;i<sides;i++){int next=(i+1)%sides;Tri(mesh,i,next,i+sides);Tri(mesh,next,next+sides,i+sides);Tri(mesh,2*sides,next,i);Tri(mesh,2*sides+1,i+sides,next+sides);}
            mesh.Freeze();parent.Children.Add(Geometry(mesh,material));
        }
        // Extruded outline, including the tapered leading edge and separate bevel face.
        public static void Plate(Model3DGroup parent,Material material,Point[] outline,double z,double thick)
        {
            var mesh=new MeshGeometry3D();int n=outline.Length;
            double minX=double.MaxValue,minY=double.MaxValue,maxX=double.MinValue,maxY=double.MinValue;
            foreach(Point p in outline){minX=Math.Min(minX,p.X);maxX=Math.Max(maxX,p.X);minY=Math.Min(minY,p.Y);maxY=Math.Max(maxY,p.Y);}
            for(int side=0;side<2;side++)foreach(Point p in outline){mesh.Positions.Add(new Point3D(p.X,p.Y,z+(side==0?-thick:thick)));mesh.TextureCoordinates.Add(new Point((p.X-minX)/Math.Max(.001,maxX-minX),1-(p.Y-minY)/Math.Max(.001,maxY-minY)));}
            // Ear clipping preserves concave bevel strips; a triangle fan fills their hollow interior.
            var ids=new List<int>();double area=0;
            for(int i=0;i<n;i++){ids.Add(i);Point p=outline[i],q=outline[(i+1)%n];area+=p.X*q.Y-q.X*p.Y;}
            double sign=area>=0?1:-1;int budget=n*n;
            while(ids.Count>2&&budget-->0)
            {
                bool found=false;
                for(int j=0;j<ids.Count;j++)
                {
                    int a=ids[(j+ids.Count-1)%ids.Count],b=ids[j],c=ids[(j+1)%ids.Count];
                    if(Cross(outline[a],outline[b],outline[c])*sign<=1e-10)continue;
                    bool inside=false;
                    foreach(int k in ids)if(k!=a&&k!=b&&k!=c&&Cross(outline[a],outline[b],outline[k])*sign>=-1e-10&&Cross(outline[b],outline[c],outline[k])*sign>=-1e-10&&Cross(outline[c],outline[a],outline[k])*sign>=-1e-10){inside=true;break;}
                    if(inside)continue;Tri(mesh,a,c,b);Tri(mesh,n+a,n+b,n+c);ids.RemoveAt(j);found=true;break;
                }
                if(!found)break;
            }
            for(int i=0;i<n;i++){int j=(i+1)%n;Tri(mesh,i,j,n+i);Tri(mesh,j,n+j,n+i);}
            mesh.Freeze();parent.Children.Add(Geometry(mesh,material));
        }
        static double Cross(Point a,Point b,Point c){return(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);}
        public static Model3DGroup Blade()
        {
            return EmberBlade();
        }
        static Model3DGroup EmberBlade()
        {
            var g=Group(null);
            Plate(g,Armor,new[]{new Point(-.055,.05),new Point(.064,.13),new Point(.079,.83),new Point(.024,1.24),new Point(-.062,1.06),new Point(-.100,.66),new Point(-.085,.30)},0,.016);
            for(int side=-1;side<=1;side+=2)
            {
                double z=side*.025;
                Plate(g,Steel,new[]{new Point(-.064,.33),new Point(-.079,.66),new Point(-.043,1.045),new Point(.024,1.24),new Point(.056,.83),new Point(.029,.29)},z,.003);
                Plate(g,Glow("#FF7B16"),new[]{new Point(-.085,.30),new Point(-.100,.66),new Point(-.062,1.06),new Point(.024,1.24),new Point(-.045,1.053),new Point(-.082,.66),new Point(-.071,.30)},z+side*.004,.002);
                Tube(g,Flame,new Point3D(-.084,.31,z+side*.007),new Point3D(-.099,.66,z+side*.007),.002,.002,6);
                Tube(g,Flame,new Point3D(-.099,.66,z+side*.007),new Point3D(-.061,1.06,z+side*.007),.002,.001,6);
                Plate(g,Armor,new[]{new Point(.012,.30),new Point(.065,.38),new Point(.071,.82),new Point(.042,.96),new Point(.018,.80)},z+side*.005,.004);
                Tube(g,Silver,new Point3D(.025,.39,z+side*.011),new Point3D(.044,.77,z+side*.011),.004,.003,6);
                Ring(g,Silver,0,.065,side*.046,.083,.009);
                Ring(g,Glove,0,.065,side*.052,.064,.012);
                Ball(g,Armor,0,.065,side*.054,.052,.052,.014);
                Ball(g,Glow("#FF4020"),0,.065,side*.071,.018,.018,.004);
                for(int i=0;i<3;i++){double a=i*Math.PI*2/3;Ball(g,Silver,Math.Cos(a)*.057,.065+Math.Sin(a)*.057,side*.069,.012,.012,.006,8,10);}
                Tube(g,Flame,new Point3D(.083,.18,z),new Point3D(.091,.30,z),.008,.008,8);
            }
            Plate(g,Engraved,new[]{new Point(-.073,-.018),new Point(-.109,.15),new Point(-.071,.26),new Point(.065,.20),new Point(.087,.09),new Point(.045,-.025)},0,.034);
            // Expose the mechanical discs in front of the housing on both faces.
            for(int side=-1;side<=1;side+=2){Ring(g,Silver,-.013,.071,side*.053,.058,.006);Ring(g,Glove,-.013,.071,side*.057,.046,.008);Ball(g,Armor,-.013,.071,side*.055,.036,.036,.010);Ball(g,Glow("#D94B25"),-.013,.071,side*.067,.009,.009,.005);}
            Tube(g,Armor,new Point3D(0,-.31,0),new Point3D(0,-.02,0),.032,.035,16);
            for(int i=0;i<8;i++)Tube(g,Glove,new Point3D(0,-.29+i*.032,0),new Point3D(0,-.278+i*.032,0),.035,.035,12);
            Ball(g,Silver,0,-.32,0,.04,.025,.036);
            return g;
        }
        static Model3DGroup ClassicBlade()
        {
            var g=Group(null);
            // Blade points upward on Y, handle from -.31 to 0. Broad asymmetric single edge.
            Plate(g,Steel,new[]{new Point(-.045,0),new Point(.065,.04),new Point(.108,.66),new Point(.064,1.04),new Point(-.105,.88),new Point(-.133,.56),new Point(-.075,.23)},0,.018);
            Plate(g,Edge,new[]{new Point(-.075,.23),new Point(-.133,.56),new Point(-.105,.88),new Point(.064,1.04),new Point(-.073,.855),new Point(-.096,.55),new Point(-.050,.23)},-.020,.002);
            Plate(g,Edge,new[]{new Point(-.075,.23),new Point(-.133,.56),new Point(-.105,.88),new Point(.064,1.04),new Point(-.073,.855),new Point(-.096,.55),new Point(-.050,.23)},.020,.002);
            Plate(g,Black,new[]{new Point(.018,.18),new Point(.05,.22),new Point(.069,.67),new Point(.045,.87),new Point(.020,.65)},-.021,.003);
            Plate(g,Black,new[]{new Point(.018,.18),new Point(.05,.22),new Point(.069,.67),new Point(.045,.87),new Point(.020,.65)},.021,.003);
            for(int i=0;i<6;i++)Plate(g,DarkGold,new[]{new Point(.060,.17+i*.066),new Point(.092,.20+i*.066),new Point(.074,.22+i*.066)},0,.026);
            Plate(g,Gold,new[]{new Point(-.16,-.008),new Point(-.12,.043),new Point(.03,.027),new Point(.12,.065),new Point(.145,.031),new Point(.08,-.027),new Point(-.09,-.032)},0,.042);
            Tube(g,Leather,new Point3D(0,-.29,0),new Point3D(0,-.015,0),.032,.033,12);
            for(int i=0;i<9;i++)Tube(g,DarkGold,new Point3D(0,-.282+i*.03,0),new Point3D(0,-.276+i*.03,0),.034,.034,12);
            Ball(g,Gold,0,-.31,0,.045,.035,.042);
            Ball(g,Gold,-.015,0,-.048,.034,.030,.010);
            // Hammered metal insets / rivets.
            for(int i=0;i<3;i++)Ball(g,Gold,.01,.065+i*.032,-.023,.008,.008,.004,6,8);
            return g;
        }
        public static Model3DGroup Hand(bool left)
        {
            var g=Group(null);
            Ball(g,HeroGlove,0,0,0,.058,.084,.041,14,20);
            for(int i=0;i<4;i++)
            {
                double y=-.05+i*.03;
                Ball(g,HeroGlove,-.023,y,-.035,.041,.014,.016,10,16);
                Ball(g,Armor,-.002,y,.034,.042,.012,.008,8,12);
            }
            Ball(g,HeroGlove,left?.045:-.045,.025,-.026,.018,.047,.022,10,16);
            Ring(g,Silver,.012,.026,.041,.016,.003);
            return g;
        }
        public static Model3DGroup Forearm()
        {
            var g=Group(null);
            Tube(g,Leather,new Point3D(0,0,0),new Point3D(0,-.38,0),.053,.092,14);
            Tube(g,Engraved,new Point3D(0,-.065,0),new Point3D(0,-.30,0),.070,.096,20);
            for(int i=0;i<3;i++)Tube(g,i==0?HeroGlove:Armor,new Point3D(0,-.05-i*.12,0),new Point3D(0,-.065-i*.12,0),.074+i*.009,.074+i*.009,16);
            Plate(g,Engraved,new[]{new Point(-.046,-.06),new Point(.052,-.08),new Point(.077,-.31),new Point(0,-.36),new Point(-.074,-.31)},-.064,.023);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<18;i++)
            {double t=i/18.0,u=(i+1)/18.0;Tube(g,Silver,new Point3D(side*(.017+.029*Math.Sin(t*5)),-.09-t*.23,-.091),new Point3D(side*(.017+.029*Math.Sin(u*5)),-.09-u*.23,-.091),.004,.004,6);}
            Ring(g,DarkGold,0,-.20,-.095,.022,.005);
            return g;
        }
    }

    sealed class RedRig
    {
        public readonly Model3DGroup Root=Models.Group(null), Body, LeftArm, RightArm, LeftLeg, RightLeg;
        public RedRig()
        {
            Body=Models.Group(Root);
            Models.Ball(Body,Models.Red,0,1.25,0,.265,.37,.155,14,18);
            Models.Ball(Body,Models.Black,0,.88,0,.19,.16,.13);
            for(int side=-1;side<=1;side+=2)
            {
                Models.Ball(Body,Models.Flesh,side*.13,1.39,-.098,.115,.12,.075);
                for(int i=0;i<4;i++)Models.Tube(Body,Models.Bone,new Point3D(side*.027,1.26-i*.065,-.15),new Point3D(side*(.19-i*.012),1.28-i*.067,-.105),.014,.009,8);
            }
            Models.Ball(Body,Models.Red,0,1.61,-.01,.085,.12,.09);
            Models.Ball(Body,Models.Flesh,0,1.77,-.035,.106,.147,.106,14,18);
            Models.Ball(Body,Models.Red,0,1.665,-.09,.083,.049,.055);
            for(int side=-1;side<=1;side+=2)
            {
                Models.Ball(Body,Models.Black,side*.045,1.795,-.124,.035,.022,.013);
                Models.Ball(Body,Models.Mat("#D9AF55",45),side*.045,1.795,-.137,.014,.010,.006,8,10);
                Models.Tube(Body,Models.Bone,new Point3D(side*.07,1.88,.008),new Point3D(side*.14,2.035,.04),.037,.001,10);
            }
            Models.Ball(Body,Models.Black,0,1.702,-.141,.058,.024,.009);
            for(int i=-2;i<=2;i++)Models.Tube(Body,Models.Bone,new Point3D(i*.021,1.72,-.15),new Point3D(i*.021,1.702,-.154),.006,.002,6);
            LeftArm=Arm(Body,-1);RightArm=Arm(Body,1);LeftLeg=Leg(Root,-1);RightLeg=Leg(Root,1);
            Animate(0,0,0);
        }
        Model3DGroup Arm(Model3DGroup parent,int side)
        {
            var g=Models.Group(parent);
            Models.Ball(g,Models.Flesh,0,-.05,0,.108,.135,.10);
            Models.Ball(g,Models.Red,0,-.21,0,.075,.18,.074);
            Models.Ball(g,Models.Flesh,0,-.44,-.045,.065,.18,.063);
            Models.Ball(g,Models.Red,0,-.61,-.06,.067,.084,.048);
            for(int i=0;i<4;i++)
            {
                double x=-.045+i*.029;
                Models.Tube(g,Models.Red,new Point3D(x,-.65,-.067),new Point3D(x*1.35,-.73,-.10),.013,.009,8);
                Models.Tube(g,Models.Bone,new Point3D(x*1.35,-.73,-.10),new Point3D(x*1.45,-.81,-.17),.013,.001,8);
            }
            for(int i=0;i<3;i++)Models.Tube(g,Models.Bone,new Point3D(side*.065,-.37-i*.067,-.015),new Point3D(side*.135,-.30-i*.067,.03),.022,.001,8);
            return g;
        }
        Model3DGroup Leg(Model3DGroup parent,int side)
        {
            var g=Models.Group(parent);
            Models.Ball(g,Models.Red,0,-.19,0,.090,.23,.095);
            Models.Ball(g,Models.Flesh,0,-.43,-.012,.066,.071,.063);
            Models.Ball(g,Models.Red,0,-.62,.01,.059,.19,.06);
            Models.Ball(g,Models.Black,0,-.82,-.063,.067,.057,.126);
            for(int i=0;i<3;i++)Models.Tube(g,Models.Bone,new Point3D(-.035+i*.035,-.827,-.13),new Point3D(-.039+i*.038,-.84,-.205),.016,.002,8);
            return g;
        }
        public void Animate(double t,double lunge,double hurt)
        {
            double walk=Math.Sin(t*7);
            Models.Pose(Body,0,Math.Abs(walk)*.018,0,5+hurt*18,0,walk*1.6);
            Models.Pose(LeftLeg,-.115,.89,0,walk*23,0,-3);Models.Pose(RightLeg,.115,.89,0,-walk*23,0,3);
            Models.Pose(LeftArm,-.29,1.49,0,22-walk*15+lunge*72,0,-12);
            Models.Pose(RightArm,.29,1.49,0,22+walk*15+lunge*85,0,12);
        }
    }
}
