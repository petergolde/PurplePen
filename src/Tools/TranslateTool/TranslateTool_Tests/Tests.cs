using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TranslateTool;

namespace TranslateTool_Tests
{
    /// <summary>
    /// Summary description for Tests
    /// </summary>
    [TestClass]
    public class Tests
    {
        public Tests()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        // Get the test file direction
        public static string GetTestFileDirectory()
        {
            Uri uri = new Uri(typeof(Tests).Assembly.CodeBase);
            string callingPath = Path.GetDirectoryName(uri.LocalPath);
            return Path.GetFullPath(Path.Combine(callingPath, @"..\..\..\TestFiles"));
        }

        // Get a file from the test file directory.
        public static string GetTestFile(string basename)
        {
            return Path.GetFullPath(Path.Combine(GetTestFileDirectory(), basename));
        }

        [TestMethod]
        public void ShowTestDirectory()
        {
            Console.WriteLine(GetTestFileDirectory());
        }

        [TestMethod]
        public void ResXFileName()
        {
            string filename = GetTestFile("AboutForm.resx");
            ResXFile resXFile = new ResXFile(filename, new CultureInfo("de"));

            Assert.AreEqual(filename, resXFile.NonLocalizedFileName, true);
            Assert.AreEqual(GetTestFile("de\\AboutForm.de.resx"), resXFile.LocalizedFileName, true);
            Assert.AreEqual("de", resXFile.Culture.Name);
        }

        [TestMethod]
        public void ReadResources()
        {
            string filename = GetTestFile("AboutForm.resx");
            ResXFile resXFile = new ResXFile(filename, new CultureInfo("fr"));
            resXFile.Read();

            ICollection<LocString> strings = resXFile.AllStrings;

            foreach (LocString locstr in strings) {
                Console.WriteLine("Name:{0}    NonLocValue:{1}   LocValue:{2}   Comment:{3}", locstr.Name, locstr.NonLocalized, locstr.Localized, locstr.Comment);
            }
        }

        [TestMethod]
        public void WriteResources()
        {
            string filename = GetTestFile("AboutForm.resx");
            ResXFile resXFile = new ResXFile(filename, new CultureInfo("fr"));
            resXFile.Read();

            LocString str = resXFile.GetString("label1.Text");
            str.Localized = "Foo";
            resXFile.Write();


            resXFile = new ResXFile(filename, new CultureInfo("fr"));
            resXFile.Read();

            ICollection<LocString> strings = resXFile.AllStrings;

            foreach (LocString locstr in strings) {
                Console.WriteLine("Name:{0}    NonLocValue:{1}   LocValue:{2}   Comment:{3}", locstr.Name, locstr.NonLocalized, locstr.Localized, locstr.Comment);
            }
        }

        [TestMethod]
        public void ReadDirectory()
        {
            string directory = GetTestFileDirectory();

            ResourceDirectory resdir = new ResourceDirectory();
            resdir.ReadFiles(directory, new CultureInfo("de"));
            resdir.ReadResources();

            foreach (ResXFile resxfile in resdir.AllFiles) {
                Console.WriteLine("Nonlocalized ResXFile: {0}   LocalizedResXFile: {1}", resxfile.NonLocalizedFileName, resxfile.LocalizedFileName);
                Console.WriteLine("----------------------------------------------------------------");
                
                foreach (LocString locstr in resxfile.AllStrings) {
                    Console.WriteLine("    Name:{0}    NonLocValue:{1}   LocValue:{2}   Comment:{3}", locstr.Name, locstr.NonLocalized, locstr.Localized, locstr.Comment);
                }

                Console.WriteLine();
            }
        }

        [TestMethod]
        public void FindSDKTool()
        {
            string result = RunProgram.FindSDKTool("foo.exe");
            Assert.IsNull(result);

            result = RunProgram.FindSDKTool("winres.exe");
            Assert.AreEqual(@"c:\program files\microsoft sdks\windows\v6.1\bin\winres.exe", result, true);
        }

