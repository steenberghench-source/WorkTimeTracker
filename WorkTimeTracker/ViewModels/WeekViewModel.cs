using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;
using WorkTimeTracker.Storage;

namespace WorkTimeTracker.ViewModels
{
    partial class WeekViewModel : ObservableObject
    {
        private const double ContractUrenPerDag = 8.0;

        public ObservableCollection<DagUrenViewModel> Dagen { get; } = new();

        [ObservableProperty]
        private DateTime huidigeWeekStart;
        public int HuidigeWeekNummer => ISOWeek.GetWeekOfYear(HuidigeWeekStart);
        public string HuidigeWeekPeriode =>
            $"{HuidigeWeekStart:dd/MM/yyyy} t/m {HuidigeWeekStart.AddDays(6):dd/MM/yyyy}";

        [ObservableProperty]
        private bool reedsAfgedrukt;

        public string WeekTitel =>
            $"Week {ISOWeek.GetWeekOfYear(HuidigeWeekStart)} - {HuidigeWeekStart:dd/MM/yyyy} t/m {HuidigeWeekStart.AddDays(6):dd/MM/yyyy}";

        public double OverurenTotaal =>
            Dagen.Sum(d =>
            {
                // We tellen alleen dagen met status Normaal en geldige tijdsinterval
                if (d.Status != DagStatus.Normaal ||
                    !d.StartTijd.HasValue ||
                    !d.EindTijd.HasValue ||
                    d.EindTijd <= d.StartTijd)
                {
                    return 0;
                }

                // Zaterdag/zondag: alles is overuur (pauze al verrekend in GewerkteUren)
                if (IsWeekend(d.Datum))
                {
                    return d.GewerkteUren;
                }

                // Ma–vr: overuren = gewerkte uren - contracturen
                return d.GewerkteUren - ContractUrenPerDag;
            });

        private readonly DispatcherTimer _autoSaveTimer;
        private bool _heeftOnopgeslagenWijzigingen;

        public WeekViewModel()
        {
            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;

            HuidigeWeekStart = StartVanWeek(DateTime.Today);
            Dagen.CollectionChanged += Dagen_CollectionChanged;
            LaadWeek();
        }

        private static DateTime StartVanWeek(DateTime datum)
        {
            // Maandag = start van de week
            int diff = (7 + (datum.DayOfWeek - DayOfWeek.Monday)) % 7;
            return datum.Date.AddDays(-diff);
        }

        private void LaadWeek()
        {
            // Unsubscribe oude dagen
            foreach (var dag in Dagen)
                dag.PropertyChanged -= Dag_PropertyChanged;

            Dagen.Clear();

            var opgeslagen = WeekRepository.Load(HuidigeWeekStart);

            // ✅ checkbox-waarde inlezen (als er niks is: false)
            ReedsAfgedrukt = opgeslagen?.ReedsAfgedrukt ?? false;

            if (opgeslagen is not null)
            {
                foreach (var dto in opgeslagen.Dagen.OrderBy(d => d.Datum))
                {
                    var dag = new DagUrenViewModel
                    {
                        Datum = dto.Datum,
                        StartTijd = dto.StartTijd,
                        EindTijd = dto.EindTijd,
                        Projectnaam = dto.Projectnaam ?? string.Empty,
                        ExtraInformatie = dto.ExtraInformatie ?? string.Empty,
                        Locatie = dto.Locatie ?? string.Empty,
                        Status = dto.Status
                    };

                    dag.PropertyChanged += Dag_PropertyChanged;
                    Dagen.Add(dag);
                }
            }
            else
            {
                // Default-logica voor een nieuwe week, incl. weekend
                for (int i = 0; i < 7; i++)
                {
                    var datum = HuidigeWeekStart.AddDays(i);
                    var dag = new DagUrenViewModel
                    {
                        Datum = datum,
                        Status = IsWeekend(datum) ? DagStatus.Weekend : DagStatus.Normaal
                    };
                    dag.PropertyChanged += Dag_PropertyChanged;
                    Dagen.Add(dag);
                }
            }

            OnPropertyChanged(nameof(WeekTitel));
            OnPropertyChanged(nameof(OverurenTotaal));
        }

