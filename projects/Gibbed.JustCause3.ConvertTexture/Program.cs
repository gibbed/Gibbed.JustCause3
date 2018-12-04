/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using NDesk.Options;

namespace Gibbed.JustCause3.ConvertTexture
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        private static void Main(string[] args)
        {
            bool showHelp = false;
            bool overwriteFiles = false;
            bool verbose = true;

            var options = new OptionSet()
            {
                { "o|overwrite", "overwrite existing files", v => overwriteFiles = v != null },
                { "v|verbose", "be verbose", v => verbose = v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 1 ||
                extras.Count > 2 ||
                showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_file.ddsc [output_file.dds]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null) + ".dds";

            using (var input = File.OpenRead(inputPath))
            {
                var texture = new TextureFile();
                texture.Deserialize(input);

                using (var output = File.Create(outputPath))
                {
                    const Endian endian = Endian.Little;

                    var header = new Squish.DDS.Header()
                    {
                        Flags = Squish.DDS.HeaderFlags.Texture | Squish.DDS.HeaderFlags.Mipmap,
                        Width = texture.Width,
                        Height = texture.Height,
                        PitchOrLinearSize = 0,
                        Depth = 0,
                        MipMapCount = texture.MipCount,
                        PixelFormat = GetPixelFormat(texture),
                        SurfaceFlags = 8,
                        CubemapFlags = 0,
                    };

                    output.WriteValueU32(0x20534444, endian);
                    header.Serialize(output, endian);

                    if (header.PixelFormat.FourCC == 0x30315844)
                    {
                        output.WriteValueU32(texture.Format, endian);
                        output.WriteValueU32(2, endian);
                        output.WriteValueU32(0, endian);
                        output.WriteValueU32(1, endian);
                        output.WriteValueU32(0, endian);
                    }

                    input.Position = texture.Elements[0].Offset;
                    output.WriteFromStream(input, texture.Elements[0].Size);
                }
            }
        }

        private static Squish.DDS.PixelFormat GetPixelFormat(TextureFile texture)
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/bb173059.aspx "DXGI_FORMAT enumeration"
            // https://msdn.microsoft.com/en-us/library/windows/desktop/cc308051.aspx "Legacy Formats: Map Direct3D 9 Formats to Direct3D 10"
            switch (texture.Format)
            {
                case 71: // DXGI_FORMAT_BC1_UNORM
                {
                    var pixelFormat = new Squish.DDS.PixelFormat();
                    pixelFormat.Initialise(Squish.DDS.FileFormat.DXT1);
                    return pixelFormat;
                }

                case 74: // DXGI_FORMAT_BC2_UNORM
                {
                    var pixelFormat = new Squish.DDS.PixelFormat();
                    pixelFormat.Initialise(Squish.DDS.FileFormat.DXT3);
                    return pixelFormat;
                }

                case 77: // DXGI_FORMAT_BC3_UNORM
                {
                    var pixelFormat = new Squish.DDS.PixelFormat();
                    pixelFormat.Initialise(Squish.DDS.FileFormat.DXT5);
                    return pixelFormat;
                }

                case 87: // DXGI_FORMAT_B8G8R8A8_UNORM
                {
                    var pixelFormat = new Squish.DDS.PixelFormat();
                    pixelFormat.Initialise(Squish.DDS.FileFormat.A8R8G8B8);
                    return pixelFormat;
                }

                case 61: // DXGI_FORMAT_R8_UNORM
                case 83: // DXGI_FORMAT_BC5_UNORM
                case 98: // DXGI_FORMAT_BC7_UNORM
                {
                    var pixelFormat = new Squish.DDS.PixelFormat();
                    pixelFormat.Size = pixelFormat.GetSize();
                    pixelFormat.FourCC = 0x30315844; // 'DX10'
                    return pixelFormat;
                }
            }

            throw new NotSupportedException();
        }
    }
}
