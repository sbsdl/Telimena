﻿CREATE PROCEDURE [dbo].[p_GetUserActivitySummary]
	@programIds nvarchar (max),
	@startDate datetime,
	@endDate datetime
AS

DECLARE @TempResults 
TABLE(
UserId nvarchar(255), 
FileVersion nvarchar(255), 
FirstSeen datetime, 
LastActive dateTime, 
ActivityScore int)


INSERT INTO @TempResults 
SELECT cau.UserIdentifier, ed.FileVersion, cau.FirstSeenDate, MAX(ed.Timestamp), COUNT(ed.Id)
FROM EventTelemetryDetails ed
JOIN EventTelemetrySummaries ets ON ed.TelemetrySummaryId = ets.Id
JOIN ClientAppUsers cau ON cau.Id = ets.ClientAppUserId
WHERE ets.Id IN (
		SELECT Id FROM EventTelemetrySummaries 
		WHERE EventId IN (
			SELECT Id FROM Events v WHERE v.ProgramId IN (SELECT Item FROM f_SplitInts(@programIds, ','))
		)
	)
AND Timestamp BETWEEN @startDate AND @endDate
GROUP BY cau.UserIdentifier, ed.FileVersion, cau.FirstSeenDate

INSERT INTO @TempResults
SELECT cau.UserIdentifier,ed.FileVersion, cau.FirstSeenDate, MAX(ed.Timestamp), COUNT(ed.Id)
FROM ViewTelemetryDetails ed
JOIN ViewTelemetrySummaries ets ON ed.TelemetrySummaryId = ets.Id
JOIN ClientAppUsers cau ON cau.Id = ets.ClientAppUserId
WHERE ets.Id IN (
		SELECT Id FROM ViewTelemetrySummaries 
		WHERE ViewId IN (
			SELECT Id FROM Views v WHERE v.ProgramId IN (SELECT Item FROM f_SplitInts(@programIds, ','))
		)
	)
AND Timestamp BETWEEN @startDate AND @endDate
GROUP BY cau.UserIdentifier, ed.FileVersion, cau.FirstSeenDate

SELECT UserId,
FileVersion, 
FirstSeen, 
LastActive, 
ActivityScore =
	(SELECT SUM(ActivityScore) FROM
		@TempResults r WHERE r.UserId = results.UserId) 
FROM @TempResults results
WHERE LastActive = 
	(SELECT MAX(LastActive) 
		FROM @TempResults r WHERE r.UserId = results.UserId)
GROUP BY UserId,FileVersion, FirstSeen, LastActive
ORDER BY ActivityScore DESC