        [TestMethod]
        public void RunTool()
        {
            string exeName = RunProgram.FindSDKTool("winres.exe");

            RunProgram runner = new RunProgram();
            int exitCode = runner.Run(exeName, "", @"C:\");
            Console.WriteLine("Exit code: {0}", exitCode);
            Console.WriteLine("Output: ");
            Console.WriteLine(runner.Output);
        }

        [TestMethod]
        public void ConvertQuotedString()
        {
            string result;

            result = PoReader.ConvertQuotedString(@" hello ");
            Assert.AreEqual(" hello ", result);

            result = PoReader.ConvertQuotedString(@" h\nllo ");
            Assert.AreEqual(" h\r\nllo ", result);

            result = PoReader.ConvertQuotedString(@" hello\""");
            Assert.AreEqual(" hello\"", result);

            result = PoReader.ConvertQuotedString(@" he\\\\llo ");
            Assert.AreEqual(" he\\\\llo ", result);

            result = PoReader.ConvertQuotedString(@" he\0llo ");
            Assert.AreEqual(" he\0llo ", result);

            result = PoReader.ConvertQuotedString(@" he\0176llo ");
            Assert.AreEqual(" he\x7ello ", result);

        }

        string DumpLines(PoReader reader)
        {
            StringWriter writer = new StringWriter();
            PoReader.PoLine line;

            while ((line = reader.ReadLine()) != null) {
                writer.Write("{0}: ", reader.lineNumber);
                switch (line.kind) {
                case PoReader.PoLineKind.Blank:
                    writer.WriteLine("blank");  break;
                case PoReader.PoLineKind.Comment:
                    writer.WriteLine("comment"); break;
                case PoReader.PoLineKind.LocationComment:
                    writer.WriteLine("location: {0},{1}", line.str1, line.str2);  break;
                case PoReader.PoLineKind.KeywordString:
                    writer.WriteLine("kw({0}): '{1}'", line.str1, line.str2); break;
                case PoReader.PoLineKind.String:
                    writer.WriteLine("str: '{0}'", line.str1); break;
                default:
                    writer.WriteLine("unknown"); break;
                }
            }

            writer.WriteLine("EOF");

            return writer.ToString();
        }

        [TestMethod]
        public void ReadSamplePoLines()
        {
            PoReader reader = new PoReader(GetTestFile("SamplePo.po"));
            string result = DumpLines(reader);
            string expected = 
@"1: comment
2: comment
3: comment
4: kw(msgid): ''
5: kw(msgstr): ''
6: str: 'Project-Id-Version: purple-pen
'
7: str: 'Report-Msgid-Bugs-To: FULL NAME <EMAIL@ADDRESS>
'
8: blank
9: location: AboutForm.resx,freeLabel.Text
10: kw(msgid): 'Purple Pen is free software and may be copied and shared.'
11: kw(msgstr): 'Purple Pen est un logiciel libre et peut être copié et partagé.'
12: blank
13: location: AboutForm.resx,okButton.Text
14: location: AddCourse.resx,okButton.Text
15: kw(msgid): 'View Full License'
16: kw(msgstr): 'Voir la licence complète'
17: blank
18: blank
19: blank
20: location: AboutForm.resx,disclaimerLabel.Text
21: kw(msgid): ''
22: str: 'THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ""AS '
23: str: 'IS"" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, '
24: kw(msgstr): ''
25: str: 'CE LOGICIEL EST FOURNI ""TEL QUEL"" PAR LES DÉTENTEURS DU COPYRIGHT ET PAR '
26: str: 'LES CONTRIBUTEURS. TOUTES GARANTIES EXPLICITES OU IMPLICITES (INCLUANT, MAIS '
EOF
";

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ReadSamplePoEntries()
        {
            PoReader reader = new PoReader(GetTestFile("SamplePo.po"));
            List<PoEntry> entries = reader.ReadPo();
            StringWriter writer = new StringWriter();

            foreach (PoEntry entry in entries) {
                writer.WriteLine("====Entry===");
                writer.WriteLine("English: '{0}'", entry.NonLocalized);
                writer.WriteLine("Translated: '{0}'", entry.Localized);
                writer.WriteLine("Locations:");
                foreach (PoLocation location in entry.Locations) {
                    writer.WriteLine("    {0},{1}", location.FileName, location.Name);
                }
            }

            string result = writer.ToString();
            string expected = 
@"====Entry===
English: 'Purple Pen is free software and may be copied and shared.'
Translated: 'Purple Pen est un logiciel libre et peut être copié et partagé.'
Locations:
    AboutForm.resx,freeLabel.Text
====Entry===
English: 'View Full License'
Translated: 'Voir la licence complète'
Locations:
    AboutForm.resx,okButton.Text
    AddCourse.resx,okButton.Text
====Entry===
English: 'THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ""AS IS"" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, '
Translated: 'CE LOGICIEL EST FOURNI ""TEL QUEL"" PAR LES DÉTENTEURS DU COPYRIGHT ET PAR LES CONTRIBUTEURS. TOUTES GARANTIES EXPLICITES OU IMPLICITES (INCLUANT, MAIS '
Locations:
    AboutForm.resx,disclaimerLabel.Text
";

            Assert.AreEqual(expected, result);
        }
    }
}
