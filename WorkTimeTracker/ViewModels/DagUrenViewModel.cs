using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;

namespace WorkTimeTracker.ViewModels
{
    public enum DagStatus
    {
        Normaal,
        Ziek,
        Vakantie,
        Feestdag,
        ADV,
        Recup,
        Weekend
    }

    public partial class DagUrenViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTime datum;

        [ObservableProperty]
        private TimeSpan? startTijd;

        [ObservableProperty]
        private TimeSpan? eindTijd;

        [ObservableProperty]
        private string projectnaam = string.Empty;

        [ObservableProperty]
        private string extraInformatie = string.Empty;

        [ObservableProperty]
        private string locatie = string.Empty;

        [ObservableProperty]
        private DagStatus status = DagStatus.Normaal;

        public bool IsVandaag => Datum.Date == DateTime.Today;

        public bool IsNormaleWerkdag => Status == DagStatus.Normaal;

        /// <summary>
        /// Handig voor contextmenu: alleen zaterdag/zondag mogen "Weekend" zijn.
        /// </summary>
        public bool IsWeekendDag =>
            Datum.DayOfWeek == DayOfWeek.Saturday ||
            Datum.DayOfWeek == DayOfWeek.Sunday;

        /// <summary>
        /// Label voor weergave in de UI (Vakantie → Verlof, etc.).
        /// </summary>
        public string StatusLabel => Status switch
        {
            DagStatus.Normaal => string.Empty,
            DagStatus.Ziek => "Ziek",
            DagStatus.Vakantie => "Verlof",
            DagStatus.Feestdag => "Feestdag",
            DagStatus.ADV => "ADV",
            DagStatus.Recup => "Recup",
            DagStatus.Weekend => "Weekend",
            _ => Status.ToString()
        };

        public double GewerkteUren
        {
            get
            {
                if (!StartTijd.HasValue || !EindTijd.HasValue || EindTijd <= StartTijd)
                    return 0;

                var start = StartTijd.Value;
                var eind = EindTijd.Value;

                // Bruto uren (zonder rekening te houden met pauze)
                var uren = (eind - start).TotalHours;

                // Als het volledige blok 12:00–12:30 binnen de werktijd ligt -> 0,5u pauze aftrekken
                if (HeeftMiddagPauze(start, eind))
                {
                    uren -= 0.5;
                }

                // Negatieve gewerkte uren hebben geen zin
                if (uren < 0)
                    uren = 0;

                return uren;
            }
        }

        private static bool HeeftMiddagPauze(TimeSpan start, TimeSpan eind)
        {
            var pauzeStart = new TimeSpan(12, 0, 0);
            var pauzeEinde = new TimeSpan(12, 30, 0);

            // Volledige pauze ligt binnen de werktijd
            return start <= pauzeStart && eind >= pauzeEinde;
        }

        public string DagEnDatum
        {
            get
            {
                var culture = new CultureInfo("nl-BE");
                var s = Datum.ToString("dddd", culture);
                if (string.IsNullOrEmpty(s))
                    return s;

                var eerste = s.Substring(0, 1).ToUpper(culture);
                return eerste + s.Substring(1);
            }
        }

        partial void OnStatusChanged(DagStatus value)
        {
            OnPropertyChanged(nameof(IsNormaleWerkdag));
            OnPropertyChanged(nameof(GewerkteUren));
            OnPropertyChanged(nameof(StatusLabel));
        }

        partial void OnStartTijdChanged(TimeSpan? value)
        {
            OnPropertyChanged(nameof(GewerkteUren));
        }

        partial void OnEindTijdChanged(TimeSpan? value)
        {
            OnPropertyChanged(nameof(GewerkteUren));
        }

        partial void OnDatumChanged(DateTime value)
        {
            OnPropertyChanged(nameof(DagEnDatum));
            OnPropertyChanged(nameof(IsVandaag));
            OnPropertyChanged(nameof(IsWeekendDag));
        }
    }
}
