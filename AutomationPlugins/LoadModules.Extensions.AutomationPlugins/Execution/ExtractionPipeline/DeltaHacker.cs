﻿using System;
using System.Linq;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataLoad.Triggers;
using Rdmp.Core.ReusableLibraryCode.Exceptions;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;

public class DeltaHacker
{
    private readonly AutomateExtractionRepository _repository;
    private readonly ExtractDatasetCommand _extractDatasetCommand;

    public DeltaHacker(AutomateExtractionRepository repository, ExtractDatasetCommand extractDatasetCommand)
    {
        _repository = repository;
        _extractDatasetCommand = extractDatasetCommand;
    }

    public BaselineHackEvaluation ExecuteHackIfAllowed(IDataLoadEventListener listener,out string hackSql)
    {
        //get ready for hack city!


        //dataset must have a single hic_validFrom field
        var validFromField = GetValidFromField(listener);
        hackSql = null;

        if(validFromField == null)
            return BaselineHackEvaluation.DatasetNotCompatibleWithBaselining;

        //there must be an existing baseline

        //get the ids that the request is for
        var configId = _extractDatasetCommand.Configuration.ID;

        //find the automation record
        var automateExtraction = _repository.GetAllObjects<AutomateExtraction>(
            $"WHERE ExtractionConfiguration_ID = {configId}");

        //there should be one! and only 1
        if(automateExtraction.Length != 1)
            throw new Exception(
                $"No AutomateExtraction was found for ExtractionConfiguration '{_extractDatasetCommand.Configuration}'");

        if(automateExtraction[0].BaselineDate == null)
            return BaselineHackEvaluation.NoCompatibleBaselineAvailable;

        //see if there is an audit of the previous execution success for this dataset
        var previousSuccess = automateExtraction[0].GetSuccessIfAnyFor(_extractDatasetCommand.DatasetBundle.DataSet);

        //no there is no record of a succesful extraction so a full baseline is needed
        if(previousSuccess == null)
            return BaselineHackEvaluation.NoCompatibleBaselineAvailable;

        //ok there is a record of the last succesful extraction let's see if the SQL is still the same
        var currentSQL = _extractDatasetCommand.QueryBuilder.SQL;
        var oldSQL = previousSuccess.SQL;

        //nope, the SQL is different, maybe the user has snuck in an extra column or some other thing
        if(!string.Equals(currentSQL.Trim(),oldSQL.Trim(),StringComparison.CurrentCultureIgnoreCase))
        {
            listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning,
                $"SQL is out of date for baseline of {_extractDatasetCommand}.  The old success will be deleted now and a new baseline will be executed",
                new ExpectedIdenticalStringsException("SQL strings did not match",currentSQL.Trim().ToLower(),oldSQL.Trim().ToLower())));

            //either way the SQL is screwey so lets nuke the baseline and make them do a full baseline
            previousSuccess.DeleteInDatabase();
            return BaselineHackEvaluation.NoCompatibleBaselineAvailable;
        }

        //ok so a hack is possible! - yay

        //we want to inject an additional AND container onto the end of the extract query that restricts what comes out to only new people (all records) or new records

        //Get the cohort ID column
        var cohortReleaseIdentifier = _extractDatasetCommand.ExtractableCohort.GetReleaseIdentifier();

        //
        var tblForJoin = _repository.DiscoveredServer.GetCurrentDatabase().ExpectTable("ReleaseIdentifiersSeen");

        if(!tblForJoin.Exists())
            throw new Exception($"Table '{tblForJoin} did not exist!");

        var tableForJoinName = tblForJoin.GetFullyQualifiedName();

        hackSql = $@"
AND
(
	(
        --new cohorts
		{cohortReleaseIdentifier} 
			not in (Select ReleaseId from {tableForJoinName} where AutomateExtraction_ID = {automateExtraction[0].ID})
	)
	OR
	(
        --new records
		{validFromField.Name} > '{automateExtraction[0].BaselineDate.Value}'
	)
)
";

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Generated the following Delta Hack SQL:{Environment.NewLine}{hackSql}"));

        return BaselineHackEvaluation.Allowed;
    }

    public ColumnInfo GetValidFromField(IDataLoadEventListener listener)
    {
        ColumnInfo validFromField = null;
        var wasPrimaryExtractionTable = false;
        const string validFromFieldName = SpecialFieldNames.ValidFrom;

        foreach (TableInfo tableInfo in _extractDatasetCommand.QueryBuilder.TablesUsedInQuery)
        {
            var col = tableInfo.ColumnInfos.SingleOrDefault(c => c.GetRuntimeName() == validFromFieldName);

            //table doesn't have a
            if(col == null)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                    $"TableInfo {tableInfo} did not have a ColumnInfo called '{validFromFieldName}'"));
                continue;
            }

            if (validFromField == null)
            {
                validFromField = col;
                wasPrimaryExtractionTable = tableInfo.IsPrimaryExtractionTable;
            }
            else
            {
                //theres already another one that was primary
                if (wasPrimaryExtractionTable)
                {
                    if (tableInfo.IsPrimaryExtractionTable)
                    {
                        //should never happen to be honest I'm pretty sure QueryBuilder will be super angry if you have 2+ TableInfos both with IsPrimary and ColumnNames should be unique anyway but who knows
                        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error,
                            $"There were multiple ColumnInfos called {validFromFieldName} both from IsPrimaryExtractionTable TableInfos ({validFromField},{col})"
                        ));
                        return null;
                    }

                    //we are not a primary and we have found a primary already so ignore this col
                }
                else //previous one was not a primary
                {
                    if (tableInfo.IsPrimaryExtractionTable) //and we are!
                    {
                        //bonus we can replace the old one we found
                        wasPrimaryExtractionTable = true;
                        validFromField = col;
                    }
                    else
                    {
                        //neither the previous or ourselves are primary
                        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error,
                            $"There were multiple ColumnInfos called {validFromFieldName} ({validFromField},{col}) try setting one of your TableInfos to IsPrimaryExtractionTable"));
                        return null;
                    }
                }
            }
        }


        if (validFromField == null)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"No ColumnInfos were found called '{validFromFieldName}'"));
            return null;
        }

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Found valid from field '{validFromField}'"));
        return validFromField;
    }
}

public enum BaselineHackEvaluation
{
    DatasetNotCompatibleWithBaselining,
    NoCompatibleBaselineAvailable,
    Allowed
}