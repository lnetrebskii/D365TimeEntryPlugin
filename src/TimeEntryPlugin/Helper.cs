using System;
using System.Collections.Generic;

namespace TimeEntryPlugin
{
	/// <summary>
	/// General utility methods
	/// </summary>
	internal static class Helper
	{
		internal const string Target = "Target";
		internal static readonly TimeZoneInfo EasternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");


		/// <summary>
		/// Returns dates collection for the period
		/// </summary>
		internal static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
		{
			for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
				yield return day;
		}

		internal static DateTime ToEst(this DateTime source)
		{
			var offset = EasternZone.BaseUtcOffset;
			var newDt = source + offset;
			return newDt;
		}

		internal static DateTime ToUtc(this DateTime source)
		{
			var offset = EasternZone.BaseUtcOffset;
			var newDt = source - offset;
			return newDt;
		}
	}
}
