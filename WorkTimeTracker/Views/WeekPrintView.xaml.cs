using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WorkTimeTracker.ViewModels;

namespace WorkTimeTracker.Views
{
    public partial class WeekPrintView : UserControl
    {
        public WeekPrintView()
        {
            InitializeComponent();
        }

        // Header bovenaan (week + datums)
        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(
                nameof(HeaderText),
                typeof(string),
                typeof(WeekPrintView),
                new PropertyMetadata(string.Empty));

        public string HeaderText
        {
            get => (string)GetValue(HeaderTextProperty);
            set => SetValue(HeaderTextProperty, value);
        }

        //Username bovenaan 
        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(
                nameof(UserName),
                typeof(string),
                typeof(WeekPrintView),
                new PropertyMetadata(string.Empty));

        public string UserName
        {
            get => (string)GetValue(UserNameProperty);
            set => SetValue(UserNameProperty, value);
        }

        // Tekst voor totaal overuren onderaan
        public static readonly DependencyProperty OverurenTekstProperty =
            DependencyProperty.Register(
                nameof(OverurenTekst),
                typeof(string),
                typeof(WeekPrintView),
                new PropertyMetadata(string.Empty));

        public string OverurenTekst
        {
            get => (string)GetValue(OverurenTekstProperty);
            set => SetValue(OverurenTekstProperty, value);
        }

        // Lijst met dagen
        public static readonly DependencyProperty DagenProperty =
            DependencyProperty.Register(
                nameof(Dagen),
                typeof(IEnumerable<DagUrenViewModel>),
                typeof(WeekPrintView),
                new PropertyMetadata(null));

        public IEnumerable<DagUrenViewModel> Dagen
        {
            get => (IEnumerable<DagUrenViewModel>)GetValue(DagenProperty);
            set => SetValue(DagenProperty, value);
        }

        // NIEUW: paginanummer-tekst, bv. "Pagina 1/3"
        public static readonly DependencyProperty PageTextProperty =
            DependencyProperty.Register(
                nameof(PageText),
                typeof(string),
                typeof(WeekPrintView),
                new PropertyMetadata(string.Empty));

        public string PageText
        {
            get => (string)GetValue(PageTextProperty);
            set => SetValue(PageTextProperty, value);
        }
    }
}
