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
    }
}
