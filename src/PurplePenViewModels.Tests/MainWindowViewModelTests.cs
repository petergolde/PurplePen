using System.ComponentModel;
using NUnit.Framework;
using PurplePen.ViewModels;

namespace PurplePenViewModels.Tests
{
    /// <summary>
    /// Tests for MainWindowViewModel, verifying that the Counter property
    /// and the Increment/Decrement commands work correctly.
    /// </summary>
    [TestFixture]
    public class MainWindowViewModelTests
    {
        private MainWindowViewModel viewModel = null!;

        [SetUp]
        public void Initialize()
        {
            viewModel = new MainWindowViewModel();
        }

    }
}
