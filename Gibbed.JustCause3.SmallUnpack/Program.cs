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

namespace Gibbed.JustCause3.SmallUnpack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool verbose = false;
            bool overwriteFiles = false;
            bool listing = false;
            bool useFullPaths = false;
            bool showHelp = false;

            var options = new OptionSet()
            {
                { "v|verbose", "be verbose (list files)", v => verbose = v != null },
                { "l|list", "just list files (don't extract)", v => listing = v != null },
                { "o|overwrite", "overwrite files if they already exist", v => overwriteFiles = v != null },
                { "f|full-path", "use full paths", v => useFullPaths = v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };

            List<string> extra;

            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extra.Count < 1 || extra.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_sarc [output_directory]", GetExecutableName());
                Console.WriteLine("Unpack specified small archive.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extra[0];
            string baseOutputPath = extra.Count > 1
                                        ? extra[1]
                                        : Path.ChangeExtension(inputPath, null) + "_unpack";

            using (var temp = File.OpenRead(inputPath))
            using (var cool = CreateCoolArchiveStream(temp))
            {
                var input = cool ?? temp;

                var smallArchive = new SmallArchiveFile();
                smallArchive.Deserialize(input);

                long counter = 0;
                long skipped = 0;
                long totalCount = smallArchive.Entries.Count;

                if (verbose == true)
                {
                    Console.WriteLine("{0} files in small archive.", totalCount);
                }

                foreach (var entry in smallArchive.Entries)
                {
                    counter++;

                    if (string.IsNullOrEmpty(entry.Name) == true)
                    {
                        throw new InvalidOperationException();
                    }

                    var entryPath = entry.Name;
                    if (entryPath[0] == '/' || entryPath[0] == '\\')
                    {
                        entryPath = entryPath.Substring(1);
                    }
                    entryPath = entryPath.Replace('/', Path.DirectorySeparatorChar);

                    if (useFullPaths == false)
                    {
                        entryPath = Path.GetFileName(entryPath);
                    }

                    var outputPath = Path.Combine(baseOutputPath, entryPath);
                    if (overwriteFiles == false && File.Exists(entryPath) == true)
                    {
                        if (verbose == true)
                        {
                            Console.WriteLine("{1:D4}/{2:D4} !! {0}", entry.Name, counter, totalCount);
                        }

                        skipped++;
                        continue;
                    }

                    if (verbose == true || listing == true)
                    {
                        Console.WriteLine("{1:D4}/{2:D4} => {0}", entry.Name, counter, totalCount);
                    }

                    if (entry.Offset == 0)
                    {
                        continue;
                    }

                    if (listing == false)
                    {
                        var parentOutputPath = Path.GetDirectoryName(outputPath);
                        if (string.IsNullOrEmpty(parentOutputPath) == false)
                        {
                            Directory.CreateDirectory(parentOutputPath);
                        }

                        using (var output = File.Create(outputPath))
                        {
                            input.Seek(entry.Offset, SeekOrigin.Begin);
                            output.WriteFromStream(input, entry.Size);
                        }
                    }
                }

                if (verbose == true && skipped > 0)
                {
                    Console.WriteLine("{0} files not overwritten.", skipped);
                }
            }
        }

        private static Stream CreateCoolArchiveStream(Stream input)
        {
            input.Seek(0, SeekOrigin.Begin);
            var isCoolArchive = CoolArchiveFile.CheckHeader(input);
            input.Seek(0, SeekOrigin.Begin);

            if (isCoolArchive == false)
            {
                return null;
            }

            var archive = new CoolArchiveFile();
            archive.Deserialize(input);

            return new CoolStream(archive, input);
        }
    }
}
