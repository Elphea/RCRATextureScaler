using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCRATextureScaler
{
    internal class Source : TextureBase
    {
        public byte[] header = Array.Empty<byte>();
        public bool STG = false;
        public byte[] textureheader = Array.Empty<byte>();
        public List<byte[]> mipmaps = new();
        public string hdfilename = "";
        public bool exportable;
        private readonly List<uint> textureIds;

        public Source()
        {
            Name = "Source";
            textureIds = new()
            {
                // Adding Spiderman Magic Value
                0x5C4580B9,
                // Adding RCRA Magic Value
                0x8F53A199
            };
        }

        public override bool Read(out string output, out int errorrow, out string errorcol)
        {
            output = "";
            errorrow = 0;
            errorcol = "";
            exportable = false;

            using var fs = File.Open(Filename!, FileMode.Open, FileAccess.Read);
            BinaryReader br = new(fs);

            if(br.ReadUInt32() == 4674643)
            {
                STG = true;
                fs.Seek(16, SeekOrigin.Begin);
            }
            else
            {
                fs.Seek(0, SeekOrigin.Begin);
            }

            if(STG)
            {
                if (!textureIds.Contains(br.ReadUInt32()) ||
                fs.Seek(92, SeekOrigin.Current) < 1 ||
                br.ReadUInt32() != 1145132081 ||
                !textureIds.Contains(br.ReadUInt32()))
                {
                    output += "Not a texture asset.  Please import the lowest resolution copy.\r\n";
                    errorcol = "This STG header texture asset is not correctly formatted. If correct, please contact on Discord";
                    return false;
                }
                ;
            }
            else
            {
                if (!textureIds.Contains(br.ReadUInt32()) ||
                fs.Seek(32, SeekOrigin.Current) < 1 ||
                br.ReadUInt32() != 1145132081 ||
                !textureIds.Contains(br.ReadUInt32()))
                {
                    output += "Not a texture asset.  Please import the lowest resolution copy.\r\n";
                    errorcol = "This is not a texture asset. Please import the lowest resolution copy";
                    return false;
                }
                ;
            }

            br.ReadUInt32();
            if (br.ReadUInt32() != 1)
            {
                output += "Multiple sections not implemented.  Woops\r\n";
                errorcol = "Multiple sections not implemented.";
                return false;
            }

            if (br.ReadUInt32() != 1323185555)
            {
                output += "Unexpected section type\r\n";
                errorcol = "Section type unknown";
                return false;
            }
            var offset = br.ReadUInt32();
            var size = br.ReadUInt32();

            fs.Seek(0, SeekOrigin.Begin);
            if(STG)
            {
                header = br.ReadBytes((int)offset + 52);
            }
            else
            {
                header = br.ReadBytes((int)offset + 36);
            }
            
            textureheader = br.ReadBytes((int)size);

            if(STG)
            {
                fs.Seek((int)offset + 112, SeekOrigin.Begin);
            }
            else
            {
                fs.Seek((int)offset + 36, SeekOrigin.Begin);
            }
            Size = br.ReadUInt32();
            HDSize = br.ReadUInt32();
            Width = br.ReadUInt16();
            Height = br.ReadUInt16();
            sd_width = br.ReadUInt16();
            sd_height = br.ReadUInt16();
            ArrayCount = br.ReadUInt16();
            br.ReadByte();
            var channels = br.ReadByte();
            var dxgi_format = br.ReadUInt16();
            Format = (DXGI_FORMAT?)dxgi_format;
            br.ReadBytes(8);

            Mipmaps = br.ReadByte();
            br.ReadByte();
            HDMipmaps = br.ReadByte();
            Cubemaps = 1;
            int expectedsize = CalculateExpectedSize();
            if (expectedsize == 0)
                br.ReadByte();

            if (expectedsize == 0)
            {
                output += $"Support for DXGI format not implemented: {Format}\r\n";
                errorcol = "This DXGI format is not supported";
                return false;
            }
            br.ReadByte();
            if (expectedsize * ArrayCount != (HDSize + Size)    )
            {
                if (expectedsize * ArrayCount * 6 == (HDSize + Size))
                    Cubemaps = 6;
                else
                {
                    output += "Image data size does not match expected\r\n";
                    errorcol = "Image data size does not match expected";
                    return false;
                }
            }

            aspect = (int)(Math.Log((double)Width / (double)Height) / Math.Log(2));

            fs.Seek(11, SeekOrigin.Current);
            mipmaps = new();
            for (int i = 0; i < Images; i++)
                mipmaps.Add(br.ReadBytes((int)(Size / Images)));

            hdfilename = Path.ChangeExtension(Filename, ".hd.texture")!;
            string hdtxt;
            if (HDSize == 0)
            {
                hdtxt = "single-part texture";
                hdfilename = "";
            }
            else if (File.Exists(hdfilename))
                hdtxt = "hd part found";
            else if (File.Exists(hdfilename!.Replace(".hd.texture", "_hd.texture")))
            {
                hdfilename = hdfilename.Replace(".hd.texture", "_hd.texture");
                hdtxt = "found SpiderTex style _hd file";
            }
            else
            {
                hdtxt = "hd part MISSING";
                hdfilename = "";
            }
            var arraytxt = Images > 1 ? $"with {ArrayCount} packed {(Cubemaps > 1 ? "cubemaps" : "textures")} " : "";
            output += $"Source {arraytxt}loaded ({hdtxt})\r\n";

            if (hdfilename != "")
            {
                var hdfilesize = new FileInfo(hdfilename).Length;
                if (hdfilesize != HDSize)
                {
                    output += $"HD component is the wrong size (expected {HDSize} bytes, got {hdfilesize})\r\n";
                    errorcol = $"HD component is the wrong size (expected {HDSize} bytes, got {hdfilesize})";
                    return false;
                }
            }

            br.Dispose();
            Ready = errorcol == "";
            return true;
        }
    }
}
