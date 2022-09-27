using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using PurplePen.MapModel;
using PurplePen.MapModel.DebugCode;

namespace ProjectionDump
{
    class Program
    {
        // Make this Main to dump the projection info from an OCAD file.
        static void DumpProjectionMain(string[] args)
        {
            if (args.Length < 1) {
                Console.WriteLine("Must provide argument.");
                return;
            }

            string mapName = args[0];

            OcadDump dumper = new OcadDump();
            dumper.DumpProjection(mapName, Console.Out);
        }

        // Make this Main to fetch projection proj4 mapping from spatial-reference.org
        // The input file is taken from https://github.com/doppelmeter/OCAD-Grid-ID_to_EPSG/blob/master/ocad_grid_id_2_epsg.csv
        // For some reason the separators in that file are semicolons, so change them to commas.
        static void Main(string[] args)
        {
            if (args.Length < 3) {
                Console.WriteLine("1st arg: input file, 2nd arg: output file, 3rd arg C# file");

                return;
            }

            TextReader reader = new StreamReader(args[0]);
            TextWriter writer = new StreamWriter(args[1]);
            TextWriter csWriter = new StreamWriter(args[2]);

            FetchProjection(reader, writer, csWriter);

            reader.Close();
            writer.Close();
            csWriter.Close();
        }

        // Make this Main to compare two sources for mapping
        static void MainCompare(string[] args)
        {
            if (args.Length < 2) {
                Console.WriteLine("1st arg: OCad Coord Systems, 2nd arg: ocad_grid_id_2_epsg");

                return;
            }

            TextReader readerCoordSystems = new StreamReader(args[0]);
            TextReader readerOcad2Epsg = new StreamReader(args[1]);

            CsvReader csvCoordSystems = new CsvReader(readerCoordSystems);
            csvCoordSystems.Configuration.RegisterClassMap<ProjectionDataMapInput>();
            List<ProjectionData> projectionDataList = csvCoordSystems.GetRecords<ProjectionData>().ToList();

            CsvReader csvOcad2Epsg = new CsvReader(readerOcad2Epsg);
            csvOcad2Epsg.Configuration.RegisterClassMap<OcadGrid2EpsgMapInput>();
            List<OcadGridId2Epsg> ocad2EpsgList = csvOcad2Epsg.GetRecords<OcadGridId2Epsg>().ToList();

            foreach (OcadGridId2Epsg ocad2Epsg in ocad2EpsgList) {
                int ocadId = ocad2Epsg.OcadCoordinateSystem;
                ProjectionData projData = projectionDataList.Where(pd => pd.OCADId == ocadId).FirstOrDefault();
                if (projData == null) {
                    Console.WriteLine("OCAD ID {0} not in the projection data list", ocadId);
                }
                else {
                    if (ocad2Epsg.CodeAuthority == "EPSG") {
                        if (projData.EPSG != ocad2Epsg.Code) {
                            Console.WriteLine("OCAD ID {0} listed in projection data as EPSG {1}, but in OCAD2EPSG as EPSG {2}", ocadId, projData.EPSG, ocad2Epsg.Code);
                        }
                    }
                    else if (ocad2Epsg.CodeAuthority == "SR-ORG") {
                        if (projData.SR != ocad2Epsg.Code) {
                            Console.WriteLine("OCAD ID {0} listed in projection data as SR {1}, but in OCAD2EPSG as SR {2}", ocadId, projData.SR, ocad2Epsg.Code);
                        }
                    }
                    else if (ocad2Epsg.CodeAuthority == "ESRI") {
                        if (projData.ESRI != ocad2Epsg.Code) {
                            Console.WriteLine("OCAD ID {0} listed in projection data as ESRI {1}, but in OCAD2EPSG as ESRI {2}", ocadId, projData.ESRI, ocad2Epsg.Code);
                        }
                    }
                }
            }

            readerCoordSystems.Close();
            readerOcad2Epsg.Close();
        }

