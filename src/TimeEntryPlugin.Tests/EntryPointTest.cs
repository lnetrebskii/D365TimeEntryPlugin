using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FakeXrmEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace TimeEntryPlugin.Tests
{
	[TestClass]
	public class EntryPointTest
	{
		[DataTestMethod]
		[DataRow("20100630", "20100702", new string[] { }, 2, DisplayName = "In the event that the start and end date are different then a time entry record should be created for every date in the date range from start to end date")]
		[DataRow("20100630", "20100702", new [] { "20100701" }, 1, DisplayName = "There are no duplicate time entry records created per date, 1 additional entry created.")]
		[DataRow("20100630", "20100702", new[] { "20100630", "20100701" }, 0)]
		[DataRow("20100630", "20100630", new string[] { }, 0, DisplayName = "In event when the start and end dates are different, than no additional entries created")]
		public void Should_CreateAdditionalEntries_When_PeriodIncludesMoreThan2NotBookedDates(string startAtStr, string endAtStr, string[] bookedDatesStrings, int additionalEntriesNumber)
		{
			var startAt = DateTime.ParseExact(startAtStr, "yyyyMMdd", CultureInfo.InvariantCulture);
			var endAt = DateTime.ParseExact(endAtStr, "yyyyMMdd", CultureInfo.InvariantCulture);
			var bookedDates = bookedDatesStrings
				.Select(x => DateTime.ParseExact(x, "yyyyMMdd", CultureInfo.InvariantCulture)).ToList();

			// Arrange
			var fakedContext = new XrmFakedContext();

			var bookableResource = new EntityReference(TimeEntryAttributes.BookableResource, Guid.NewGuid());
			var timeEntry = CreateTimeEntry(startAt, endAt, bookableResource);

			var inputParameter = new ParameterCollection();
			inputParameter.Add("Target", timeEntry);

			var pluginContext = fakedContext.GetDefaultPluginContext();
			pluginContext.Stage = 20;
			pluginContext.MessageName = "Create";
			pluginContext.PrimaryEntityName = TimeEntryAttributes.EntityName;
			pluginContext.PrimaryEntityId = timeEntry.Id;
			pluginContext.InputParameters = inputParameter;

			var existingEntries = bookedDates.Select(x => CreateTimeEntry(x, x, bookableResource)).ToList();
			fakedContext.Initialize(existingEntries);

			// Act
			fakedContext.ExecutePluginWithTarget(new EntryPoint(), timeEntry);

			// Assert
			var service = fakedContext.GetOrganizationService();
			var result = service.RetrieveMultiple(new QueryExpression(TimeEntryAttributes.EntityName) { ColumnSet = new ColumnSet(true) });
			var newEntities = result.Entities.Where(x => !existingEntries.Exists(t => t.Id == x.Id)).ToList();
			Assert.AreEqual(additionalEntriesNumber, newEntities.Count);
			Assert.AreEqual(timeEntry[TimeEntryAttributes.Start], timeEntry[TimeEntryAttributes.End]);
			var entities = result.Entities.Select(x => x[TimeEntryAttributes.Start]).ToList();
			entities.Add(timeEntry[TimeEntryAttributes.Start]);
			foreach (var day in EachDay(startAt, endAt))
			{
				Assert.IsTrue(entities.Contains(day));
			}
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidPluginExecutionException))]
		public void Should_ThrowException_When_AllEntriesForPeriodBooked()
		{
			Should_CreateAdditionalEntries_When_PeriodIncludesMoreThan2NotBookedDates("20100630", "20100702", new[] { "20100630", "20100701", "20100702" }, 0);
		}

		private static Entity CreateTimeEntry(DateTime startAt, DateTime endAt, EntityReference bookableResource)
		{
			var entity = new Entity(TimeEntryAttributes.EntityName, Guid.NewGuid());

			entity.Attributes.Add(TimeEntryAttributes.Start, startAt);
			entity.Attributes.Add(TimeEntryAttributes.End, endAt);
			entity.Attributes.Add(TimeEntryAttributes.BookableResource, bookableResource);

			return entity;
		}

		private static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
		{
			for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
				yield return day;
		}
	}
}
