﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.Templates.Core;
using Microsoft.Templates.Core.Locations;

namespace Microsoft.Templates.Test
{
    public class GenerationTestsFixture : IDisposable
    {
        internal string TestRunPath = @"..\..\TestRuns\{0}\";
        internal string TestProjectsPath;
        internal string TestPagesPath;

        private static readonly Lazy<TemplatesRepository> _repos = new Lazy<TemplatesRepository>(() => CreateNewRepos(), true);
        public static IEnumerable<ITemplateInfo> Templates => _repos.Value.GetAll();

        private static TemplatesRepository CreateNewRepos()
        {
            var repos = new TemplatesRepository(new LocalTemplatesLocation());
            repos.Sync();
            return repos;
        }

        public GenerationTestsFixture()
        {
            TestRunPath = string.Format(TestRunPath, DateTime.Now.ToString("yyyyMMdd_hhmm"));
            TestProjectsPath = Path.GetFullPath(Path.Combine(TestRunPath, "Projects"));
            TestPagesPath = Path.GetFullPath(Path.Combine(TestRunPath, "Pages"));
        }

        public void Dispose()
        {
            if (Directory.Exists(TestRunPath))
            {
                if ((!Directory.Exists(TestProjectsPath) || Directory.EnumerateDirectories(TestProjectsPath).Count() == 0) 
                    && (!Directory.Exists(TestPagesPath) || Directory.EnumerateDirectories(TestPagesPath).Count() == 0))
                {
                    Directory.Delete(TestRunPath, true);
                }
            }
        }
    }
}