        private static void FetchProjection(TextReader reader, TextWriter writer, TextWriter csWriter)
        {
            CsvReader csv = new CsvReader(reader);
            csv.Configuration.RegisterClassMap<OcadGrid2EpsgMapInput>();
            var records = csv.GetRecords<OcadGridId2Epsg>().ToList();
            List<ProjectionData> outputRecords = new List<ProjectionData>();

            for (int i = 0; i < records.Count; ++i) {
                Console.WriteLine("Processing {0} of {1}...", i + 1, records.Count);

                OcadGridId2Epsg ocadToEpsg = records[i];
                ProjectionData projectionData = new ProjectionData();
                projectionData.OCADId = ocadToEpsg.OcadCoordinateSystem;
                projectionData.OcadZone = ocadToEpsg.OcadZoneName;

                WebClient webClient = new WebClient();

                if (ocadToEpsg.CodeAuthority  == "EPSG") {
                    try {
                        projectionData.EPSG = ocadToEpsg.Code;
                        // EPSG.IO seems to have better data.
                        //data.Proj4 = webClient.DownloadString("http://spatialreference.org/ref/epsg/" + data.EPSG.ToString() + "/proj4/");
                        projectionData.Proj4 = webClient.DownloadString("http://epsg.io/" + projectionData.EPSG.ToString() + ".proj4");
                        outputRecords.Add(projectionData);
                    }
                    catch (WebException) {
                        Console.WriteLine("Failed to get data for ESPG={0}", projectionData.EPSG);
                    }
                }
                else if (ocadToEpsg.CodeAuthority == "ESRI") {
                    try {
                        projectionData.ESRI = ocadToEpsg.Code;
                        projectionData.Proj4 = webClient.DownloadString("http://spatialreference.org/ref/esri/" + projectionData.ESRI.ToString() + "/proj4/");
                        outputRecords.Add(projectionData);
                    }
                    catch (WebException) {
                        Console.WriteLine("Failed to get data for ESRI={0}", projectionData.ESRI);
                    }
                }
                else if (ocadToEpsg.CodeAuthority == "SR-ORG") {
                    try {
                        projectionData.SR = ocadToEpsg.Code;
                        projectionData.Proj4 = webClient.DownloadString("http://spatialreference.org/ref/sr-org/" + projectionData.SR.ToString() + "/proj4/");
                        outputRecords.Add(projectionData);
                    }
                    catch (WebException) {
                        Console.WriteLine("Failed to get data for SR={0}", projectionData.SR);
                    }
                }
            }

            outputRecords.Sort((r1, r2) => r1.OCADId.CompareTo(r2.OCADId));

            CsvWriter csvWriter = new CsvWriter(writer);
            csv.Configuration.RegisterClassMap<ProjectionDataMapOutput>();
            csvWriter.WriteRecords(outputRecords);

            csWriter.WriteLine("new OcadToProj4Projection[] {");
            foreach (var rec in outputRecords) {
                if (rec.Proj4 != null)
                    csWriter.WriteLine("    new OcadToProj4Projection({0}, @\"{1}\"),", rec.OCADId, rec.Proj4.Trim().Replace("\"", "\"\""));
            }
            csWriter.WriteLine("};");
        }
    }

    public class ProjectionData
    {
        public string OcadCoordinateSystem { get; set; }
        public string OcadZone { get; set; }
        public int OCADId { get; set; }
        public int EPSG { get; set; }
        public int ESRI { get; set; }
        public int SR { get; set; }
        public string Proj4 { get; set; }
    }

    public sealed class ProjectionDataMapInput: CsvClassMap<ProjectionData>
    {
        public ProjectionDataMapInput()
        {
            Map(m => m.OcadCoordinateSystem).Index(0);
            Map(m => m.OcadZone).Index(1);
            Map(m => m.OCADId).Index(2);
            Map(m => m.EPSG).Index(3).Default(0);
            Map(m => m.ESRI).Index(4).Default(0);
            Map(m => m.SR).Index(5).Default(0);
        }
    }

    public sealed class ProjectionDataMapOutput : CsvClassMap<ProjectionData>
    {
        public ProjectionDataMapOutput()
        {
            Map(m => m.OcadCoordinateSystem).Index(0);
            Map(m => m.OcadZone).Index(1);
            Map(m => m.OCADId).Index(2);
            Map(m => m.EPSG).Index(3).Default(0);
            Map(m => m.ESRI).Index(4).Default(0);
            Map(m => m.SR).Index(5).Default(0);
            Map(m => m.Proj4).Index(6);
        }
    }

    public class OcadGridId2Epsg
    {
        public int OcadCoordinateSystem { get; set; }
        public int Code { get; set; }
        public string CodeAuthority { get; set; }
        public string OcadZoneName { get; set; }
        public string Comment { get; set; }
    }

    public sealed class OcadGrid2EpsgMapInput : CsvClassMap<OcadGridId2Epsg>
    {
        public OcadGrid2EpsgMapInput()
        {
            Map(m => m.OcadCoordinateSystem).Index(0);
            Map(m => m.Code).Index(1).Default(0);
            Map(m => m.CodeAuthority).Index(2).Default("");
            Map(m => m.OcadZoneName).Index(3);
            Map(m => m.Comment).Index(4).Default("");
        }
    }



}
