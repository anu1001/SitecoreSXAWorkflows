using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;
using Sitecore.XA.Foundation.SitecoreExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace ServiceMaster.SVMBrands.Foundation.Workflows.EventHandlers
{
    public class SiteAndWorkflow
    {
        public string site { get; set; }
        public string workflow { get; set; }
    }
    public class ItemWorkflowEventHandler
    {
        private readonly List<ID> IncludeTemplates;
        private readonly List<string> IncludePaths;
        private readonly List<string> ExcludePaths;
        private readonly List<SiteAndWorkflow> IncludedSites;
        //private readonly List<WorkflowType> Workflows;


        public string Workflow { get; set; }

        public string DefaultWorkflowState { get; set; }

        Database database = Factory.GetDatabase("master");

        public ItemWorkflowEventHandler()
        {
            IncludeTemplates = new List<ID>();
            IncludePaths = new List<string>();
            ExcludePaths = new List<string>();
            IncludedSites = new List<SiteAndWorkflow>();
            //Workflows = new List<WorkflowType>();

        }
        public void IncludeTemplate(string templateId)
        {
            if (!string.IsNullOrWhiteSpace(templateId))
            {
                this.IncludeTemplates.Add(new ID(templateId.ToLower()));
            }
        }
        //public void IncludeWorkflow(string workflow)
        //{
        //    if (!string.IsNullOrWhiteSpace(Workflow))
        //    {
        //        //WorkflowType workflowtype = new WorkflowType();
        //        workflowtype.Workflow = workflow;
        //        this.Workflows.Add(workflowtype);
        //    }
        //}
        public void IncludePath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.IncludePaths.Add(path.ToLower());
            }
        }

        public void IncludeSite(XmlNode node)
        {
            if (node != null)
            {
                this.IncludedSites.Add(new SiteAndWorkflow() { site = node.Attributes["name"].Value, workflow = node.Attributes["workflow"].Value });
            }
        }

        public void ExcludePath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.ExcludePaths.Add(path.ToLower());
            }
        }

        public void OnItemCreated(object sender, EventArgs args)
        {

            //var workflows = this.Workflows;
            if (JobsHelper.IsPublishing())
            {
                return;
            }
            var item = Event.ExtractParameter<ItemCreatedEventArgs>(args, 0).Item;
            if (CanExecute(item))
            {
                var workflow = GetSiteWorkflow(item);
                if (workflow != null)
                {
                    Workflow = workflow.WorkflowID;
                    DefaultWorkflowState = GetWorkflowState(workflow);
                    using (EditContext editContext = new EditContext(item))
                    {
                        item[FieldIDs.Workflow] = Workflow;
                        item[FieldIDs.DefaultWorkflow] = Workflow;
                        item[FieldIDs.WorkflowState] = DefaultWorkflowState;
                    }
                }
            }
        }

        private bool CanExecute(Item item)
        {
            return item != null && string.IsNullOrWhiteSpace(item[FieldIDs.Workflow]) && IsMatchingPath(item, IncludePaths) && !IsMatchingPath(item, ExcludePaths) && IsMatchingTemplate(item, IncludeTemplates);
            //&& !IsDerivedFromTemplate(item, excludeTemplateIds);
        }

        private IWorkflow GetSiteWorkflow(Item item)
        {
            //var site = item.Axes.GetAncestors().Where(t => t.TemplateID.ToString().Equals("{8798C6EF-AA7C-42EC-883C-A217165F1104}")).FirstOrDefault();

            if (IncludedSites.Any(name => item.Paths.Path.ToLower().Contains(name.site)))
            {
                var currentworkflow = IncludedSites.Where(x => item.Paths.Path.ToLower().Contains(x.site)).Select(w => w.workflow).FirstOrDefault();
                var currentSiteWorkflow = database.WorkflowProvider.GetWorkflow(currentworkflow);
                if (currentSiteWorkflow != null)
                    return currentSiteWorkflow;
            }
            return null;
        }

        private string GetWorkflowState(IWorkflow workflow)
        {
            string workflowState = database.GetItem(workflow.WorkflowID).Fields["Initial State"].Value;
            if (!string.IsNullOrEmpty(workflowState))
                return workflowState;
            else return string.Empty;
        }

        private bool IsMatchingTemplate(Item item, List<ID> templateIds)
        {
            return templateIds.Any(tid => item.TemplateID == tid);
        }
        private bool IsDerivedFromTemplate(Item item, List<ID> templateIds)
        {
            //return templateIds.Any(templateid => item.IsDerived(templateid));
            return true;
        }

        private bool IsMatchingPath(Item item, List<string> paths)
        {
            return paths.Any(path => item.Paths.FullPath.StartsWith(path, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}