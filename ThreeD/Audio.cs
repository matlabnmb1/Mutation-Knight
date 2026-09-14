using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

namespace GhostBlade3D
{
    // Independent waveOut channels allow swooshes, impacts and clangs to overlap.
    sealed class Audio : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)] struct Format {public ushort tag,channels;public uint rate,bytes;public ushort align,bits,extra;}
        [StructLayout(LayoutKind.Sequential)] struct Header {public IntPtr data;public uint length,recorded;public IntPtr user;public uint flags,loops;public IntPtr next,reserved;}
        [DllImport("winmm.dll")] static extern int waveOutOpen(out IntPtr h,uint device,ref Format format,IntPtr callback,IntPtr instance,uint flags);
        [DllImport("winmm.dll")] static extern int waveOutPrepareHeader(IntPtr h,IntPtr header,uint size);
        [DllImport("winmm.dll")] static extern int waveOutWrite(IntPtr h,IntPtr header,uint size);
        [DllImport("winmm.dll")] static extern int waveOutUnprepareHeader(IntPtr h,IntPtr header,uint size);
        [DllImport("winmm.dll")] static extern int waveOutReset(IntPtr h);
        [DllImport("winmm.dll")] static extern int waveOutClose(IntPtr h);
        sealed class Voice {public IntPtr Handle,Header,Data;public bool Busy;}
        readonly List<Voice> voices=new List<Voice>();
        readonly Dictionary<string,short[]> samples=new Dictionary<string,short[]>();
        public bool Available;
        const int Rate=22050;
        public Audio()
        {
            foreach(string key in new[]{"light","heavy","thrust","execute","dash","block","clang","switch","impact","heavy-hit","thrust-hit","execute-hit","hurt","kill","execute-kill"})samples[key]=Generate(key);
            for(int i=1;i<=8;i++)
            {string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"local-audio","streak-"+i+".wav");if(File.Exists(path))try{samples["streak-"+i]=ReadWave(path);}catch(Exception e){if(!(e is IOException)&&!(e is InvalidDataException)&&!(e is UnauthorizedAccessException))throw;}}
            foreach(string key in new[]{"mascot-laugh","mascot-chubby","mascot-cow"})
            {string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"local-audio",key+".wav");if(File.Exists(path))try{samples[key]=ReadWave(path);}catch(Exception e){if(!(e is IOException)&&!(e is InvalidDataException)&&!(e is UnauthorizedAccessException))throw;}}
            for(int i=0;i<7;i++)
            {
                var f=new Format{tag=1,channels=1,rate=Rate,bytes=Rate*2,align=2,bits=16};IntPtr h;
                if(waveOutOpen(out h,0xffffffff,ref f,IntPtr.Zero,IntPtr.Zero,0)!=0)break;
                voices.Add(new Voice{Handle=h});
            }Available=voices.Count>0;
        }
        void Free(Voice v)
        {
            if(!v.Busy)return;
            waveOutReset(v.Handle);waveOutUnprepareHeader(v.Handle,v.Header,(uint)Marshal.SizeOf(typeof(Header)));
            Marshal.FreeHGlobal(v.Header);Marshal.FreeHGlobal(v.Data);v.Busy=false;
        }
        public void Play(string key)
        {
            if(!Available||!samples.ContainsKey(key))return;Voice use=null;
            bool announcement=key.StartsWith("streak-")||key.StartsWith("mascot-");
            if(announcement){if(voices.Count<7)return;use=voices[6];}
            else for(int i=0;i<Math.Min(6,voices.Count);i++){var v=voices[i];if(!v.Busy){use=v;break;}var h=(Header)Marshal.PtrToStructure(v.Header,typeof(Header));if((h.flags&1)!=0){use=v;break;}}
            if(use==null)use=voices[0];Free(use);
            short[] s=samples[key];use.Data=Marshal.AllocHGlobal(s.Length*2);Marshal.Copy(s,0,use.Data,s.Length);
            var header=new Header{data=use.Data,length=(uint)(s.Length*2)};use.Header=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Header)));Marshal.StructureToPtr(header,use.Header,false);
            use.Busy=true;waveOutPrepareHeader(use.Handle,use.Header,(uint)Marshal.SizeOf(typeof(Header)));waveOutWrite(use.Handle,use.Header,(uint)Marshal.SizeOf(typeof(Header)));
        }
        public bool PlayStreak(int kills)
        {string key="streak-"+Math.Min(8,kills);if(kills<1||voices.Count<7||!samples.ContainsKey(key))return false;Play(key);return true;}
        public void StopAnnouncement(){if(voices.Count>=7)Free(voices[6]);}
        public static short[] ReadWave(string path)
        {
            using(var reader=new BinaryReader(File.OpenRead(path)))
            {
                if(new string(reader.ReadChars(4))!="RIFF")throw new InvalidDataException("Not a RIFF wave");reader.ReadUInt32();if(new string(reader.ReadChars(4))!="WAVE")throw new InvalidDataException("Not WAVE");
                bool valid=false;byte[] data=null;
                while(reader.BaseStream.Position+8<=reader.BaseStream.Length)
                {
                    string id=new string(reader.ReadChars(4));uint size=reader.ReadUInt32();long end=reader.BaseStream.Position+size;if(end>reader.BaseStream.Length)throw new InvalidDataException("Truncated wave");
                    if(id=="fmt "&&size>=16){int format=reader.ReadUInt16(),channels=reader.ReadUInt16();int rate=reader.ReadInt32();reader.ReadUInt32();reader.ReadUInt16();int bits=reader.ReadUInt16();valid=format==1&&channels==1&&rate==Rate&&bits==16;}
                    if(id=="data"){if(size>Rate*2*10||size%2!=0)throw new InvalidDataException("Invalid audio length");data=reader.ReadBytes((int)size);}
                    reader.BaseStream.Position=Math.Min(reader.BaseStream.Length,end+(size%2));
                }
                if(!valid||data==null||data.Length==0)throw new InvalidDataException("Expected mono 22050 Hz 16-bit PCM");var result=new short[data.Length/2];Buffer.BlockCopy(data,0,result,0,data.Length);return result;
            }
        }
        short[] Generate(string key)
        {
            bool metal=key=="clang"||key=="block"||key=="switch";
            bool contact=key=="impact"||key.EndsWith("-hit");
            bool kill=key=="kill"||key=="execute-kill";
            double duration=kill?.85:contact?(key=="execute-hit"?.48:.32):key=="execute"?.60:key=="heavy"?.43:metal?.36:.22;
            var result=new short[(int)(Rate*duration)];var random=new Random(37);double low=0,phase=0;
            for(int i=0;i<result.Length;i++)
            {
                double t=(double)i/Rate,p=t/duration,n=random.NextDouble()*2-1;low=low*.72+n*.28;
                double env=Math.Sin(Math.PI*Math.Min(1,p*1.5))*Math.Exp(-p*3);
                double value;
                if(kill)
                {
                    double note=key=="execute-kill"?880:740;
                    value=(Math.Sin(2*Math.PI*note*t)*.35+Math.Sin(2*Math.PI*note*1.5*t)*.18)*Math.Exp(-t*5)*Math.Min(1,t*180);
                    if(t>.095){double q=t-.095;value+=(Math.Sin(2*Math.PI*note*2*q)*.28+Math.Sin(2*Math.PI*note*2.5*q)*.13)*Math.Exp(-q*5)*Math.Min(1,q*160);}
                    value+=(low*.75+Math.Sin(2*Math.PI*70*t)*.45)*Math.Exp(-t*24);
                }
                else if(metal)value=(Math.Sin(t*2*Math.PI*1800)+.45*Math.Sin(t*2*Math.PI*2873)+.20*Math.Sin(t*2*Math.PI*4340))*Math.Exp(-p*8)*Math.Min(1,p*80)*.24;
                else if(contact)
                {
                    double weight=key=="heavy-hit"||key=="execute-hit"?1.25:1;
                    double attack=Math.Min(1,t*1200);
                    double thud=Math.Sin(2*Math.PI*(95*t-50*t*t))*Math.Exp(-t*29)*.80;
                    double cut=(n-low)*Math.Exp(-t*48)*.72+low*Math.Exp(-t*17)*1.3;
                    double ring=(Math.Sin(2*Math.PI*1367*t)+.3*Math.Sin(2*Math.PI*2249*t))*Math.Exp(-t*23)*.12;
                    value=(thud+cut+ring)*weight*attack;
                }
                else if(key=="hurt")value=(low*1.6+Math.Sin(t*2*Math.PI*83)*.5)*Math.Exp(-p*10)*Math.Min(1,p*70);
                else{phase+=2*Math.PI*(key=="heavy"?95:180)*(1-p*.75)/Rate;value=((n-low)*.5+low*(key=="heavy"?2:1)+Math.Sin(phase)*.15)*env;}
                // Layer the execution draw, contact crack and short metallic tail.
                if(key=="execute")
                {
                    value+=Math.Sin(t*2*Math.PI*132)*Math.Exp(-p*10)*.22;
                    // Contact is a separate event, so cancelling cannot leave a phantom hit sound.
                }
                if(key=="heavy")value+=Math.Sin(t*2*Math.PI*67)*Math.Sin(Math.PI*p)*.16;
                if(key=="thrust")value+=Math.Sin(t*2*Math.PI*(730-400*p))*Math.Sin(Math.PI*p)*.10;
                result[i]=(short)(Combat.Clamp(value*.42,-.9,.9)*32767);
            }return result;
        }
        public void Dispose(){foreach(var v in voices){Free(v);waveOutClose(v.Handle);}voices.Clear();}
    }
}
