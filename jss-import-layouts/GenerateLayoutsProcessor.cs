using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Abstractions;
using Sitecore.JavaScriptServices.AppServices.Pipelines.Import;

namespace jss_import_layouts
{
    public class GenerateLayoutsProcessor : IImportPipelineProcessor
    {
        public void Process(ImportPipelineArgs args)
        {
            Sitecore.Diagnostics.Log.Info("GenerateLayoutsProcessor Triggered!", this);
        }
    }
}
