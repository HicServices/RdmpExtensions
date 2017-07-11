using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueManager.Icons.IconProvision;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.ExtractionTime.Commands;
using DataExportLibrary.ExtractionTime.ExtractionPipeline;
using HIC.Logging;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using RDMPObjectVisualisation.Pipelines;

namespace LoadModules.Extensions.AutomationPluginsUIs.Dialogs
{
    public partial class EnqueueExtractionConfigurationUI : Form
    {
        private readonly ExtractionConfiguration _config;
        private readonly AutomateExtractionRepository _repository;
        private PipelineSelectionUI<DataTable> _selectionUI;

        public EnqueueExtractionConfigurationUI(ExtractionConfiguration config, AutomateExtractionRepository repository)
        {
            _config = config;
            _repository = repository;
            InitializeComponent();
            pbExtractionConfiguration.Image = CatalogueIcons.ExtractionConfiguration;
            lblExtractionConfigurationName.Text = config.ToString();

            _selectionUI = new PipelineSelectionUI<DataTable>(null, null, repository.CatalogueRepository);
            _selectionUI.Context = ExtractionPipelineHost.Context;
            _selectionUI.Dock = DockStyle.Fill;
            _selectionUI.PipelineChanged += SelectionUIOnPipelineChanged;
            pPipeline.Controls.Add(_selectionUI);

            _selectionUI.InitializationObjectsForPreviewPipeline.Add(ExtractDatasetCommand.EmptyCommand);
            _selectionUI.InitializationObjectsForPreviewPipeline.Add(DataLoadInfo.Empty);

            ddDay.DataSource = Enum.GetValues(typeof (DayDelay));
            ddTimescale.DataSource = Enum.GetValues(typeof(TimeDelay));

        }

        private void SelectionUIOnPipelineChanged(object sender, EventArgs eventArgs)
        {
            btnQueExtraction.Enabled = _selectionUI.Pipeline != null && ddTimescale.SelectedItem != null;
        }

        private void ddTimescale_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnQueExtraction.Enabled = _selectionUI.Pipeline != null && ddTimescale.SelectedItem != null;
        }

        public DateTime AdjustFor(TimeDelay timeDelay, DateTime inDate)
        {
            switch (timeDelay)
            {
                case TimeDelay.Now:
                    return inDate;
                case TimeDelay.FiveMinutes:
                    return inDate.AddMinutes(5);
                case TimeDelay.OneHour:
                    return inDate.AddHours(1);
                case TimeDelay.TwoHours:
                    return inDate.AddHours(2);
                case TimeDelay.ThreeHours:
                    return inDate.AddHours(3);
                case TimeDelay.FiveHours:
                    return inDate.AddHours(5);
                case TimeDelay.SevenHours:
                    return inDate.AddHours(7);
                case TimeDelay.ElevenHours:
                    return inDate.AddHours(11);
                case TimeDelay.ThirteenHours:
                    return inDate.AddHours(13);
                case TimeDelay.SeventeenHours:
                    return inDate.AddHours(17);
                case TimeDelay.NineteenHours:
                    return inDate.AddHours(19);
                case TimeDelay.TwentyThreeHours:
                    return inDate.AddHours(23);
                default:
                    throw new ArgumentOutOfRangeException("timeDelay");
            }
        }

