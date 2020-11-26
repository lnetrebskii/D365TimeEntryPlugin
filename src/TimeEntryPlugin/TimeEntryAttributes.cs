using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeEntryPlugin
{
	/// <summary>
	/// TimeEntry attributes names constants
	/// </summary>
	public static class TimeEntryAttributes
	{
		public static readonly string EntityName = "msdyn_timeentry";
		public static readonly string TimeEntryId = "msdyn_timeentryid";
		public static readonly string Start = "msdyn_start";
		public static readonly string End = "msdyn_end";
		public static readonly string Duration = "msdyn_duration";
		public static readonly string BookableResource = "msdyn_bookableresource";
	}
}
