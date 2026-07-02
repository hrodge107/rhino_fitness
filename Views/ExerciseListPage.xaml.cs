using FitnessApp.ViewModels;

namespace FitnessApp.Views
{
    public partial class ExerciseListPage : ContentPage
    {
        public ExerciseListPage(ExerciseListViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