        private static bool IsWeekend(DateTime datum)
        {
            return datum.DayOfWeek == DayOfWeek.Saturday
                || datum.DayOfWeek == DayOfWeek.Sunday;
        }

        private void Dag_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DagUrenViewModel.GewerkteUren) ||
                e.PropertyName == nameof(DagUrenViewModel.Status) ||
                e.PropertyName == nameof(DagUrenViewModel.StartTijd) ||
                e.PropertyName == nameof(DagUrenViewModel.EindTijd) ||
                e.PropertyName == nameof(DagUrenViewModel.Projectnaam) ||
                e.PropertyName == nameof(DagUrenViewModel.ExtraInformatie) ||
                e.PropertyName == nameof(DagUrenViewModel.Locatie))
            {
                OnPropertyChanged(nameof(OverurenTotaal));
                PlanAutoSave();
            }
        }

        private void Dagen_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(OverurenTotaal));
            PlanAutoSave();
        }

        private void PlanAutoSave()
        {
            _heeftOnopgeslagenWijzigingen = true;
            _autoSaveTimer.Stop();
            _autoSaveTimer.Start();   // vanaf nu 2s geen nieuwe wijziging -> save
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            _autoSaveTimer.Stop();
            if (!_heeftOnopgeslagenWijzigingen)
                return;

            _heeftOnopgeslagenWijzigingen = false;
            WeekRepository.Save(HuidigeWeekStart, Dagen, ReedsAfgedrukt);
        }

        private static DagStatus VolgendeStatus(DagStatus huidige, bool isWeekendDag)
        {
            if (isWeekendDag)
            {
                // Voor zaterdag/zondag mag Weekend gebruikt worden
                return huidige switch
                {
                    DagStatus.Normaal => DagStatus.Weekend,
                    _ => DagStatus.Normaal
                };
            }
            else
            {
                // Voor weekdagen géén Weekend in de cyclus
                return huidige switch
                {
                    DagStatus.Normaal => DagStatus.Ziek,
                    DagStatus.Ziek => DagStatus.Vakantie,
                    DagStatus.Vakantie => DagStatus.Feestdag,
                    DagStatus.Feestdag => DagStatus.ADV,
                    DagStatus.ADV => DagStatus.Recup,
                    DagStatus.Recup => DagStatus.Normaal,

                    // Als er ooit toch Weekend op een weekdag staat: terug naar Normaal
                    DagStatus.Weekend => DagStatus.Normaal,

                    _ => DagStatus.Normaal
                };
            }
        }

        [RelayCommand]
        private void VorigeWeek()
        {
            WeekRepository.Save(HuidigeWeekStart, Dagen, ReedsAfgedrukt);
            HuidigeWeekStart = HuidigeWeekStart.AddDays(-7);
            LaadWeek();
        }

        [RelayCommand]
        private void VolgendeWeek()
        {
            WeekRepository.Save(HuidigeWeekStart, Dagen, ReedsAfgedrukt);
            HuidigeWeekStart = HuidigeWeekStart.AddDays(7);
            LaadWeek();
        }

        [RelayCommand]
        private void ToggleDagStatus(DagUrenViewModel? dag)
        {
            if (dag is null)
                return;

            bool isWeekendDag = IsWeekend(dag.Datum);
            dag.Status = VolgendeStatus(dag.Status, isWeekendDag);
        }

        partial void OnHuidigeWeekStartChanged(DateTime value)
        {
            OnPropertyChanged(nameof(WeekTitel));
            OnPropertyChanged(nameof(HuidigeWeekNummer));
            OnPropertyChanged(nameof(HuidigeWeekPeriode));
        }

        partial void OnReedsAfgedruktChanged(bool value)
        {
            PlanAutoSave();
        }
    }
}
