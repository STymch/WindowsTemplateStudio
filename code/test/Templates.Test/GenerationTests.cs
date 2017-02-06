using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.Templates.Core;
using Microsoft.Templates.Wizard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Templates.Test
{
    public class GenerationTests : IClassFixture<GenerationTestsFixture>
    {
        private GenerationTestsFixture fixture;
        private const string Platform = "x86";
        private const string Configuration = "Debug";

        public GenerationTests(GenerationTestsFixture fixture)
        {
            this.fixture = fixture;
        }


        [Theory, MemberData("GetProjectTemplates"), Trait("Type", "ProjectGeneration")]
        public void GenerateEmptyProject(string projId)
        {
            var projectTemplate = GenerationTestsFixture.Templates.Where(t => t.Identity == projId).FirstOrDefault();

            var projectName = $"{projectTemplate.GetFramework()}{projectTemplate.GetProjectType()}";

            var generator = new TemplatesGen(new FakeGenShell(projectName, fixture.TestProjectsPath, new TextBlock()));
            var genItems = new List<GenInfo>
            {
                GetProjectGenInfo(projectTemplate, projectName)
            };
        
            generator.Generate(genItems);

            //Build solution
            var outputPath = Path.Combine(fixture.TestProjectsPath, projectName);
            var outputFile = Path.Combine(outputPath, "_buildOutput.txt");
            int exitCode = BuildSolution(projectName, outputPath, outputFile);

            //Assert
            Assert.True(exitCode.Equals(0), string.Format("Solution {0} was not built successfully. Please see {1} for more details.", projectTemplate.Name, Path.GetFullPath(outputFile)));

            //Clean
            Directory.Delete(outputPath, true);

        }

        
        [Theory, MemberData("GetPageTemplates"), Trait("Type", "PageGeneration")]
        public void GeneratePage(string pageId, string projId)
        {
            var targetProjectTemplate = GenerationTestsFixture.Templates.Where(t => t.Identity == projId).FirstOrDefault();
            var pageTemplates = GenerationTestsFixture.Templates.Where(t => t.Identity == pageId).FirstOrDefault();

            var projectName = $"{targetProjectTemplate.GetFramework()}{targetProjectTemplate.GetProjectType()}";

            var generator = new TemplatesGen(new FakeGenShell(projectName, fixture.TestPagesPath, new TextBlock()));
            var genItems = new List<GenInfo>
            {
                GetProjectGenInfo(targetProjectTemplate, projectName)
            };


            genItems.Add(GetPageGenInfo(pageTemplates));

            generator.Generate(genItems);

            //Build solution
            var outputPath = Path.Combine(fixture.TestPagesPath, projectName);
            var outputFile = Path.Combine(outputPath, "_buildOutput.txt");
            int exitCode = BuildSolution(projectName, outputPath, outputFile);

            //Assert
            Assert.True(exitCode.Equals(0), string.Format("Solution {0} was not built successfully. Please see {1} for more details.", targetProjectTemplate.Name, Path.GetFullPath(outputFile)));

            //Clean
            Directory.Delete(outputPath, true);

        }

        [Theory, MemberData("GetProjectTemplates"), Trait("Type", "ProjectGeneration")]
        public void GenerateProjectWithAllPages(string projId)
        {
            var targetProjectTemplate = GenerationTestsFixture.Templates.Where(t => t.Identity == projId).FirstOrDefault();

            var projectName = $"{targetProjectTemplate.GetFramework()}{targetProjectTemplate.GetProjectType()}All";

            var generator = new TemplatesGen(new FakeGenShell(projectName, fixture.TestProjectsPath, new TextBlock()));
            var genItems = new List<GenInfo>
            {
                GetProjectGenInfo(targetProjectTemplate, projectName)
            };

            var pageTemplates = GenerationTestsFixture.Templates
                .Where(t => t.GetFramework() == targetProjectTemplate.GetFramework() &&
                t.GetType() == t.GetType() &&
                t.GetTemplateType() == TemplateType.Page);

            genItems.AddRange(GetPagesGenInfo(pageTemplates));

            generator.Generate(genItems);

            //Build solution
            var outputPath = Path.Combine(fixture.TestProjectsPath, projectName);
            var outputFile = Path.Combine(outputPath, "_buildOutput.txt");
            int exitCode = BuildSolution(projectName, outputPath, outputFile);

            //Assert
            Assert.True(exitCode.Equals(0), string.Format("Solution {0} was not built successfully. Please see {1} for more details.", targetProjectTemplate.Name, Path.GetFullPath(outputFile)));

            //Clean
            Directory.Delete(outputPath, true);
        }


        private static GenInfo GetProjectGenInfo(ITemplateInfo targetProjectTemplate, string projectName)
        {
            var genInfo = new GenInfo()
            {
                Name = projectName,
                Template = targetProjectTemplate
            };
            genInfo.Parameters.Add("UserName", Environment.UserName);
            return genInfo;
        }

        private static GenInfo GetPageGenInfo(ITemplateInfo pageTemplate)
        {
            return new GenInfo()
            {
                Name = Naming.Infer(new List<string>(), pageTemplate.Name),
                Template = pageTemplate
            };
        }


        private static IEnumerable<GenInfo> GetPagesGenInfo(IEnumerable<ITemplateInfo> pageTemplates)
        {
            var pageGenInfos = new List<GenInfo>();
            var usedNames = new List<string>();
            foreach (var pageTemplate in pageTemplates)
            {
                var pageGenInfo = new GenInfo()
                {
                    Name = Naming.Infer(usedNames, pageTemplate.Name),
                    Template = pageTemplate
                };
                pageGenInfos.Add(pageGenInfo);
                usedNames.Add(pageGenInfo.Name);
            }
            return pageGenInfos;
        }



        public static IEnumerable<object[]> GetPageTemplates()
        {
            var pageTemplates = GenerationTestsFixture.Templates.Where(t => t.GetTemplateType() == TemplateType.Page);

            foreach (var template in pageTemplates)
            {
                var projectTemplate = GenerationTestsFixture.Templates.Where(t => t.GetFramework() == template.GetFramework() && t.GetTemplateType() == TemplateType.Project).FirstOrDefault();
                if (projectTemplate != null)
                {
                    yield return new object[] { template.Identity, projectTemplate.Identity };
                }
            }
        }

        public static IEnumerable<object[]> GetProjectTemplates()
        {
            var projectTemplates = GenerationTestsFixture.Templates.Where(t => t.GetTemplateType() == TemplateType.Project);
            foreach (var template in projectTemplates)
            {
                yield return new object[] { template.Identity };
            }
        }

        private static int BuildSolution(string solutionName, string outputPath, string outputFile)
        {
            //Build
            var solutionFile = Path.GetFullPath(outputPath + @"\" + solutionName + ".sln");
            var startInfo = new ProcessStartInfo(GetPath("RestoreAndBuild.bat"))
            {
                Arguments = $"\"{solutionFile}\" {Platform} {Configuration}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = false,
                WorkingDirectory = outputPath
            };

            var process = Process.Start(startInfo);
            File.WriteAllText(outputFile, process.StandardOutput.ReadToEnd());
            process.WaitForExit();

            return process.ExitCode;
        }

        internal static string GetPath(string fileName)
        {
            string path = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, fileName);
            if (!File.Exists(path))
            {
                path = Path.GetFullPath($@".\{fileName}");
                if (!File.Exists(path))
                {
                    throw new ApplicationException($"Can not find {fileName}");
                }
            }
            return path;
        }
    }
}
