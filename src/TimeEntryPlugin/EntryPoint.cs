using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Web;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;


namespace TimeEntryPlugin
{
	/// <summary>
	/// On creation of a Time Entry record the plugin should evaluate if the start and end date contain different values from each other.
	/// In the event that the start and end date are different then a time entry record should be created for every date in the date range from start to end date.
	/// The plugin should also ensure that there are no duplicate time entry records created per date.  
	/// </summary>
	public class EntryPoint: IPlugin
    {
		/// <summary>
		/// Useful info https://docs.microsoft.com/en-us/dynamics365/project-operations/time/customize-weekly-time-entry-grid
		/// </summary>
		/// <param name="serviceProvider"></param>
		public void Execute(IServiceProvider serviceProvider)
	    {
			// Obtain the tracing service
			ITracingService tracingService =
				(ITracingService)serviceProvider.GetService(typeof(ITracingService));

			// Obtain the execution context from the service provider.  
			IPluginExecutionContext context = (IPluginExecutionContext)
				serviceProvider.GetService(typeof(IPluginExecutionContext));
			
			// The InputParameters collection contains all the data passed in the message request.  
			if (context.InputParameters.Contains(Helper.Target) &&
			    context.InputParameters[Helper.Target] is Entity)
			{
				// Obtain the target entity from the input parameters.  
				Entity entity = (Entity)context.InputParameters[Helper.Target];

				// Obtain the organization service reference which you will need for  
				// web service calls.  
				IOrganizationServiceFactory serviceFactory =
					(IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
				IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

				try
				{
					BusinessLogic.Execute(entity, service, tracingService);
				}
				catch (FaultException<OrganizationServiceFault> ex)
				{
					throw new InvalidPluginExecutionException($"An error occurred in TimeEntryPlugin.", ex);
				}
				catch (Exception ex)
				{
					tracingService.Trace("TimeEntryPlugin: {0}", ex.ToString());
					throw;
				}
			}
		}
    }
}
