using System;
using System.Data;
using System.Windows.Forms;
using FAnsi.Implementations.MicrosoftSQL;
using Rdmp.Core.DataFlowPipeline;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.Interactive.DeAnonymise
{
    public class DeAnonymiseAgainstCohort: IPluginDataFlowComponent<DataTable>
    {
        /// <summary>
        /// When null (default) we launch a new DeAnonymiseAgainstCohortUI in order that the user selects which cohort he wants to deanonymise against. If you set this then you can 
        /// (for example in unit tests) specify an explicit implementation and dodge the gui.
        /// </summary>
        public IDeAnonymiseAgainstCohortConfigurationFulfiller ConfigurationGetter;
        
        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            if (toProcess == null)
                return null;

            Check(new FromDataLoadEventListenerToCheckNotifier(listener));

            if (ConfigurationGetter == null)
            {
                ConfigurationGetter = new DeAnonymiseAgainstCohortUI(toProcess);
                if(((Form)ConfigurationGetter).ShowDialog() != DialogResult.OK)
                    throw new Exception("User cancelled cohort picking dialog");

                if (ConfigurationGetter.ChosenCohort == null)
                    throw new Exception("User closed dialog without picking a cohort");
            }


            if (ConfigurationGetter.OverrideReleaseIdentifier != null)
            {
                string replacementName = new MicrosoftQuerySyntaxHelper().GetRuntimeName(ConfigurationGetter.ChosenCohort.GetReleaseIdentifier());

                if (!toProcess.Columns.Contains(ConfigurationGetter.OverrideReleaseIdentifier))
                    throw new ArgumentException("Cannot DeAnonymise cohort because you specified OverrideReleaseIdentifier of '" + ConfigurationGetter.OverrideReleaseIdentifier + "' but the DataTable toProcess did not contain a column of that name");

                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Renaming DataTable column " + ConfigurationGetter.OverrideReleaseIdentifier + " to " + replacementName));
                toProcess.Columns[ConfigurationGetter.OverrideReleaseIdentifier].ColumnName = replacementName;
            }

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "About to call ReverseAnonymiseDataTable"));
            ConfigurationGetter.ChosenCohort.ReverseAnonymiseDataTable(toProcess, listener, true);

            return toProcess;
        }


        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {

        }

        public void Abort(IDataLoadEventListener listener)
        {

        }
        public void Check(ICheckNotifier notifier)
        {
           
        }

    }
}
