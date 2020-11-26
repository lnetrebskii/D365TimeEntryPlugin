﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace TimeEntryPlugin
{
	/// <summary>
	/// Business logic for the plugin
	/// </summary>
	public static class BusinessLogic
	{
		private const string NothingToCreate = "All Time Entries for the specified period exist. Nothing has been created.";

		/// <summary>
		/// Business logic entry point.
		/// </summary>
		public static void Execute(Entity entity, IOrganizationService service, ITracingService tracingService)
		{
			var startAt = (DateTime)entity[TimeEntryAttributes.Start];
			var endAt = (DateTime)entity[TimeEntryAttributes.End];

			if (startAt.Date == endAt.Date)
			{
				SetTimeEntryDate(entity, startAt.Date);
				return;
			}

			var datesToCreate = GetDatesToCreateTimeEntries(entity, service, startAt, endAt);
			ThrowExceptionIfAllDatesOccupied(datesToCreate);

			SetTargetEntityDateToTheFirstAvailableOne(entity, datesToCreate);

			foreach (var date in datesToCreate)
			{
				CreateTimeEntryForDate(entity, service, tracingService, date);
			}
		}

		private static void SetTargetEntityDateToTheFirstAvailableOne(Entity entity, List<DateTime> datesToCreate)
		{
			SetTimeEntryDate(entity, datesToCreate[0]);
			datesToCreate.RemoveAt(0);
		}

		private static void CreateTimeEntryForDate(Entity entity, IOrganizationService service, ITracingService tracingService,
			DateTime date)
		{
			var followup = new Entity(TimeEntryAttributes.EntityName);

			foreach (var attribute in entity.Attributes)
			{
				if (attribute.Key != TimeEntryAttributes.TimeEntryId)
				{
					followup[attribute.Key] = attribute.Value;
				}
			}

			SetTimeEntryDate(followup, date);

			tracingService.Trace("TimeEntryPlugin: Creating a Time Entry.");
			service.Create(followup);
		}

		private static void ThrowExceptionIfAllDatesOccupied(List<DateTime> datesToCreate)
		{
			if (datesToCreate.Count == 0)
			{
				throw new InvalidPluginExecutionException(NothingToCreate);
			}
		}

		private static List<DateTime> GetDatesToCreateTimeEntries(Entity entity, IOrganizationService service, DateTime startAt,
			DateTime endAt)
		{
			var existingDates = GetExistingTimerEntriesForPeriod(service, (EntityReference) entity[TimeEntryAttributes.BookableResource],
				startAt, endAt);

			return Helper.EachDay(startAt, endAt).Except(existingDates).ToList();
		}

		private static void SetTimeEntryDate(Entity entity, DateTime date)
		{
			entity[TimeEntryAttributes.Start] = date;
			entity[TimeEntryAttributes.End] = date;
			entity[TimeEntryAttributes.Duration] = 0;
		}

		private static IEnumerable<DateTime> GetExistingTimerEntriesForPeriod(IOrganizationService service, EntityReference entityReference, DateTime startAt, DateTime endAt)
		{
			var query = new QueryExpression(TimeEntryAttributes.EntityName)
			{
				ColumnSet = new ColumnSet(TimeEntryAttributes.Start, TimeEntryAttributes.End),
				Criteria = new FilterExpression(LogicalOperator.And)
			};
			query.Criteria.AddCondition(TimeEntryAttributes.BookableResource, ConditionOperator.Equal, entityReference.Id);
			query.Criteria.AddCondition(TimeEntryAttributes.Start, ConditionOperator.GreaterEqual, startAt.Date);
			query.Criteria.AddCondition(TimeEntryAttributes.End, ConditionOperator.LessEqual, endAt.Date.AddDays(1).AddMilliseconds(-1));

			var results = service.RetrieveMultiple(query);
			var dates = new List<DateTime>();

			results.Entities.ToList().ForEach(entity =>
			{
				var entityStartAt = (DateTime)entity[TimeEntryAttributes.Start];
				var entityEndAt = (DateTime)entity[TimeEntryAttributes.End];
				dates = Helper.EachDay(entityStartAt, entityEndAt).Union(dates).Distinct().ToList();
			});

			return dates;
		}
	}
}