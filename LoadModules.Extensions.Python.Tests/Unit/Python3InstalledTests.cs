﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine.Job;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;
using Rhino.Mocks;
using Tests.Common;

namespace LoadModules.Extensions.Python.Tests.Unit
{
    public class Python3InstalledTests
    {
        [SetUp]
        public void IsPython3Installed()
        {
            PythonDataProvider p = new PythonDataProvider();
            p.Version = PythonVersion.Version3;
            try
            {
                string version = p.GetPythonVersion();

                Console.WriteLine("Found python version:" + version);
            }
            catch (Exception e)
            {
                Console.WriteLine("Tests are inconclusive because python version 3 is not installed in the expected location");

                Console.WriteLine(e.ToString());
                Assert.Inconclusive();
            }
        }

        [Test]
        [ExpectedException(ExpectedMessage = "SyntaxError: Missing parentheses in call to 'print'", MatchType = MessageMatch.Contains)]
        public void PythonScript_Version3_DodgySyntax()
        {
            string MyPythonScript = @"print 'Hello World'";

            File.Delete("Myscript.py");
            File.WriteAllText("Myscript.py", MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version3;
            provider.FullPathToPythonScriptToRun = "Myscript.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 0;

            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
        }

        [Test]
        public void PythonScript_ValidScript()
        {
            string MyPythonScript = @"print (""Hello World"")";

            File.Delete("Myscript.py");
            File.WriteAllText("Myscript.py", MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version3;
            provider.FullPathToPythonScriptToRun = "Myscript.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 0;

            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            //new MockRepository().DynamicMock<IDataLoadJob>()
            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
        }

    }
}
