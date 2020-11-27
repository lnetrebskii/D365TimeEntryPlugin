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
		[DataRow("2010-06-30T00:00:00Z", "2010-06-30T05:00:00Z", new string[] { }, 1, DisplayName = "Consider EST timezone when create Time Entries.")]
		[DataRow("2010-06-30T05:00:00Z", "2010-07-02T05:00:00Z", new string[] { }, 2, DisplayName = "In the event that the start and end date are different then a time entry record should be created for every date in the date range from start to end date")]
		[DataRow("2010-06-30T05:00:00Z", "2010-07-02T05:00:00Z", new [] { "2010-07-01T05:00:00Z" }, 1, DisplayName = "There are no duplicate time entry records created per date, 1 additional entry created.")]
		[DataRow("2010-06-30T05:00:00Z", "2010-07-02T05:00:00Z", new[] { "2010-06-30T05:00:00Z", "2010-07-01T05:00:00Z" }, 0)]
		[DataRow("2010-06-30T05:00:00Z", "2010-06-30T05:00:00Z", new string[] { }, 0, DisplayName = "In event when the start and end dates are different, than no additional entries created")]
		public void Should_CreateAdditionalEntries_When_PeriodIncludesMoreThan2NotBookedDates(string startAtStr, string endAtStr, string[] bookedDatesStrings, int additionalEntriesNumber)
		{
			var startAt = ParseTestInputDateTime(startAtStr);
			var endAt = ParseTestInputDateTime(endAtStr);
			var bookedDates = bookedDatesStrings
				.Select(ParseTestInputDateTime).ToList();

			// Arrange
			var fakedContext = CreateTimeEntryInContext(startAt, endAt, bookedDates, out var timeEntry, out var existingEntries);

			// Act
			fakedContext.ExecutePluginWithTarget(new EntryPoint(), timeEntry);

			// Assert
			var service = fakedContext.GetOrganizationService();
			var result = service.RetrieveMultiple(new QueryExpression(TimeEntryAttributes.EntityName) { ColumnSet = new ColumnSet(true) });
			var newEntities = result.Entities.Where(x => !existingEntries.Exists(t => t.Id == x.Id)).ToList();
			Assert.AreEqual(additionalEntriesNumber, newEntities.Count);
			Assert.AreEqual(timeEntry[TimeEntryAttributes.Start], timeEntry[TimeEntryAttributes.End]);
			var entities = result.Entities.Select(x => (DateTime)x[TimeEntryAttributes.Start]).ToList();
			entities.Add((DateTime)timeEntry[TimeEntryAttributes.Start]);
			entities = entities.ConvertAll(x => x.Date).ToList();
			foreach (var day in EachDay(startAt, endAt))
			{
				Assert.IsTrue(entities.Contains(day));
			}
		}

		[DataTestMethod]
		[DataRow("2010-06-30T05:00:00Z", "2010-07-02T05:00:00Z", new[] { "2010-06-30T05:00:00Z", "2010-07-01T05:00:00Z", "2010-07-02T05:00:00Z" })]
		[DataRow("2010-06-30T05:00:00Z", "2010-06-30T05:00:00Z", new[] { "2010-06-30T05:00:00Z" })]
		public void Should_ThrowException_When_AllEntriesForPeriodOccupied(string startAtStr, string endAtStr, string[] bookedDatesStrings)
		{
			var startAt = ParseTestInputDateTime(startAtStr);
			var endAt = ParseTestInputDateTime(endAtStr);
			var bookedDates = bookedDatesStrings
				.Select(ParseTestInputDateTime).ToList();

			// Arrange
			var fakedContext = CreateTimeEntryInContext(startAt, endAt, bookedDates, out var timeEntry, out _);

			// Act & Assert
			Assert.ThrowsException<InvalidPluginExecutionException>(() => fakedContext.ExecutePluginWithTarget(new EntryPoint(), timeEntry));
		}

		private static DateTime ParseTestInputDateTime(string x) => DateTime.Parse(x).ToUniversalTime();

		private static XrmFakedContext CreateTimeEntryInContext(DateTime startAt, DateTime endAt, 
			List<DateTime> bookedDates, out Entity timeEntry, out List<Entity> existingEntries)
		{
			var fakedContext = new XrmFakedContext();
			var bookableResource = new EntityReference(TimeEntryAttributes.BookableResource, Guid.NewGuid());
			timeEntry = CreateTimeEntry(startAt, endAt, bookableResource);

			var inputParameter = new ParameterCollection {{"Target", timeEntry}};

			var pluginContext = fakedContext.GetDefaultPluginContext();
			pluginContext.Stage = 20;
			pluginContext.MessageName = "Create";
			pluginContext.PrimaryEntityName = TimeEntryAttributes.EntityName;
			pluginContext.PrimaryEntityId = timeEntry.Id;
			pluginContext.InputParameters = inputParameter;

			existingEntries = bookedDates.Select(x => CreateTimeEntry(x, x, bookableResource)).ToList();
			fakedContext.Initialize(existingEntries);
			return fakedContext;
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
