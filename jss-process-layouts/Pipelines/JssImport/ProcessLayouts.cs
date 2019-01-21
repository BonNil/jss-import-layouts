using jss_process_layouts.Models;
using Newtonsoft.Json.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.JavaScriptServices.AppServices.Data;
using Sitecore.JavaScriptServices.AppServices.Pipelines.Import;
using Sitecore.JavaScriptServices.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace jss_process_layouts.Pipelines.JssImport
{
    public class ProcessLayouts : IImportPipelineProcessor
    {
        private Database _db;
        private IdManager _idManager;
        private AppConfiguration _app;

        public ProcessLayouts()
        {
            _db = Database.GetDatabase("master");
            _idManager = new IdManager();
        }

        public void Process(ImportPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, nameof(args));
            Assert.IsNotNull(args.ImportData, "args.ImportData is null");
            Assert.IsNotNull(args.App, "args.App is null");

            _app = args.App;

            var layouts = ParseLayouts(args.ImportData.AdditionalData["layouts"]);
            var placeholders = ParseAllPlaceholdersDefinedInLayouts(layouts);

            using (new Sitecore.SecurityModel.SecurityDisabler())
            {
                var placeholdersInPlay = CreatePlaceholderItems(placeholders);
                CreateLayoutItems(layouts, placeholdersInPlay);
            }
        }

        private IEnumerable<Layout> ParseLayouts(JToken layoutsJson)
        {
            var layoutsObj = layoutsJson.Value<JArray>();

            return layoutsObj.ToObject<Layout[]>();
        }

        private IEnumerable<Placeholder> ParseAllPlaceholdersDefinedInLayouts(IEnumerable<Layout> layouts)
        {
            var allPlaceholders = layouts.SelectMany(x => x.Placeholders);

            return allPlaceholders
              .GroupBy(x => x.Name)
              .Select(g => g.First());
        }

        private List<Item> CreatePlaceholderItems(IEnumerable<Placeholder> placeholders)
        {
            var placeholdersFolderItem = _db.GetItem(_app.PlaceholdersPath);
            var placeholdersTemplate = _db.GetItem(Sitecore.JavaScriptServices.Core.TemplateIDs.PlaceholderSetting);

            // placeholdersInPlay is essentially a collection of all placeholders that are mentioned at least once
            // in any of the imported layouts, whether it's a preexisting placeholder item or a newly created one.
            var placeholdersInPlay = new List<Item>();
            foreach (var placeholder in placeholders)
            {
                if (placeholdersFolderItem.Axes.GetDescendants().Any(x => x["Placeholder Key"] == placeholder.Key))
                {
                    placeholdersInPlay.Add(placeholdersFolderItem.Axes.GetDescendants().Where(x => x["Placeholder Key"] == placeholder.Key).First());
                    continue;
                }

                var calculatedId = _idManager.GetPredictableId(_app.Name, placeholder.Name, "layout-placeholders");
                _idManager.AssertIdIsUnused(calculatedId, string.Format("placeholder setting {0}", placeholder.Name));

                var newPlaceholderItem = placeholdersFolderItem.Add(placeholder.Name, placeholdersTemplate, calculatedId);
                newPlaceholderItem.Editing.BeginEdit();
                newPlaceholderItem["Placeholder Key"] = placeholder.Key;
                newPlaceholderItem.Editing.EndEdit();

                placeholdersInPlay.Add(newPlaceholderItem);
            }

            return placeholdersInPlay;
        }

        private void CreateLayoutItems(IEnumerable<Layout> layouts, List<Item> placeholdersInventory)
        {
            var indexOfLastSlash = _app.LayoutPath.LastIndexOf('/');
            var layoutFolderPath = (indexOfLastSlash > 0 ? _app.LayoutPath.Substring(0, indexOfLastSlash) : "/sitecore/layout/Layouts").TrimEnd('/');
            var layoutFolder = _db.GetItem(layoutFolderPath);
            var layoutTemplatePath = _app.LayoutTemplate;
            var layoutTemplate = (TemplateItem)_db.GetItem(layoutTemplatePath);

            foreach (var layout in layouts)
                CreateLayoutItem(layout, layoutFolder, layoutTemplate, placeholdersInventory);

        }

        private void CreateLayoutItem(Layout layout, Item layoutFolder, TemplateItem layoutTemplate, List<Item> placeholdersInventory)
        {
            var placeholdersRaw = BuildRawPlaceholdersValue(layout.Placeholders, placeholdersInventory);

            var existingLayout = GetExistingLayout(layoutFolder, layout.Name);
            if (existingLayout != null)
            {
                existingLayout.Editing.BeginEdit();
                existingLayout["Placeholders"] = placeholdersRaw.TrimEnd('|');
                existingLayout.Editing.EndEdit();

                return;
            }

            var calculatedId = _idManager.GetPredictableId(_app.Name, layout.Name, "layouts");
            _idManager.AssertIdIsUnused(calculatedId, string.Format("layout {0}", layout.Name));

            var newLayoutItem = layoutFolder.Add(layout.Name, layoutTemplate, calculatedId);
            newLayoutItem.Editing.BeginEdit();
            newLayoutItem.Appearance.DisplayName = layout.DisplayName;
            newLayoutItem["Placeholders"] = placeholdersRaw.TrimEnd('|');
            newLayoutItem.Editing.EndEdit();
        }

        private string BuildRawPlaceholdersValue(Placeholder[] placeholders, List<Item> placeholdersInventory)
        {
            var result = "";
            foreach (var placeholder in placeholders)
            {
                var placeholderItem = placeholdersInventory.Where(x => x["Placeholder Key"] == placeholder.Key).FirstOrDefault();
                result += $"{placeholderItem.ID.ToString()}|";
            }

            return result;
        }

        private Item GetExistingLayout(Item layoutFolder, string layoutName)
        {
            return layoutFolder.Axes.GetDescendants().Where(x => x.Name == layoutName).FirstOrDefault();
        }
    }
}
