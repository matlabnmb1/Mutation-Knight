using System.Windows.Media.Media3D;

namespace GhostBlade3D
{
    // Clean-room rooftop mascots based only on broad visual traits. They are
    // decorative Model3D geometry and deliberately have no gameplay collision.
    static class RooftopMascots
    {
        static readonly Material Yellow=Models.Mat("#F6C928",45),Cream=Models.Mat("#FFF0B3",18),Green=Models.Glow("#67D690");
        static readonly Material ChubbyYellow=Models.Mat("#F4C514",34),ChubbyBelly=Models.Mat("#FFE79A",15),NoseRed=Models.Mat("#A92D18",26);
        static readonly Material Cow=Models.Mat("#D98A35",18),CowDark=Models.Mat("#6B3E27",8),Horn=Models.Mat("#F4DFC1",12),Black=Models.Mat("#20262B",12),White=Models.Mat("#FFFFFF",20);

        public static void Add(Model3DGroup root)
        {
            Bull(root,-3.1,4.78,10.75);
            Chubby(root,0,4.78,10.75);
            Dragon(root,3.1,4.78,10.75);
        }

        static void Eye(Model3DGroup g,double x,double y,double z,double scale=1)
        {
            Models.Ball(g,White,x,y,z,.072*scale,.090*scale,.030*scale,7,10);
            Models.Ball(g,Black,x,y-.006,z-.030*scale,.031*scale,.050*scale,.018*scale,7,10);
        }

        static void Dragon(Model3DGroup root,double x,double y,double z)
        {
            var g=Models.Group(root);
            Models.Ball(g,Yellow,0,.70,0,.58,.76,.46,12,18);
            Models.Ball(g,Cream,0,.70,-.445,.405,.55,.038,10,16);
            Models.Ball(g,Yellow,0,1.52,-.015,.30,.39,.28,11,16);
            Models.Tube(g,Black,new Point3D(-.20,1.70,-.282),new Point3D(-.07,1.66,-.314),.018,.010,7);
            Models.Tube(g,Black,new Point3D(.07,1.66,-.314),new Point3D(.20,1.70,-.282),.018,.010,7);
            Models.Ball(g,Black,0,1.50,-.300,.205,.235,.075,10,16);
            Models.Ball(g,White,0,1.61,-.370,.155,.055,.020,7,12);
            Models.Ball(g,NoseRed,0,1.39,-.378,.105,.050,.020,7,11);
            Models.Tube(g,Yellow,new Point3D(-.46,1.03,-.02),new Point3D(-.23,.68,-.39),.145,.105,9);
            Models.Tube(g,Yellow,new Point3D(.46,1.03,-.02),new Point3D(.23,.68,-.39),.145,.105,9);
            Models.Ball(g,Black,-.15,.62,-.455,.16,.105,.070,8,12);
            Models.Ball(g,Black,.15,.62,-.455,.16,.105,.070,8,12);
            for(int i=-1;i<=1;i++){Models.Tube(g,Black,new Point3D(-.18+i*.035,.61,-.49),new Point3D(-.10+i*.035,.56,-.50),.018,.010,5);Models.Tube(g,Black,new Point3D(.10+i*.035,.56,-.50),new Point3D(.18+i*.035,.61,-.49),.018,.010,5);}
            Models.Tube(g,Yellow,new Point3D(-.24,.18,.02),new Point3D(-.33,-.15,-.09),.17,.125,9);
            Models.Tube(g,Yellow,new Point3D(.24,.18,.02),new Point3D(.33,-.15,-.09),.17,.125,9);
            Models.Ball(g,Black,-.36,-.22,-.16,.18,.075,.25,7,11);
            Models.Ball(g,Black,.36,-.22,-.16,.18,.075,.25,7,11);
            Models.Pose(g,x,y,z,-4,0,0);
        }

