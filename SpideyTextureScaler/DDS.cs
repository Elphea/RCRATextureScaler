using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace RCRATextureScaler
{
    internal class DDS : TextureBase
    {
        public long dataoffset;

        public DDS()
        {
            Name = "Modded";
        }

        public override bool Read(out string output, out int errorrow, out int errorcol)
        {
            output = "";
            errorrow = 1;
            errorcol = -1;
            bool notPowerofTwo = false;

            using var fs = File.Open(Filename, FileMode.Open, FileAccess.Read);
            using BinaryReader br = new BinaryReader(fs);
            if (br.ReadUInt32() != 542327876)
            {
                output += "Not a DDS file\r\n";
                errorcol = 1;
                return false;
            }

            var flags = (DDS_Flags)br.ReadUInt32();

            br.ReadUInt32();
            Height = br.ReadUInt32();
            Width = br.ReadUInt32();
            if (Height * Width == 0 ||
                // power of 2 trick
                (Height & (Height - 1)) != 0 ||
                (Width & (Width - 1)) != 0)
            {
                notPowerofTwo = true;
            }
            if(notPowerofTwo)
            {
                if((Height/16) * (Width/9) == 0)
                {
                    output += "Texture widths and heights must be a power of 2 or 16/9\r\n";
                    errorcol = 2;
                    return false;
                }
            }
            aspect = (int)(Math.Log((double)Width / (double)Height) / Math.Log(2));

            // avoid Microsoft's pitch / linearsize screw-up
            br.ReadUInt32();

            // depth
            br.ReadUInt32();
            Mipmaps = br.ReadUInt32();

            fs.Seek(0x54, SeekOrigin.Begin);
            bool hasDX10Header = br.ReadUInt32() == 808540228;
            fs.Seek(0x80, SeekOrigin.Begin);

            int formatBits = -1;

            if (hasDX10Header)
            {
                Format = (DXGI_FORMAT?)br.ReadUInt32();
                formatBits = BitsPerPixel(Format);
                fs.Seek(0x94, SeekOrigin.Begin);
            }

            dataoffset = fs.Position;

            // calculate based on remaining data
            Size = (uint)(fs.Length - fs.Position);

            if (formatBits > 0)
            {
                BytesPerPixel =  formatBits / 8;
            }
            else
            {
                int maxmipexp = (int)Math.Floor(Math.Log((double)Size) / Math.Log(2));
                basemipsize = 1 << maxmipexp;
                BytesPerPixel = (float)basemipsize / Width / Height;
            }

            output += $"DDS loaded\r\n";
            Ready = true;
            return true;
        }

        static public int BitsPerPixel(DXGI_FORMAT? Format)
        {
            switch (Format)
            {
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_SINT:
                    return 128;

                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_SINT:
                    return 96;

                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G8X24_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT_S8X24_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_X32_TYPELESS_G8X24_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_Y416:
                case DXGI_FORMAT.DXGI_FORMAT_Y210:
                case DXGI_FORMAT.DXGI_FORMAT_Y216:
                    return 64;

                case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R11G11B10_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R32_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R24G8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_D24_UNORM_S8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R24_UNORM_X8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_X24_TYPELESS_G8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R9G9B9E5_SHAREDEXP:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_B8G8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_G8R8_G8B8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_AYUV:
                case DXGI_FORMAT.DXGI_FORMAT_Y410:
                case DXGI_FORMAT.DXGI_FORMAT_YUY2:
                    return 32;

                case DXGI_FORMAT.DXGI_FORMAT_P010:
                case DXGI_FORMAT.DXGI_FORMAT_P016:
                    return 24;

                case DXGI_FORMAT.DXGI_FORMAT_R8G8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R16_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_D16_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R16_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_A8P8:
                case DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM:
                    return 16;

                case DXGI_FORMAT.DXGI_FORMAT_NV12:
                case DXGI_FORMAT.DXGI_FORMAT_420_OPAQUE:
                case DXGI_FORMAT.DXGI_FORMAT_NV11:
                    return 12;

                case DXGI_FORMAT.DXGI_FORMAT_R8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R8_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_A8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_AI44:
                case DXGI_FORMAT.DXGI_FORMAT_IA44:
                case DXGI_FORMAT.DXGI_FORMAT_P8:
                    return 8;

                case DXGI_FORMAT.DXGI_FORMAT_R1_UNORM:
                    return 1;

                case DXGI_FORMAT.DXGI_FORMAT_BC1_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM:
                    return 4;

                case DXGI_FORMAT.DXGI_FORMAT_BC2_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB:
                    return 8;

                default:
                    return -1;
            }
        }

        public bool Write(byte[] hdmipmaps, List<byte[]> mipmaps, out string output)
        {
           if (Images > 1)
            {
                output = "";
                bool ret = false;
                for (int i = 0; i < Images; i++)
                {
                    string o2;
                    ret |= WriteSingle(
                        hdmipmaps is null ? null : hdmipmaps.Skip(i * hdmipmaps.Length / (int)Images).Take(hdmipmaps.Length / (int)Images).ToArray(),
                        mipmaps[i],
                        i, out o2);
                    output += o2;
                }
                output += "\r\n";
                return ret;
            }
            else
                return WriteSingle(hdmipmaps, mipmaps[0], -1, out output);
        }

        public bool WriteSingle(byte[] hdmipmaps, byte[] mipmaps, int image, out string output)
        {
            // just assume everything has been set correctly!
            output = "";
            string fn = Filename;
            if (image > -1)
                fn = Path.ChangeExtension(fn, $".A{image}.dds");

            using var fs = File.Open(fn, FileMode.Create);
            using var bw = new BinaryWriter(fs);
            bw.Write(Encoding.ASCII.GetBytes("DDS "));
            bw.Write((uint)0x7c);
            bw.Write((uint)(DDS_Flags.DDSD_CAPS | DDS_Flags.DDSD_HEIGHT | DDS_Flags.DDSD_WIDTH | DDS_Flags.DDSD_PIXELFORMAT | DDS_Flags.DDSD_LINEARSIZE | DDS_Flags.DDSD_MIPMAPCOUNT));
            bw.Write((uint)Height);
            bw.Write((uint)Width);
            // linearsize
            bw.Write((uint)(hdmipmaps is null ? basemipsize : basemipsize * 1 << (2 * (int)HDMipmaps)));
            // depth
            bw.Write((uint)0);
            bw.Write((uint)((uint)Mipmaps + (uint)HDMipmaps));
            // reserved
            bw.Write(new byte[11 * 4]);

            // pixelformat
            bw.Write((uint)32);
            // FourCC
            bw.Write((uint)4);
            bw.Write(Encoding.ASCII.GetBytes("DX10"));
            bw.Write(new byte[5 * 4]);

            // caps
            bw.Write((uint)(DDS_Caps.DDSCAPS_TEXTURE | (Mipmaps + HDMipmaps > 0 ? DDS_Caps.DDSCAPS_COMPLEX | DDS_Caps.DDSCAPS_MIPMAP : 0)));
            // caps2-4, reserved
            bw.Write(new byte[4 * 4]);

            // DXT10 header
            bw.Write((uint)Format);
            // dimension - 2d or 3d
            bw.Write((uint)(Height > 1 ? 3 : 2));
            // misc
            bw.Write((uint)0);
            // arraySize
            bw.Write((uint)1);
            // misc flags - DDS_ALPHA_MODE_UNKNOWN
            bw.Write((uint)0);

            if (hdmipmaps is not null)
                bw.Write(hdmipmaps);

            bw.Write(mipmaps);

            output += $"Wrote {fs.Position} bytes to: {fn}\r\n";

            return true;
        }
    }
}