        public DateTime AdjustFor(DayDelay dayDelay, DateTime inDate)
        {
            switch (dayDelay)
            {
                case DayDelay.Today:
                    return inDate;
                case DayDelay.Tomorrow:
                    return inDate.AddDays(1);
                case DayDelay.NextMonday:
                    return GetNextDayX(DayOfWeek.Monday, inDate);
                case DayDelay.NextTuesday:
                    return GetNextDayX(DayOfWeek.Tuesday, inDate);
                case DayDelay.NextWednesday:
                    return GetNextDayX(DayOfWeek.Wednesday, inDate);
                case DayDelay.NextThursday:
                    return GetNextDayX(DayOfWeek.Thursday, inDate);
                case DayDelay.NextFriday:
                    return GetNextDayX(DayOfWeek.Friday, inDate);
                case DayDelay.NextSaturday:
                    return GetNextDayX(DayOfWeek.Saturday, inDate);
                case DayDelay.NextSunday:
                    return GetNextDayX(DayOfWeek.Sunday, inDate);
                case DayDelay.InOneWeeksTime:
                    return inDate.AddDays(7);
                case DayDelay.InTwoWeeksTime:
                    return inDate.AddDays(14);
                case DayDelay.NextFullMoon:

                    double age = 0;
                    while (age < 13 || age > 14)
                    {
                        inDate = inDate.AddDays(1);
                        age = MoonAge(inDate.Day, inDate.Month, inDate.Year);
                    }

                    return inDate;
                default:
                    throw new ArgumentOutOfRangeException("dayDelay");
            }
        }

        public enum TimeDelay
        {
            Now,
            FiveMinutes,
            OneHour,
            TwoHours,
            ThreeHours,
            FiveHours,
            SevenHours,
            ElevenHours,
            ThirteenHours,
            SeventeenHours,
            NineteenHours,
            TwentyThreeHours
        }

        public enum DayDelay
        {
            Today,
            Tomorrow,
            NextMonday,
            NextTuesday,
            NextWednesday,
            NextThursday,
            NextFriday,
            NextSaturday,
            NextSunday,
            InOneWeeksTime,
            InTwoWeeksTime,
            NextFullMoon
        }

        private DateTime GetNextDayX(DayOfWeek dayOfWeek, DateTime inDate)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysUntilDayX = ((int)dayOfWeek - (int)inDate.DayOfWeek + 7) % 7;
            if (daysUntilDayX == 0)
                daysUntilDayX = 7;
            
            return inDate.AddDays(daysUntilDayX);
        }

        private int JulianDate(int d, int m, int y)
        {
            int mm, yy;
            int k1, k2, k3;
            int j;

            yy = y - (int)((12 - m) / 10);
            mm = m + 9;
            if (mm >= 12)
            {
                mm = mm - 12;
            }
            k1 = (int)(365.25 * (yy + 4712));
            k2 = (int)(30.6001 * mm + 0.5);
            k3 = (int)((int)((yy / 100) + 49) * 0.75) - 38;
            // 'j' for dates in Julian calendar:
            j = k1 + k2 + d + 59;
            if (j > 2299160)
            {
                // For Gregorian calendar:
                j = j - k3; // 'j' is the Julian date at 12h UT (Universal Time)
            }
            return j;
        }
        private double MoonAge(int d, int m, int y)
        {
            int j = JulianDate(d, m, y);
            //Calculate the approximate phase of the moon
            var ip = (j + 4.867) / 29.53059;
            ip = ip - Math.Floor(ip);

            double ag;

            //After several trials I've seen to add the following lines, 
            //which gave the result was not bad 
            if (ip < 0.5)
                ag = ip * 29.53059 + 29.53059 / 2;
            else
                ag = ip * 29.53059 - 29.53059 / 2;
            // Moon's age in days
            ag = Math.Floor(ag) + 1;
            return ag;
        }

        private void btnQueExtraction_Click(object sender, EventArgs e)
        {
            var timeDelay = (TimeDelay) ddTimescale.SelectedItem;
            var dayDelay = (DayDelay) ddDay.SelectedItem;

            var plannedTime = AdjustFor(dayDelay, AdjustFor(timeDelay, DateTime.Now));

            if (
                MessageBox.Show(
                    "ExtractionConfiguration will be queued for '" + plannedTime + "'. Is this what you want?",
                    "Confirm planned time", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var que = new QueuedExtraction(_repository, _config, _selectionUI.Pipeline, plannedTime);
                this.Close();
            }


        }


    }
}
