using Sitecore.LayoutService.ItemRendering.Pipelines.GetLayoutServiceContext;
using System.Collections.Generic;
using Sitecore.Diagnostics;

namespace jss_process_layouts.Pipelines.GetLayoutServiceContext
{
    public class ResolveLayout : IGetLayoutServiceContextProcessor
    {
        public const string _key = "layout";
        public void Process(GetLayoutServiceContextArgs args)
        {
            Assert.IsNotNull(args, "args is null");

            if (args.RenderedItem == null)
            {
                Log.Warn("args.RenderedItem is null in ResolveLayout", this);
                return;
            }

            var layout = args.RenderedItem.Visualization.Layout;

            IDictionary<string, object> contextData = args.ContextData;
            var data = new
            {
                name = layout.Name,
                id = layout.ID.ToString()
            };
            contextData.Add(_key, (object)data);
        }
    }
}