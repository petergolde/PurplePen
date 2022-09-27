using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

using System.Diagnostics;

using PurplePen.MapModel;

namespace WpfMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: Window
    {
        CachedViewer mapViewer;

        public MainWindow()
        {
            InitializeComponent();

            CommandBinding openCommandBinding =
                new CommandBinding(ApplicationCommands.Open);
            openCommandBinding.Executed += ExecutedOpenFile;
            CommandBindings.Add(openCommandBinding);

            mapViewer = new CachedViewer();
            mapViewer.PanAndZoomControl = panAndZoom;
            panAndZoom.AddLayer(mapViewer);
        }

        void ExecutedOpenFile(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            if (dlg.ShowDialog() == true) {
                string filename = dlg.FileName;

                Map map = new Map(new GDIPlus_TextMetrics(), null);
                InputOutput.ReadFile(filename, map);
                NewMap(map);
            }
        }

        void NewMap(Map map)
        {
            mapViewer.RenderSource = new MapRenderer(map);
        }
    }
}
