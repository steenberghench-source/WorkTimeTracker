using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkTimeTracker.ViewModels
{
    partial class WeekViewModel : ObservableObject
    {
        private const double ContractUrenPerDag = 8.0;

        public ObservableCollection<DagUrenViewModel> Dagen { get; } = new();

        [ObservableProperty]
        private DateTime huidigeWeekStart;

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

        public WeekViewModel()
        {
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
            {
                dag.PropertyChanged -= Dag_PropertyChanged;
            }

            Dagen.Clear();

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
                e.PropertyName == nameof(DagUrenViewModel.Status))
            {
                OnPropertyChanged(nameof(OverurenTotaal));
            }
        }

        private void Dagen_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(OverurenTotaal));
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
            HuidigeWeekStart = HuidigeWeekStart.AddDays(-7);
            LaadWeek();
        }

        [RelayCommand]
        private void VolgendeWeek()
        {
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

        [RelayCommand]
        private void Print()
        {
            ReedsAfgedrukt = true;
            // Hier later echte print-logica toevoegen (PrintDialog, FlowDocument, ...).
        }

        partial void OnHuidigeWeekStartChanged(DateTime value)
        {
            OnPropertyChanged(nameof(WeekTitel));
        }
    }
}