        static void Chubby(Model3DGroup root,double x,double y,double z)
        {
            var g=Models.Group(root);
            Models.Ball(g,ChubbyYellow,0,.55,0,.49,.61,.38,10,16);
            Models.Ball(g,ChubbyBelly,0,.52,-.36,.27,.37,.032,9,14);
            Models.Ball(g,ChubbyYellow,0,1.19,-.02,.39,.38,.34,10,16);
            // Long upright ears and a narrow crown match the supplied meme image.
            Models.Tube(g,ChubbyYellow,new Point3D(-.19,1.42,.01),new Point3D(-.30,1.88,.02),.105,.070,9);
            Models.Tube(g,ChubbyYellow,new Point3D(.19,1.42,.01),new Point3D(.38,1.83,.02),.105,.070,9);
            Models.Ball(g,ChubbyYellow,-.31,1.89,.02,.095,.12,.075,8,12);
            Models.Ball(g,ChubbyYellow,.39,1.84,.02,.095,.12,.075,8,12);
            Eye(g,-.14,1.28,-.34,.78);Eye(g,.14,1.28,-.34,.78);
            Models.Ball(g,NoseRed,0,1.08,-.39,.15,.12,.085,8,12);
            Models.Tube(g,Black,new Point3D(-.07,.94,-.37),new Point3D(.07,.94,-.37),.013,.009,6);
            Models.Tube(g,ChubbyYellow,new Point3D(-.38,.77,0),new Point3D(-.61,.48,-.10),.115,.078,8);
            Models.Tube(g,ChubbyYellow,new Point3D(.38,.77,0),new Point3D(.61,.48,-.10),.115,.078,8);
            Models.Tube(g,ChubbyYellow,new Point3D(-.21,.12,0),new Point3D(-.25,-.06,-.08),.16,.135,9);
            Models.Tube(g,ChubbyYellow,new Point3D(.21,.12,0),new Point3D(.25,-.06,-.08),.16,.135,9);
            Models.Tube(g,ChubbyYellow,new Point3D(.38,.43,.20),new Point3D(.70,.28,.34),.11,.035,8);
            Models.Pose(g,x,y,z,0,0,0);
        }

        static void Bull(Model3DGroup root,double x,double y,double z)
        {
            var g=Models.Group(root);
            Models.Ball(g,Cow,0,.62,.08,.50,.61,.37,6,9);
            Models.Ball(g,Cow,0,1.25,-.04,.39,.43,.31,6,9);
            Models.Ball(g,Horn,0,1.03,-.37,.29,.19,.115,6,9);
            Models.Ball(g,CowDark,-.09,1.01,-.465,.035,.026,.020,5,7);Models.Ball(g,CowDark,.09,1.01,-.465,.035,.026,.020,5,7);
            Eye(g,-.15,1.35,-.31,.90);Eye(g,.15,1.35,-.31,.90);
            Models.Tube(g,Horn,new Point3D(-.27,1.51,-.01),new Point3D(-.58,1.72,-.03),.085,.016,6);
            Models.Tube(g,Horn,new Point3D(.27,1.51,-.01),new Point3D(.58,1.72,-.03),.085,.016,6);
            Models.Ball(g,Cow,-.36,1.36,-.02,.18,.22,.08,6,9);Models.Ball(g,Cow,.36,1.36,-.02,.18,.22,.08,6,9);
            Models.Tube(g,CowDark,new Point3D(-.24,.18,.02),new Point3D(-.27,-.07,-.04),.14,.10,7);
            Models.Tube(g,CowDark,new Point3D(.24,.18,.02),new Point3D(.27,-.07,-.04),.14,.10,7);
            Models.Tube(g,Cow,new Point3D(-.40,.78,.03),new Point3D(-.58,.49,-.06),.11,.075,7);
            Models.Tube(g,Cow,new Point3D(.40,.78,.03),new Point3D(.58,.49,-.06),.11,.075,7);
            Models.Pose(g,x,y,z,0,0,0);
        }
    }
}
