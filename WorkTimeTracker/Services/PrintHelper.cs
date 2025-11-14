using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using WorkTimeTracker.Storage;    // WeekDto, DagUrenDto, WeekRepository
using WorkTimeTracker.ViewModels; // DagUrenViewModel, DagStatus
using WorkTimeTracker.Views;      // WeekPrintView

namespace WorkTimeTracker.Services
{
    public static class PrintHelper
    {
        private const double ContractUrenPerDag = 8.0;

        private class WeekOverzicht
        {
            public int WeekNummer { get; set; }
            public DateTime WeekStart { get; set; }
            public double Overuren { get; set; }
            public int RecupDagenCount { get; set; }
        }

        public static void PrintWeeks(int jaar, int vanWeek, int totWeek, string gebruikersNaam)
        {
            var printDialog = new PrintDialog();

            // Probeer vooraf A4 + landscape
            if (printDialog.PrintTicket != null)
            {
                printDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
            }

            if (printDialog.ShowDialog() != true)
                return;

            // Na de keuze nogmaals A4 + landscape (voor zover driver toelaat)
            if (printDialog.PrintTicket != null)
            {
                printDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
            }

            var pageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

            var fixedDoc = new FixedDocument();
            fixedDoc.DocumentPaginator.PageSize = pageSize;

            var overzicht = new List<WeekOverzicht>();

            // ✅ lijst om weken te onthouden die al afgedrukt waren
            var reedsAfgedrukteWeken = new List<(int Week, DateTime Start)>();

            // ✅ eerst bepalen welke weken effectief geldig zijn
            var wekenTePrinten = new List<(int Week, DateTime WeekStart)>();
            for (int week = vanWeek; week <= totWeek; week++)
            {
                try
                {
                    var weekStart = ISOWeek.ToDateTime(jaar, week, DayOfWeek.Monday);
                    wekenTePrinten.Add((week, weekStart));
                }
                catch
                {
                    // ongeldige week -> negeren
                }
            }

            if (!wekenTePrinten.Any())
                return;

            int totaalWeekPaginas = wekenTePrinten.Count;
            int pageIndex = 0;

            // 1) Week-pagina's
            foreach (var wk in wekenTePrinten)
            {
                pageIndex++;

                int week = wk.Week;
                DateTime weekStart = wk.WeekStart;

                var weekDto = WeekRepository.Load(weekStart)
                             ?? new WeekDto { WeekStart = weekStart, Dagen = new List<DagUrenDto>() };

                // Was deze week al eens als 'afgedrukt' opgeslagen?
                bool wasAlAfgedrukt = weekDto.ReedsAfgedrukt;

                var dagenVm = MaakDagViewModelsVoorWeek(weekStart, weekDto);
                double overuren = BerekenWeekOveruren(dagenVm);
                int recupCount = dagenVm.Count(d => d.Status == DagStatus.Recup);

                overzicht.Add(new WeekOverzicht
                {
                    WeekNummer = week,
                    WeekStart = weekStart,
                    Overuren = overuren,
                    RecupDagenCount = recupCount
                });

                if (wasAlAfgedrukt)
                {
                    reedsAfgedrukteWeken.Add((week, weekStart));
                }

                // ✅ Markeer elke geprinte week nu als 'afgedrukt' in de storage
                WeekRepository.Save(weekStart, dagenVm, true);

                string headerText =
                    $"Week {ISOWeek.GetWeekOfYear(weekStart)} " +
                    $"({weekStart:dd/MM/yyyy} - {weekStart.AddDays(6):dd/MM/yyyy})";

                string overurenTekst = $"Totaal overuren: {overuren:F2} u";

                // Paginanummer alleen tonen als er meerdere week-pagina's zijn
                string pageText = totaalWeekPaginas > 1
                    ? $"Pagina {pageIndex}/{totaalWeekPaginas}"
                    : string.Empty;

                // inhoud iets smaller dan de pagina
                double contentWidth = pageSize.Width * 0.90;

                var view = new WeekPrintView
                {
                    UserName = gebruikersNaam,        // ✅ NAAM MEEGEVEN
                    HeaderText = headerText,
                    OverurenTekst = overurenTekst,
                    Dagen = dagenVm,
                    PageText = pageText,              // ✅ paginanummer voor onderaan
                    Width = contentWidth
                };

                // layout laten uitrekenen
                view.Measure(new Size(contentWidth, pageSize.Height));
                view.Arrange(new Rect(new Point(0, 0), view.DesiredSize));
                view.UpdateLayout();

                var page = new FixedPage
                {
                    Width = pageSize.Width,
                    Height = pageSize.Height,
                    Background = Brushes.White
                };

                double left = (pageSize.Width - contentWidth) / 2;
                double top = 40;   // vaste marge bovenaan -> totaal overuren dichter bij tabel

                FixedPage.SetLeft(view, left);
                FixedPage.SetTop(view, top);
                page.Children.Add(view);

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(page);
                fixedDoc.Pages.Add(pageContent);
            }

            // 2) Extra pagina: overzicht overuren per week + totaalsom
            //    alleen als er minstens 2 weken geprint zijn
            if (overzicht.Count >= 2)
            {
                var summaryPage = MaakOverzichtPagina(overzicht, pageSize, gebruikersNaam);
                fixedDoc.Pages.Add(summaryPage);
            }

            // 3) Afdrukken
            printDialog.PrintDocument(fixedDoc.DocumentPaginator, "Urenregistratie");

            // 4) Waarschuwing voor weken die al eerder als 'afgedrukt' stonden
            if (reedsAfgedrukteWeken.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Opgelet: de volgende weken stonden al als 'afgedrukt' gemarkeerd:");
                sb.AppendLine();

                foreach (var w in reedsAfgedrukteWeken.OrderBy(x => x.Start))
                {
                    var start = w.Start;
                    var eind = start.AddDays(6);
                    sb.AppendLine($"• Week {w.Week} ({start:dd/MM/yyyy} - {eind:dd/MM/yyyy})");
                }

                sb.AppendLine();
                sb.AppendLine("Deze afdruk kan dus (deels) een dubbele afdruk zijn.");

                MessageBox.Show(
                    sb.ToString(),
                    "Mogelijke dubbele afdruk",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static PageContent MaakOverzichtPagina(
            List<WeekOverzicht> overzicht,
            Size pageSize,
            string? gebruikersNaam)
        {
            double contentWidth = pageSize.Width * 0.80;
            double contentHeight = pageSize.Height * 0.70;

            var page = new FixedPage
            {
                Width = pageSize.Width,
                Height = pageSize.Height,
                Background = Brushes.White
            };

            var grid = new Grid
            {
                Width = contentWidth,
                Height = contentHeight
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                         // header
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });    // lijst
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                         // grote total

            // Header (met optioneel naam)
            string headerText = "Overzicht overuren per week";
            if (!string.IsNullOrWhiteSpace(gebruikersNaam))
            {
                headerText += $" – {gebruikersNaam}";
            }

            var header = new TextBlock
            {
                Text = headerText,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 16)
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Lijst
            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            foreach (var w in overzicht.OrderBy(o => o.WeekStart))
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                string label =
                    $"Week {w.WeekNummer} " +
                    $"({w.WeekStart:dd/MM} - {w.WeekStart.AddDays(6):dd/MM})";

                var weekText = new TextBlock
                {
                    Text = label,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(weekText, 0);
                row.Children.Add(weekText);

                string recupSuffix = string.Empty;
                if (w.RecupDagenCount > 0)
                {
                    recupSuffix = w.RecupDagenCount == 1
                        ? " (1 recupdag)"
                        : $" ({w.RecupDagenCount} recupdagen)";
                }

                var urenText = new TextBlock
                {
                    Text = $"{w.Overuren:F2} u{recupSuffix}",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(urenText, 1);
                row.Children.Add(urenText);

                stack.Children.Add(row);
            }

            var scrollViewer = new ScrollViewer
            {
                Content = stack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid.SetRow(scrollViewer, 1);
            grid.Children.Add(scrollViewer);

            double totaal = overzicht.Sum(o => o.Overuren);

            // weekrange bepalen op basis van de werkelijk geprinte weken
            int eersteWeek = overzicht.Min(o => o.WeekNummer);
            int laatsteWeek = overzicht.Max(o => o.WeekNummer);

            string weekRange = eersteWeek == laatsteWeek
                ? $"W{eersteWeek}"
                : $"W{eersteWeek}-W{laatsteWeek}";

            var totaalText = new TextBlock
            {
                Text = $"Totaal overuren {weekRange}: {totaal:F2} u",
                FontWeight = FontWeights.Bold,
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            Grid.SetRow(totaalText, 2);
            grid.Children.Add(totaalText);

            // centreer de grid op de pagina
            double left = (pageSize.Width - contentWidth) / 2;
            double top = (pageSize.Height - contentHeight) / 2;

            FixedPage.SetLeft(grid, left);
            FixedPage.SetTop(grid, top);
            page.Children.Add(grid);

            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(page);
            return pageContent;
        }

        private static List<DagUrenViewModel> MaakDagViewModelsVoorWeek(DateTime weekStart, WeekDto weekDto)
        {
            var result = new List<DagUrenViewModel>();

            for (int i = 0; i < 7; i++)
            {
                var datum = weekStart.AddDays(i);

                var dto = weekDto.Dagen
                    .FirstOrDefault(d => d.Datum.Date == datum.Date);

                if (dto == null)
                {
                    var isWeekend = IsWeekend(datum);
                    dto = new DagUrenDto
                    {
                        Datum = datum,
                        Status = isWeekend ? DagStatus.Weekend : DagStatus.Normaal
                    };
                }

                var vm = new DagUrenViewModel
                {
                    Datum = dto.Datum,
                    StartTijd = dto.StartTijd,
                    EindTijd = dto.EindTijd,
                    Projectnaam = dto.Projectnaam ?? string.Empty,
                    ExtraInformatie = dto.ExtraInformatie ?? string.Empty,
                    Locatie = dto.Locatie ?? string.Empty,
                    Status = dto.Status
                };

                result.Add(vm);
            }

            return result
                .OrderBy(d => d.Datum)
                .ToList();
        }

        private static double BerekenWeekOveruren(IEnumerable<DagUrenViewModel> dagen)
        {
            double totaal = 0.0;

            foreach (var d in dagen)
            {
                if (d.Status != DagStatus.Normaal ||
                    !d.StartTijd.HasValue ||
                    !d.EindTijd.HasValue ||
                    d.EindTijd <= d.StartTijd)
                {
                    continue;
                }

                double gewerkteUren = d.GewerkteUren; // bevat al pauze-logica

                if (IsWeekend(d.Datum))
                {
                    // zaterdag/zondag: alles is overuur
                    totaal += gewerkteUren;
                }
                else
                {
                    // ma–vr: verschil t.o.v. contracturen
                    totaal += gewerkteUren - ContractUrenPerDag;
                }
            }

            return totaal;
        }

        private static bool IsWeekend(DateTime datum) =>
            datum.DayOfWeek == DayOfWeek.Saturday ||
            datum.DayOfWeek == DayOfWeek.Sunday;
    }
}
