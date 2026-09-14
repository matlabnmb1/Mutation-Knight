using System;
using System.Collections.Generic;
namespace GhostBlade3D
{
    // Shared ground flow field plus explicit, collision-checked stair routes.
    // Static links are built once; enemies reuse the same search result.
    public sealed class Navigation
    {
        sealed class Node {public double X,Y,Z;public List<int> Links=new List<int>();}
        readonly Village map;readonly List<Node> nodes=new List<Node>();
        readonly Dictionary<int,List<int>> bins=new Dictionary<int,List<int>>();
        int[] costs;int goal=-1;double refresh;
        public Navigation(Village village)
        {
            map=village;int[,] grid=new int[95,95];
            for(int x=0;x<95;x++)for(int z=0;z<95;z++)
                grid[x,z]=map.Clear(x*.5-23.5,0,z*.5-23.5,1.95,.28)?Add(x*.5-23.5,0,z*.5-23.5):-1;
            for(int x=0;x<95;x++)for(int z=0;z<95;z++)if(grid[x,z]>=0)
            {if(x>0)Link(grid[x,z],grid[x-1,z]);if(z>0)Link(grid[x,z],grid[x,z-1]);}
            foreach(var r in map.Routes)
            {
                int previous=Add(r.X,0,r.Z);ConnectGround(previous);
                double y=0,z=r.Z;
                int count=(int)Math.Ceiling((r.EndZ-r.Z)/.3);
                for(int i=1;i<=count;i++)
                {double nz=r.Z+(r.EndZ-r.Z)*i/count,x=r.X;map.Move(ref x,ref y,ref z,0,nz-z,1.95);int n=Add(x,y,z);Link(previous,n);previous=n;}
                double sx=r.X,sz=r.EndZ;count=(int)Math.Ceiling(Math.Abs(r.RoofX-sx)/.3);
                for(int i=1;i<=count;i++)
                {double nx=r.X+(r.RoofX-r.X)*i/count;map.Move(ref sx,ref y,ref sz,nx-sx,0,1.95);int n=Add(sx,y,sz);Link(previous,n);previous=n;}
            }
            costs=new int[nodes.Count];for(int i=0;i<costs.Length;i++)costs[i]=-1;
        }
        int Key(double x,double z){return ((int)Math.Floor(x*2)+100)*256+(int)Math.Floor(z*2)+100;}
        int Add(double x,double y,double z)
        {int id=nodes.Count;nodes.Add(new Node{X=x,Y=y,Z=z});int key=Key(x,z);List<int> list;if(!bins.TryGetValue(key,out list)){list=new List<int>();bins[key]=list;}list.Add(id);return id;}
        IEnumerable<int> Nearby(double x,double z)
        {int key=Key(x,z);for(int a=-2;a<=2;a++)for(int b=-2;b<=2;b++){List<int> list;if(bins.TryGetValue(key+a*256+b,out list))foreach(int i in list)yield return i;}}
        void ConnectGround(int id){var n=nodes[id];foreach(int other in Nearby(n.X,n.Z))if(other!=id&&nodes[other].Y<.01)Link(id,other);}
        void Link(int a,int b)
        {if(a<0||b<0||a==b)return;var u=nodes[a];var v=nodes[b];if(Walkable(u.X,u.Y,u.Z,v.X,v.Y,v.Z)&&Walkable(v.X,v.Y,v.Z,u.X,u.Y,u.Z)){u.Links.Add(b);v.Links.Add(a);}}
        public bool Walkable(double x,double y,double z,double tx,double ty,double tz)
        {
            double dx=tx-x,dz=tz-z;int count=Math.Max(1,(int)Math.Ceiling(Math.Sqrt(dx*dx+dz*dz)/.08));
            for(int i=0;i<count;i++)
            {double nx=x+dx/count,nz=z+dz/count,ny=map.Floor(nx,nz,y+.22,.24);if(y-ny>.25||!map.Clear(nx,ny,nz,1.95,.24))return false;x=nx;y=ny;z=nz;}
            return Math.Abs(y-ty)<.24;
        }
        public void Update(double dt,double x,double y,double z)
        {
            refresh-=dt;if(refresh>0)return;refresh=.4;
            int nearest=-1;double score=double.MaxValue;
            for(int i=0;i<nodes.Count;i++){var n=nodes[i];double dy=n.Y-y,s=(n.X-x)*(n.X-x)+(n.Z-z)*(n.Z-z)+dy*dy*100;if(s<score){score=s;nearest=i;}}
            if(nearest==goal)return;goal=nearest;for(int i=0;i<costs.Length;i++)costs[i]=-1;
            if(goal<0)return;var queue=new Queue<int>();queue.Enqueue(goal);costs[goal]=0;
            while(queue.Count>0){int i=queue.Dequeue();foreach(int next in nodes[i].Links)if(costs[next]<0){costs[next]=costs[i]+1;queue.Enqueue(next);}}
        }
        public void Aim(Target t,double x,double y,double z,out double ax,out double az)
        {
            ax=t.X;az=t.Z;
            if(Walkable(t.X,t.Y,t.Z,x,y,z)){ax=x;az=z;return;}
            int best=-1;double score=double.MaxValue;
            foreach(int i in Nearby(t.X,t.Z))
            {var n=nodes[i];if(costs[i]<0||Math.Abs(n.Y-t.Y)>.45)continue;double distance=Math.Sqrt((n.X-t.X)*(n.X-t.X)+(n.Z-t.Z)*(n.Z-t.Z));double s=costs[i]+distance*2;if(s<score&&Walkable(t.X,t.Y,t.Z,n.X,n.Y,n.Z)){score=s;best=i;}}
            if(best>=0){var n=nodes[best];ax=n.X;az=n.Z;if((ax-t.X)*(ax-t.X)+(az-t.Z)*(az-t.Z)<.04)foreach(int i in n.Links)if(costs[i]>=0&&costs[i]<costs[best]){best=i;ax=nodes[i].X;az=nodes[i].Z;}}
        }
    }
}
