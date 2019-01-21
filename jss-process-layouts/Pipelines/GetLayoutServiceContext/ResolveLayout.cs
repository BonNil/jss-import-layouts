using Sitecore.LayoutService.ItemRendering.Pipelines.GetLayoutServiceContext;
using System.Collections.Generic;

namespace jss_process_layouts.Pipelines.GetLayoutServiceContext
{
    public class ResolveLayout : IGetLayoutServiceContextProcessor
    {
        public const string _key = "layout";
        public void Process(GetLayoutServiceContextArgs args)
        {
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