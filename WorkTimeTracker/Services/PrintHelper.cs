using System;
using System.Globalization;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WorkTimeTracker.Storage;
using WorkTimeTracker.ViewModels;

namespace WorkTimeTracker.Services
{
    public static class PrintHelper
    {
        public static void PrintWeeks(int jaar, int vanWeek, int totWeek)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                PagePadding = new Thickness(50),
                ColumnWidth = double.PositiveInfinity // geen krantenkolommen
            };

            bool firstWeek = true;

            for (int week = vanWeek; week <= totWeek; week++)
            {
                // ISO-week naar maandag-datum
                DateTime weekStart;
                try
                {
                    weekStart = ISOWeek.ToDateTime(jaar, week, DayOfWeek.Monday);
                }
                catch
                {
                    // als week niet geldig is voor dat jaar, sla over
                    continue;
                }

                var weekDto = WeekRepository.Load(weekStart)
                             ?? new WeekDto { WeekStart = weekStart };

                var weekTable = MaakWeekTabel(weekDto);

                // vanaf de 2e week: steeds op nieuwe pagina beginnen
                if (!firstWeek)
                    weekTable.BreakPageBefore = true;

                firstWeek = false;

                doc.Blocks.Add(weekTable);
            }

            var printDialog = new PrintDialog();

            // standaard naar landscape zetten vóór de dialoog
            if (printDialog.PrintTicket != null)
                printDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;

            if (printDialog.ShowDialog() == true)
            {
                // na keuze printer nog eens expliciet naar landscape
                if (printDialog.PrintTicket != null)
                    printDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;

                // doc afstemmen op printbare ruimte van de printer
                doc.PageHeight = printDialog.PrintableAreaHeight;
                doc.PageWidth = printDialog.PrintableAreaWidth;
                doc.ColumnWidth = double.PositiveInfinity;

                IDocumentPaginatorSource dps = doc;
                printDialog.PrintDocument(dps.DocumentPaginator, "Urenregistratie");
            }
        }

        private static Table MaakWeekTabel(WeekDto week)
        {
            var culture = new CultureInfo("nl-BE");
            var table = new Table { CellSpacing = 0 };

            // kolommen: Dag, Tijd, Project, Extra, Locatie
            for (int i = 0; i < 5; i++)
                table.Columns.Add(new TableColumn());

            // Week kop
            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow();

            headerRow.Cells.Add(new TableCell(new Paragraph(
                new Run($"Week {ISOWeek.GetWeekOfYear(week.WeekStart)} " +
                        $"({week.WeekStart:dd/MM/yyyy} - {week.WeekStart.AddDays(6):dd/MM/yyyy})")))
            {
                ColumnSpan = 5,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Padding = new Thickness(0, 0, 0, 4)
            });

            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // Kolomtitels
            var titlesGroup = new TableRowGroup();
            var titlesRow = new TableRow();
            titlesRow.Cells.Add(MakeHeaderCell("Dag"));
            titlesRow.Cells.Add(MakeHeaderCell("Tijd"));
            titlesRow.Cells.Add(MakeHeaderCell("Project"));
            titlesRow.Cells.Add(MakeHeaderCell("Extra info"));
            titlesRow.Cells.Add(MakeHeaderCell("Locatie"));
            titlesGroup.Rows.Add(titlesRow);
            table.RowGroups.Add(titlesGroup);

            // Dagen
            var daysGroup = new TableRowGroup();

            for (int i = 0; i < 7; i++)
            {
                var datum = week.WeekStart.AddDays(i);
                var dagDto = week.Dagen.Find(d => d.Datum.Date == datum.Date)
                            ?? new DagUrenDto { Datum = datum, Status = DagStatus.Weekend };

                var row = new TableRow();

                string dagNaam = culture.TextInfo.ToTitleCase(
                    datum.ToString("dddd dd MMMM", culture));

                row.Cells.Add(MakeCell(dagNaam));

                string tijd = "";
                if (dagDto.StartTijd.HasValue && dagDto.EindTijd.HasValue)
                    tijd = $"{dagDto.StartTijd:hh\\:mm} - {dagDto.EindTijd:hh\\:mm}";

                row.Cells.Add(MakeCell(tijd));
                row.Cells.Add(MakeCell(dagDto.Projectnaam));
                row.Cells.Add(MakeCell(dagDto.ExtraInformatie));
                row.Cells.Add(MakeCell(dagDto.Locatie));

                if (dagDto.Status != DagStatus.Normaal)
                {
                    row.Cells[3].Blocks.Clear();
                    row.Cells[3].Blocks.Add(new Paragraph(
                        new Run(dagDto.Status.ToString().ToUpper()))
                    {
                        FontWeight = FontWeights.Bold
                    });
                }

                daysGroup.Rows.Add(row);
            }

            table.RowGroups.Add(daysGroup);
            return table;
        }

        private static TableCell MakeHeaderCell(string text) =>
            new TableCell(new Paragraph(new Run(text)))
            {
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(2, 0, 4, 2)
            };

        private static TableCell MakeCell(string? text) =>
            new TableCell(new Paragraph(new Run(text ?? string.Empty)))
            {
                Padding = new Thickness(2, 0, 4, 0)
            };
    }
}
