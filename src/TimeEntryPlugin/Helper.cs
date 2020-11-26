using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeEntryPlugin
{
	internal static class Helper
	{
		internal const string Target = "Target";

		/// <summary>
		/// Returns dates collection for the period
		/// </summary>
		internal static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
		{
			for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
				yield return day;
		}
	}
}
